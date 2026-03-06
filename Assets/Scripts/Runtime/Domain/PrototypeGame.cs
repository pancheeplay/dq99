using System;
using System.Collections.Generic;

namespace Dq99.Prototype.Domain
{
    public sealed class PrototypeGame
    {
        private const float InteractionRadius = 1.85f;
        private const float MoveSpeed = 3.6f;

        private readonly Dictionary<string, DialogueDefinition> _dialoguesById = new Dictionary<string, DialogueDefinition>();
        private readonly List<ActorRuntime> _actors = new List<ActorRuntime>();
        private readonly List<PortalRuntime> _portals = new List<PortalRuntime>();
        private readonly HashSet<string> _flags = new HashSet<string>();

        private Float2 _playerPosition;
        private Float2 _playerFacing = new Float2(0f, 1f);
        private bool _playerIsMoving;
        private InteractTarget _hoverTarget;
        private DialogueRuntime _activeDialogue;
        private string _lastPortalLabel;
        private SceneTransitionRequest? _pendingSceneTransition;

        public PrototypeGame(PrototypeContent content, PrototypeSceneBindings sceneBindings, PrototypePersistentState persistentState = null)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
            SceneBindings = sceneBindings ?? throw new ArgumentNullException(nameof(sceneBindings));
            PersistentState = persistentState ?? new PrototypePersistentState();
            Initialize();
        }

        public PrototypeContent Content { get; }
        public PrototypeSceneBindings SceneBindings { get; }
        public PrototypePersistentState PersistentState { get; }

        public bool TryConsumeSceneTransition(out SceneTransitionRequest request)
        {
            if (_pendingSceneTransition.HasValue)
            {
                request = _pendingSceneTransition.Value;
                _pendingSceneTransition = null;
                return true;
            }

            request = default;
            return false;
        }

        public PrototypeSnapshot Tick(PrototypeInputFrame input, float deltaTime, Func<Float2, bool> canMoveTo)
        {
            if (_activeDialogue == null)
            {
                UpdateMovement(input.Move, deltaTime, canMoveTo);
                UpdateHoverTarget();

                if (input.InteractPressed && _hoverTarget != null)
                {
                    ActivateHoverTarget();
                }
            }
            else
            {
                HandleDialogueInput(input);
            }

            return BuildSnapshot();
        }

        private void Initialize()
        {
            _playerPosition = SceneBindings.PlayerSpawn;
            if (PersistentState.Flags != null)
            {
                foreach (var flag in PersistentState.Flags)
                {
                    if (!string.IsNullOrEmpty(flag))
                    {
                        _flags.Add(flag);
                    }
                }
            }

            if (Content.dialogues != null)
            {
                foreach (var dialogue in Content.dialogues)
                {
                    if (dialogue != null && !string.IsNullOrEmpty(dialogue.id))
                    {
                        _dialoguesById[dialogue.id] = dialogue;
                    }
                }
            }

            if (Content.actors != null)
            {
                foreach (var actor in Content.actors)
                {
                    if (actor == null || string.IsNullOrEmpty(actor.id))
                    {
                        continue;
                    }

                    _actors.Add(new ActorRuntime
                    {
                        Id = actor.id,
                        MarkerId = actor.markerId,
                        DisplayName = actor.displayName,
                        DialogueId = actor.dialogueId,
                        Position = ResolveMarker(actor.markerId),
                        Facing = new Float2(0f, -1f),
                        Tint = actor.tint
                    });
                }
            }

            if (Content.portals == null)
            {
                return;
            }

            foreach (var portal in Content.portals)
            {
                if (portal == null || string.IsNullOrEmpty(portal.id))
                {
                    continue;
                }

                _portals.Add(new PortalRuntime
                {
                    Id = portal.id,
                    MarkerId = portal.markerId,
                    DestinationSceneName = portal.destinationSceneName,
                    DestinationMarkerId = portal.destinationMarkerId,
                    Position = ResolveMarker(portal.markerId),
                    Destination = ResolveMarker(portal.destinationMarkerId),
                    DisplayName = portal.displayName,
                    PromptText = string.IsNullOrEmpty(portal.promptText) ? "进入" : portal.promptText,
                    RequiredFlags = portal.requiredFlags ?? Array.Empty<string>(),
                    SetFlags = portal.setFlags ?? Array.Empty<string>()
                });
            }
        }

