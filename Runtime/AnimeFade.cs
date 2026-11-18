using UnityEngine;

namespace AnimeShaders
{
    /**
     * <summary>The dither fade from code: a coverage (the share of a body's pixels kept) as the level a renderer's user
     * value carries, and which shaders read it. The fade's pattern has <see cref="Steps" /> thresholds, so a coverage is
     * kept in sixteenths: a quarter keeps exactly four of each sixteen pixels.</summary>
     */
    public static class AnimeFade
    {
        #region Properties

        /** <summary>The thresholds in the fade's screen pattern: how many coverages it can show.</summary> */
        public const int Steps = 16;

        #endregion

        #region Public API

        /** <summary>The level that keeps <paramref name="coverage" /> of the pixels (1 solid, 0 gone), rounded to the pattern's steps.</summary> */
        public static uint LevelFor(float coverage)
        {
            float kept = Mathf.Round(Mathf.Clamp01(coverage) * Steps) / Steps;
            return (uint)Mathf.Round((1f - kept) * AnimeShaderIds.Fade.Full);
        }

        /** <summary>Fades <paramref name="renderer" /> to <paramref name="level" /> (the pack owns the whole user value; its other bits are reserved). Only mesh and skinned renderers carry one; any other is left as it is.</summary> */
        public static void Apply(Renderer renderer, uint level)
        {
            uint value = level & AnimeShaderIds.Fade.LevelMask;
            switch (renderer)
            {
                case MeshRenderer mesh:
                    mesh.SetShaderUserValue(value);
                    break;
                case SkinnedMeshRenderer skinned:
                    skinned.SetShaderUserValue(value);
                    break;
            }
        }

        /** <summary>Whether <paramref name="shader" /> reads the fade: the character, face, hair and eye shaders (the world's and the ground's leave it out).</summary> */
        public static bool ReadsFade(Shader shader)
        {
            if (shader == null)
                return false;
            string name = shader.name;
            return name == AnimeShaderIds.CharacterShader || name == AnimeShaderIds.FaceShader || name == AnimeShaderIds.HairShader
                || name == AnimeShaderIds.EyesShader;
        }

        /** <summary>Whether <paramref name="renderer" /> can fade: a mesh or skinned renderer whose every material reads the fade (none: false).</summary> */
        public static bool Fades(Renderer renderer)
        {
            if (renderer is not (MeshRenderer or SkinnedMeshRenderer))
                return false;
            Material[] materials = renderer.sharedMaterials;
            if (materials.Length == 0)
                return false;
            foreach (Material material in materials)
            {
                if (material == null || !ReadsFade(material.shader))
                    return false;
            }
            return true;
        }

        #endregion
    }
}
