using System.Collections.Generic;

namespace Dq99.Prototype.Domain
{
    public sealed class PrototypeSceneBindings
    {
        public Float2 PlayerSpawn;
        public Dictionary<string, Float2> MarkerPositions = new Dictionary<string, Float2>();
    }
}
