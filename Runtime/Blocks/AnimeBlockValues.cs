using System.Collections.Generic;
using UnityEngine;

namespace AnimeShaders
{
    /** <summary>One owner's values for one renderer slot's property block: colours, numbers and vectors by property id.</summary> */
    public sealed class AnimeBlockValues
    {
        #region Private Fields

        private readonly Dictionary<int, Color> _colours = new();
        private readonly Dictionary<int, float> _floats = new();
        private readonly Dictionary<int, Vector4> _vectors = new();

        #endregion

        #region Properties

        /** <summary>Whether no value is set.</summary> */
        public bool IsEmpty => _colours.Count == 0 && _floats.Count == 0 && _vectors.Count == 0;

        #endregion

        #region Public API

        /** <summary>Sets colour property <paramref name="property" />.</summary> */
        public void SetColour(int property, Color colour)
        {
            _colours[property] = colour;
        }

        /** <summary>Sets number property <paramref name="property" />.</summary> */
        public void SetFloat(int property, float value)
        {
            _floats[property] = value;
        }

        /** <summary>Sets vector property <paramref name="property" />.</summary> */
        public void SetVector(int property, Vector4 value)
        {
            _vectors[property] = value;
        }

        /** <summary>Leaves property <paramref name="property" /> to the material.</summary> */
        public void Remove(int property)
        {
            _colours.Remove(property);
            _floats.Remove(property);
            _vectors.Remove(property);
        }

        /** <summary>Leaves every property to the material.</summary> */
        public void Clear()
        {
            _colours.Clear();
            _floats.Clear();
            _vectors.Clear();
        }

        /** <summary>Adds these values to <paramref name="block" />.</summary> */
        public void AddTo(MaterialPropertyBlock block)
        {
            foreach (KeyValuePair<int, Color> colour in _colours)
                block.SetColor(colour.Key, colour.Value);
            foreach (KeyValuePair<int, float> value in _floats)
                block.SetFloat(value.Key, value.Value);
            foreach (KeyValuePair<int, Vector4> vector in _vectors)
                block.SetVector(vector.Key, vector.Value);
        }

        #endregion
    }
}
