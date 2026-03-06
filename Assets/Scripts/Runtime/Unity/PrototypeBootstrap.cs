using UnityEngine;

namespace Dq99.Prototype.Unity
{
    public static class PrototypeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureRunner()
        {
            if (Object.FindObjectOfType<PrototypeRunner>() != null)
            {
                return;
            }

            var root = new GameObject("PrototypeRunner");
            Object.DontDestroyOnLoad(root);
            root.AddComponent<PrototypeRunner>();
        }
    }
}
