using System.Collections.Generic;
using System.Collections;
using Dq99.Prototype.Domain;
using Dq99.Prototype.Infrastructure;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dq99.Prototype.Unity
{
    public sealed class PrototypeRunner : MonoBehaviour
    {
        private const float PlayerRadius = 0.35f;
        private readonly Dictionary<string, GameObject> _actorViews = new Dictionary<string, GameObject>();
        private readonly Dictionary<string, GameObject> _portalViews = new Dictionary<string, GameObject>();

        private PrototypeGame _game;
        private PrototypeHud _hud;
        private Camera _camera;
        private GameObject _playerView;
        private ActorPresentation _playerPresentation;
        private Material _sharedMaterial;
        private PrototypeContent _content;
        private bool _isTransitioning;
        private readonly PrototypePersistentState _persistentState = new PrototypePersistentState();

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            _hud = PrototypeHud.Create();
            _sharedMaterial = CreateWorldMaterial();
            SceneManager.sceneLoaded += HandleSceneLoaded;
            RebuildForActiveScene();
        }

        private void Update()
        {
            if (_game == null || _isTransitioning)
            {
                return;
            }

            var input = ReadInput();
            var snapshot = _game.Tick(input, Time.deltaTime, CanMoveTo);
            Sync(snapshot);

            if (_game.TryConsumeSceneTransition(out var request))
            {
                StartCoroutine(TransitionToScene(request));
            }
        }

        private PrototypeInputFrame ReadInput()
        {
            var horizontal = Input.GetAxisRaw("Horizontal");
            var vertical = Input.GetAxisRaw("Vertical");
            var interactPressed = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Z);

            var dialogueChoice = 0;
            for (var i = 0; i < 9; i++)
            {
                if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i)) || Input.GetKeyDown((KeyCode)((int)KeyCode.Keypad1 + i)))
                {
                    dialogueChoice = i + 1;
                    break;
                }
            }

            return new PrototypeInputFrame(new Float2(horizontal, vertical), interactPressed, dialogueChoice);
        }

        private bool CanMoveTo(Float2 candidate)
        {
            var bottom = new Vector3(candidate.X, 0.25f, candidate.Y);
            var top = new Vector3(candidate.X, 1.4f, candidate.Y);
            var colliders = Physics.OverlapCapsule(bottom, top, PlayerRadius);

            foreach (var collider in colliders)
            {
                if (collider.isTrigger || collider.gameObject == _playerView || collider.gameObject.name == "Ground")
                {
                    continue;
                }

                if (collider.GetComponentInParent<WalkBlocker>() != null)
                {
                    return false;
                }
            }

            return true;
        }

        private void EnsureCamera()
        {
            _camera = Camera.main;
            if (_camera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                _camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            _camera.transform.position = new Vector3(0f, 11f, -8f);
            _camera.transform.rotation = Quaternion.Euler(48f, 0f, 0f);
        }

        private void BuildWorld()
        {
            EnsureLighting();
            BuildActors();
            BuildPortals();
        }

        private void EnsureLighting()
        {
            if (FindObjectOfType<Light>() != null)
            {
                return;
            }

            var lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private void BuildActors()
        {
            _playerView = FindObjectOfType<PlayerAvatarMarker>()?.gameObject;
            if (_playerView == null)
            {
                _playerView = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                _playerView.name = "PlayerAvatar";
                _playerView.AddComponent<PlayerAvatarMarker>();
                _playerView.GetComponent<Renderer>().sharedMaterial = CloneMaterialWithColor("#4DA3FF", new Color(0.30f, 0.64f, 1f));
            }

            SetAllCollidersTrigger(_playerView);
            _playerPresentation = EnsurePresentation(_playerView);
            foreach (var pair in PrototypeSceneAuthoring.EnsureActorViews(_content, CloneMaterialWithColor))
            {
                _actorViews[pair.Key] = pair.Value;
            }
        }

        private void BuildPortals()
        {
            foreach (var pair in PrototypeSceneAuthoring.EnsurePortalViews(_content, CloneMaterialWithColor))
            {
                _portalViews[pair.Key] = pair.Value;
            }
        }

        private void Sync(PrototypeSnapshot snapshot)
        {
            if (_playerView == null)
            {
                return;
            }

            foreach (var actor in snapshot.Actors)
            {
                if (actor.IsPlayer)
                {
                    _playerPresentation?.Apply(actor, Time.deltaTime);
                    continue;
                }

                if (!_actorViews.TryGetValue(actor.Id, out var actorObject))
                {
                    continue;
                }

                var presentation = EnsurePresentation(actorObject);
                presentation.Apply(actor, Time.deltaTime);
                actorObject.transform.localScale = actor.IsHighlighted ? new Vector3(1.1f, 1.1f, 1.1f) : Vector3.one;
            }

            foreach (var portalObject in _portalViews.Values)
            {
                if (portalObject == null)
                {
                    continue;
                }

                portalObject.transform.Rotate(0f, 45f * Time.deltaTime, 0f, Space.World);
            }

            _camera.transform.position = new Vector3(snapshot.PlayerPosition.X, 11f, snapshot.PlayerPosition.Y - 8f);
            _camera.transform.LookAt(new Vector3(snapshot.PlayerPosition.X, 0.8f, snapshot.PlayerPosition.Y));
            _hud.Render(snapshot);
        }

        private Material CreateWorldMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            return new Material(shader);
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RebuildForActiveScene();
        }

        private void RebuildForActiveScene()
        {
            _actorViews.Clear();
            _portalViews.Clear();
            _playerView = null;

            _content = PrototypeContentLoader.LoadForActiveScene();
            EnsureCamera();
            BuildWorld();
            var bindings = PrototypeSceneAuthoring.CollectBindings(_content);
            _game = new PrototypeGame(_content, bindings, _persistentState);
            Sync(_game.Tick(new PrototypeInputFrame(Float2.Zero, false, 0), 0f, CanMoveTo));
        }

        private IEnumerator TransitionToScene(SceneTransitionRequest request)
        {
            _isTransitioning = true;
            var operation = SceneManager.LoadSceneAsync(request.SceneName, LoadSceneMode.Single);
            if (operation == null)
            {
                Debug.LogError($"Could not load scene '{request.SceneName}'. Add it to Build Settings.");
                _isTransitioning = false;
                yield break;
            }

            while (!operation.isDone)
            {
                yield return null;
            }

            yield return null;

            if (_game == null)
            {
                _isTransitioning = false;
                yield break;
            }

            var bindings = PrototypeSceneAuthoring.CollectBindings(_content);
            if (!string.IsNullOrEmpty(request.MarkerId) && bindings.MarkerPositions.TryGetValue(request.MarkerId, out var spawn))
            {
                _game = new PrototypeGame(_content, new PrototypeSceneBindings
                {
                    PlayerSpawn = spawn,
                    MarkerPositions = bindings.MarkerPositions
                }, _persistentState);
                Sync(_game.Tick(new PrototypeInputFrame(Float2.Zero, false, 0), 0f, CanMoveTo));
            }

            _isTransitioning = false;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private Material CloneMaterialWithColor(string html, Color fallback)
        {
            var color = ParseColor(html, fallback);
            var material = new Material(_sharedMaterial);
            material.color = color;
            return material;
        }

        private static void SetAllCollidersTrigger(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
            {
                collider.isTrigger = true;
            }
        }

        private static ActorPresentation EnsurePresentation(GameObject root)
        {
            if (root == null)
            {
                return null;
            }

            var presentation = root.GetComponent<ActorPresentation>();
            if (presentation == null)
            {
                presentation = root.AddComponent<ActorPresentation>();
            }

            return presentation;
        }

        private static Color ParseColor(string html, Color fallback)
        {
            return ColorUtility.TryParseHtmlString(html, out var color) ? color : fallback;
        }
    }
}
