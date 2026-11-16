using UnityEngine;
using UnityEngine.Rendering;

namespace AnimeShaders
{
    /**
     * <summary>Changes to a pack material that must stay in step: a feature's keyword and the toggle property the
     * inspector shows for it.</summary>
     * <remarks>Setting a toggle's float from code does not change its keyword, and the keyword is what the shader
     * reads. Features compiled with shader_feature are stripped from a build unless some material in it uses them, so
     * a feature turned on at runtime needs a material in the build with it on.</remarks>
     */
    public static class AnimeMaterials
    {
        #region Public API

        /**
         * <summary>Turns the feature <paramref name="keyword" /> on or off on <paramref name="material" />, with its
         * toggle property <paramref name="toggle" /> (an id from <see cref="AnimeShaderIds.Switches" />) kept in step.
         * For Receive Shadows, whose keyword switches shadows off, use <see cref="SetReceiveShadows" />.</summary>
         */
        public static void SetFeature(Material material, string keyword, int toggle, bool on)
        {
            material.SetKeyword(new LocalKeyword(material.shader, keyword), on);
            material.SetFloat(toggle, on ? 1f : 0f);
        }

        /** <summary>Whether <paramref name="material" /> receives cast shadows (its keyword is on when it does not).</summary> */
        public static void SetReceiveShadows(Material material, bool receive)
        {
            material.SetKeyword(new LocalKeyword(material.shader, AnimeShaderIds.Keywords.ReceiveShadowsOffName), !receive);
            material.SetFloat(AnimeShaderIds.Switches.ReceiveShadows, receive ? 1f : 0f);
        }

        /** <summary>Turns the depth-edge rim on or off: its switch and its pass together, so a material without it costs no draw.</summary> */
        public static void SetRimDepth(Material material, bool on)
        {
            material.SetFloat(AnimeShaderIds.MaterialProperties.RimDepth, on ? 1f : 0f);
            material.SetShaderPassEnabled(AnimeShaderIds.RimLightMode, on);
        }

        /** <summary>Draws or hides <paramref name="material" />'s line pass.</summary> */
        public static void SetLine(Material material, bool on)
        {
            material.SetShaderPassEnabled(AnimeShaderIds.OutlineLightMode, on);
        }

        #endregion
    }
}
