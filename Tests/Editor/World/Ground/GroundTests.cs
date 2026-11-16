using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AnimeShaders.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Ids = AnimeShaders.AnimeShaderIds.MaterialProperties;
using Object = UnityEngine.Object;

namespace AnimeShaders.Tests
{
    /**
     * <summary>Anime/Ground seen from above with made detail maps: the circle's layer inside and the field's outside;
     * the shade picks between a layer's dark and light, the mask between its pairs; steps, the detail normal's frame,
     * break up tiling, the edge band, the distance fade, the default maps and far from the origin. Every colour and
     * setting expected is set on the made material; the reads' structure is checked in the source.</summary>
     */
    public class GroundTests
    {
        /** <summary>The ground's two layers.</summary> */
        public enum Layer
        {
            /** <summary>Outside the circle.</summary> */
            Field,

            /** <summary>Inside the circle.</summary> */
            Circle,
        }

        private readonly struct LayerIds
        {
            public int Detail { get; }

            public int[] Colours { get; }

            public int Steps { get; }

            public int Normal { get; }

            public int BreakUp { get; }

            public LayerIds(int detail, int[] colours, int steps, int normal, int breakUp)
            {
                Detail = detail;
                Colours = colours;
                Steps = steps;
                Normal = normal;
                BreakUp = breakUp;
            }
        }

        #region Private Fields

        private const float View = 3.2f;
        private const int Pixels = 128;

        private static readonly Color Green = new(0.1f, 0.6f, 0.1f);
        private static readonly Color Grey = new(0.8f, 0.8f, 0.8f);
        private static readonly Color Black = new(0f, 0f, 0f);
        private static readonly Color White = new(1f, 1f, 1f);

        private static readonly int[] FieldColours = { Ids.FieldDark, Ids.FieldLight, Ids.FieldSecondDark, Ids.FieldSecondLight };
        private static readonly int[] CircleColours = { Ids.CircleDark, Ids.CircleLight, Ids.CircleSecondDark, Ids.CircleSecondLight };

        private readonly List<Object> _made = new();
        private LitStage _stage;
        private Material _material;
        private MeshRenderer _floor;

        #endregion

        #region Helpers

        private Color At(Color[] frame, float x, float z)
        {
            return FrameReads.At(frame, _stage.Column(x), _stage.Column(z));
        }

        private static float Distance(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);
        }

        private static bool Finite(Color pixel)
        {
            return !float.IsNaN(pixel.r + pixel.g + pixel.b) && !float.IsInfinity(pixel.r + pixel.g + pixel.b);
        }

        private void Colours(int[] properties, params Color[] colours)
        {
            for (int i = 0; i < properties.Length; i++)
                TestMaterials.SetLinear(_material, properties[i], colours[i % colours.Length]);
        }

        private void Circle(float x, float z, float radius, float wobble = 0f)
        {
            _material.SetVector(Ids.GroundCircle, new Vector4(x, z, radius, wobble));
        }

