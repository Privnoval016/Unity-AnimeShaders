using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace AnimeShaders.Tests
{
    /** <summary>Editor checks on a compiled shader that Unity keeps internal.</summary> */
    public static class ShaderChecks
    {
        #region Public API

        /** <summary>Why the SRP Batcher refuses <paramref name="shader" />'s first subshader, or null when it is compatible.</summary> */
        public static string SrpBatcherProblem(Shader shader)
        {
            const BindingFlags any = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            MethodInfo code = typeof(ShaderUtil).GetMethod("GetSRPBatcherCompatibilityCode", any);
            MethodInfo reason = typeof(ShaderUtil).GetMethod("GetSRPBatcherCompatibilityIssueReason", any);
            if (code == null)
                return "the batcher check moved in this Unity";
            int result = (int)code.Invoke(null, new object[] { shader, 0 });
            if (result == 0)
                return null;
            return reason?.Invoke(null, new object[] { shader, 0, result }) as string ?? $"code {result}";
        }

        #endregion
    }
}
