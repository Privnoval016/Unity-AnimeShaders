using NUnit.Framework;
using UnityEngine;
using Ids = AnimeShaders.AnimeShaderIds.MaterialProperties;

namespace AnimeShaders.Tests
{
    /** <summary>Anime/World's base map in world XZ: the same colour at the same place whatever the mesh's own UVs.</summary> */
    public class WorldShaderTests
    {
        #region Private Fields

        private LitStage _stage;
        private Material _material;
        private Texture2D _checker;
        private Mesh _floor;
        private MeshRenderer _renderer;

        #endregion

        #region Helpers

        private Color[] WithUvsScaledBy(float scale)
        {
            var uvs = new[] { new Vector2(0f, 0f), new Vector2(0f, scale), new Vector2(scale, scale), new Vector2(scale, 0f) };
            _floor.SetUVs(0, uvs);
            return _stage.Render();
        }

        private static int Differing(Color[] a, Color[] b)
        {
            int count = 0;
            for (int i = 0; i < a.Length; i++)
            {
                if (!FrameReads.Near(a[i], b[i], 0.01f))
                    count++;
            }
            return count;
        }

        #endregion

        [SetUp]
        public void SetUp()
        {
            _stage = new LitStage(64);
            _stage.Camera.transform.SetPositionAndRotation(new Vector3(0f, 10f, 0f), Quaternion.Euler(90f, 0f, 0f));
            _stage.Camera.orthographicSize = 2f;
            _stage.LightFrom(Vector3.up);
            _checker = new Texture2D(2, 2, TextureFormat.RGBA32, false, true) { filterMode = FilterMode.Point };
            _checker.SetPixels(new[] { Color.red, Color.green, Color.blue, Color.yellow });
            _checker.Apply();
            _material = TestMaterials.WorldLit("Anime/World");
            _material.SetTexture(Ids.BaseMap, _checker);
            _material.SetTextureScale(Ids.BaseMap, new Vector2(0.5f, 0.5f));
            _floor = TestMeshes.Quad();
            _renderer = _stage.Add(_floor, _material, Vector3.zero, Quaternion.Euler(90f, 0f, 0f), Vector3.one * 8f);
        }

        [TearDown]
        public void TearDown()
        {
            _stage.Dispose();
            Object.DestroyImmediate(_material);
            Object.DestroyImmediate(_checker);
            Object.DestroyImmediate(_floor);
        }

        [Test]
        public void InWorldXZ_TheMeshUvsDoNotMatter_ByUvTheyDo()
        {
            Color[] byUvOnce = WithUvsScaledBy(1f);
            Color[] byUvTwice = WithUvsScaledBy(2f);
            Assert.Greater(Differing(byUvOnce, byUvTwice), 0, "by UV, scaling the UVs changes the picture");

            AnimeMaterials.SetFeature(_material, AnimeShaderIds.Keywords.WorldUVName, AnimeShaderIds.Switches.WorldUV, true);
            Color[] worldOnce = WithUvsScaledBy(1f);
            Color[] worldTwice = WithUvsScaledBy(2f);
            Assert.AreEqual(0, Differing(worldOnce, worldTwice), "in world XZ the UVs are ignored");
        }
    }
}
