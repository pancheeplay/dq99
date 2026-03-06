namespace Dq99.Prototype.Domain
{
    public sealed class PrototypeSnapshot
    {
        public Float2 PlayerPosition;
        public ActorSnapshot[] Actors;
        public string HoverLabel;
        public string HoverPrompt;
        public DialogueSnapshot Dialogue;
        public bool IsDialogueOpen;
        public string LastPortalLabel;
    }

    public sealed class ActorSnapshot
    {
        public string Id;
        public string DisplayName;
        public Float2 Position;
        public Float2 Facing;
        public string Tint;
        public bool IsPlayer;
        public bool IsMoving;
        public bool IsHighlighted;
    }

    public sealed class DialogueSnapshot
    {
        public string Speaker;
        public string Text;
        public DialogueChoiceSnapshot[] Choices;
    }

    public sealed class DialogueChoiceSnapshot
    {
        public int Index;
        public string Text;
    }
}
