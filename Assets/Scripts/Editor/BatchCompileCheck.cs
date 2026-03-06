using System;
using UnityEditor;

namespace Dq99.Prototype.Editor
{
    public static class BatchCompileCheck
    {
        public static void Run()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            if (EditorUtility.scriptCompilationFailed)
            {
                UnityEngine.Debug.LogError("Batch compile check failed: Unity reports script compilation errors.");
                EditorApplication.Exit(1);
                return;
            }

            UnityEngine.Debug.Log("Batch compile check passed.");
            EditorApplication.Exit(0);
        }
    }
}
