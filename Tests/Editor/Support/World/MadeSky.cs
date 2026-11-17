using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AnimeShaders.Tests
{
    /**
     * <summary>A sky preset made for a test, starting from the defaults, its state set field by field (by the state's
     * serialized names: a wrong name fails at once) and read back as the <see cref="SkyState" /> a scene would get.</summary>
     */
    public sealed class MadeSky : IDisposable
    {
        #region Private Fields

        private const string Root = "state.";

        private readonly SkyPreset _preset;
        private readonly SerializedObject _serialized;

        #endregion

        #region Properties

        /** <summary>The state as it stands.</summary> */
        public SkyState State => _preset.Resolve();

        /** <summary>The made preset itself, for an applier to take.</summary> */
        public SkyPreset Preset => _preset;

        #endregion

        #region Public API

        /** <summary>A preset at the defaults.</summary> */
        public MadeSky()
        {
            _preset = ScriptableObject.CreateInstance<SkyPreset>();
            _serialized = new SerializedObject(_preset);
        }

        /** <summary>Sets a number.</summary> */
        public MadeSky Set(string field, float value)
        {
            Field(field).floatValue = value;
            return Apply();
        }

        /** <summary>Sets a switch.</summary> */
        public MadeSky Set(string field, bool value)
        {
            Field(field).boolValue = value;
            return Apply();
        }

        /** <summary>Sets a colour.</summary> */
        public MadeSky Set(string field, Color value)
        {
            Field(field).colorValue = value;
            return Apply();
        }

        /** <summary>Sets a pair (the wind).</summary> */
        public MadeSky Set(string field, Vector2 value)
        {
            Field(field).vector2Value = value;
            return Apply();
        }

        /** <summary>Sets the ridges, farthest first.</summary> */
        public MadeSky Ridges(params SkyRidge[] ridges)
        {
            SerializedProperty list = Field("ridges");
            list.arraySize = ridges.Length;
            for (int i = 0; i < ridges.Length; i++)
            {
                SerializedProperty ridge = list.GetArrayElementAtIndex(i);
                ridge.FindPropertyRelative("colour").colorValue = ridges[i].Colour;
                ridge.FindPropertyRelative("height").floatValue = ridges[i].Height;
                ridge.FindPropertyRelative("roughness").floatValue = ridges[i].Roughness;
                ridge.FindPropertyRelative("hills").floatValue = ridges[i].Hills;
                ridge.FindPropertyRelative("seed").floatValue = ridges[i].Seed;
                ridge.FindPropertyRelative("distance").floatValue = ridges[i].Distance;
            }
            return Apply();
        }

        /** <summary>Destroys the preset.</summary> */
        public void Dispose()
        {
            Object.DestroyImmediate(_preset);
        }

        #endregion

        #region Helpers

        private SerializedProperty Field(string field)
        {
            _serialized.Update();
            SerializedProperty property = _serialized.FindProperty(Root + field);
            if (property == null)
                throw new ArgumentException($"A sky state has no field {field}.");
            return property;
        }

        private MadeSky Apply()
        {
            _serialized.ApplyModifiedPropertiesWithoutUndo();
            return this;
        }

        #endregion
    }
}
