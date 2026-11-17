using AnimeShaders.EditorTools;
using NUnit.Framework;
using UnityEngine;
using Ids = AnimeShaders.AnimeShaderIds.MaterialProperties;

namespace AnimeShaders.Tests
{
    /**
     * <summary>The hull line: drawn only by its pass; around a sphere (radius 1) in perspective its thickness in pixels
     * follows the width and the director's multiplier, holds as the camera backs away inside the distance clamp and
     * thins past it; on a hard-edged cube, baked normals close the corners whatever the tangents' handedness; a flat
     * two-sided sheet seen face-on has a line past its edges with Line On Open Edges, from the front and the back, none
     * without it, and the line never covers the sheet even pulled toward the camera.</summary>
     */
    public class LineTests
    {
        #region Private Fields

        private const int Size = 512;
        private const float FieldOfView = 30f;
        private const float Width = 40f;

        private static readonly Color LineColour = new(1f, 0f, 1f);

        private LitStage _stage;
        private Material _material;
        private MeshRenderer _body;
        private DirectorGlobals _director;

        #endregion

        #region Helpers

        private static bool IsLine(Color pixel)
        {
            return FrameReads.Covered(pixel) && FrameReads.Near(pixel, LineColour, 0.02f);
        }

        /** <summary>The line's thickness in pixels on the middle row, at the body's left edge, seen from <paramref name="distance" />.</summary> */
        private int ThicknessFrom(float distance)
        {
            _stage.Perspective(FieldOfView, distance);
            return FrameReads.RunFromLeft(_stage.Render(), Size / 2, IsLine);
        }

        /** <summary>Whether the line covers the four points just outside the corners of a unit cube seen face on.</summary> */
        private bool[] CubeCornersCovered()
        {
            Color[] frame = _stage.Render();
            const float justOutside = 0.53f;
            var covered = new bool[4];
            int index = 0;
            foreach (float x in new[] { -justOutside, justOutside })
            {
                foreach (float y in new[] { -justOutside, justOutside })
                    covered[index++] = IsLine(FrameReads.At(frame, _stage.Column(x), _stage.Column(y)));
            }
            return covered;
        }

        #endregion

        [SetUp]
        public void SetUp()
        {
            _director = new DirectorGlobals();
            _stage = new LitStage(Size);
            _material = TestMaterials.Character(Color.green, Color.gray);
            AnimeMaterials.SetLine(_material, true);
            TestMaterials.SetLinear(_material, Ids.OutlineColor, LineColour);
            _material.SetFloat(Ids.OutlineWidth, Width);
            _material.SetFloat(Ids.OutlineZOffset, 0f);
            _material.SetFloat(Ids.OutlineDistanceMin, 1f);
            _material.SetFloat(Ids.OutlineDistanceMax, 20f);
            _body = _stage.Add(TestMeshes.Sphere(), _material, Vector3.zero, Quaternion.identity, Vector3.one * 2f);
        }

        [TearDown]
        public void TearDown()
        {
            _stage.Dispose();
            Object.DestroyImmediate(_material);
            _director.Dispose();
        }

        [Test]
        public void Line_DrawsOnlyWithItsPass()
        {
            Assert.Greater(ThicknessFrom(6f), 8);
            AnimeMaterials.SetLine(_material, false);
            _stage.Perspective(FieldOfView, 6f);
            Assert.AreEqual(0, FrameReads.Count(_stage.Render(), IsLine));
        }

        [Test]
        public void Thickness_FollowsTheWidth()
        {
            int single = ThicknessFrom(6f);
            _material.SetFloat(Ids.OutlineWidth, 2f * Width);
            int twice = ThicknessFrom(6f);
            Assert.Greater(single, 8);
            Assert.That(twice, Is.InRange(2 * single - 2, 2 * single + 2), $"{single} then {twice}");
        }

        [Test]
        public void Thickness_HoldsOnScreen_InsideTheDistanceClamp()
        {
            int near = ThicknessFrom(8f);
            int far = ThicknessFrom(16f);
            Assert.Greater(near, 8);
            Assert.That(far, Is.InRange(near - 2, near + 2), $"near {near}, far {far}");
        }

        [Test]
        public void Thickness_ThinsWithDistance_PastTheClamp()
        {
            _material.SetFloat(Ids.OutlineDistanceMax, 8f);
            int atClamp = ThicknessFrom(8f);
            int twiceAsFar = ThicknessFrom(16f);
            Assert.Greater(atClamp, 8);
            Assert.That(twiceAsFar, Is.InRange(atClamp / 2 - 1, atClamp / 2 + 1), $"{atClamp} then {twiceAsFar}");
        }

        [Test]
        public void DirectorMultiplier_ScalesTheThickness()
        {
            int plain = ThicknessFrom(6f);
            Shader.SetGlobalVector(AnimeShaderIds.Director.Scalars, new Vector4(0f, 0f, 1f, 0f));
            int doubled = ThicknessFrom(6f);
            Assert.Greater(plain, 8);
            Assert.That(doubled, Is.InRange(2 * plain - 2, 2 * plain + 2), $"{plain} then {doubled}");
        }

