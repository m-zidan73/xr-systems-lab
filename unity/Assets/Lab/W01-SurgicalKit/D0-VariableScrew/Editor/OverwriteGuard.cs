using System;
using UnityEditor;

namespace Lab.W01.VariableScrew.EditorTools
{
    /// <summary>Stops a builder menu item from silently replacing tuned scenes or hand-edited data.</summary>
    public static class OverwriteGuard
    {
        public static bool Allow(string assetPath) => Allow(assetPath, AskInDialog);

        public static bool Allow(string assetPath, Func<string, bool> confirm)
        {
            var exists = !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(assetPath, AssetPathToGUIDOptions.OnlyExistingAssets));
            return !exists || confirm(assetPath);
        }

        static bool AskInDialog(string assetPath) => EditorUtility.DisplayDialog(
            "Replace this asset?",
            $"{assetPath} already exists. Building it again replaces its tuning and any hand-made changes.",
            "Replace",
            "Cancel");
    }
}
