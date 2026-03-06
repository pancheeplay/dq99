using Dq99.Prototype.Domain;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dq99.Prototype.Infrastructure
{
    public static class PrototypeContentLoader
    {
        private const string FallbackResourcePath = "Prototype/SampleScene";

        public static PrototypeContent LoadForActiveScene()
        {
            var sceneName = SceneManager.GetActiveScene().name;
            if (!string.IsNullOrEmpty(sceneName))
            {
                var sceneResourcePath = $"Prototype/{sceneName}";
                var sceneTextAsset = Resources.Load<TextAsset>(sceneResourcePath);
                if (sceneTextAsset != null)
                {
                    var sceneContent = JsonUtility.FromJson<PrototypeContent>(sceneTextAsset.text);
                    return sceneContent ?? new PrototypeContent();
                }
            }

            return LoadFallback();
        }

        private static PrototypeContent LoadFallback()
        {
            var textAsset = Resources.Load<TextAsset>(FallbackResourcePath);
            if (textAsset == null)
            {
                Debug.LogError($"Missing prototype content at Resources/{FallbackResourcePath}.json");
                return new PrototypeContent();
            }

            var content = JsonUtility.FromJson<PrototypeContent>(textAsset.text);
            return content ?? new PrototypeContent();
        }
    }
}
