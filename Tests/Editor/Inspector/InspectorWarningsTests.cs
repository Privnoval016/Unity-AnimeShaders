using System.Collections.Generic;
using AnimeShaders.EditorTools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AnimeShaders.Tests
{
    /** <summary>The depth-edge rim's warning: it reads every renderer of the pipeline assets it is given for an active Anime Rim feature.</summary> */
    public class InspectorWarningsTests
    {
        #region Private Fields

        private readonly List<Object> _made = new();

        #endregion

        [TearDown]
        public void TearDown()
        {
            foreach (Object made in _made)
            {
                if (made != null)
                    Object.DestroyImmediate(made);
            }
            _made.Clear();
        }

        private UniversalRenderPipelineAsset Pipeline(bool withRim, bool rimActive = true)
        {
            var data = ScriptableObject.CreateInstance<UniversalRendererData>();
            _made.Add(data);
            if (withRim)
            {
                var rim = ScriptableObject.CreateInstance<AnimeRimFeature>();
                rim.SetActive(rimActive);
                _made.Add(rim);
                data.rendererFeatures.Add(rim);
            }
            UniversalRenderPipelineAsset pipeline = UniversalRenderPipelineAsset.Create(data);
            _made.Add(pipeline);
            return pipeline;
        }

        [Test]
        public void TheRimIsDrawn_OnlyWhenSomeRendererHasAnActiveRimFeature()
        {
            Assert.IsFalse(InspectorWarnings.AnyDrawsTheRim(new RenderPipelineAsset[] { Pipeline(false) }), "no feature");
            Assert.IsFalse(InspectorWarnings.AnyDrawsTheRim(new RenderPipelineAsset[] { Pipeline(true, rimActive: false) }), "a feature switched off");
            Assert.IsTrue(InspectorWarnings.AnyDrawsTheRim(new RenderPipelineAsset[] { Pipeline(false), Pipeline(true) }), "one renderer of several is enough");
        }
    }
}
