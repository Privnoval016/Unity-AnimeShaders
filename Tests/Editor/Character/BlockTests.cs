using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AnimeShaders.Tests
{
    /**
     * <summary>The shared block store: two owners on one renderer slot keep both their values in either order; one
     * letting go leaves the other's; the last letting go (or writing nothing) leaves no block; slots stay apart; values
     * changed and written again show; a destroyed renderer's slot is dropped as an owner lets go.</summary>
     */
    public class BlockTests
    {
        #region Private Fields

        private static readonly int First = Shader.PropertyToID("_BlockTestFirst");
        private static readonly int Second = Shader.PropertyToID("_BlockTestSecond");

        private GameObject _holder;
        private Renderer _renderer;
        private Owner _a;
        private Owner _b;

        private sealed class Owner : IAnimeBlockOwner
        {
        }

        #endregion

        #region Helpers

        private static AnimeBlockValues Values(int property, float value)
        {
            var values = new AnimeBlockValues();
            values.SetFloat(property, value);
            return values;
        }

        private float Shown(int property, int slot = 0)
        {
            var block = new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(block, slot);
            return block.GetFloat(property);
        }

        #endregion

        [SetUp]
        public void SetUp()
        {
            _holder = new GameObject("BlockTest");
            _renderer = _holder.AddComponent<MeshRenderer>();
            _renderer.sharedMaterials = new Material[2];
            _a = new Owner();
            _b = new Owner();
        }

        [TearDown]
        public void TearDown()
        {
            AnimeBlocks.Release(_a);
            AnimeBlocks.Release(_b);
            Object.DestroyImmediate(_holder);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void TwoOwnersOnOneSlot_KeepBoth_InEitherOrder(bool firstFirst)
        {
            if (firstFirst)
            {
                AnimeBlocks.Write(_a, _renderer, 0, Values(First, 1f));
                AnimeBlocks.Write(_b, _renderer, 0, Values(Second, 2f));
            }
            else
            {
                AnimeBlocks.Write(_b, _renderer, 0, Values(Second, 2f));
                AnimeBlocks.Write(_a, _renderer, 0, Values(First, 1f));
            }
            Assert.AreEqual(1f, Shown(First));
            Assert.AreEqual(2f, Shown(Second));
        }

        [Test]
        public void OneLettingGo_LeavesTheOthers_AndTheLastLeavesNoBlock()
        {
            AnimeBlocks.Write(_a, _renderer, 0, Values(First, 1f));
            AnimeBlocks.Write(_b, _renderer, 0, Values(Second, 2f));
            AnimeBlocks.Release(_a);
            Assert.AreEqual(0f, Shown(First), "the first's value gone");
            Assert.AreEqual(2f, Shown(Second), "the second's kept");
            AnimeBlocks.Write(_b, _renderer, 0, new AnimeBlockValues());
            Assert.IsFalse(_renderer.HasPropertyBlock(), "writing nothing lets go: no block");
        }

        [Test]
        public void AnOwnersNewValues_ReplaceItsOld()
        {
            AnimeBlocks.Write(_a, _renderer, 0, Values(First, 1f));
            AnimeBlocks.Write(_a, _renderer, 0, Values(First, 3f));
            Assert.AreEqual(3f, Shown(First));
        }

        [Test]
        public void Slots_StayApart()
        {
            AnimeBlocks.Write(_a, _renderer, 0, Values(First, 1f));
            AnimeBlocks.Write(_b, _renderer, 1, Values(Second, 2f));
            Assert.AreEqual(1f, Shown(First, 0));
            Assert.AreEqual(0f, Shown(Second, 0), "slot 0 has only its own");
            Assert.AreEqual(2f, Shown(Second, 1));
        }

        [Test]
        public void TheSameValues_ChangedAndWrittenAgain_Show()
        {
            AnimeBlockValues values = Values(First, 1f);
            AnimeBlocks.Write(_a, _renderer, 0, values);
            values.SetFloat(First, 5f);
            AnimeBlocks.Write(_a, _renderer, 0, values);
            Assert.AreEqual(5f, Shown(First));
        }

        [Test]
        public void ADestroyedRenderersSlot_IsDropped_AsAnOwnerLetsGo()
        {
            var slots = (System.Collections.IDictionary)typeof(AnimeBlocks).GetField("Slots", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            var gone = new GameObject("Gone").AddComponent<MeshRenderer>();
            gone.sharedMaterials = new Material[1];
            AnimeBlocks.Write(_b, gone, 0, Values(First, 1f));
            int held = slots.Count;
            Object.DestroyImmediate(gone.gameObject);
            AnimeBlocks.Release(_a);
            Assert.AreEqual(held - 1, slots.Count, "the destroyed renderer's slot dropped");
        }
    }
}
