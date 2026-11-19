using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;

namespace AnimeShaders
{
    /**
     * <summary>
     * Warms the graphics states a trace recorded (<see cref="AnimeTrace" />), so their shaders are compiled before they are
     * first drawn: all at once (<see cref="WarmNow" />, before anything is drawn: at launch) or a few each frame
     * (<see cref="WarmAsync" />, behind a loading screen whose own shaders are warm). A trace holds one graphics API's
     * states: the one for the API running is warmed, and with none for it nothing is.
     * </summary>
     */
    public static class AnimeWarmUp
    {
        #region Private Fields

        /** <summary>Frames <see cref="WarmAsync" /> waits on a trace that warms nothing more before it gives up.</summary> */
        private const int StalledFrames = 300;

        #endregion

        #region Public API

        /** <summary>The trace among <paramref name="traces" /> recorded on <paramref name="api" />; null for none.</summary> */
        public static GraphicsStateCollection For(IReadOnlyList<GraphicsStateCollection> traces, GraphicsDeviceType api)
        {
            if (traces == null)
                return null;
            foreach (GraphicsStateCollection trace in traces)
            {
                if (trace != null && trace.graphicsDeviceType == api)
                    return trace;
            }
            return null;
        }

        /** <summary>Warms every state of the trace for the running API before returning (with none for it, warns and warms nothing).</summary> <returns>Whether there was a trace to warm.</returns> */
        public static bool WarmNow(IReadOnlyList<GraphicsStateCollection> traces)
        {
            GraphicsStateCollection trace = ForRunningApi(traces);
            if (trace == null)
                return false;
            trace.WarmUp().Complete();
            return true;
        }

        /**
         * <summary>Warms the trace for the running API, <paramref name="perFrame" /> states a frame, finishing when all are, when
         * <paramref name="cancel" /> is cancelled, or (with a warning) when the trace stops warming more; at once with no
         * trace for the API (warned). Never throws a cancel. Whatever is drawn meanwhile compiles as it is drawn.</summary>
         */
        public static async Awaitable WarmAsync(IReadOnlyList<GraphicsStateCollection> traces, int perFrame, CancellationToken cancel)
        {
            GraphicsStateCollection trace = ForRunningApi(traces);
            if (trace == null)
                return;
            int last = -1;
            int still = 0;
            while (!trace.isWarmedUp && !cancel.IsCancellationRequested)
            {
                trace.WarmUpProgressively(Mathf.Max(1, perFrame));
                still = trace.completedWarmupCount == last ? still + 1 : 0;
                last = trace.completedWarmupCount;
                if (still >= StalledFrames)
                {
                    Debug.LogWarning($"Shader warm-up stalled at {last} of {trace.totalGraphicsStateCount}.");
                    return;
                }
                try
                {
                    await Awaitable.NextFrameAsync(cancel);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }

        #endregion

        #region Helpers

        private static GraphicsStateCollection ForRunningApi(IReadOnlyList<GraphicsStateCollection> traces)
        {
            GraphicsStateCollection trace = For(traces, SystemInfo.graphicsDeviceType);
            if (trace == null)
                Debug.LogWarning($"No shader trace for {SystemInfo.graphicsDeviceType}: nothing warmed.");
            return trace;
        }

        #endregion
    }
}
