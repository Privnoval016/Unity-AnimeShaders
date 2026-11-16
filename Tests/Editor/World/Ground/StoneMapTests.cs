using System.Collections.Generic;
using System.Linq;
using AnimeShaders.EditorTools;
using NUnit.Framework;
using UnityEngine;

namespace AnimeShaders.Tests
{
    /**
     * <summary>Finding stones on made occlusion maps: a grid of bright stones on dark joints whose joints are placed so
     * stones cross the tile's edges and one is centred exactly on its corner. Every texel of a stone, walked in floor
     * coordinates across the tile's edges, names the same home tile and value; pitting doesn't split a stone; joints take
     * a neighbouring stone; a band round the whole tile, a flat map and stones under the minimum give none; the result is
     * deterministic and the shift codes round-trip.</summary>
     */
    public class StoneMapTests
    {
        #region Private Fields

        private const int Size = 64;
        private const int Cell = 16;
        private const int Joint = 2;
        private const int CornerOffset = 7;
        private const int MapScale = StoneMath.MapSize / Size;
        private const byte Stone = 230;
        private const byte Grout = 40;

        private static readonly StoneSettings Settings = new(100f / Size, 0.5f, 1f);

        #endregion

        #region Helpers

        /** <summary>An occlusion map: stones on a grid of joints starting at <paramref name="offset" />, optionally with vertical joints, optionally pitted.</summary> */
        private static DetailImage Grid(int offset = CornerOffset, bool vertical = true, bool pitted = false)
        {
            var pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    bool joint = DataTextures.Wrap(y - offset, Cell) < Joint || (vertical && DataTextures.Wrap(x - offset, Cell) < Joint);
                    bool pit = pitted && !joint && ((x * 73856093) ^ (y * 19349663)) % 7 == 0;
                    byte value = joint || pit ? Grout : Stone;
                    pixels[y * Size + x] = new Color32(value, value, value, 255);
                }
            }
            return new DetailImage(Size, Size, pixels);
        }

        /** <summary>The home tile and value of the floor texel at (<paramref name="x" />, <paramref name="y" />), in source texels, any copy of the tile.</summary> */
        private static (int X, int Y, byte Value) Home(StoneMap map, int x, int y)
        {
            int wrappedX = DataTextures.Wrap(x, Size), wrappedY = DataTextures.Wrap(y, Size);
            Color32 texel = map.Image.Pixels[(wrappedY * MapScale + MapScale / 2) * StoneMath.MapSize + wrappedX * MapScale + MapScale / 2];
            Assert.IsTrue(StoneMath.TryShift(texel.g, out int shiftX, out int shiftY), $"a stone at ({x}, {y})");
            return (Mathf.FloorToInt((float)x / Size) + shiftX, Mathf.FloorToInt((float)y / Size) + shiftY, texel.r);
        }

        /** <summary>Every distinct home and value over the stone in grid cell (<paramref name="column" />, <paramref name="row" />), its texels away from the joints.</summary> */
        private static HashSet<(int, int, byte)> Homes(StoneMap map, int column, int row, int offset = CornerOffset)
        {
            var homes = new HashSet<(int, int, byte)>();
            int left = offset + column * Cell + Joint + 1, bottom = offset + row * Cell + Joint + 1;
            for (int y = bottom; y < bottom + Cell - Joint - 2; y++)
            {
                for (int x = left; x < left + Cell - Joint - 2; x++)
                    homes.Add(Home(map, x, y));
            }
            return homes;
        }

        #endregion

        [Test]
        public void EveryStone_IsFound_WithOneHomeAndValue_AcrossTheTilesEdges()
        {
            StoneMap map = StoneMath.Find(Grid(), Settings);
            Assert.AreEqual(Size / Cell * (Size / Cell), map.Count, "a stone per cell");
            Assert.AreEqual(0, map.Dropped);
            var stones = new List<(int, int, byte)>();
            for (int row = 0; row < Size / Cell; row++)
            {
                for (int column = 0; column < Size / Cell; column++)
                {
                    HashSet<(int, int, byte)> homes = Homes(map, column, row);
                    Assert.AreEqual(1, homes.Count, $"one home and value over the stone in cell ({column}, {row}): {string.Join(", ", homes)}");
                    stones.Add(homes.First());
                }
            }
            Assert.GreaterOrEqual(stones.Distinct().Count(), stones.Count - 2, "stones of the floor differ (a value collides about once in 255)");
        }

        [Test]
        public void AStoneCentredOnTheTilesCorner_AgreesOnEverySide()
        {
            StoneMap map = StoneMath.Find(Grid(), Settings);
            Assert.AreEqual(Size, CornerOffset + 3 * Cell + Joint + (Cell - Joint) / 2, "the made stone's centre sits on the corner");
            HashSet<(int, int, byte)> homes = Homes(map, 3, 3);
            Assert.AreEqual(1, homes.Count, $"the corner stone, across both edges: {string.Join(", ", homes)}");
            Assert.AreEqual((1, 1), (homes.First().Item1, homes.First().Item2), "its home is the tile its centre opens");
        }

        [Test]
        public void Pitting_DoesNotSplitAStone()
        {
            StoneMap map = StoneMath.Find(Grid(pitted: true), Settings);
            Assert.AreEqual(Size / Cell * (Size / Cell), map.Count, "the same stones");
            Assert.AreEqual(1, Homes(map, 1, 1).Count, "a pitted stone is one stone");
        }

        [Test]
        public void AJoint_TakesANeighbouringStone()
        {
            StoneMap map = StoneMath.Find(Grid(), Settings);
            int x = CornerOffset + Cell, y = CornerOffset + Cell + Cell / 2;
            (int, int, byte) joint = Home(map, x, y);
            CollectionAssert.Contains(new[] { Homes(map, 0, 1).First(), Homes(map, 1, 1).First() }, joint, "the joint between two stones takes one of them");

            var beside = new[] { Homes(map, 3, 2).First(), Homes(map, 3, 3).First() };
            int jointRow = CornerOffset + 3 * Cell;
            for (int across = CornerOffset + 3 * Cell + Joint + 1; across < Size + CornerOffset - 1; across++)
                CollectionAssert.Contains(beside, Home(map, across, jointRow), $"a joint crossing the tile's edge at {across} takes a stone beside it");
        }

        [Test]
        public void TheFill_CrossesTheTilesEdge_IntoTheNearestCopyOfAStone()
        {
            const int low = 10, high = 21;
            var pixels = new Color32[Size * Size];
            for (int i = 0; i < pixels.Length; i++)
            {
                int x = i % Size, y = i / Size;
                byte value = x >= low && x < high && y >= low && y < high ? Stone : Grout;
                pixels[i] = new Color32(value, value, value, 255);
            }
            StoneMap map = StoneMath.Find(new DetailImage(Size, Size, pixels), Settings);
            Assert.AreEqual(1, map.Count);
            byte value0 = Home(map, low + 2, low + 2).Value;
            int halfway = (high - 1 + low + Size) / 2;
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    if (Mathf.Abs(x - halfway) <= 1 || Mathf.Abs(y - halfway) <= 1)
                        continue;
                    (int, int, byte) expected = (x > halfway ? 1 : 0, y > halfway ? 1 : 0, value0);
                    Assert.AreEqual(expected, Home(map, x, y), $"the texel at ({x}, {y}) takes the nearer copy of the stone");
                }
            }
        }

        [Test]
        public void StonesUnderTheMinimum_AreNotKept()
        {
            float area = (Cell - Joint) * (float)(Cell - Joint) / (Size * Size) * 100f;
            Assert.AreEqual(16, StoneMath.Find(Grid(), new StoneSettings(Settings.Blur, Settings.Threshold, area * 0.5f)).Count);
            StoneMap none = StoneMath.Find(Grid(), new StoneSettings(Settings.Blur, Settings.Threshold, area * 2f));
            Assert.AreEqual(0, none.Count);
            Assert.IsTrue(none.Image.Pixels.All(pixel => pixel.g == StoneMath.None), "no stone anywhere");
        }

        [Test]
        public void ABandRoundTheWholeTile_IsDropped()
        {
            StoneMap map = StoneMath.Find(Grid(vertical: false), Settings);
            Assert.AreEqual(0, map.Count);
            Assert.AreEqual(Size / Cell, map.Dropped, "each band wraps onto itself");
        }

        [TestCase(Stone)]
        [TestCase(Grout)]
        public void AFlatMap_FindsNoStones(byte value)
        {
            StoneMap map = StoneMath.Find(DetailImage.Filled(Size, Size, new Color32(value, value, value, 255)), Settings);
            Assert.AreEqual(0, map.Count);
            Assert.IsTrue(map.Image.Pixels.All(pixel => pixel.g == StoneMath.None));
        }

        [Test]
        public void Finding_IsDeterministic()
        {
            DetailImage occlusion = Grid(pitted: true);
            CollectionAssert.AreEqual(StoneMath.Find(occlusion, Settings).Image.Pixels, StoneMath.Find(occlusion, Settings).Image.Pixels);
        }

        [Test]
        public void ShiftCodes_RoundTrip_AndNoneHoldsNoShift()
        {
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    Assert.IsTrue(StoneMath.TryShift(StoneMath.Code(x, y), out int shiftX, out int shiftY));
                    Assert.AreEqual((x, y), (shiftX, shiftY));
                }
            }
            Assert.IsFalse(StoneMath.TryShift(StoneMath.None, out _, out _));
            Assert.IsFalse(StoneMath.TryShift(10, out _, out _), "out of range");
        }
    }
}
