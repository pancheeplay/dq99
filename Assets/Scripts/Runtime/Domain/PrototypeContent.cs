using System;

namespace Dq99.Prototype.Domain
{
    [Serializable]
    public sealed class PrototypeContent
    {
        public string playerSpawnMarkerId;
        public BuildingDefinition[] buildings;
        public ActorDefinition[] actors;
        public PortalDefinition[] portals;
        public DialogueDefinition[] dialogues;
    }

    [Serializable]
    public sealed class Float3Data
    {
        public float x;
        public float y;
        public float z;
    }

    [Serializable]
    public sealed class BuildingDefinition
    {
        public string id;
        public Float3Data position;
        public Float3Data scale;
        public string tint;
    }

    [Serializable]
    public sealed class ActorDefinition
    {
        public string id;
        public string markerId;
        public string displayName;
        public string dialogueId;
        public string tint;
    }

    [Serializable]
    public sealed class PortalDefinition
    {
        public string id;
        public string markerId;
        public string destinationSceneName;
        public string destinationMarkerId;
        public string displayName;
        public string promptText;
        public string[] requiredFlags;
        public string[] setFlags;
    }

    [Serializable]
    public sealed class DialogueDefinition
    {
        public string id;
        public DialogueNodeDefinition[] nodes;
    }

    [Serializable]
    public sealed class DialogueNodeDefinition
    {
        public string id;
        public string speaker;
        public string text;
        public string[] setFlags;
        public DialogueChoiceDefinition[] choices;
    }

    [Serializable]
    public sealed class DialogueChoiceDefinition
    {
        public string text;
        public string jump;
        public string[] requiredFlags;
        public string[] setFlags;
    }
}
