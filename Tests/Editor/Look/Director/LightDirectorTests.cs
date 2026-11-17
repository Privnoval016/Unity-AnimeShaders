using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Director = AnimeShaders.AnimeShaderIds.Director;
using Object = UnityEngine.Object;

namespace AnimeShaders.Tests
{
    /**
     * <summary>The light director's stack, read from the globals it writes: nothing pushed zeroes them (from non-zero); a
     * light sets only the groups it switches on; a higher level wins only where it sets, a lower showing through
     * elsewhere; at one level the later push wins; disposing in either order, twice, or a token from before a clear does
     * the right thing; an edited set rewrites, an unpushed one does not; a suspension zeroes and puts back, nested; a
     * light area pushes while its target is inside, keeps it within its edge margin, removes it past it and when switched
     * off; an isolation starts empty and puts back only what was not removed; an exempt camera renders with no director light and puts it back after.</summary>
     */
    public class LightDirectorTests
    {
        #region Private Fields

        private const float Close = 1e-4f;

        private readonly List<Object> _made = new();
        private DirectorGlobals _director;
        private IDisposable _isolated;

        #endregion

        #region Helpers

        /** <summary>A light set whose characters' light colour is <paramref name="light" /> (switched on).</summary> */
        private LightSet Coloured(Color light)
        {
            var set = ScriptableObject.CreateInstance<LightSet>();
            set.Characters.Colours.Overrides = true;
            set.Characters.Colours.Light = light;
            _made.Add(set);
            return set;
        }

        private static float LightRed => Shader.GetGlobalVector(Director.LightColor).x;

        private static float Expected(Color colour)
        {
            return colour.linear.r - 1f;
        }

        #endregion

        [SetUp]
        public void SetUp()
        {
            _director = new DirectorGlobals();
            _isolated = AnimeLightDirector.Isolate();
        }

        [TearDown]
        public void TearDown()
        {
            _isolated.Dispose();
            _director.Dispose();
            foreach (Object made in _made)
                Object.DestroyImmediate(made);
            _made.Clear();
        }

        [Test]
        public void NothingPushed_EveryGlobalIsZero()
        {
            Shader.SetGlobalVector(Director.LightColor, Vector4.one);
            Shader.SetGlobalVector(Director.WorldAmbient, Vector4.one);
            AnimeLightDirector.Clear();
            Assert.AreEqual(Vector4.zero, Shader.GetGlobalVector(Director.LightColor), "zeroed from non-zero");
            Assert.AreEqual(Vector4.zero, Shader.GetGlobalVector(Director.WorldAmbient), "the world's too");
        }

        [Test]
        public void ALight_SetsOnlyTheGroupsItSwitchesOn()
        {
            AnimeLightDirector.Push(Coloured(new Color(0.5f, 1f, 1f)), LightLevel.Base);
            Assert.AreEqual(Expected(new Color(0.5f, 1f, 1f)), LightRed, Close, "its colour, in the shaders' encoding");
            Assert.AreEqual(Vector4.zero, Shader.GetGlobalVector(Director.Ambient), "its ambient unswitched: no change");
            Assert.AreEqual(Vector4.zero, Shader.GetGlobalVector(Director.WorldLightColor), "the world untouched");
        }

        [Test]
        public void AHigherLevel_WinsOnlyWhereItSets()
        {
            LightSet below = Coloured(new Color(0.5f, 1f, 1f));
            below.Characters.Ambient.Overrides = true;
            below.Characters.Ambient.Colour = Color.red;
            AnimeLightDirector.Push(below, LightLevel.Base);
            AnimeLightDirector.Push(Coloured(new Color(0.25f, 1f, 1f)), LightLevel.Shot);
            Assert.AreEqual(Expected(new Color(0.25f, 1f, 1f)), LightRed, Close, "the shot's colour over the base's");
            Assert.Greater(Shader.GetGlobalVector(Director.Ambient).w, 0.5f, "the base's ambient shows through");
            AnimeLightDirector.Push(Coloured(new Color(0.75f, 1f, 1f)), LightLevel.Base);
            Assert.AreEqual(Expected(new Color(0.25f, 1f, 1f)), LightRed, Close, "a base pushed after the shot still lies under it");
        }

