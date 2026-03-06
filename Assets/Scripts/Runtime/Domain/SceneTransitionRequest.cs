namespace Dq99.Prototype.Domain
{
    public readonly struct SceneTransitionRequest
    {
        public SceneTransitionRequest(string sceneName, string markerId, string portalLabel)
        {
            SceneName = sceneName;
            MarkerId = markerId;
            PortalLabel = portalLabel;
        }

        public string SceneName { get; }
        public string MarkerId { get; }
        public string PortalLabel { get; }
    }
}
