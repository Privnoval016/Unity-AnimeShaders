using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

namespace AnimeShaders.Tests
{
    /**
     * <summary>Every pass of every pack shader compiles for each graphics API a build can use, from whichever Editor
     * runs the tests, through each lighting path: no keywords; every material feature with Forward+ and cascades;
     * plain Forward with additional lights and their shadows; screen-space shadows with receiving off; a normal map
     * alone.</summary>
     */
    public class PlatformCompileTests
    {
        #region Private Fields

        private static readonly string[] ForwardPlus =
        {
            "_MAIN_LIGHT_SHADOWS_CASCADE", "_CLUSTER_LIGHT_LOOP", "_SHADOWS_SOFT_HIGH", "_LIGHT_LAYERS",
            "_WRITE_RENDERING_LAYERS", "FOG_EXP2", "_CASTING_PUNCTUAL_LIGHT_SHADOW", "_GBUFFER_NORMALS_OCT",
            AnimeShaderIds.World.AtmosphereKeyword, AnimeShaderIds.World.CloudShadowsKeyword, "_SCREEN_SPACE_OCCLUSION",
        };

        private static readonly string[] PlainForward =
        {
            "_MAIN_LIGHT_SHADOWS", "_ADDITIONAL_LIGHTS", "_ADDITIONAL_LIGHT_SHADOWS", "_SHADOWS_SOFT", "FOG_LINEAR",
            AnimeShaderIds.Keywords.ExtraLightsName, AnimeShaderIds.World.AtmosphereKeyword,
        };

        private static readonly string[] ScreenShadows =
        {
            "_MAIN_LIGHT_SHADOWS_SCREEN", AnimeShaderIds.Keywords.ReceiveShadowsOffName,
        };

        private static readonly string[] NormalMapAlone = { AnimeShaderIds.Keywords.NormalmapName };

        private static readonly string[] PackShaders = { "Anime/Character", "Anime/World", "Anime/Ground", "Anime/Sky", AnimeShaderIds.FaceShader };

        #endregion

        #region Helpers

        private static Shader Character()
        {
            return Shader.Find(TestMaterials.CharacterShader);
        }

        private static IEnumerable<string[]> KeywordSets()
        {
            string[] everyMaterialFeature = PackSources.NameTable(typeof(AnimeShaderIds.Keywords))
                .Where(name => name != AnimeShaderIds.Keywords.ReceiveShadowsOffName).ToArray();
            yield return new string[0];
            yield return everyMaterialFeature.Concat(ForwardPlus).ToArray();
            yield return PlainForward;
            yield return ScreenShadows;
            yield return NormalMapAlone;
        }

        private static IEnumerable<string> Failures(Shader shader, ShaderCompilerPlatform platform, BuildTarget target)
        {
            ShaderData.Subshader subshader = ShaderUtil.GetShaderData(shader).ActiveSubshader;
            var declared = new HashSet<string>(shader.keywordSpace.keywordNames);
            foreach (string[] set in KeywordSets())
            {
                string[] keywords = set.Where(declared.Contains).ToArray();
                for (int index = 0; index < subshader.PassCount; index++)
                {
                    ShaderData.Pass pass = subshader.GetPass(index);
                    foreach (ShaderType stage in new[] { ShaderType.Vertex, ShaderType.Fragment })
                    {
                        ShaderData.VariantCompileInfo result = pass.CompileVariant(stage, keywords, platform, target);
                        if (!result.Success)
                        {
                            string errors = string.Join("; ", result.Messages.Select(message => message.message));
                            yield return $"{shader.name} {pass.Name} {stage} [{string.Join(" ", keywords)}]: {errors}";
                        }
                    }
                }
            }
        }

        #endregion

        [Test]
        public void EveryKeywordCompiled_IsOneTheShaderDeclares()
        {
            var declared = new HashSet<string>(PackShaders.SelectMany(name => Shader.Find(name).keywordSpace.keywordNames));
            var unknown = KeywordSets().SelectMany(set => set).Where(name => !declared.Contains(name)).Distinct().ToList();
            Assert.IsEmpty(unknown, "a typo here would silently drop a path from the compile check");
        }

        [TestCase(ShaderCompilerPlatform.D3D, BuildTarget.StandaloneWindows64)]
        [TestCase(ShaderCompilerPlatform.Vulkan, BuildTarget.StandaloneWindows64)]
        [TestCase(ShaderCompilerPlatform.OpenGLCore, BuildTarget.StandaloneLinux64)]
        [TestCase(ShaderCompilerPlatform.Metal, BuildTarget.StandaloneOSX)]
        [TestCase(ShaderCompilerPlatform.GLES3x, BuildTarget.Android)]
        public void EveryPass_Compiles(ShaderCompilerPlatform platform, BuildTarget target)
        {
            List<string> failures = PackShaders.SelectMany(name => Failures(Shader.Find(name), platform, target)).ToList();
            Assert.IsEmpty(failures, string.Join("\n", failures));
        }
    }
}
