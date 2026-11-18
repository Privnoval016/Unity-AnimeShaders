using System;
using System.Collections.Generic;
using System.Reflection;
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
     * off; an isolation starts empty and puts back only what was not removed;
     * each group writes where the shaders read it; a push while suspended waits; a destroyed set is passed over;
     * clearing releases tokens; a suspension survives an isolation; an area contains by its own space with its margin in
     * metres; an exempt camera renders with no director light and puts it back after.</summary>
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

        /** <summary>A light area on a new object, its box <paramref name="size" />, margin <paramref name="margin" />, lit by <paramref name="set" />.</summary> */
        private LightArea Area(LightSet set, Vector3 size, float margin)
        {
            var holder = new GameObject("LightArea");
            _made.Add(holder);
            var area = holder.AddComponent<LightArea>();
            var serialized = new SerializedObject(area);
            serialized.FindProperty("lightSet").objectReferenceValue = set;
            serialized.FindProperty("size").vector3Value = size;
            serialized.FindProperty("edgeMargin").floatValue = margin;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return area;
        }

        private static void AreClose(Vector4 expected, Vector4 actual, string message)
        {
            Assert.Less((expected - actual).magnitude, Close, $"{message}: expected {expected}, got {actual}");
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
        public void TheSky_IsWrittenOnlyWhenSwitched_BesideTheWorldsCastShadow()
        {
            var set = ScriptableObject.CreateInstance<LightSet>();
            _made.Add(set);
            set.World.Sky.Brightness = 0.5f;
            set.World.Shadow.Overrides = true;
            set.World.Shadow.CastShadow = 0.75f;
            AnimeLightDirector.Push(set, LightLevel.Base);
            Assert.AreEqual(0f, Shader.GetGlobalVector(Director.WorldScalars).y, Close, "unswitched: no change to the sky");
            set.World.Sky.Overrides = true;
            AnimeLightDirector.Changed(set);
            Vector4 scalars = Shader.GetGlobalVector(Director.WorldScalars);
            Assert.AreEqual(-0.5f, scalars.y, Close, "switched: the sky, less one");
            Assert.AreEqual(-0.25f, scalars.x, Close, "the cast shadow beside it, untouched");
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
        public void AWorldLight_WritesEachOfItsOwnGlobals_AndNoneOfTheCharacters()
        {
            var set = ScriptableObject.CreateInstance<LightSet>();
            _made.Add(set);
            set.World.Colours.Overrides = true;
            set.World.Colours.Light = new Color(0.5f, 0.6f, 0.7f);
            set.World.Colours.Shadow = new Color(0.25f, 0.35f, 0.45f);
            set.World.Ambient.Overrides = true;
            set.World.Ambient.Colour = new Color(0.2f, 0.3f, 0.4f);
            set.World.Ambient.Weight = 0.7f;
            set.World.Shadow.Overrides = true;
            set.World.Shadow.CastShadow = 0.6f;
            set.World.Sky.Overrides = true;
            set.World.Sky.Brightness = 0.8f;
            AnimeLightDirector.Push(set, LightLevel.Base);

            Color light = new Color(0.5f, 0.6f, 0.7f).linear, shadow = new Color(0.25f, 0.35f, 0.45f).linear, ambient = new Color(0.2f, 0.3f, 0.4f).linear;
            AreClose(new Vector4(light.r - 1f, light.g - 1f, light.b - 1f, 0f), Shader.GetGlobalVector(Director.WorldLightColor), "the light, less one");
            AreClose(new Vector4(shadow.r - 1f, shadow.g - 1f, shadow.b - 1f, 0f), Shader.GetGlobalVector(Director.WorldShadowColor), "the shadow, less one");
            AreClose(new Vector4(ambient.r, ambient.g, ambient.b, 0.7f), Shader.GetGlobalVector(Director.WorldAmbient), "the ambient and its weight");
            AreClose(new Vector4(-0.4f, -0.2f, 0f, 0f), Shader.GetGlobalVector(Director.WorldScalars), "cast shadow and sky, less one");
            foreach (int character in new[] { Director.LightDirection, Director.LightColor, Director.ShadowColor, Director.Ambient, Director.Scalars })
                Assert.AreEqual(Vector4.zero, Shader.GetGlobalVector(character), "the characters' globals untouched");
        }

        [Test]
        public void TheCharactersStrengthsAndDirection_WriteWhereTheShadersReadThem()
        {
            var set = ScriptableObject.CreateInstance<LightSet>();
            _made.Add(set);
            set.Characters.Strengths.Overrides = true;
            set.Characters.Strengths.CastShadow = 0.5f;
            set.Characters.Strengths.Rim = 2f;
            set.Characters.Strengths.Line = 1.5f;
            set.Characters.Direction.Overrides = true;
            set.Characters.Direction.Yaw = 90f;
            set.Characters.Direction.Pitch = 0f;
            set.Characters.Direction.Weight = 0.4f;
            AnimeLightDirector.Push(set, LightLevel.Base);
            AreClose(new Vector4(-0.5f, 1f, 0.5f, 0f), Shader.GetGlobalVector(Director.Scalars), "cast shadow, rim and line, less one");
            AreClose(new Vector4(1f, 0f, 0f, 0.4f), Shader.GetGlobalVector(Director.LightDirection), "toward +X, its weight in w");
            Assert.AreEqual(Vector4.zero, Shader.GetGlobalVector(Director.WorldScalars), "the world's untouched");
        }

        [Test]
        public void APushWhileSuspended_StaysNeutral_UntilTheLastSuspensionEnds()
        {
            AnimeLightDirector.BeginSuspend();
            AnimeLightDirector.Push(Coloured(new Color(0.5f, 1f, 1f)), LightLevel.Base);
            Assert.AreEqual(0f, LightRed, Close, "suspended: neutral, though pushed");
            AnimeLightDirector.EndSuspend();
            Assert.AreEqual(Expected(new Color(0.5f, 1f, 1f)), LightRed, Close, "the suspension over: written");
            AnimeLightDirector.EndSuspend();
            Assert.AreEqual(Expected(new Color(0.5f, 1f, 1f)), LightRed, Close, "an end with nothing to end changes nothing");
        }

        [Test]
        public void ASetDestroyedWhilePushed_IsPassedOver()
        {
            AnimeLightDirector.Push(Coloured(new Color(0.5f, 1f, 1f)), LightLevel.Base);
            var gone = ScriptableObject.CreateInstance<LightSet>();
            gone.Characters.Colours.Overrides = true;
            gone.Characters.Colours.Light = new Color(0.25f, 1f, 1f);
            AnimeLightDirector.Push(gone, LightLevel.Shot);
            Object.DestroyImmediate(gone);
            LightToken next = AnimeLightDirector.Push(Coloured(Color.white), LightLevel.Area);
            next.Dispose();
            Assert.AreEqual(Expected(new Color(0.5f, 1f, 1f)), LightRed, Close, "the destroyed shot read as nothing; the base shows");
        }

        [Test]
        public void Clearing_ReleasesEveryToken_SoAnAreaStillInsidePushesAgain()
        {
            LightArea area = Area(Coloured(new Color(0.5f, 1f, 1f)), Vector3.one * 2f, 0.5f);
            area.Step(Vector3.zero);
            LightToken held = AnimeLightDirector.Pushed[0];
            AnimeLightDirector.Clear();
            Assert.IsTrue(held.Released, "its token reads as released");
            Assert.IsFalse(area.Lit, "the area knows it is no longer lit");
            area.Step(Vector3.zero);
            Assert.AreEqual(1, AnimeLightDirector.Pushed.Count, "and pushes again while inside");
        }

        [Test]
        public void ASuspensionOpenedBeforeAnIsolation_EndsInsideIt_AndTheLightComesBackAfter()
        {
            AnimeLightDirector.Push(Coloured(new Color(0.5f, 1f, 1f)), LightLevel.Base);
            AnimeLightDirector.BeginSuspend();
            IDisposable isolation = AnimeLightDirector.Isolate();
            AnimeLightDirector.EndSuspend();
            isolation.Dispose();
            Assert.AreEqual(Expected(new Color(0.5f, 1f, 1f)), LightRed, Close, "the end was not swallowed: lit again");
        }

        [Test]
        public void AScaledTurnedArea_ContainsByItsOwnSpace_ItsMarginInMetres()
        {
            LightArea area = Area(Coloured(new Color(0.5f, 1f, 1f)), Vector3.one * 2f, 0.5f);
            area.transform.localScale = new Vector3(2f, 1f, 1f);
            area.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            area.Step(new Vector3(0f, 0f, 1.8f));
            Assert.IsTrue(area.Lit, "its long side, turned onto z, holds 1.8 m out");
            area.Step(new Vector3(0f, 0f, 2.4f));
            Assert.IsTrue(area.Lit, "within half a metre past its 2 m edge: kept");
            area.Step(new Vector3(0f, 0f, 2.6f));
            Assert.IsFalse(area.Lit, "past it: gone (a margin scaled with the box would keep it to 3 m)");
            area.Step(new Vector3(1.2f, 0f, 0f));
            Assert.IsFalse(area.Lit, "its short side, turned onto x, ends at 1 m");
        }

        [Test]
        public void AnExemptCameraThatLostItsEnd_PutsTheLightBackBeforeTheNextFrame()
        {
            AnimeLightDirector.Push(Coloured(new Color(0.5f, 1f, 1f)), LightLevel.Base);
            var holder = new GameObject("Exempt");
            _made.Add(holder);
            holder.AddComponent<Camera>().enabled = false;
            var exempt = holder.AddComponent<AnimeLightExempt>();
            AnimeLightDirector.BeginSuspend();
            typeof(AnimeLightExempt).GetField("_suspended", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(exempt, true);
            Assert.AreEqual(0f, LightRed, Close, "suspended by a render whose end never came");
            typeof(AnimeLightExempt).GetMethod("LateUpdate", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(exempt, null);
            Assert.AreEqual(Expected(new Color(0.5f, 1f, 1f)), LightRed, Close, "between frames: lit again");
        }

        [Test]
        public void AnAreaWithNoLight_NeverPushes()
        {
            LightArea area = Area(null, Vector3.one * 2f, 0.5f);
            area.Step(Vector3.zero);
            Assert.IsFalse(area.Lit);
            Assert.AreEqual(0, AnimeLightDirector.Pushed.Count);
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
