using UnityEngine;
using UnityEngine.Rendering;
using Ids = AnimeShaders.AnimeShaderIds.MaterialProperties;

namespace AnimeShaders.Tests
{
    /**
     * <summary>Materials made for render tests, every value a test reads set here or by the test, never taken from a
     * shipped material. The caller destroys each material.</summary>
     */
    public static class TestMaterials
    {
        #region Properties

        /** <summary>The shader every character test draws with.</summary> */
        public const string CharacterShader = "Anime/Character";

        #endregion

        #region Public API

        /**
         * <summary>A character material (colours as the shader reads them, linear) whose lit tone is exactly <paramref name="baseColor" /> and whose shadow tone
         * is exactly <paramref name="baseColor" /> times <paramref name="shadowTint" />: light colour, ambient and cast
         * shadows play no part, the ramp's softness is a hair, and the line pass is off.</summary>
         */
        public static Material Character(Color baseColor, Color shadowTint)
        {
            var material = new Material(Shader.Find(CharacterShader)) { name = "TestCharacter" };
            SetLinear(material, Ids.BaseColor, baseColor);
            SetLinear(material, Ids.ShadowTint, shadowTint);
            material.SetFloat(Ids.ShadowThreshold, 0.5f);
            material.SetFloat(Ids.ShadowSoftness, 0.004f);
            material.SetFloat(Ids.LightInfluence, 0f);
            material.SetFloat(Ids.AmbientStrength, 0f);
            material.SetFloat(Ids.CastShadowStrength, 0f);
            AnimeMaterials.SetLine(material, false);
            return material;
        }

        /** <summary>A material on the pack's sky shader, with its defaults.</summary> */
        public static Material Sky()
        {
            return new Material(Shader.Find(AnimeShaderIds.Sky.ShaderName)) { name = "TestSky" };
        }

        /**
         * <summary>A material on <paramref name="shader" /> (Anime/World or Anime/Ground) whose lit tone is its
         * surface colour exactly: light colour, ambient and cast shadows play no part, the ramp's softness a hair.</summary>
         */
        public static Material WorldLit(string shader)
        {
            var material = new Material(Shader.Find(shader)) { name = "TestWorld" };
            SetLinear(material, Ids.BaseColor, Color.white);
            SetLinear(material, Ids.ShadowTint, new Color(0.5f, 0.5f, 0.5f));
            material.SetFloat(Ids.ShadowThreshold, 0.5f);
            material.SetFloat(Ids.ShadowSoftness, 0.004f);
            material.SetFloat(Ids.LightInfluence, 0f);
            material.SetFloat(Ids.AmbientStrength, 0f);
            material.SetFloat(Ids.CastShadowStrength, 0f);
            return material;
        }

        /**
         * <summary>Sets a colour property so the shader reads <paramref name="colour" />: in a linear project a
         * colour property that is not HDR is converted from sRGB however it is set, so its gamma value goes in.</summary>
         */
        public static void SetLinear(Material material, int property, Color colour)
        {
            bool converted = QualitySettings.activeColorSpace == ColorSpace.Linear && !IsHdr(material.shader, property);
            material.SetColor(property, converted ? colour.gamma : colour);
        }

        #endregion

        #region Helpers

        private static bool IsHdr(Shader shader, int property)
        {
            for (int i = 0; i < shader.GetPropertyCount(); i++)
            {
                if (shader.GetPropertyNameId(i) == property)
                    return (shader.GetPropertyFlags(i) & ShaderPropertyFlags.HDR) != 0;
            }
            return false;
        }

        #endregion
    }
}
