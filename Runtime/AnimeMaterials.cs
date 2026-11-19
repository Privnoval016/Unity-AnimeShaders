using System;
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
        #region Private Fields

        private const string ToggleMark = "Toggle";
        private const string ToggleOffMark = "ToggleOff";

        #endregion

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

        /**
         * <summary>Sets <paramref name="material" />'s Line On Open Edges (0 to 1) and the line's cull with it: drawn
         * from both sides while it is on, as two-sided cloth needs, else with front faces culled.</summary>
         */
        public static void SetOpenEdges(Material material, float amount)
        {
            material.SetFloat(AnimeShaderIds.MaterialProperties.OutlineOpenEdges, amount);
            SyncLineCull(material);
        }

        /** <summary>Puts <paramref name="material" />'s line cull in step with its Line On Open Edges.</summary> */
        public static void SyncLineCull(Material material)
        {
            if (!material.HasFloat(AnimeShaderIds.MaterialProperties.OutlineOpenEdges) || !material.HasFloat(AnimeShaderIds.Switches.OutlineCull))
                return;
            bool open = material.GetFloat(AnimeShaderIds.MaterialProperties.OutlineOpenEdges) > 0f;
            material.SetFloat(AnimeShaderIds.Switches.OutlineCull, (float)(open ? CullMode.Off : CullMode.Front));
        }

        /** <summary>Draws or hides <paramref name="material" />'s line pass.</summary> */
        public static void SetLine(Material material, bool on)
        {
            material.SetShaderPassEnabled(AnimeShaderIds.OutlineLightMode, on);
        }

        /**
         * <summary>
         * Sets on <paramref name="material" /> the settings <paramref name="look" /> covers that its shader has with the
         * same kind (a range held to its limits) and its covered pass switches, then keeps every toggle's keyword, the
         * depth rim's pass and the line's cull in step. Settings the look lacks are left as they are. Answers how many
         * settings it set.
         * </summary>
         */
        public static int ApplyLook(Material material, AnimeLookPreset look)
        {
            Shader shader = material.shader;
            int set = 0;
            foreach (AnimeLookPreset.Setting setting in look.Settings)
            {
                if (!look.Sets(setting.section) || !setting.Fits(shader, out int index))
                    continue;
                setting.ApplyTo(material, index);
                set++;
            }
            foreach (AnimeLookPreset.PassSwitch pass in look.Passes)
            {
                if (look.Sets(pass.section) && HasPass(shader, pass.lightMode))
                    material.SetShaderPassEnabled(pass.lightMode, pass.on);
            }
            SyncDerived(material);
            return set;
        }

        /** <summary>Keeps what follows from <paramref name="material" />'s settings in step with them: each toggle's keyword, the depth rim's pass, the line's cull.</summary> */
        public static void SyncDerived(Material material)
        {
            SyncToggleKeywords(material);
            if (material.HasFloat(AnimeShaderIds.MaterialProperties.RimDepth))
                material.SetShaderPassEnabled(AnimeShaderIds.RimLightMode, material.GetFloat(AnimeShaderIds.MaterialProperties.RimDepth) > 0.5f);
            SyncLineCull(material);
        }

        /**
         * <summary>
         * Puts each toggle's keyword in step with its float, as the shader declares them: <c>[Toggle(KW)]</c> on while the
         * float is on, <c>[Toggle]</c> with <c>_NAME_ON</c>, <c>[ToggleOff(KW)]</c> on while it is off.
         * </summary>
         */
        public static void SyncToggleKeywords(Material material)
        {
            Shader shader = material.shader;
            for (int i = 0; i < shader.GetPropertyCount(); i++)
            {
                foreach (string attribute in shader.GetPropertyAttributes(i))
                {
                    bool off = attribute.StartsWith(ToggleOffMark);
                    if (!off && !(attribute == ToggleMark || attribute.StartsWith(ToggleMark + "(")))
                        continue;
                    string keyword = KeywordOf(attribute, shader.GetPropertyName(i), off);
                    bool on = material.GetFloat(shader.GetPropertyNameId(i)) > 0.5f;
                    material.SetKeyword(new LocalKeyword(shader, keyword), off ? !on : on);
                }
            }
        }

        /** <summary>Whether <paramref name="shader" /> has a pass of <paramref name="lightMode" /> (built-in light modes come back in capitals).</summary> */
        public static bool HasPass(Shader shader, string lightMode)
        {
            var tag = new ShaderTagId("LightMode");
            for (int i = 0; i < shader.passCount; i++)
            {
                if (string.Equals(shader.FindPassTagValue(i, tag).name, lightMode, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        #endregion

        #region Helpers

        /** <summary>The keyword a toggle attribute names; a bare one is the property's name in capitals with _ON (or _OFF).</summary> */
        private static string KeywordOf(string attribute, string property, bool off)
        {
            int open = attribute.IndexOf('(');
            if (open >= 0 && attribute.EndsWith(")"))
                return attribute.Substring(open + 1, attribute.Length - open - 2).Trim();
            return property.ToUpperInvariant() + (off ? "_OFF" : "_ON");
        }

        #endregion
    }
}