        private void UpdateMovement(Float2 inputMove, float deltaTime, Func<Float2, bool> canMoveTo)
        {
            _playerIsMoving = false;
            if (inputMove.Magnitude <= 0.001f)
            {
                return;
            }

            _playerFacing = inputMove.Normalized;

            var movement = inputMove.Normalized * (MoveSpeed * deltaTime);
            if (TryMove(movement, canMoveTo))
            {
                _playerIsMoving = true;
                return;
            }

            var xMovement = new Float2(movement.X, 0f);
            var yMovement = new Float2(0f, movement.Y);

            if (Math.Abs(movement.X) >= Math.Abs(movement.Y))
            {
                if (TryMove(xMovement, canMoveTo))
                {
                    TryMove(yMovement, canMoveTo);
                }
                else
                {
                    TryMove(yMovement, canMoveTo);
                }

                return;
            }

            if (TryMove(yMovement, canMoveTo))
            {
                TryMove(xMovement, canMoveTo);
            }
            else
            {
                TryMove(xMovement, canMoveTo);
            }
        }

        private bool TryMove(Float2 movement, Func<Float2, bool> canMoveTo)
        {
            if (movement.Magnitude <= 0.0001f)
            {
                return false;
            }

            var candidate = _playerPosition + movement;
            if (canMoveTo != null && !canMoveTo(candidate))
            {
                return false;
            }

            _playerPosition = candidate;
            return true;
        }

        private void UpdateHoverTarget()
        {
            InteractTarget nearestTarget = null;
            var nearestDistance = float.MaxValue;

            foreach (var actor in _actors)
            {
                var distance = Float2.Distance(_playerPosition, actor.Position);
                if (distance > InteractionRadius || distance >= nearestDistance)
                {
                    continue;
                }

                nearestDistance = distance;
                nearestTarget = new InteractTarget(actor.Id, InteractTargetType.Actor, actor.DisplayName, "交谈");
            }

            foreach (var portal in _portals)
            {
                if (!HasRequiredFlags(portal.RequiredFlags))
                {
                    continue;
                }

                var distance = Float2.Distance(_playerPosition, portal.Position);
                if (distance > InteractionRadius || distance >= nearestDistance)
                {
                    continue;
                }

                nearestDistance = distance;
                nearestTarget = new InteractTarget(portal.Id, InteractTargetType.Portal, portal.DisplayName, portal.PromptText);
            }

            _hoverTarget = nearestTarget;
        }

        private void ActivateHoverTarget()
        {
            if (_hoverTarget == null)
            {
                return;
            }

            switch (_hoverTarget.Type)
            {
                case InteractTargetType.Actor:
                    StartDialogueFor(_hoverTarget.Id);
                    break;
                case InteractTargetType.Portal:
                    UsePortal(_hoverTarget.Id);
                    break;
            }
        }

        private void StartDialogueFor(string actorId)
        {
            var actor = _actors.Find(candidate => candidate.Id == actorId);
            if (actor == null || string.IsNullOrEmpty(actor.DialogueId))
            {
                return;
            }

            if (!_dialoguesById.TryGetValue(actor.DialogueId, out var dialogue))
            {
                return;
            }

            _activeDialogue = DialogueRuntime.Start(dialogue, _flags);
        }

        private void UsePortal(string portalId)
        {
            var portal = _portals.Find(candidate => candidate.Id == portalId);
            if (portal == null || !HasRequiredFlags(portal.RequiredFlags))
            {
                return;
            }

            ApplyFlags(portal.SetFlags);
            _lastPortalLabel = portal.DisplayName;
            _hoverTarget = null;

            if (!string.IsNullOrEmpty(portal.DestinationSceneName))
            {
                _pendingSceneTransition = new SceneTransitionRequest(portal.DestinationSceneName, portal.DestinationMarkerId, portal.DisplayName);
                return;
            }

            _playerPosition = portal.Destination;
        }

