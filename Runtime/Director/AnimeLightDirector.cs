using System;
using System.Collections.Generic;
using UnityEngine;
using Director = AnimeShaders.AnimeShaderIds.Director;

namespace AnimeShaders
{
    /**
     * <summary>The lights in play, stacked: each group of each channel (characters, world) takes its values from the
     * highest pushed light that sets it (a later <see cref="LightLevel" /> over an earlier; at one level the later push),
     * and the shaders' director globals are written from that whenever the stack or a pushed set changes, never per
     * frame. Nothing pushed, every global is zero (no change). Cleared, globals zeroed, as scripts reload and as play
     * starts.</summary>
     */
    public static class AnimeLightDirector
    {
        #region Private Fields

        private static readonly List<LightToken> Layers = new();
        private static readonly int[] Globals =
        {
            Director.LightDirection, Director.LightColor, Director.ShadowColor, Director.Ambient, Director.Scalars,
            Director.WorldLightColor, Director.WorldShadowColor, Director.WorldAmbient, Director.WorldScalars,
        };
        private static long _pushes;
        private static int _suspended;

        #endregion

        #region Properties

        /** <summary>How many times the globals have been written: a write happens only on a change.</summary> */
        public static int Writes { get; private set; }

        /** <summary>The lights pushed, lowest first.</summary> */
        public static IReadOnlyList<LightToken> Pushed => Layers;

        #endregion

        #region Public API

        /** <summary>Pushes <paramref name="set" /> at <paramref name="level" />; dispose the token to remove it.</summary> */
        public static LightToken Push(LightSet set, LightLevel level)
        {
            var token = new LightToken(set, level, ++_pushes);
            int at = Layers.Count;
            while (at > 0 && Layers[at - 1].Level > level)
                at--;
            Layers.Insert(at, token);
            Write();
            return token;
        }

        /** <summary>Rewrites the globals if <paramref name="set" /> is pushed (its values were edited).</summary> */
        public static void Changed(LightSet set)
        {
            foreach (LightToken token in Layers)
            {
                if (token.Set != set)
                    continue;
                Write();
                return;
            }
        }

        /**
         * <summary>Renders under no director light until disposed (an offscreen capture, a camera keeping its own light);
         * nested scopes stack, and the last to go puts the lights back.</summary>
         */
        public static IDisposable Suspend()
        {
            BeginSuspend();
            return new Suspension();
        }

        /** <summary>As <see cref="Suspend" />, without a scope: each call is paired with <see cref="EndSuspend" />.</summary> */
        public static void BeginSuspend()
        {
            _suspended++;
            WriteNeutral();
        }

        /** <summary>Ends one <see cref="BeginSuspend" />; the last puts the lights back.</summary> */
        public static void EndSuspend()
        {
            if (_suspended == 0)
                return;
            _suspended--;
            if (_suspended == 0)
                Write();
        }

        /**
         * <summary>Sets the pushed lights aside and starts with none until disposed (a test, a tool lighting its own
         * renders), then puts back those not removed meanwhile; what was pushed inside is dropped.</summary>
         */
        public static IDisposable Isolate()
        {
            var isolation = new Isolation(new List<LightToken>(Layers), _suspended);
            Layers.Clear();
            _suspended = 0;
            WriteNeutral();
            return isolation;
        }

        /** <summary>Removes every light and zeroes the globals.</summary> */
        public static void Clear()
        {
            Layers.Clear();
            _suspended = 0;
            WriteNeutral();
        }

        #endregion

        #region Helpers

        internal static void Release(LightToken token)
        {
            if (Layers.Remove(token))
                Write();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            Clear();
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void ResetOnReload()
        {
            Clear();
        }
#endif

        private static void Write()
        {
            if (_suspended > 0)
                return;
            LightDirectionGroup direction = null;
            LightColours colours = null, worldColours = null;
            LightAmbient ambient = null, worldAmbient = null;
            LightStrengths strengths = null;
            WorldLight worldCast = null, worldSky = null;
            foreach (LightToken token in Layers)
            {
                if (token.Set == null)
                    continue;
                CharacterLight characters = token.Set.Characters;
                WorldLight world = token.Set.World;
                direction = characters.Direction.Overrides ? characters.Direction : direction;
                colours = characters.Colours.Overrides ? characters.Colours : colours;
                ambient = characters.Ambient.Overrides ? characters.Ambient : ambient;
                strengths = characters.Strengths.Overrides ? characters.Strengths : strengths;
                worldColours = world.Colours.Overrides ? world.Colours : worldColours;
                worldAmbient = world.Ambient.Overrides ? world.Ambient : worldAmbient;
                worldCast = world.CastShadowOverrides ? world : worldCast;
                worldSky = world.SkyOverrides ? world : worldSky;
            }

            Shader.SetGlobalVector(Director.LightDirection, direction == null ? Vector4.zero : (Vector4)direction.TowardLight + new Vector4(0f, 0f, 0f, direction.Weight));
            Shader.SetGlobalVector(Director.LightColor, LessOne(colours?.Light));
            Shader.SetGlobalVector(Director.ShadowColor, LessOne(colours?.Shadow));
            Shader.SetGlobalVector(Director.Ambient, Ambient(ambient));
            Shader.SetGlobalVector(Director.Scalars, strengths == null ? Vector4.zero : new Vector4(strengths.CastShadow - 1f, strengths.Rim - 1f, strengths.Line - 1f, 0f));
            Shader.SetGlobalVector(Director.WorldLightColor, LessOne(worldColours?.Light));
            Shader.SetGlobalVector(Director.WorldShadowColor, LessOne(worldColours?.Shadow));
            Shader.SetGlobalVector(Director.WorldAmbient, Ambient(worldAmbient));
            Shader.SetGlobalVector(Director.WorldScalars, new Vector4(worldCast == null ? 0f : worldCast.CastShadow - 1f, worldSky == null ? 0f : worldSky.Sky - 1f, 0f, 0f));
            Writes++;
        }

        private static void WriteNeutral()
        {
            foreach (int id in Globals)
                Shader.SetGlobalVector(id, Vector4.zero);
            Writes++;
        }

        /** <summary>A multiplier colour in the shaders' encoding: linear, less one (white is zero, no change).</summary> */
        private static Vector4 LessOne(Color? colour)
        {
            if (colour == null)
                return Vector4.zero;
            Color linear = colour.Value.linear;
            return new Vector4(linear.r - 1f, linear.g - 1f, linear.b - 1f, 0f);
        }

        private static Vector4 Ambient(LightAmbient ambient)
        {
            if (ambient == null)
                return Vector4.zero;
            Color linear = ambient.Colour.linear;
            return new Vector4(linear.r, linear.g, linear.b, ambient.Weight);
        }

        private sealed class Isolation : IDisposable
        {
            private readonly List<LightToken> _aside;
            private readonly int _wasSuspended;
            private bool _done;

            public Isolation(List<LightToken> aside, int suspended)
            {
                _aside = aside;
                _wasSuspended = suspended;
            }

            public void Dispose()
            {
                if (_done)
                    return;
                _done = true;
                Layers.Clear();
                foreach (LightToken token in _aside)
                {
                    if (!token.Released)
                        Layers.Add(token);
                }
                _suspended = _wasSuspended;
                if (_wasSuspended > 0)
                    WriteNeutral();
                else
                    Write();
            }
        }

        private sealed class Suspension : IDisposable
        {
            private bool _done;

            public void Dispose()
            {
                if (_done)
                    return;
                _done = true;
                EndSuspend();
            }
        }

        #endregion
    }
}
