using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace AnimeShaders.Tests
{
    /**
     * <summary>The head frame on a made rig whose head bone's axes and mesh's axes both differ from the model's (the
     * bone turned so its own forward points down, the mesh turned as Blender's imports are, its own +Z pointing up): it
     * writes the model's front, right and up as the head carries them, turning with the head; the
     * centre offset along the head's own up; nothing rewritten while the head is still, rewritten once it moves; let go
     * on disable; the bind axes false for a mesh not skinned to the bone. Measured each frame, the frame is written only
     * as a camera that can see the renderer draws: not one looking away, not one skipping its layer, not another
     * scene's.</summary>
     */
    public class HeadFrameTests
    {
        #region Private Fields

        private const float Close = 1e-3f;
        private const int HiddenLayer = 31;
        private static readonly Quaternion BoneAtRest = Quaternion.Euler(90f, 0f, 0f);
        private static readonly Quaternion MeshTurned = Quaternion.Euler(270f, 0f, 0f);

        private GameObject _root;
        private Transform _head;
        private SkinnedMeshRenderer _skinned;
        private Mesh _mesh;
        private AnimeHeadFrame _frame;
        private LitStage _stage;
        private LitStage _otherStage;

        #endregion

        #region Helpers

        private Vector4 Shown(int property)
        {
            var block = new MaterialPropertyBlock();
            _skinned.GetPropertyBlock(block, 0);
            return block.GetVector(property);
        }

        private void Tick()
        {
            _frame.Refresh();
        }

        /** <summary>A frame's own pass: the head measured, nothing written yet.</summary> */
        private void Animate()
        {
            typeof(AnimeHeadFrame).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_frame, null);
        }

        /** <summary>The rig on a stage whose camera sees it, the head then turned and measured but not yet written.</summary> */
        private void TurnedOnStage()
        {
            _stage = new LitStage();
            SceneManager.MoveGameObjectToScene(_root, _stage.Camera.scene);
            _head.rotation = Quaternion.Euler(0f, 30f, 0f) * _head.rotation;
            Animate();
            AssertDirection(Vector3.forward, Shown(AnimeShaderIds.Head.Forward), "measured, not yet drawn: as before");
        }

        private static void AssertDirection(Vector3 expected, Vector4 actual, string message)
        {
            Assert.Less(Vector3.Distance(expected, actual), Close, $"{message}: {actual}");
        }

        #endregion

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Rig");
            _head = new GameObject("Head").transform;
            _head.SetParent(_root.transform, false);
            _head.SetLocalPositionAndRotation(new Vector3(0f, 1.5f, 0f), BoneAtRest);
            var body = new GameObject("Face");
            body.transform.SetParent(_root.transform, false);
            body.transform.localRotation = MeshTurned;
            _skinned = body.AddComponent<SkinnedMeshRenderer>();
            _mesh = new Mesh
            {
                vertices = new[] { new Vector3(0f, 1.5f, 0f), new Vector3(0.1f, 1.5f, 0f), new Vector3(0f, 1.6f, 0f) },
                triangles = new[] { 0, 1, 2 },
                boneWeights = new[] { new BoneWeight { weight0 = 1f }, new BoneWeight { weight0 = 1f }, new BoneWeight { weight0 = 1f } },
                bindposes = new[] { _head.worldToLocalMatrix * body.transform.localToWorldMatrix },
            };
            _skinned.sharedMesh = _mesh;
            _skinned.bones = new[] { _head };
            _skinned.sharedMaterials = new Material[1];

            _frame = _root.AddComponent<AnimeHeadFrame>();
            _frame.enabled = false;
            var serialized = new SerializedObject(_frame);
            serialized.FindProperty("head").objectReferenceValue = _head;
            serialized.FindProperty("centreOffset").vector3Value = new Vector3(0f, 0.1f, 0f);
            SerializedProperty listed = serialized.FindProperty("renderers");
            listed.arraySize = 1;
            listed.GetArrayElementAtIndex(0).objectReferenceValue = _skinned;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            _frame.enabled = true;
        }

        [TearDown]
        public void TearDown()
        {
            _stage?.Dispose();
            _otherStage?.Dispose();
            Object.DestroyImmediate(_root);
            Object.DestroyImmediate(_mesh);
        }

        [Test]
        public void AtRest_TheModelsOwnAxes_AreWritten_WhateverTheBonesAxes()
        {
            AssertDirection(Vector3.forward, Shown(AnimeShaderIds.Head.Forward), "forward is the model's front, not the bone's");
            AssertDirection(Vector3.up, Shown(AnimeShaderIds.Head.Up), "up");
            AssertDirection(Vector3.right, Shown(AnimeShaderIds.Head.Right), "right");
            AssertDirection(new Vector3(0f, 1.6f, 0f), Shown(AnimeShaderIds.Head.Centre), "the centre, offset along the head's up");
        }

        [Test]
        public void TheHeadTurning_TurnsTheFrame()
        {
            _head.rotation = Quaternion.Euler(0f, 90f, 0f) * _head.rotation;
            Tick();
            AssertDirection(Vector3.right, Shown(AnimeShaderIds.Head.Forward), "turned a quarter to the right");
            AssertDirection(Vector3.back, Shown(AnimeShaderIds.Head.Right), "its right turned with it");
        }

        [Test]
        public void WhileStill_NothingIsRewritten_AndMovingRewrites()
        {
            var block = new MaterialPropertyBlock();
            block.SetVector(AnimeShaderIds.Head.Forward, new Vector4(7f, 7f, 7f, 0f));
            _skinned.SetPropertyBlock(block, 0);
            Tick();
            Assert.AreEqual(new Vector4(7f, 7f, 7f, 0f), Shown(AnimeShaderIds.Head.Forward), "still: left as it was");
            _head.rotation = Quaternion.Euler(0f, 30f, 0f) * _head.rotation;
            Tick();
            AssertDirection(Quaternion.Euler(0f, 30f, 0f) * Vector3.forward, Shown(AnimeShaderIds.Head.Forward), "moved: written again");
        }

        [Test]
        public void Disabled_LetsGoOfTheBlock()
        {
            Assert.IsTrue(_skinned.HasPropertyBlock());
            _frame.enabled = false;
            Assert.IsFalse(_skinned.HasPropertyBlock());
        }

        [Test]
        public void BindAxes_FalseForAMeshNotSkinnedToTheBone()
        {
            var other = new GameObject("Other").transform;
            try
            {
                Assert.IsFalse(AnimeHeadFrame.BindAxes(_skinned, other, _root.transform, out _, out _));
                Assert.IsTrue(AnimeHeadFrame.BindAxes(_skinned, _head, _root.transform, out Vector3 forward, out _));
                AssertDirection(Quaternion.Inverse(BoneAtRest) * Vector3.forward, forward, "the model's front in the bone's own space");
            }
            finally
            {
                Object.DestroyImmediate(other.gameObject);
            }
        }

        [Test]
        public void Measured_IsWrittenAsACameraThatSeesItDraws_NotOneLookingAway()
        {
            TurnedOnStage();
            _stage.Camera.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            _stage.Render();
            AssertDirection(Vector3.forward, Shown(AnimeShaderIds.Head.Forward), "a camera looking away: not written");
            _stage.Camera.transform.rotation = Quaternion.identity;
            _stage.Render();
            AssertDirection(Quaternion.Euler(0f, 30f, 0f) * Vector3.forward, Shown(AnimeShaderIds.Head.Forward), "a camera that sees it: written");
        }

        [Test]
        public void ACameraSkippingItsLayer_DoesNotWriteIt()
        {
            TurnedOnStage();
            _skinned.gameObject.layer = HiddenLayer;
            _stage.Camera.cullingMask = ~(1 << HiddenLayer);
            _stage.Render();
            AssertDirection(Vector3.forward, Shown(AnimeShaderIds.Head.Forward), "its layer not drawn: not written");
            _stage.Camera.cullingMask = ~0;
            _stage.Render();
            AssertDirection(Quaternion.Euler(0f, 30f, 0f) * Vector3.forward, Shown(AnimeShaderIds.Head.Forward), "its layer drawn: written");
        }

        [Test]
        public void AnotherScenesCamera_DoesNotWriteIt()
        {
            TurnedOnStage();
            _otherStage = new LitStage();
            _otherStage.Render();
            AssertDirection(Vector3.forward, Shown(AnimeShaderIds.Head.Forward), "a camera drawing another scene: not written");
            _stage.Render();
            AssertDirection(Quaternion.Euler(0f, 30f, 0f) * Vector3.forward, Shown(AnimeShaderIds.Head.Forward), "its own scene's camera: written");
        }
    }
}
