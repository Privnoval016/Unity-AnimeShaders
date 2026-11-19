using UnityEditor;
using UnityEngine;

namespace AnimeShaders.EditorTools
{
    /**
     * <summary>
     * Hands <see cref="AnimeTiers" /> the project's quality tiers in the Editor (after each reload, outside Play Mode), so the
     * Scene and Game views and offscreen renders draw the quality level's tier: the project's one
     * <see cref="AnimeQualityTiers" /> asset; with none, nothing is dropped; with several, none is used (warned).
     * </summary>
     */
    [InitializeOnLoad]
    internal static class AnimeTiersEditorBoot
    {
        static AnimeTiersEditorBoot() => EditorApplication.delayCall += Apply;

        #region Helpers

        private static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            string[] found = AssetDatabase.FindAssets("t:" + nameof(AnimeQualityTiers));
            if (found.Length > 1)
                Debug.LogWarning("Several Anime Quality Tiers assets: the Editor uses none.");
            AnimeTiers.Use(found.Length == 1 ? AssetDatabase.LoadAssetAtPath<AnimeQualityTiers>(AssetDatabase.GUIDToAssetPath(found[0])) : null);
        }

        #endregion
    }
}
