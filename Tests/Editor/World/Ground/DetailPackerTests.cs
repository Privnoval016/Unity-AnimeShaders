using System;
using System.Linq;
using AnimeShaders.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AnimeShaders.Tests
{
    /**
     * <summary>The detail packing's maths on made sources: the shade follows the colour's brightness by its luminance
     * weights and the occlusion, fills 0..1 about a mean of a half and stays finite when flat; the mask and the normal
     * land in their channels (a DirectX normal flipped); sources of other sizes resample with a wrap; the soften blurs
     * and wraps, so a tiling set packs to a tiling map; images need not be square; the set's hash follows every
     * setting.</summary>
     */
    public class DetailPackerTests
    {
        #region Private Fields

        private const int Size = 32;
        private const float Step = 1f / 255f;

        private static readonly Color32 FlatNormal = new(128, 128, 255, 255);

        #endregion

        #region Helpers

        private static DetailImage Image(Func<int, int, Color32> pixel, int width = Size, int height = Size)
        {
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                    pixels[y * width + x] = pixel(x, y);
            }
            return new DetailImage(width, height, pixels);
        }

        private static DetailImage Grey(Func<int, int, byte> value, int width = Size, int height = Size)
        {
            return Image((x, y) =>
            {
                byte v = value(x, y);
                return new Color32(v, v, v, 255);
            }, width, height);
        }

        private static DetailImage Flat(byte value, int size = Size)
        {
            return Grey((x, y) => value, size, size);
        }

        private static DetailImage Normals(Color32 normal, int width = Size, int height = Size)
        {
            return DetailImage.Filled(width, height, normal);
        }

        /** <summary>A brightness that rises along x, the same on every row.</summary> */
        private static DetailImage RampAcross(int width = Size, int height = Size)
        {
            return Grey((x, y) => (byte)(40 + x * 160 / width), width, height);
        }

        private static byte Hash(int x, int y)
        {
            return (byte)(((x * 73856093) ^ (y * 19349663)) & 0xff);
        }

        /** <summary>A brightness different at every texel (a hash), repeating nowhere.</summary> */
        private static DetailImage Speckled()
        {
            return Grey(Hash);
        }

        /** <summary>A soften of <paramref name="texels" /> texels of a <see cref="Size" />-wide map, as the percentage the settings take.</summary> */
        private static float Texels(float texels)
        {
            return texels / Size * 100f;
        }

        private static DetailSettings Settings(float soften = 0f, float occlusionWeight = 0f, int size = 0, NormalConvention normals = NormalConvention.OpenGL)
        {
            return new DetailSettings(soften, occlusionWeight, size, normals);
        }

        private static DetailImage Pack(DetailImage colour, DetailSettings settings, DetailImage occlusion = null, DetailImage mask = null, DetailImage normal = null)
        {
            return DetailMath.Pack(new DetailSources(colour, occlusion, normal ?? Normals(FlatNormal, colour.Width, colour.Height), mask), settings);
        }

        private static float Shade(DetailImage image, int x, int y)
        {
            return image.Pixels[y * image.Width + x].r / 255f;
        }

        private static float NeighbourStep(DetailImage image)
        {
            float total = 0f;
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x + 1 < image.Width; x++)
                    total += Mathf.Abs(Shade(image, x + 1, y) - Shade(image, x, y));
            }
            return total / (image.Height * (image.Width - 1));
        }

        #endregion

        [Test]
        public void TheShade_RisesWithTheColoursBrightness()
        {
            DetailImage packed = Pack(RampAcross(), Settings());
            for (int x = 1; x < Size; x++)
                Assert.GreaterOrEqual(Shade(packed, x, 3), Shade(packed, x - 1, 3), $"column {x}");
            Assert.Greater(Shade(packed, Size - 1, 3), Shade(packed, 0, 3));
        }

        [Test]
        public void TheShade_WeighsTheChannelsAsLuminanceDoes()
        {
            DetailImage colour = Image((x, y) => x < Size / 2 ? new Color32(0, 200, 0, 255) : new Color32(0, 0, 255, 255));
            DetailImage packed = Pack(colour, Settings());
            Assert.Greater(Shade(packed, 4, 4), Shade(packed, Size - 4, 4), "a green outweighs a brighter blue");
        }

        [Test]
        public void Occlusion_DarkensTheShade_OnlyWithAWeight()
        {
            DetailImage occlusion = Grey((x, y) => (byte)(y < Size / 2 ? 255 : 64));
            DetailImage none = Pack(RampAcross(), Settings(occlusionWeight: 0f), occlusion);
            DetailImage half = Pack(RampAcross(), Settings(occlusionWeight: 0.5f), occlusion);
            DetailImage full = Pack(RampAcross(), Settings(occlusionWeight: 1f), occlusion);
            int x = Size / 2;
            Assert.AreEqual(Shade(none, x, 2), Shade(none, x, Size - 2), Step / 2f, "no weight, no darkening");
            float halfGap = Shade(half, x, 2) - Shade(half, x, Size - 2);
            float fullGap = Shade(full, x, 2) - Shade(full, x, Size - 2);
            Assert.Greater(halfGap, 0f, "half the weight darkens the occluded rows");
            Assert.Greater(fullGap, halfGap, "the full weight darkens them more");
        }

        [Test]
        public void NoOcclusion_IsTheSameAsAWhiteOne()
        {
            CollectionAssert.AreEqual(Pack(Speckled(), Settings(occlusionWeight: 1f), Flat(255)).Pixels,
                Pack(Speckled(), Settings(occlusionWeight: 1f)).Pixels);
        }

        [Test]
        public void TheShade_FillsZeroToOne_AboutAMeanOfAHalf()
        {
            DetailImage skewed = Grey((x, y) => (byte)(Mathf.Pow(Hash(x, y) / 255f, 2f) * 255f));
            DetailImage packed = Pack(skewed, Settings());
            float[] shades = packed.Pixels.Select(pixel => pixel.r / 255f).ToArray();
            Assert.LessOrEqual(shades.Min(), Step, "the darkest reach 0");
            Assert.GreaterOrEqual(shades.Max(), 1f - Step, "the lightest reach 1");
            Assert.AreEqual(DetailMath.Middle, shades.Average(), 0.03f, "the mean, though the source is mostly dark");
        }

        [TestCase(0f)]
        [TestCase(1f)]
        public void AFlatColour_PacksToTheMiddle(float occlusionWeight)
        {
            DetailImage packed = Pack(Flat(100), Settings(occlusionWeight: occlusionWeight), Flat(200));
            Assert.IsTrue(packed.Pixels.All(pixel => Mathf.Abs(pixel.r - 128) <= 1), "every shade the middle");
        }

        [Test]
        public void TheMask_LandsInGreen_UnsoftenedAndWhiteWithNone()
        {
            DetailImage mask = Grey((x, y) => (byte)(x * 8));
            DetailImage masked = Pack(Speckled(), Settings(soften: Texels(2f)), mask: mask);
            DetailImage unmasked = Pack(Speckled(), Settings());
            for (int x = 0; x < Size; x++)
                Assert.AreEqual(x * 8, masked.Pixels[5 * Size + x].g, $"column {x}, the soften leaving it alone");
            Assert.IsTrue(unmasked.Pixels.All(pixel => pixel.g == 255));
        }

        [Test]
        public void TheNormal_LandsInBlueAndAlpha_DirectXFlippedInYOnly()
        {
            var tilted = new Color32(200, 60, 230, 255);
            Color32 flat = Pack(Speckled(), Settings()).Pixels[0];
            Color32 open = Pack(Speckled(), Settings(), normal: Normals(tilted)).Pixels[0];
            Color32 direct = Pack(Speckled(), Settings(normals: NormalConvention.DirectX), normal: Normals(tilted)).Pixels[0];
            Assert.AreEqual((128, 128), ((int)flat.b, (int)flat.a), "flat");
            Assert.AreEqual((200, 60), ((int)open.b, (int)open.a), "OpenGL as it is");
            Assert.AreEqual((200, 255 - 60), ((int)direct.b, (int)direct.a), "DirectX's green flipped, its red kept");
        }

        [Test]
        public void ASmallerSource_ResamplesBilinearly_AndWraps()
        {
            byte[] row = { 0, 80, 160, 240 };
            DetailImage mask = Grey((x, y) => row[x], 4, 4);
            DetailImage packed = Pack(Speckled(), Settings(size: 8), mask: mask);
            float[] expected = { Mathf.Lerp(240, 0, 0.75f), Mathf.Lerp(0, 80, 0.25f), Mathf.Lerp(0, 80, 0.75f), Mathf.Lerp(80, 160, 0.25f) };
            for (int x = 0; x < expected.Length; x++)
                Assert.AreEqual(expected[x], packed.Pixels[2 * 8 + x].g, 1f, $"column {x}{(x == 0 ? ", across the wrap" : "")}");
        }

        [Test]
        public void ANonSquareSet_KeepsItsShape_AndItsDirection()
        {
            DetailImage colour = RampAcross(16, 8);
            DetailImage packed = DetailMath.Pack(new DetailSources(colour, null, Normals(FlatNormal, 16, 8), null), Settings(soften: 5f));
            Assert.AreEqual((16, 8), (packed.Width, packed.Height));
            Assert.Greater(Shade(packed, 12, 4), Shade(packed, 3, 4), "still rising along x");
        }

        [Test]
        public void Soften_SmoothsTheShade_AndZeroKeepsATexelAlone()
        {
            Assert.Less(NeighbourStep(Pack(Speckled(), Settings(soften: Texels(1.5f)))), NeighbourStep(Pack(Speckled(), Settings())) * 0.5f);

            DetailImage dot = Grey((x, y) => (byte)(x == 10 && y == 10 ? 255 : 0));
            DetailImage kept = Pack(dot, Settings());
            DetailImage spread = Pack(dot, Settings(soften: Texels(1f)));
            Assert.AreEqual(Shade(kept, 11, 10), Shade(kept, 20, 20), "no soften: the neighbour stays dark");
            Assert.Greater(Shade(spread, 11, 10), Shade(spread, 20, 20), "softened: it reaches the neighbour");
        }

        [Test]
        public void Soften_Wraps_AcrossBothEdges()
        {
            DetailImage packed = Pack(Grey((x, y) => (byte)(x == 0 || y == Size - 1 ? 255 : 0)), Settings(soften: Texels(1.5f)));
            Assert.Greater(Shade(packed, Size - 1, Size / 2), Shade(packed, Size / 2, Size / 2), "the left column reaches the right edge");
            Assert.Greater(Shade(packed, Size / 2, 0), Shade(packed, Size / 2, Size / 2), "the top row reaches the bottom edge");
        }

        [Test]
        public void ARadiusPastTheSize_FlattensToTheMiddle()
        {
            DetailImage packed = Pack(Speckled(), Settings(soften: Texels(Size * 2f)));
            Assert.IsTrue(packed.Pixels.All(pixel => Mathf.Abs(pixel.r - 128) <= 2), "blurred flat, then moved to the middle");
        }

        [Test]
        public void ATilingSet_PacksToATilingMap()
        {
            DetailImage colour = Grey((x, y) => (byte)(128 + 100 * Mathf.Sin(2f * Mathf.PI * x / Size) * Mathf.Cos(2f * Mathf.PI * y / Size)));
            DetailImage packed = Pack(colour, Settings(soften: Texels(1f)));
            float wrap = 0f, inside = 0f;
            for (int y = 0; y < Size; y++)
            {
                wrap = Mathf.Max(wrap, Mathf.Abs(Shade(packed, 0, y) - Shade(packed, Size - 1, y)));
                for (int x = 0; x + 1 < Size; x++)
                    inside = Mathf.Max(inside, Mathf.Abs(Shade(packed, x + 1, y) - Shade(packed, x, y)));
            }
            Assert.LessOrEqual(wrap, inside + Step, "the step across the wrap is no rougher than inside");
        }

        [Test]
        public void SourcesOfOtherSizes_PackAtTheAskedSize()
        {
            var sources = new DetailSources(Speckled(), Flat(255, 16), Normals(FlatNormal, 8, 8), Flat(255, 64));
            DetailImage packed = DetailMath.Pack(sources, Settings());
            Assert.AreEqual((Size, Size), (packed.Width, packed.Height), "0: the colour's size");
            DetailImage asked = DetailMath.Pack(sources, Settings(size: 16));
            Assert.AreEqual((16, 16), (asked.Width, asked.Height));
        }

        [Test]
        public void AMissingColourOrNormal_IsNamed()
        {
            var noNormal = Assert.Throws<ArgumentException>(() => DetailMath.Pack(new DetailSources(Speckled(), null, null, null), Settings()));
            StringAssert.Contains("normal", noNormal.Message);
            var noColour = Assert.Throws<ArgumentException>(() => DetailMath.Pack(new DetailSources(null, null, Normals(FlatNormal), null), Settings()));
            StringAssert.Contains("colour", noColour.Message);
        }

        [Test]
        public void TheNeutralMap_IsTheMiddleShade_FullMask_FlatNormal()
        {
            Assert.IsTrue(DetailMath.Neutral(4).Pixels.All(pixel => pixel.r == 128 && pixel.g == 255 && pixel.b == 128 && pixel.a == 128));
        }

        [TestCase("soften", 1f)]
        [TestCase("occlusionWeight", 0.25f)]
        [TestCase("size", 16f)]
        [TestCase("normalConvention", 1f)]
        public void TheSetsHash_ChangesWithEverySetting(string field, float change)
        {
            var set = ScriptableObject.CreateInstance<AnimeDetailSet>();
            try
            {
                string before = AnimeDetailPacker.HashOf(set);
                var serialized = new SerializedObject(set);
                SerializedProperty property = serialized.FindProperty(field);
                Assert.IsNotNull(property, field);
                if (property.propertyType == SerializedPropertyType.Float)
                    property.floatValue += change;
                else if (property.propertyType == SerializedPropertyType.Enum)
                    property.enumValueIndex = (property.enumValueIndex + 1) % property.enumNames.Length;
                else
                    property.intValue += (int)change;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Assert.AreNotEqual(before, AnimeDetailPacker.HashOf(set));
            }
            finally
            {
                Object.DestroyImmediate(set);
            }
        }
    }
}
