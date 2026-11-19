using UnityEngine;
using UnityEngine.Rendering;

namespace AnimeShaders
{
    /**
     * <summary>
     * Records every graphics state the game draws with (each shader variant with its render state) from
     * <see cref="Record" /> until the game quits, then saves them to a file: a trace that a later build warms from
     * (<see cref="AnimeWarmUp" />), so nothing compiles while a fight plays. Record on the platform's real GPU, playing
     * what the trace should cover. It is saved only on a normal quit (a crash loses the recording). A later recording of
     * something new can be merged into the trace (the Editor's <c>AnimeTraceTools.Merge</c>), so only the new thing is
     * played.
     * </summary>
     */
    public static class AnimeTrace
    {
        #region Private Fields

        private static GraphicsStateCollection _trace;
        private static string _path;

        #endregion

        #region Properties

        /** <summary>Whether a trace is recording.</summary> */
        public static bool Recording => _trace != null && _trace.isTracing;

        #endregion

        #region Public API

        /** <summary>Starts recording, to be saved to <paramref name="path" /> when the game quits.</summary> <returns>Whether it started (never without a GPU, never twice).</returns> */
        public static bool Record(string path)
        {
            if (Recording)
                return false;
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                Debug.LogWarning("No GPU: no shader trace recorded.");
                return false;
            }
            _trace = new GraphicsStateCollection();
            if (!_trace.BeginTrace())
            {
                Debug.LogWarning("The shader trace didn't start.");
                _trace = null;
                return false;
            }
            _path = path;
            Application.quitting += Save;
            return true;
        }

        #endregion

        #region Event Handlers

        private static void Save()
        {
            Application.quitting -= Save;
            _trace.EndTrace();
            if (!_trace.SaveToFile(_path))
                Debug.LogWarning($"The shader trace wasn't saved to {_path}.");
            _trace = null;
        }

        #endregion
    }
}