        /** <summary>A made detail map, <paramref name="size" /> square, repeating and filtered as a packed one is.</summary> */
        private Texture2D Detail(Func<int, int, Color32> pixel, int size = 64)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
            };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                    pixels[y * size + x] = pixel(x, y);
            }
            texture.SetPixels32(pixels);
            texture.Apply(false);
            _made.Add(texture);
            return texture;
        }

        private Texture2D Uniform(Color32 pixel)
        {
            return Detail((x, y) => pixel, 4);
        }

        /** <summary>The perceptual shade a pixel shows on a black-to-white gradient (the gradient blends square roots).</summary> */
        private static float ShadeOf(Color pixel)
        {
            return Mathf.Sqrt(Mathf.Max(pixel.r, 0f));
        }

        /** <summary>Lights the floor from the side so a flat surface sits in the middle of a wide ramp.</summary> */
        private void MidRamp(Vector3 towardLight)
        {
            _stage.LightFrom(towardLight);
            float flat = Vector3.Dot(Vector3.up, towardLight.normalized) * 0.5f + 0.5f;
            _material.SetFloat(Ids.ShadowThreshold, flat);
            _material.SetFloat(Ids.ShadowSoftness, 0.4f);
        }

        private float LitFrom(Vector3 towardLight)
        {
            MidRamp(towardLight);
            return At(_stage.Render(), 0.5f, 0.5f).r;
        }

        private static float Luminance(Color pixel)
        {
            return pixel.r * 0.2126729f + pixel.g * 0.7151522f + pixel.b * 0.0721750f;
        }

        private static float Spread(IEnumerable<float> values)
        {
            float[] all = values.ToArray();
            float mean = all.Average();
            return Mathf.Sqrt(all.Sum(value => (value - mean) * (value - mean)) / all.Length);
        }

        private IEnumerable<float> CoveredShades(Color[] frame)
        {
            return frame.Where(FrameReads.Covered).Select(ShadeOf);
        }

        /** <summary>Shows only <paramref name="layer" />: the circle off for the field, a circle past the view for the circle.</summary> */
        private void Show(Layer layer)
        {
            Circle(0f, 0f, layer == Layer.Circle ? 100f : 0f);
        }

        private static LayerIds Of(Layer layer)
        {
            return layer == Layer.Field
                ? new LayerIds(Ids.FieldDetail, FieldColours, Ids.FieldSteps, Ids.FieldNormal, Ids.FieldBreakUp)
                : new LayerIds(Ids.CircleDetail, CircleColours, Ids.CircleSteps, Ids.CircleNormal, Ids.CircleBreakUp);
        }

        /** <summary>Lights the floor straight down with the ramp back as the stage made it: every flat pixel fully lit.</summary> */
        private void Lit()
        {
            _stage.LightFrom(Vector3.up);
            _material.SetFloat(Ids.ShadowThreshold, 0.5f);
            _material.SetFloat(Ids.ShadowSoftness, 0.004f);
        }

        /** <summary>A shade rising across the tile from 0 to 1, mask on, flat normal.</summary> */
        private static Color32 RampAcross(int x, int y)
        {
            return new Color32((byte)Mathf.RoundToInt(x * 255f / 63f), 255, 128, 128);
        }

        /** <summary>Shade dark in the left half of the tile and light in the right, mask on, flat normal.</summary> */
        private static Color32 HalvesAcross(int x, int y)
        {
            return new Color32((byte)(x < 32 ? 0 : 255), 255, 128, 128);
        }

        /** <summary>Shade dark in the left half and light in the right; the mask on in the bottom quarter, off in the next, half in the top half.</summary> */
        private static Color32 ShadeHalvesMaskQuarters(int x, int y)
        {
            return new Color32((byte)(x < 32 ? 0 : 255), (byte)(y < 16 ? 255 : y < 32 ? 0 : 128), 128, 128);
        }

        /** <summary>A shade different at every texel. Made coarse and magnified on screen: a fine one minified with no mips averages away its own range.</summary> */
        private static Color32 CoarseHash(int x, int y)
        {
            return new Color32((byte)(((x * 73856093) ^ (y * 19349663)) & 0xff), 255, 128, 128);
        }

        private static string Body(string text, string signature)
        {
            int start = text.IndexOf(signature, StringComparison.Ordinal);
            Assert.GreaterOrEqual(start, 0, signature);
            int open = text.IndexOf('{', start), depth = 0;
            for (int i = open; i < text.Length; i++)
            {
                depth += text[i] == '{' ? 1 : text[i] == '}' ? -1 : 0;
                if (depth == 0)
                    return text.Substring(open, i - open + 1);
            }
            return text.Substring(open);
        }

        #endregion

        [SetUp]
        public void SetUp()
        {
            _stage = new LitStage(Pixels);
            _stage.Camera.transform.SetPositionAndRotation(new Vector3(0f, 10f, 0f), Quaternion.Euler(90f, 0f, 0f));
            _stage.Camera.orthographicSize = View;
            _stage.LightFrom(Vector3.up);
            _material = TestMaterials.WorldLit("Anime/Ground");
            _made.Add(_material);
            Colours(FieldColours, Green);
            Colours(CircleColours, Grey);
            foreach (int setting in new[] { Ids.FieldSteps, Ids.CircleSteps, Ids.FieldNormal, Ids.CircleNormal, Ids.FieldBreakUp, Ids.CircleBreakUp, Ids.PatchStrength, Ids.EarthAmount })
                _material.SetFloat(setting, 0f);
            _material.SetFloat(Ids.FieldTile, 1.6f);
            _material.SetFloat(Ids.CircleTile, 1.6f);
            _material.SetFloat(Ids.EdgeSoftness, 0.05f);
            _material.SetFloat(Ids.DetailFadeStart, 100f);
            Circle(0f, 0f, 1.5f);
            Mesh floor = TestMeshes.Quad();
            _made.Add(floor);
            _floor = _stage.Add(floor, _material, Vector3.zero, Quaternion.Euler(90f, 0f, 0f), Vector3.one * 20f);
        }

        [TearDown]
        public void TearDown()
        {
            _stage.Dispose();
            foreach (Object made in _made)
                Object.DestroyImmediate(made);
            _made.Clear();
        }

        [Test]
        public void InsideTheCircle_IsTheCircle_OutsideIsTheField()
        {
            Color[] frame = _stage.Render();
            Assert.Greater(FrameReads.Count(frame, FrameReads.Covered), 0, "the floor drew");
            Assert.Less(Distance(At(frame, 0.5f, 0f), Grey), Distance(At(frame, 0.5f, 0f), Green), "inside");
            Assert.Less(Distance(At(frame, 2.5f, 0f), Green), Distance(At(frame, 2.5f, 0f), Grey), "outside");
        }

        [Test]
        public void TheCircle_FollowsItsCentre_AndItsRadius()
        {
            Circle(2.5f, 0f, 1.5f);
            Color[] moved = _stage.Render();
            Assert.IsTrue(FrameReads.Near(At(moved, 2.5f, 0f), Grey, 0.05f), "the new centre");
            Assert.IsTrue(FrameReads.Near(At(moved, -0.5f, 0f), Green, 0.05f), "two metres from the new centre");

            Circle(0f, 0f, 3f);
            Assert.IsTrue(FrameReads.Near(At(_stage.Render(), 2.5f, 0f), Grey, 0.05f), "inside the doubled radius");
        }

        [Test]
        public void NoRadius_IsAllField()
        {
            Circle(0f, 0f, 0f);
            Color[] frame = _stage.Render();
            Assert.Greater(FrameReads.Count(frame, FrameReads.Covered), 0, "the floor drew");
            Assert.AreEqual(0, FrameReads.Count(frame, pixel => FrameReads.Covered(pixel) && !FrameReads.Near(pixel, Green, 0.05f)));
        }

        [Test]
        public void EachLayer_ReadsItsOwnMap_AtItsOwnTileSize()
        {
            Colours(FieldColours, Black, White);
            Colours(CircleColours, Black, White);
            _material.SetFloat(Ids.CircleTile, 0.8f);
            _material.SetTexture(Ids.CircleDetail, Detail(HalvesAcross));
            _material.SetTexture(Ids.FieldDetail, Uniform(new Color32(255, 255, 128, 128)));
            Circle(0f, 0f, 100f);
            Color[] frame = _stage.Render();
            Assert.IsTrue(FrameReads.Near(At(frame, 0.2f, 0.1f), Black, 0.05f), "a quarter into the circle's 0.8 m tile: its dark half");
            Assert.IsTrue(FrameReads.Near(At(frame, 0.6f, 0.1f), White, 0.05f), "three quarters in: its light half");
        }

        [Test]
        public void TheShade_PicksBetweenDarkAndLight_AndTheMaskBetweenThePairs()
        {
            Circle(0f, 0f, 0f);
            Color dark = new(0.1f, 0.1f, 0.5f), light = new(0.9f, 0.9f, 0.2f), secondDark = new(0.5f, 0f, 0f), secondLight = new(0f, 0.8f, 0.8f);
            Colours(FieldColours, dark, light, secondDark, secondLight);
            _material.SetTexture(Ids.FieldDetail, Detail(ShadeHalvesMaskQuarters));
            Color[] frame = _stage.Render();
            const float tile = 1.6f, quarter = tile / 4f;
            Assert.IsTrue(FrameReads.Near(At(frame, quarter, quarter / 2f), dark, 0.05f), $"dark {At(frame, quarter, quarter / 2f)}");
            Assert.IsTrue(FrameReads.Near(At(frame, 3f * quarter, quarter / 2f), light, 0.05f), "light");
            Assert.IsTrue(FrameReads.Near(At(frame, quarter, 1.5f * quarter), secondDark, 0.05f), "second dark where the mask is off");
            Assert.IsTrue(FrameReads.Near(At(frame, 3f * quarter, 1.5f * quarter), secondLight, 0.05f), "second light");
            Color half = Color.Lerp(secondDark, dark, 128f / 255f);
            Assert.IsTrue(FrameReads.Near(At(frame, quarter, 3f * quarter), half, 0.05f), $"half a mask between the pairs: {At(frame, quarter, 3f * quarter)}");
        }

        [TestCase(Layer.Field, 0f, 0)]
        [TestCase(Layer.Field, 1f, 0)]
        [TestCase(Layer.Field, 3f, 3)]
        [TestCase(Layer.Field, 4f, 4)]
        [TestCase(Layer.Circle, 3f, 3)]
        public void ShadeSteps_MakeThatManyTones(Layer layer, float steps, int tones)
        {
            Show(layer);
            Colours(Of(layer).Colours, Black, White);
            _material.SetFloat(Of(layer).Steps, steps);
            _material.SetTexture(Of(layer).Detail, Detail(RampAcross));
            Color[] frame = _stage.Render();
            Assert.IsTrue(frame.All(Finite), "finite at every shade");
            float[] shades = CoveredShades(frame).ToArray();
            Assert.Greater(shades.Length, 0, "the floor drew");
            if (tones == 0)
            {
                Assert.Greater(shades.Select(shade => Mathf.RoundToInt(shade * 20f)).Distinct().Count(), 10, "smooth: many shades");
                return;
            }
            float[] levels = Enumerable.Range(0, tones).Select(i => i / (tones - 1f)).ToArray();
            int onALevel = shades.Count(shade => levels.Any(level => Mathf.Abs(shade - level) < 0.04f));
            Assert.Greater(onALevel, shades.Length * 0.8f, "almost every pixel on one of the tones");
            foreach (float level in levels)
                Assert.IsTrue(shades.Any(shade => Mathf.Abs(shade - level) < 0.04f), $"tone {level} shows");
        }

        [TestCase(Layer.Field)]
        [TestCase(Layer.Circle)]
        public void TheDetailNormal_TiltsTheLight_InXAndInZ(Layer layer)
        {
            Show(layer);
            Colours(Of(layer).Colours, White);
            _material.SetFloat(Of(layer).Normal, 1f);
            _material.SetTexture(Of(layer).Detail, Uniform(new Color32(128, 255, 210, 128)));
            Assert.Greater(LitFrom(new Vector3(1f, 1f, 0f)), LitFrom(new Vector3(-1f, 1f, 0f)) + 0.05f, "tilted toward +X: lit from +X");
            _material.SetTexture(Of(layer).Detail, Uniform(new Color32(128, 255, 128, 210)));
            Assert.Greater(LitFrom(new Vector3(0f, 1f, 1f)), LitFrom(new Vector3(0f, 1f, -1f)) + 0.05f, "tilted toward +Z: lit from +Z");

            _material.SetFloat(Of(layer).Normal, 0f);
            Assert.AreEqual(LitFrom(new Vector3(0f, 1f, 1f)), LitFrom(new Vector3(0f, 1f, -1f)), 0.01f, "no strength, no tilt");
        }

        [Test]
        public void ADirectXNormal_PackedFlipped_TiltsAsTheOpenGLOneDoes()
        {
            Circle(0f, 0f, 0f);
            Colours(FieldColours, White);
            _material.SetFloat(Ids.FieldNormal, 1f);
            var sources = new DetailSources(DetailImage.Filled(4, 4, new Color32(128, 128, 128, 255)), null,
                DetailImage.Filled(4, 4, new Color32(128, 46, 230, 255)), null);
            DetailImage packed = DetailMath.Pack(sources, new DetailSettings(0f, 0f, 0, NormalConvention.DirectX));
            _material.SetTexture(Ids.FieldDetail, Detail((x, y) => packed.Pixels[y * 4 + x], 4));
            Assert.Greater(LitFrom(new Vector3(0f, 1f, 1f)), LitFrom(new Vector3(0f, 1f, -1f)) + 0.05f, "DirectX green down is +Z once flipped");
        }

        [Test]
        public void FarAway_TheNormalFlattens_AndTheStepsSmooth()
        {
            Circle(0f, 0f, 0f);
            Colours(FieldColours, White);
            _material.SetFloat(Ids.FieldNormal, 1f);
            _material.SetTexture(Ids.FieldDetail, Uniform(new Color32(128, 255, 210, 128)));
            Assert.Greater(LitFrom(new Vector3(1f, 1f, 0f)), LitFrom(new Vector3(-1f, 1f, 0f)) + 0.05f, "near, the tilt shows");
            _material.SetFloat(Ids.DetailFadeStart, 0f);
            _material.SetFloat(Ids.DetailFadeRange, 1f);
            Assert.AreEqual(LitFrom(new Vector3(1f, 1f, 0f)), LitFrom(new Vector3(-1f, 1f, 0f)), 0.01f, "far, the tilt faded");

            _material.SetFloat(Ids.FieldNormal, 0f);
            Lit();
            Colours(FieldColours, Black, White);
            _material.SetFloat(Ids.FieldSteps, 2f);
            _material.SetTexture(Ids.FieldDetail, Detail(RampAcross));
            Assert.Greater(CoveredShades(_stage.Render()).Select(shade => Mathf.RoundToInt(shade * 20f)).Distinct().Count(), 10, "far, the steps smoothed");
            _material.SetFloat(Ids.DetailFadeStart, 100f);
            float[] near = CoveredShades(_stage.Render()).ToArray();
            Assert.Greater(near.Count(shade => shade < 0.04f || shade > 0.96f), near.Length * 0.8f, "near, the same steps hold");
        }

        [TestCase(Layer.Field)]
        [TestCase(Layer.Circle)]
        public void BreakUpTiling_BreaksTheRepeat_AndKeepsTheContrast(Layer layer)
        {
            Show(layer);
            Colours(Of(layer).Colours, Black, White);
            _material.SetFloat(Ids.PatchScale, 1f);
            _material.SetFloat(Ids.BreakUpScale, 0.43f);
            _material.SetFloat(Ids.BreakUpAngle, 37f);
            _material.SetTexture(Of(layer).Detail, Detail(CoarseHash, 8));
            int tilePixels = Mathf.RoundToInt(1.6f / (2f * View) * Pixels);

            Color[] plain = _stage.Render();
            float plainSpread = Spread(CoveredShades(plain));
            for (int x = 10; x < 60; x += 7)
                Assert.IsTrue(FrameReads.Near(FrameReads.At(plain, x, 40), FrameReads.At(plain, x + tilePixels, 40), 0.02f), $"repeats one tile on at column {x}");

            _material.SetFloat(Of(layer).BreakUp, 1f);
            Color[] broken = _stage.Render();
            int differing = Enumerable.Range(0, Pixels - tilePixels).Count(x => !FrameReads.Near(FrameReads.At(broken, x, 40), FrameReads.At(broken, x + tilePixels, 40), 0.02f));
            Assert.Greater(differing, 10, "no longer repeats");
            Assert.GreaterOrEqual(Spread(CoveredShades(broken)), plainSpread * 0.95f, "the blend keeps the contrast (a plain lerp loses about a tenth)");
        }

        [Test]
        public void BareEarth_ShowsOnTheField_OnlyWithAnAmount()
        {
            Circle(0f, 0f, 0f);
            var earth = new Color(0.6f, 0.3f, 0.1f);
            TestMaterials.SetLinear(_material, Ids.EarthColor, earth);
            _material.SetFloat(Ids.PatchScale, 1f);
            Assert.AreEqual(0, FrameReads.Count(_stage.Render(), pixel => FrameReads.Near(pixel, earth, 0.05f)), "no amount, no earth");
            _material.SetFloat(Ids.EarthAmount, 0.5f);
            Assert.Greater(FrameReads.Count(_stage.Render(), pixel => FrameReads.Near(pixel, earth, 0.05f)), 0, "patches of earth");
        }

        [Test]
        public void PatchTint_TakesTheTintsHue_KeepsTheBrightness_OnlyOnTheField_WithStrengthAndCover()
        {
            var dim = new Color(0.3f, 0.3f, 0.3f);
            Show(Layer.Field);
            Colours(FieldColours, dim);
            _material.SetFloat(Ids.PatchTintScale, 1f);
            _material.SetFloat(Ids.PatchTintCover, 0.5f);
            _material.SetFloat(Ids.PatchTintSoftness, 0.05f);
            Color[] plain = _stage.Render();
            Assert.Greater(FrameReads.Count(plain, FrameReads.Covered), 0, "the floor drew");

            TestMaterials.SetLinear(_material, Ids.PatchTint, new Color(0.5f, 0.1f, 0.1f, 0f));
            Assert.IsTrue(plain.SequenceEqual(_stage.Render()), "no strength, no tint");

            TestMaterials.SetLinear(_material, Ids.PatchTint, new Color(0.5f, 0.1f, 0.1f, 1f));
            Color[] tinted = _stage.Render();
            Assert.Greater(Luminance(plain[0]) - 0.185f, 0.05f, "the tint is darker than the ground, so keeping the brightness shows");
            int[] patches = Enumerable.Range(0, tinted.Length).Where(i => FrameReads.Covered(tinted[i]) && tinted[i].r > tinted[i].g + 0.1f).ToArray();
            Assert.Greater(patches.Length, tinted.Length / 20, "patches take the tint's hue");
            Assert.Less(patches.Length, tinted.Length * 19 / 20, "and only patches");
            foreach (int i in patches)
                Assert.AreEqual(Luminance(plain[i]), Luminance(tinted[i]), 0.03f, $"a tinted pixel keeps its brightness ({plain[i]} to {tinted[i]})");

            _material.SetFloat(Ids.PatchTintCover, 0f);
            Assert.IsTrue(plain.SequenceEqual(_stage.Render()), "no cover, no patches");

            _material.SetFloat(Ids.PatchTintCover, 0.5f);
            Show(Layer.Circle);
            Colours(CircleColours, dim);
            Color[] circle = _stage.Render();
            Assert.AreEqual(0, circle.Count(pixel => FrameReads.Covered(pixel) && pixel.r > pixel.g + 0.1f), "the circle takes none");
        }

        [TestCase(0f)]
        [TestCase(0.6f)]
        public void TheEdgeBand_HasNoStep(float wobble)
        {
            Colours(FieldColours, Black, White);
            Colours(CircleColours, Black, White);
            _material.SetFloat(Ids.EdgeSoftness, 0.8f);
            _material.SetTexture(Ids.FieldDetail, Uniform(new Color32(51, 255, 128, 128)));
            _material.SetTexture(Ids.CircleDetail, Uniform(new Color32(204, 255, 128, 128)));
            Circle(0f, 0f, 1.5f, wobble);
            Color[] frame = _stage.Render();
            float worst = 0f;
            foreach (int row in new[] { Pixels / 4, Pixels / 2, 3 * Pixels / 4 })
            {
                for (int x = 0; x + 1 < Pixels; x++)
                    worst = Mathf.Max(worst, Mathf.Abs(FrameReads.At(frame, x, row).r - FrameReads.At(frame, x + 1, row).r));
            }
            Assert.Less(worst, 0.1f, "the band blends the layers smoothly (a layer skipped in it would jump by about 0.2 or more)");
        }

        [Test]
        public void TheNeutralMap_AndAClearedSlot_PaintThePalettesMiddle()
        {
            Circle(0f, 0f, 0f);
            Colours(FieldColours, Black, White);
            Color middle = new(0.25f, 0.25f, 0.25f);
            var importer = (ShaderImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(Shader.Find("Anime/Ground")));
            _material.SetTexture(Ids.FieldDetail, importer.GetDefaultTexture(Ids.FieldDetailName));
            Color[] defaulted = _stage.Render();
            Assert.Greater(FrameReads.Count(defaulted, FrameReads.Covered), 0, "the floor drew");
            Assert.IsTrue(defaulted.All(Finite), "finite with the neutral map");
            Assert.IsTrue(FrameReads.Near(At(defaulted, 0.5f, 0.5f), middle, 0.03f), $"the gradient's middle: {At(defaulted, 0.5f, 0.5f)}");

            _material.SetTexture(Ids.FieldDetail, null);
            Color[] cleared = _stage.Render();
            Assert.IsTrue(cleared.All(Finite), "finite with no map");
            Assert.IsTrue(FrameReads.Near(At(cleared, 0.5f, 0.5f), middle, 0.03f), $"the gradient's middle: {At(cleared, 0.5f, 0.5f)}");
        }

        [TestCase("_EdgeSoftness", 0f)]
        [TestCase("_FieldTile", 0f)]
        [TestCase("_CircleTile", 0f)]
        [TestCase("_DetailFadeRange", 0f)]
        [TestCase("_BreakUpScale", 0f)]
        public void ADegenerateSetting_StaysFinite(string setting, float value)
        {
            _material.SetFloat(Ids.FieldBreakUp, 1f);
            _material.SetFloat(Ids.FieldSteps, 3f);
            _material.SetFloat(Ids.FieldNormal, 1f);
            _material.SetFloat(Ids.CircleNormal, 1f);
            _material.SetFloat(Shader.PropertyToID(setting), value);
            Color[] frame = _stage.Render();
            Assert.Greater(FrameReads.Count(frame, FrameReads.Covered), 0, "the floor drew");
            Assert.IsTrue(frame.All(Finite), $"{setting} at {value}");
        }

        [TestCase(0f)]
        [TestCase(60f)]
        [TestCase(80f)]
        public void ASlopedFloor_StaysFinite_WithItsDetailNormal(float slope)
        {
            _material.SetFloat(Ids.FieldNormal, 1f);
            _material.SetFloat(Ids.CircleNormal, 1f);
            _material.SetTexture(Ids.FieldDetail, Uniform(new Color32(128, 255, 210, 210)));
            _stage.LightFrom(new Vector3(1f, 1f, 0.3f));
            _floor.transform.rotation = Quaternion.Euler(0f, 0f, slope) * Quaternion.Euler(90f, 0f, 0f);
            Color[] frame = _stage.Render();
            Assert.Greater(FrameReads.Count(frame, FrameReads.Covered), 0, "the floor drew");
            Assert.IsTrue(frame.All(Finite), $"sloped {slope} degrees toward X");
        }

        [Test]
        public void FarFromTheOrigin_AWholeNumberOfTilesOn_IsTheSame()
        {
            Circle(0f, 0f, 0f);
            Colours(FieldColours, Black, White);
            _material.SetFloat(Ids.FieldTile, 2f);
            _material.SetFloat(Ids.PatchScale, 0.04f);
            _material.SetFloat(Ids.PatchStrength, 0.3f);
            _material.SetTexture(Ids.FieldDetail, Detail(CoarseHash));
            Color[] home = _stage.Render();

            var offset = new Vector3(1000f, 0f, 1000f);
            _stage.Camera.transform.position += offset;
            Mesh floor = TestMeshes.Quad();
            _made.Add(floor);
            _stage.Add(floor, _material, offset, Quaternion.Euler(90f, 0f, 0f), Vector3.one * 20f);
            Color[] away = _stage.Render();
            int differing = Enumerable.Range(0, home.Length).Count(i => !FrameReads.Near(home[i], away[i], 0.03f));
            Assert.Less(differing, home.Length / 100, "the same ground a thousand metres on");
        }

        [Test]
        public void TheLayerReads_SitInBranches_WithNoDerivativeInside()
        {
            string text = PackSources.IncludeText(Shader.Find("Anime/Ground"), "AnimeGround.hlsl");
            string read = Body(text, "AnimeGroundSample AnimeGroundRead(");
            foreach (string derivative in new[] { "ddx(", "ddy(", "fwidth(", "SAMPLE_TEXTURE2D(" })
                StringAssert.DoesNotContain(derivative, read, "a layer's reads take their gradients from outside the branch");
            string surface = Body(text, "void AnimeCustomSurface(");
            Assert.AreEqual(2, Regex.Matches(surface, @"AnimeGroundNoise\(").Count, "two noise reads: the patches' and the tint's");
            StringAssert.IsMatch(@"UNITY_BRANCH\s*if \(_PatchTint\.a > 0\.0h\)\s*tintNoise = AnimeGroundNoise\(", surface, "the tint's read only with a tint");
            Assert.AreEqual(2, Regex.Matches(surface, @"UNITY_BRANCH\s*if \([^)]*\)\s*\w+ = AnimeGroundRead\([^;]*;").Count,
                "each layer's branch holds its read and nothing else, so painting, steps and the tilt run outside");
            Assert.AreEqual(2, Regex.Matches(read, @"SAMPLE_TEXTURE2D_GRAD\(").Count, "at most two reads a layer");
        }
    }
}
