namespace Dq99.Prototype.Domain
{
    public readonly struct PrototypeInputFrame
    {
        public PrototypeInputFrame(Float2 move, bool interactPressed, int dialogueChoice)
        {
            Move = move;
            InteractPressed = interactPressed;
            DialogueChoice = dialogueChoice;
        }

        public Float2 Move { get; }
        public bool InteractPressed { get; }
        public int DialogueChoice { get; }
    }
}
