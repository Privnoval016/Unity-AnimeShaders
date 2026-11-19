using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Ids = AnimeShaders.AnimeShaderIds.MaterialProperties;

namespace AnimeShaders.EditorTools
{
    /**
     * <summary>
     * The warnings the material inspector shows under the setting they are about: a slot holding the wrong kind of the
     * pack's own maps (detail, stones, face), cloth lines with no offset, a depth-edge rim with nothing to draw it.
     * </summary>
     */
    internal static class InspectorWarnings
    {
        #region Private Fields

        private static readonly string[] DetailSlots = { Ids.FieldDetailName, Ids.CircleDetailName };

        private static readonly Dictionary<string, string[]> StoneSlots = new()
        {
            [Ids.FieldStonesName] = new[] { Ids.FieldStoneShadeName, Ids.FieldStoneMixName },
            [Ids.CircleStonesName] = new[] { Ids.CircleStoneShadeName, Ids.CircleStoneMixName },
        };

        #endregion

        #region Public API

        /** <summary>Every warning about <paramref name="property" /> of <paramref name="material" />, under its row.</summary> */
        public static void Under(MaterialProperty property, Material material)
        {
            WarnIfNotPacked(property);
            WarnAboutStones(property);
            WarnAboutFaceMap(property);
            WarnAboutOpenEdges(property, material);
            WarnAboutDepthRim(property);
        }

        /** <summary>Whether any renderer of <paramref name="pipelines" /> has an active <see cref="AnimeRimFeature" />.</summary> */
        public static bool AnyDrawsTheRim(IEnumerable<RenderPipelineAsset> pipelines)
        {
            foreach (RenderPipelineAsset pipeline in pipelines)
            {
                if (pipeline is not UniversalRenderPipelineAsset universal)
                    continue;
                foreach (ScriptableRendererData renderer in universal.rendererDataList)
                {
                    if (renderer == null)
                        continue;
                    foreach (ScriptableRendererFeature feature in renderer.rendererFeatures)
                    {
                        if (feature is AnimeRimFeature && feature.isActive)
                            return true;
                    }
                }
            }
            return false;
        }

        /** <summary>The graphics setting's pipeline asset and each quality level's.</summary> */
        public static List<RenderPipelineAsset> PipelinesInUse()
        {
            var pipelines = new List<RenderPipelineAsset>();
            if (GraphicsSettings.defaultRenderPipeline != null)
                pipelines.Add(GraphicsSettings.defaultRenderPipeline);
            for (int i = 0; i < QualitySettings.count; i++)
            {
                RenderPipelineAsset level = QualitySettings.GetRenderPipelineAssetAt(i);
                if (level != null && !pipelines.Contains(level))
                    pipelines.Add(level);
            }
            return pipelines;
        }

        #endregion

        #region Helpers

        /** <summary>Under Use Depth Edge Rim: a warning when it is on and no renderer of the pipeline assets in use has an active Anime Rim feature.</summary> */
        private static void WarnAboutDepthRim(MaterialProperty property)
        {
            if (property.name != Ids.RimDepthName || property.floatValue < 0.5f || AnyDrawsTheRim(PipelinesInUse()))
                return;
            EditorGUILayout.HelpBox("No renderer in the pipeline assets in use has an active Anime Rim feature, so the depth-edge rim draws nothing: add one to the renderer.", MessageType.Warning);
        }

        /** <summary>Under a ground layer's detail slot: a warning when the texture there is not a packed detail map.</summary> */
        private static void WarnIfNotPacked(MaterialProperty property)
        {
            if (Array.IndexOf(DetailSlots, property.name) < 0 || property.textureValue == null)
                return;
            string path = AssetDatabase.GetAssetPath(property.textureValue);
            if (!string.IsNullOrEmpty(path) && !DataTextures.HasLabel(path, AnimeDetailImport.Label))
                EditorGUILayout.HelpBox("Not a packed detail map: pack one from a Detail Set (Create > Anime Shaders > Detail Set).", MessageType.Warning);
        }

        private static void WarnAboutStones(MaterialProperty property)
        {
            if (!StoneSlots.TryGetValue(property.name, out string[] variation) || property.targets.Length == 0)
                return;
            var material = (Material)property.targets[0];
            if (property.textureValue == null)
            {
                if (Array.Exists(variation, name => material.HasFloat(name) && material.GetFloat(name) > 0f))
                    EditorGUILayout.HelpBox("Stone Shade and Stone Mix need a stone map: tick Stones on the layer's Detail Set and Pack.", MessageType.Warning);
                return;
            }
            string path = AssetDatabase.GetAssetPath(property.textureValue);
            if (!string.IsNullOrEmpty(path) && !DataTextures.HasLabel(path, AnimeDetailImport.StonesLabel))
                EditorGUILayout.HelpBox("Not a stone map: pack one from a Detail Set with Stones ticked.", MessageType.Warning);
        }

        /** <summary>Under a face map slot: what an empty slot or a texture that isn't a baked face map means.</summary> */
        private static void WarnAboutFaceMap(MaterialProperty property)
        {
            if (property.name != Ids.FaceMapName)
                return;
            if (property.textureValue == null)
            {
                EditorGUILayout.HelpBox("No face map: the face shades by its normals. Bake one (Create > Anime Shaders > Face Map); the character also needs a head frame.", MessageType.Warning);
                return;
            }
            string path = AssetDatabase.GetAssetPath(property.textureValue);
            if (!string.IsNullOrEmpty(path) && !DataTextures.HasLabel(path, AnimeFaceMapImport.Label))
                EditorGUILayout.HelpBox("Not a baked face map: bake one from a Face Map asset.", MessageType.Warning);
        }

        /** <summary>Under Line On Open Edges: closed parts sharing the material need a Z offset, or the line shows across their sides.</summary> */
        private static void WarnAboutOpenEdges(MaterialProperty property, Material material)
        {
            if (property.name != Ids.OutlineOpenEdgesName || property.floatValue <= 0f || !material.HasFloat(Ids.OutlineZOffset))
                return;
            if (material.GetFloat(Ids.OutlineZOffset) <= 0f)
                EditorGUILayout.HelpBox("With no Z offset, a closed part drawn with this material shows the line across its sides. Give the line a Z offset (a few centimetres on a character).", MessageType.Warning);
        }

        #endregion
    }
}