        [Test]
        public void BakedNormals_OnASmoothMesh_DrawTheSameLine()
        {
            int plain = ThicknessFrom(6f);
            Mesh baked = Object.Instantiate(TestMeshes.Sphere());
            try
            {
                Assert.IsTrue(OutlineNormalBaker.Bake(baked));
                _body.GetComponent<MeshFilter>().sharedMesh = baked;
                AnimeMaterials.SetFeature(_material, AnimeShaderIds.Keywords.OutlineBakedName,
                    AnimeShaderIds.Switches.OutlineBakedNormals, true);
                Assert.That(ThicknessFrom(6f), Is.InRange(plain - 1, plain + 1));
            }
            finally
            {
                Object.DestroyImmediate(baked);
            }
        }

        [TestCase(1f)]
        [TestCase(-1f)]
        public void BakedNormals_CloseAHardCubesCorners_WithEitherTangentHandedness(float tangentSign)
        {
            Mesh cube = TestMeshes.HardCube(tangentSign: tangentSign);
            try
            {
                Assert.IsTrue(OutlineNormalBaker.Bake(cube));
                _body.GetComponent<MeshFilter>().sharedMesh = cube;
                _body.transform.localScale = Vector3.one;
                CollectionAssert.AreEqual(new[] { false, false, false, false }, CubeCornersCovered(),
                    "split normals leave the corners open");
                AnimeMaterials.SetFeature(_material, AnimeShaderIds.Keywords.OutlineBakedName,
                    AnimeShaderIds.Switches.OutlineBakedNormals, true);
                CollectionAssert.AreEqual(new[] { true, true, true, true }, CubeCornersCovered(),
                    "baked normals close every corner");
            }
            finally
            {
                Object.DestroyImmediate(cube);
            }
        }

        /** <summary>A flat two-sided sheet (a unit quad, baked), drawn with Line On Open Edges at <paramref name="openEdges" />.</summary> */
        private Mesh Sheet(float openEdges)
        {
            Mesh sheet = TestMeshes.Quad();
            Assert.IsTrue(OutlineNormalBaker.Bake(sheet));
            _body.GetComponent<MeshFilter>().sharedMesh = sheet;
            _body.transform.localScale = Vector3.one;
            _material.SetFloat(AnimeShaderIds.Switches.Cull, (float)UnityEngine.Rendering.CullMode.Off);
            AnimeMaterials.SetFeature(_material, AnimeShaderIds.Keywords.OutlineBakedName, AnimeShaderIds.Switches.OutlineBakedNormals, true);
            AnimeMaterials.SetOpenEdges(_material, openEdges);
            return sheet;
        }

        /** <summary>How many of the four points just past the sheet's edge midpoints the line covers.</summary> */
        private int EdgesLined()
        {
            Color[] frame = _stage.Render();
            const float justOutside = 0.53f;
            int lined = 0;
            foreach ((float x, float y) in new[] { (-justOutside, 0f), (justOutside, 0f), (0f, -justOutside), (0f, justOutside) })
            {
                if (IsLine(FrameReads.At(frame, _stage.Column(x), _stage.Column(y))))
                    lined++;
            }
            return lined;
        }

        [Test]
        public void OpenEdges_LineAFlatSheetsEdges_FromTheFrontAndTheBack()
        {
            Mesh sheet = Sheet(1f);
            try
            {
                Assert.AreEqual(4, EdgesLined(), "facing the camera");
                _body.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                Assert.AreEqual(4, EdgesLined(), "turned away");
            }
            finally
            {
                Object.DestroyImmediate(sheet);
            }
        }

        [Test]
        public void WithoutOpenEdges_AFlatSheetSeenFaceOnHasNoLine()
        {
            Mesh sheet = Sheet(0f);
            try
            {
                Assert.AreEqual(0, EdgesLined(), "an inverted hull has nothing past a flat sheet's edge");
            }
            finally
            {
                Object.DestroyImmediate(sheet);
            }
        }

        [Test]
        public void OpenEdges_TheLineNeverCoversTheSheet_EvenPulledForward()
        {
            Mesh sheet = Sheet(1f);
            try
            {
                float width = Width * 0.001f * 2f * _stage.Camera.orthographicSize;
                _material.SetFloat(Ids.OutlineZOffset, -0.5f * width);
                Color[] frame = _stage.Render();
                int covered = 0;
                for (float x = -0.45f; x <= 0.45f; x += 0.05f)
                {
                    for (float y = -0.45f; y <= 0.45f; y += 0.05f)
                    {
                        if (IsLine(FrameReads.At(frame, _stage.Column(x), _stage.Column(y))))
                            covered++;
                    }
                }
                Assert.AreEqual(0, covered, "the hull's part toward the camera turned behind the sheet");
                Assert.AreEqual(4, EdgesLined(), "and still past its edges");
            }
            finally
            {
                Object.DestroyImmediate(sheet);
            }
        }
    }
}