        private void HandleDialogueInput(PrototypeInputFrame input)
        {
            if (_activeDialogue == null || input.DialogueChoice <= 0)
            {
                return;
            }

            var optionIndex = input.DialogueChoice - 1;
            if (!_activeDialogue.TryChoose(optionIndex, _flags, out var nextDialogue))
            {
                return;
            }

            _activeDialogue = nextDialogue;
        }

        private PrototypeSnapshot BuildSnapshot()
        {
            var actors = new ActorSnapshot[_actors.Count + 1];
            actors[0] = new ActorSnapshot
            {
                Id = "player",
                DisplayName = "主角",
                Position = _playerPosition,
                Facing = _playerFacing,
                Tint = "#4DA3FF",
                IsPlayer = true,
                IsMoving = _playerIsMoving
            };

            for (var i = 0; i < _actors.Count; i++)
            {
                var actor = _actors[i];
                if (actor.Id == _hoverTarget?.Id && _hoverTarget.Type == InteractTargetType.Actor)
                {
                    actor.Facing = (_playerPosition - actor.Position).Normalized;
                }

                actors[i + 1] = new ActorSnapshot
                {
                    Id = actor.Id,
                    DisplayName = actor.DisplayName,
                    Position = actor.Position,
                    Facing = actor.Facing.Magnitude > 0.001f ? actor.Facing : new Float2(0f, -1f),
                    Tint = actor.Tint,
                    IsMoving = false,
                    IsHighlighted = _hoverTarget != null && _hoverTarget.Type == InteractTargetType.Actor && actor.Id == _hoverTarget.Id
                };
            }

            return new PrototypeSnapshot
            {
                PlayerPosition = _playerPosition,
                Actors = actors,
                HoverLabel = _hoverTarget?.Label,
                HoverPrompt = _hoverTarget?.Prompt,
                Dialogue = _activeDialogue?.ToSnapshot(),
                IsDialogueOpen = _activeDialogue != null,
                LastPortalLabel = _lastPortalLabel
            };
        }

        private bool HasRequiredFlags(string[] requiredFlags)
        {
            if (requiredFlags == null || requiredFlags.Length == 0)
            {
                return true;
            }

            foreach (var requiredFlag in requiredFlags)
            {
                if (!_flags.Contains(requiredFlag))
                {
                    return false;
                }
            }

            return true;
        }

        private void ApplyFlags(string[] flagsToSet)
        {
            if (flagsToSet == null)
            {
                return;
            }

            foreach (var flag in flagsToSet)
            {
                if (!string.IsNullOrEmpty(flag))
                {
                    _flags.Add(flag);
                }
            }

            PersistentState.Flags = new List<string>(_flags).ToArray();
        }

        private Float2 ResolveMarker(string markerId)
        {
            if (string.IsNullOrEmpty(markerId))
            {
                return Float2.Zero;
            }

            return SceneBindings.MarkerPositions.TryGetValue(markerId, out var position) ? position : Float2.Zero;
        }

        private sealed class ActorRuntime
        {
            public string Id;
            public string MarkerId;
            public string DisplayName;
            public string DialogueId;
            public Float2 Position;
            public Float2 Facing;
            public string Tint;
        }

        private sealed class PortalRuntime
        {
            public string Id;
            public string MarkerId;
            public string DestinationSceneName;
            public string DestinationMarkerId;
            public Float2 Position;
            public Float2 Destination;
            public string DisplayName;
            public string PromptText;
            public string[] RequiredFlags;
            public string[] SetFlags;
        }

        private sealed class InteractTarget
        {
            public InteractTarget(string id, InteractTargetType type, string label, string prompt)
            {
                Id = id;
                Type = type;
                Label = label;
                Prompt = prompt;
            }

            public string Id { get; }
            public InteractTargetType Type { get; }
            public string Label { get; }
            public string Prompt { get; }
        }

        private enum InteractTargetType
        {
            Actor,
            Portal
        }

