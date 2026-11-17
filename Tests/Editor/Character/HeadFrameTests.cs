using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
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
     * scene's; an ordinary game camera does; every listed renderer's every slot; one frame switched off leaves another
     * writing; switched back on it writes where the head is now.</summary>
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
        private GameObject _extra;
        private Camera _gameCamera;
        private RenderTexture _gameTarget;

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

        /** <summary>An ordinary camera (no scene of its own) in front of the rig, rendering into a small target.</summary> */
        private Camera GameCamera()
        {
            _gameTarget = new RenderTexture(16, 16, 16);
            _gameCamera = new GameObject("HeadFrameGameCamera").AddComponent<Camera>();
            _gameCamera.transform.SetPositionAndRotation(new Vector3(0f, 0f, -5f), Quaternion.identity);
            _gameCamera.targetTexture = _gameTarget;
            return _gameCamera;
        }

        private static void Render(Camera camera)
        {
            bool wasAsync = ShaderUtil.allowAsyncCompilation;
            ShaderUtil.allowAsyncCompilation = false;
            try
            {
                camera.Render();
            }
            finally
            {
                ShaderUtil.allowAsyncCompilation = wasAsync;
            }
        }

        private void SetOffset(Vector3 offset)
        {
            var serialized = new SerializedObject(_frame);
            serialized.FindProperty("centreOffset").vector3Value = offset;
            serialized.ApplyModifiedPropertiesWithoutUndo();
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
            if (_gameCamera != null)
                _gameCamera.targetTexture = null;
            Object.DestroyImmediate(_gameTarget);
            if (_gameCamera != null)
                Object.DestroyImmediate(_gameCamera.gameObject);
            Object.DestroyImmediate(_extra);
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

        [Test]
        public void TheHeadTipped_CarriesEveryAxis_AndTheOffsetAlongItsOwnAxes()
        {
            var offset = new Vector3(0.02f, 0.1f, 0.03f);
            SetOffset(offset);
            Quaternion turn = Quaternion.Euler(30f, 20f, 0f);
            _head.rotation = turn * _head.rotation;
            Tick();
            AssertDirection(turn * Vector3.forward, Shown(AnimeShaderIds.Head.Forward), "forward");
            AssertDirection(turn * Vector3.up, Shown(AnimeShaderIds.Head.Up), "up");
            AssertDirection(turn * Vector3.right, Shown(AnimeShaderIds.Head.Right), "right");
            AssertDirection(_head.position + turn * offset, Shown(AnimeShaderIds.Head.Centre), "the centre, offset along the head's right, up and forward");
        }

        [Test]
        public void EveryListedRenderer_EverySlot_IsWritten()
        {
            _extra = new GameObject("Hair");
            _extra.transform.SetParent(_root.transform, false);
            var hair = _extra.AddComponent<MeshRenderer>();
            hair.sharedMaterials = new Material[2];
            var serialized = new SerializedObject(_frame);
            SerializedProperty listed = serialized.FindProperty("renderers");
            listed.arraySize = 3;
            listed.GetArrayElementAtIndex(1).objectReferenceValue = null;
            listed.GetArrayElementAtIndex(2).objectReferenceValue = hair;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Tick();
            var block = new MaterialPropertyBlock();
            for (int slot = 0; slot < 2; slot++)
            {
                hair.GetPropertyBlock(block, slot);
                AssertDirection(Vector3.forward, block.GetVector(AnimeShaderIds.Head.Forward), $"the second renderer's slot {slot}");
            }
            AssertDirection(Vector3.forward, Shown(AnimeShaderIds.Head.Forward), "the first renderer, past a missing entry");
        }

        [Test]
        public void AnOrdinaryGameCamera_WritesIt()
        {
            Camera camera = GameCamera();
            _head.rotation = Quaternion.Euler(0f, 30f, 0f) * _head.rotation;
            Animate();
            Render(camera);
            AssertDirection(Quaternion.Euler(0f, 30f, 0f) * Vector3.forward, Shown(AnimeShaderIds.Head.Forward), "a camera with no scene of its own draws every scene");
        }

        [Test]
        public void OneFrameSwitchedOff_LeavesAnotherWriting()
        {
            Camera camera = GameCamera();
            _extra = Object.Instantiate(_root);
            var other = _extra.GetComponent<AnimeHeadFrame>();
            var otherSkinned = _extra.GetComponentInChildren<SkinnedMeshRenderer>();
            Transform otherHead = _extra.transform.Find("Head");
            _frame.enabled = false;
            otherHead.rotation = Quaternion.Euler(0f, 30f, 0f) * otherHead.rotation;
            typeof(AnimeHeadFrame).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(other, null);
            Render(camera);
            var block = new MaterialPropertyBlock();
            otherSkinned.GetPropertyBlock(block, 0);
            AssertDirection(Quaternion.Euler(0f, 30f, 0f) * Vector3.forward, block.GetVector(AnimeShaderIds.Head.Forward), "the one left on still writes");
        }

        [Test]
        public void SwitchedBackOn_ItWritesWhereTheHeadIsNow()
        {
            _frame.enabled = false;
            _head.rotation = Quaternion.Euler(0f, 45f, 0f) * _head.rotation;
            _frame.enabled = true;
            AssertDirection(Quaternion.Euler(0f, 45f, 0f) * Vector3.forward, Shown(AnimeShaderIds.Head.Forward), "written as it wakes");
        }

        [Test]
        public void Reset_ListsTheChildrenWhoseMaterialsReadTheHead()
        {
            var face = new Material(Shader.Find(AnimeShaderIds.FaceShader));
            var plain = new Material(Shader.Find("Anime/Character"));
            try
            {
                _skinned.sharedMaterials = new[] { plain, face };
                _extra = new GameObject("Body");
                _extra.transform.SetParent(_root.transform, false);
                _extra.AddComponent<MeshRenderer>().sharedMaterial = plain;
                typeof(AnimeHeadFrame).GetMethod("Reset", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_frame, null);
                CollectionAssert.AreEqual(new Renderer[] { _skinned }, _frame.Renderers, "the face's renderer, not the body's");
            }
            finally
            {
                Object.DestroyImmediate(face);
                Object.DestroyImmediate(plain);
            }
        }

        [Test]
        public void NoHead_TriesOnceMore_ThenWarnsOnce()
        {
            _extra = new GameObject("Headless");
            var frame = _extra.AddComponent<AnimeHeadFrame>();
            Assert.IsFalse(frame.Ready, "no head found as it wakes");
            LogAssert.Expect(LogType.Warning, "Headless: no head to frame.");
            MethodInfo late = typeof(AnimeHeadFrame).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
            late.Invoke(frame, null);
            late.Invoke(frame, null);
            LogAssert.NoUnexpectedReceived();
            Assert.IsFalse(frame.Ready);
        }
    }
}