        [Test]
        public void AtOneLevel_TheLaterPushWins_WhateverOrderThePushesSit()
        {
            AnimeLightDirector.Push(Coloured(new Color(0.25f, 1f, 1f)), LightLevel.Shot);
            AnimeLightDirector.Push(Coloured(new Color(0.75f, 1f, 1f)), LightLevel.Base);
            AnimeLightDirector.Push(Coloured(new Color(0.5f, 1f, 1f)), LightLevel.Shot);
            Assert.AreEqual(Expected(new Color(0.5f, 1f, 1f)), LightRed, Close);
        }

        [Test]
        public void Disposing_InEitherOrder_Twice_OrStale_LeavesTheRightLight()
        {
            LightToken baseLight = AnimeLightDirector.Push(Coloured(new Color(0.75f, 1f, 1f)), LightLevel.Base);
            LightToken shot = AnimeLightDirector.Push(Coloured(new Color(0.25f, 1f, 1f)), LightLevel.Shot);
            baseLight.Dispose();
            Assert.AreEqual(Expected(new Color(0.25f, 1f, 1f)), LightRed, Close, "the base gone first: the shot stays");
            shot.Dispose();
            Assert.AreEqual(0f, LightRed, Close, "both gone");
            LightToken next = AnimeLightDirector.Push(Coloured(new Color(0.5f, 1f, 1f)), LightLevel.Base);
            shot.Dispose();
            baseLight.Dispose();
            Assert.AreEqual(Expected(new Color(0.5f, 1f, 1f)), LightRed, Close, "disposing again removes nothing else");
            AnimeLightDirector.Clear();
            LightToken after = AnimeLightDirector.Push(Coloured(new Color(0.75f, 1f, 1f)), LightLevel.Base);
            next.Dispose();
            Assert.AreEqual(Expected(new Color(0.75f, 1f, 1f)), LightRed, Close, "a token from before a clear removes nothing");
            after.Dispose();
        }

        [Test]
        public void AnIsolation_StartsEmpty_AndPutsBackOnlyWhatWasNotRemoved()
        {
            LightToken kept = AnimeLightDirector.Push(Coloured(new Color(0.5f, 1f, 1f)), LightLevel.Base);
            LightToken removed = AnimeLightDirector.Push(Coloured(new Color(0.25f, 1f, 1f)), LightLevel.Shot);
            IDisposable isolation = AnimeLightDirector.Isolate();
            Assert.AreEqual(0, AnimeLightDirector.Pushed.Count, "isolated: no lights");
            Assert.AreEqual(0f, LightRed, Close, "and neutral");
            removed.Dispose();
            AnimeLightDirector.Push(Coloured(new Color(0.75f, 1f, 1f)), LightLevel.Area);
            isolation.Dispose();
            Assert.AreEqual(1, AnimeLightDirector.Pushed.Count, "what was pushed inside dropped, what was removed meanwhile not put back");
            Assert.AreSame(kept, AnimeLightDirector.Pushed[0]);
            Assert.AreEqual(Expected(new Color(0.5f, 1f, 1f)), LightRed, Close, "and written again");
            isolation.Dispose();
            Assert.AreEqual(1, AnimeLightDirector.Pushed.Count, "disposed twice: nothing more");
        }

        [Test]
        public void AnEditedSet_Rewrites_AnUnpushedOneDoesNot()
        {
            LightSet set = Coloured(new Color(0.5f, 1f, 1f));
            AnimeLightDirector.Push(set, LightLevel.Base);
            set.Characters.Colours.Light = new Color(0.25f, 1f, 1f);
            AnimeLightDirector.Changed(set);
            Assert.AreEqual(Expected(new Color(0.25f, 1f, 1f)), LightRed, Close, "edited: rewritten");
            int writes = AnimeLightDirector.Writes;
            AnimeLightDirector.Changed(Coloured(Color.white));
            Assert.AreEqual(writes, AnimeLightDirector.Writes, "a set not pushed: nothing written");
        }

