using System;
using System.IO;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace AnimeShaders.EditorTools
{
    /**
     * <summary>A face map's bake on disk: reads the face mesh from its model at rest (positions and normals turned into
     * the model's own space, front +Z and up +Y as <see cref="AnimeHeadFrame" /> reads them; each vertex's face weight
     * its skin weight to the humanoid head and the bones below it), bakes it (<see cref="FaceMapMath" />), writes the map
     * as a labelled half-float EXR beside the asset and records it; and whether a map is out of date.</summary>
     */
    public static class AnimeFaceMapBaker
    {
        #region Private Fields

        private const int Version = 1;

        #endregion

        #region Public API

        /** <summary>Bakes <paramref name="faceMap" /> into an EXR beside it (named after it), imports it and records it on the asset.</summary> */
        public static void Bake(AnimeFaceMap faceMap)
        {
            string assetPath = AssetDatabase.GetAssetPath(faceMap);
            if (string.IsNullOrEmpty(assetPath))
                throw new InvalidOperationException($"Face map {faceMap.name} is not saved as an asset.");
            FaceMap baked = FaceMapMath.Bake(Source(faceMap.Mesh), faceMap.Settings);
            string path = Path.ChangeExtension(assetPath, "exr");
            DataTextures.WriteHalf(baked.Pixels, baked.Size, baked.Size, path, AnimeFaceMapImport.Label);
            faceMap.Record(AssetDatabase.LoadAssetAtPath<Texture2D>(path), HashOf(faceMap), baked);
            EditorUtility.SetDirty(faceMap);
            AssetDatabase.SaveAssetIfDirty(faceMap);
        }

        /** <summary>A hash of the mesh's model file and the settings: when it differs from the recorded one, the map is out of date.</summary> */
        public static string HashOf(AnimeFaceMap faceMap)
        {
            var hash = new Hash128();
            hash.Append(Version);
            Mesh mesh = faceMap.Mesh;
            string path = mesh == null ? string.Empty : AssetDatabase.GetAssetPath(mesh);
            hash.Append(string.IsNullOrEmpty(path) ? string.Empty : DataTextures.FileHash(path));
            hash.Append(mesh == null ? string.Empty : mesh.name);
            FaceMapSettings settings = faceMap.Settings;
            hash.Append(settings.Size);
            hash.Append(settings.Smoothing);
            hash.Append(settings.SphereBlend);
            hash.Append(settings.Gutter);
            return hash.ToString();
        }

        /** <summary>What stops <paramref name="mesh" /> being baked, or null when nothing does.</summary> */
        public static string Problem(Mesh mesh)
        {
            return Find(mesh, out _, out _, out _);
        }

        /** <summary>The face as the bake reads it; throws with what stops it.</summary> */
        public static FaceMapSource Source(Mesh mesh)
        {
            string problem = Find(mesh, out GameObject model, out SkinnedMeshRenderer skinned, out Transform head);
            if (problem != null)
                throw new InvalidOperationException(problem);

            Matrix4x4 meshFromModel = AnimeHeadFrame.MeshFromModel(skinned.transform, model.transform);
            Matrix4x4 modelFromMesh = meshFromModel.inverse;
            Matrix4x4 normalsToModel = meshFromModel.transpose;
            Vector3[] positions = mesh.vertices;
            Vector3[] normals = mesh.normals;
            for (int i = 0; i < positions.Length; i++)
            {
                positions[i] = modelFromMesh.MultiplyPoint3x4(positions[i]);
                normals[i] = normalsToModel.MultiplyVector(normals[i]).normalized;
            }
            return new FaceMapSource(positions, normals, mesh.uv, mesh.triangles, FaceWeights(mesh, skinned.bones, head));
        }

        #endregion

        #region Helpers

        /** <summary>The mesh's model, its skinned renderer and the head bone; or what is missing.</summary> */
        private static string Find(Mesh mesh, out GameObject model, out SkinnedMeshRenderer skinned, out Transform head)
        {
            model = null;
            skinned = null;
            head = null;
            if (mesh == null)
                return "Needs the face's mesh.";
            model = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GetAssetPath(mesh));
            if (model == null)
                return $"{mesh.name} is not inside a model.";
            foreach (SkinnedMeshRenderer candidate in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (candidate.sharedMesh == mesh)
                    skinned = candidate;
            }
            if (skinned == null)
                return $"{mesh.name} is not skinned in its model.";
            if (!mesh.isReadable)
                return $"{model.name}: turn on Read/Write in the model's import settings.";
            if (mesh.uv.Length != mesh.vertexCount || mesh.normals.Length != mesh.vertexCount)
                return $"{mesh.name} needs UVs and normals.";
            head = HeadOf(model);
            if (head == null)
                return $"{model.name} needs a humanoid avatar with a head bone.";
            return Array.IndexOf(skinned.bones, head) < 0 && !SkinnedBelow(skinned.bones, head) ? $"{mesh.name} isn't moved by the head." : null;
        }

        /** <summary>The model's humanoid head bone, by the name its avatar maps it to.</summary> */
        private static Transform HeadOf(GameObject model)
        {
            Animator animator = model.GetComponentInChildren<Animator>(true);
            Avatar avatar = animator != null ? animator.avatar : null;
            if (avatar == null || !avatar.isHuman)
                return null;
            string human = HumanTrait.BoneName[(int)HumanBodyBones.Head];
            foreach (HumanBone bone in avatar.humanDescription.human)
            {
                if (bone.humanName != human)
                    continue;
                foreach (Transform child in model.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name == bone.boneName)
                        return child;
                }
            }
            return null;
        }

        /** <summary>Whether any of <paramref name="bones" /> sits below <paramref name="head" />.</summary> */
        private static bool SkinnedBelow(Transform[] bones, Transform head)
        {
            foreach (Transform bone in bones)
            {
                if (bone != null && bone.IsChildOf(head))
                    return true;
            }
            return false;
        }

        /** <summary>Each vertex's summed skin weight to the head and the bones below it.</summary> */
        private static float[] FaceWeights(Mesh mesh, Transform[] bones, Transform head)
        {
            var underHead = new bool[bones.Length];
            for (int b = 0; b < bones.Length; b++)
                underHead[b] = bones[b] != null && bones[b].IsChildOf(head);
            NativeArray<byte> perVertex = mesh.GetBonesPerVertex();
            NativeArray<BoneWeight1> weights = mesh.GetAllBoneWeights();
            var face = new float[mesh.vertexCount];
            int at = 0;
            for (int v = 0; v < face.Length; v++)
            {
                for (int k = 0; k < perVertex[v]; k++)
                {
                    BoneWeight1 weight = weights[at + k];
                    if (underHead[weight.boneIndex])
                        face[v] += weight.weight;
                }
                at += perVertex[v];
            }
            return face;
        }

        #endregion
    }
}
