using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace AnimeShaders.Tests
{
    /**
     * <summary>A small lit scene for render tests: a camera looking along +Z from z = -5, one white directional light,
     * and the meshes a test adds, drawn by the active render pipeline into a linear half-float target and read back,
     * so a pixel holds what the shader wrote. The world's globals (clouds, haze) are held neutral while it stands, whatever
     * scene the Editor has open; a test publishes its own.</summary>
     */
    public sealed class LitStage : IDisposable
    {
        #region Private Fields

        private const float CameraDistance = 5f;

        private readonly Scene _scene;
        private readonly RenderTexture _target;
        private readonly Texture2D _frame;
        private readonly List<Object> _owned = new();
        private readonly WorldGlobals _world;

        #endregion

        #region Properties

        /** <summary>The stage's camera: orthographic, half-height 1.25, until a test changes it.</summary> */
        public Camera Camera { get; }

        /** <summary>The stage's directional light: white, intensity 1, no shadows, until a test changes it.</summary> */
        public Light Light { get; }

        /** <summary>The width and height of the target in pixels.</summary> */
        public int Size { get; }

        #endregion

        #region Public API

        /** <summary>A stage rendering <paramref name="size" /> by <paramref name="size" /> pixels.</summary> */
        public LitStage(int size = 128)
        {
            Size = size;
            _world = new WorldGlobals();
            _scene = EditorSceneManager.NewPreviewScene();
            Camera = Place(new GameObject("LitStageCamera")).AddComponent<Camera>();
            Camera.scene = _scene;
            Camera.clearFlags = CameraClearFlags.SolidColor;
            Camera.backgroundColor = Color.clear;
            Camera.orthographic = true;
            Camera.orthographicSize = 1.25f;
            Camera.fieldOfView = 30f;
            Camera.nearClipPlane = 0.1f;
            Camera.farClipPlane = 40f;
            Camera.allowMSAA = false;
            Camera.transform.SetPositionAndRotation(new Vector3(0f, 0f, -CameraDistance), Quaternion.identity);

            Light = Place(new GameObject("LitStageLight")).AddComponent<Light>();
            Light.type = LightType.Directional;
            Light.color = Color.white;
            Light.intensity = 1f;
            Light.shadows = LightShadows.None;
            LightFrom(Vector3.back);

            _target = new RenderTexture(size, size, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            _frame = new Texture2D(size, size, TextureFormat.RGBAHalf, false, true);
            Camera.targetTexture = _target;
        }

        /** <summary>Points the light so it shines from <paramref name="towardLight" /> (a world direction toward the light).</summary> */
        public void LightFrom(Vector3 towardLight)
        {
            Light.transform.rotation = Quaternion.LookRotation(-towardLight.normalized);
        }

        /** <summary>Switches the camera to a perspective view (vertical field of view in degrees) from <paramref name="distance" /> along -Z.</summary> */
        public void Perspective(float fieldOfView, float distance)
        {
            Camera.orthographic = false;
            Camera.fieldOfView = fieldOfView;
            Camera.transform.position = new Vector3(0f, 0f, -distance);
        }

        /** <summary>Adds <paramref name="mesh" /> drawn with <paramref name="material" />; the stage destroys the object, never the mesh or material.</summary> */
        public MeshRenderer Add(Mesh mesh, Material material, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            var body = Place(new GameObject(mesh.name));
            body.transform.SetPositionAndRotation(position, rotation);
            body.transform.localScale = scale;
            body.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = body.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            return renderer;
        }

        /**
         * <summary>Renders and reads back the frame, bottom row first (index y * Size + x). Shaders compile before
         * drawing: with the Editor's asynchronous compilation a variant's first draw is skipped.</summary>
         */
        public Color[] Render()
        {
            bool wasAsync = ShaderUtil.allowAsyncCompilation;
            ShaderUtil.allowAsyncCompilation = false;
            try
            {
                Camera.Render();
            }
            finally
            {
                ShaderUtil.allowAsyncCompilation = wasAsync;
            }
            var previous = RenderTexture.active;
            RenderTexture.active = _target;
            _frame.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
            _frame.Apply(false);
            RenderTexture.active = previous;
            return _frame.GetPixels();
        }

        /** <summary>The pixel column a world x falls in, for the orthographic camera.</summary> */
        public int Column(float worldX)
        {
            float halfWidth = Camera.orthographicSize;
            return Mathf.FloorToInt((worldX + halfWidth) / (2f * halfWidth) * Size);
        }

        /** <summary>Tears the stage down; the camera lets go of its target first so it never draws over the game view.</summary> */
        public void Dispose()
        {
            if (Camera != null)
                Camera.targetTexture = null;
            foreach (var owned in _owned)
            {
                if (owned != null)
                    Object.DestroyImmediate(owned);
            }
            Object.DestroyImmediate(_target);
            Object.DestroyImmediate(_frame);
            EditorSceneManager.ClosePreviewScene(_scene);
            _world.Dispose();
        }

        #endregion

        #region Helpers

        private GameObject Place(GameObject made)
        {
            SceneManager.MoveGameObjectToScene(made, _scene);
            _owned.Add(made);
            return made;
        }

        #endregion
    }
}