        [Test]
        public void ASuspension_ZeroesAndPutsBack_Nested()
        {
            AnimeLightDirector.Push(Coloured(new Color(0.5f, 1f, 1f)), LightLevel.Base);
            using (AnimeLightDirector.Suspend())
            {
                using (AnimeLightDirector.Suspend())
                    Assert.AreEqual(0f, LightRed, Close, "suspended");
                Assert.AreEqual(0f, LightRed, Close, "the outer still suspends");
            }
            Assert.AreEqual(Expected(new Color(0.5f, 1f, 1f)), LightRed, Close, "put back");
        }

        [Test]
        public void ALightArea_PushesInside_KeepsItWithinTheMargin_RemovesItPast_AndWhenOff()
        {
            var holder = new GameObject("LightArea");
            _made.Add(holder);
            var area = holder.AddComponent<LightArea>();
            var serialized = new SerializedObject(area);
            serialized.FindProperty("lightSet").objectReferenceValue = Coloured(new Color(0.5f, 1f, 1f));
            serialized.FindProperty("size").vector3Value = Vector3.one * 2f;
            serialized.FindProperty("edgeMargin").floatValue = 0.5f;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            area.Step(new Vector3(1.2f, 0f, 0f));
            Assert.IsFalse(area.Lit, "outside, never in: the margin does not draw it in");
            area.Step(Vector3.zero);
            Assert.IsTrue(area.Lit, "inside");
            Assert.AreEqual(Expected(new Color(0.5f, 1f, 1f)), LightRed, Close, "its light pushed");
            area.Step(new Vector3(1.2f, 0f, 0f));
            Assert.IsTrue(area.Lit, "past the box, within the margin: kept");
            area.Step(new Vector3(1.6f, 0f, 0f));
            Assert.IsFalse(area.Lit, "past the margin: removed");
            Assert.AreEqual(0f, LightRed, Close);
            area.Step(Vector3.zero);
            holder.SetActive(false);
            Assert.AreEqual(0f, LightRed, Close, "switched off: removed");
        }

        [Test]
        public void AnExemptCamera_RendersWithNoDirectorLight_AndPutsItBack()
        {
            using var stage = new LitStage();
            stage.LightFrom(Vector3.forward);
            Material material = TestMaterials.Character(Color.white, new Color(0.5f, 0.5f, 0.5f));
            _made.Add(material);
            Mesh quad = TestMeshes.Quad();
            _made.Add(quad);
            stage.Add(quad, material, Vector3.zero, Quaternion.identity, Vector3.one * 2f);
            Color neutral = FrameReads.Centre(stage.Render());

            LightSet set = ScriptableObject.CreateInstance<LightSet>();
            _made.Add(set);
            set.Characters.Colours.Overrides = true;
            set.Characters.Colours.Shadow = new Color(0.5f, 0.5f, 0.5f);
            AnimeLightDirector.Push(set, LightLevel.Base);
            Assert.Less(FrameReads.Centre(stage.Render()).r, neutral.r * 0.9f, "the director's shadow colour reaches it");

            var other = new GameObject("Other Camera");
            _made.Add(other);
            other.AddComponent<Camera>().enabled = false;
            other.AddComponent<AnimeLightExempt>();
            Assert.Less(FrameReads.Centre(stage.Render()).r, neutral.r * 0.9f, "another camera's exemption: still lit");

            stage.Camera.gameObject.AddComponent<AnimeLightExempt>();
            Assert.AreEqual(neutral.r, FrameReads.Centre(stage.Render()).r, 0.01f, "exempt: as with no director light");
            Assert.Less(Shader.GetGlobalVector(Director.ShadowColor).x, -0.1f, "put back after the camera");
        }
    }
}
