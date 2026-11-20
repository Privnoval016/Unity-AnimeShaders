using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

namespace AnimeShaders.EditorTools
{
    /**
     * <summary>Compiles every pass of a shader, vertex and fragment, for one graphics API and build target from any
     * Editor, and names what fails. Tests use it to hold a shader to every API a build can use.</summary>
     */
    public static class ShaderCompileCheck
    {
        #region Public API

        /**
         * <summary>One message per failed compile of <paramref name="shader" />, each naming the shader, the pass, the
         * stage, the keywords and the errors. Each set in <paramref name="keywordSets" /> is compiled with only the
         * keywords the shader declares; a shader with no pass is a failure too.</summary>
         */
        public static List<string> Failures(Shader shader, IEnumerable<string[]> keywordSets, ShaderCompilerPlatform platform, BuildTarget target)
        {
            var failures = new List<string>();
            ShaderData.Subshader subshader = ShaderUtil.GetShaderData(shader).ActiveSubshader;
            if (subshader == null || subshader.PassCount == 0)
            {
                failures.Add($"{shader.name}: no pass to compile");
                return failures;
            }

            var declared = new HashSet<string>(shader.keywordSpace.keywordNames);
            foreach (string[] set in keywordSets)
            {
                string[] keywords = set.Where(declared.Contains).ToArray();
                for (int index = 0; index < subshader.PassCount; index++)
                {
                    ShaderData.Pass pass = subshader.GetPass(index);
                    foreach (ShaderType stage in new[] { ShaderType.Vertex, ShaderType.Fragment })
                    {
                        ShaderData.VariantCompileInfo result = pass.CompileVariant(stage, keywords, platform, target);
                        if (result.Success) continue;

                        string errors = string.Join("; ", result.Messages.Select(message => message.message));
                        failures.Add($"{shader.name} {pass.Name} {stage} [{string.Join(" ", keywords)}]: {errors}");
                    }
                }
            }

            return failures;
        }

        #endregion
    }
}