        private sealed class DialogueRuntime
        {
            private readonly DialogueDefinition _definition;
            private readonly Dictionary<string, DialogueNodeDefinition> _nodesById;
            private readonly DialogueNodeDefinition _currentNode;
            private readonly DialogueChoiceDefinition[] _availableChoices;

            private DialogueRuntime(
                DialogueDefinition definition,
                Dictionary<string, DialogueNodeDefinition> nodesById,
                DialogueNodeDefinition currentNode,
                DialogueChoiceDefinition[] availableChoices)
            {
                _definition = definition;
                _nodesById = nodesById;
                _currentNode = currentNode;
                _availableChoices = availableChoices;
            }

            public static DialogueRuntime Start(DialogueDefinition definition, HashSet<string> flags)
            {
                var nodesById = new Dictionary<string, DialogueNodeDefinition>();
                if (definition.nodes == null || definition.nodes.Length == 0)
                {
                    return null;
                }

                foreach (var node in definition.nodes)
                {
                    if (node != null && !string.IsNullOrEmpty(node.id))
                    {
                        nodesById[node.id] = node;
                    }
                }

                var startNode = definition.nodes[0];
                return startNode == null ? null : EnterNode(definition, nodesById, startNode, flags);
            }

            public bool TryChoose(int choiceIndex, HashSet<string> flags, out DialogueRuntime nextDialogue)
            {
                nextDialogue = this;
                if (choiceIndex < 0 || choiceIndex >= _availableChoices.Length)
                {
                    return false;
                }

                var choice = _availableChoices[choiceIndex];
                ApplyFlags(choice.setFlags, flags);

                if (string.IsNullOrEmpty(choice.jump) || !_nodesById.TryGetValue(choice.jump, out var nextNode))
                {
                    nextDialogue = null;
                    return true;
                }

                nextDialogue = EnterNode(_definition, _nodesById, nextNode, flags);
                return true;
            }

            public DialogueSnapshot ToSnapshot()
            {
                var choices = new DialogueChoiceSnapshot[_availableChoices.Length];
                for (var i = 0; i < _availableChoices.Length; i++)
                {
                    choices[i] = new DialogueChoiceSnapshot
                    {
                        Index = i + 1,
                        Text = _availableChoices[i].text
                    };
                }

                return new DialogueSnapshot
                {
                    Speaker = _currentNode.speaker,
                    Text = _currentNode.text,
                    Choices = choices
                };
            }

            private static DialogueRuntime EnterNode(
                DialogueDefinition definition,
                Dictionary<string, DialogueNodeDefinition> nodesById,
                DialogueNodeDefinition node,
                HashSet<string> flags)
            {
                ApplyFlags(node.setFlags, flags);

                var availableChoices = FilterChoices(node.choices, flags);
                if (availableChoices.Length == 0)
                {
                    availableChoices = new[]
                    {
                        new DialogueChoiceDefinition
                        {
                            text = "结束对话",
                            jump = null
                        }
                    };
                }

                return new DialogueRuntime(definition, nodesById, node, availableChoices);
            }

            private static DialogueChoiceDefinition[] FilterChoices(DialogueChoiceDefinition[] choices, HashSet<string> flags)
            {
                if (choices == null || choices.Length == 0)
                {
                    return Array.Empty<DialogueChoiceDefinition>();
                }

                var list = new List<DialogueChoiceDefinition>(choices.Length);
                foreach (var choice in choices)
                {
                    if (choice.requiredFlags == null || choice.requiredFlags.Length == 0)
                    {
                        list.Add(choice);
                        continue;
                    }

                    var allow = true;
                    foreach (var requiredFlag in choice.requiredFlags)
                    {
                        if (!flags.Contains(requiredFlag))
                        {
                            allow = false;
                            break;
                        }
                    }

                    if (allow)
                    {
                        list.Add(choice);
                    }
                }

                return list.ToArray();
            }

            private static void ApplyFlags(string[] flagsToSet, HashSet<string> flags)
            {
                if (flagsToSet == null)
                {
                    return;
                }

                foreach (var flag in flagsToSet)
                {
                    if (!string.IsNullOrEmpty(flag))
                    {
                        flags.Add(flag);
                    }
                }
            }
        }
    }
}
