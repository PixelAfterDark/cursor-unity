using UnityEditor;
using Cursor.Core;

namespace Cursor.Editor
{
    /// <summary>
    /// Automatically configures Script Execution Order for core systems.
    /// Runs when scripts are compiled or when explicitly invoked from menu.
    /// </summary>
    [InitializeOnLoad]
    public static class ScriptExecutionOrderConfig
    {
        // Execution order values (lower = earlier execution)
        private const int SystemsManagerOrder = -100;
        private const int EventSystemOrder = -90;
        private const int GameManagerOrder = -70;

        static ScriptExecutionOrderConfig()
        {
            ApplyExecutionOrder();
        }

        [MenuItem("Cursor/Apply Script Execution Order")]
        public static void ApplyExecutionOrder()
        {
            SetExecutionOrder<SystemsManager>(SystemsManagerOrder);
            SetExecutionOrder<EventSystem>(EventSystemOrder);
            SetExecutionOrder<GameManager>(GameManagerOrder);
        }

        private static void SetExecutionOrder<T>(int order) where T : UnityEngine.MonoBehaviour
        {
            string scriptPath = GetScriptPath<T>();
            if (string.IsNullOrEmpty(scriptPath))
            {
                UnityEngine.Debug.LogWarning($"[ScriptExecutionOrderConfig] Could not find script for {typeof(T).Name}");
                return;
            }

            MonoScript monoScript = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
            if (monoScript == null)
            {
                UnityEngine.Debug.LogWarning($"[ScriptExecutionOrderConfig] Could not load MonoScript at {scriptPath}");
                return;
            }

            int currentOrder = MonoImporter.GetExecutionOrder(monoScript);
            if (currentOrder != order)
            {
                MonoImporter.SetExecutionOrder(monoScript, order);
                UnityEngine.Debug.Log($"[ScriptExecutionOrderConfig] Set {typeof(T).Name} execution order to {order}");
            }
        }

        private static string GetScriptPath<T>() where T : UnityEngine.MonoBehaviour
        {
            string[] guids = AssetDatabase.FindAssets($"t:MonoScript {typeof(T).Name}");
            if (guids.Length == 0) return null;

            // Find the exact MonoScript matching the target type to avoid
            // collisions when multiple scripts share the same name.
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                MonoScript monoScript = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (monoScript != null && monoScript.GetClass() == typeof(T))
                {
                    return path;
                }
            }

            return null;
        }
    }
}
