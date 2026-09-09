using UnityEditor;
using UnityEngine;

namespace TextBoxEnhance.EditorTools
{
    /// <summary>
    /// A slider that gives the left half of its track to the small quarter of its range,
    /// so the values effects actually use are not crammed against the left edge.
    /// </summary>
    /// <remarks>
    /// Every effect preset lives in the bottom sixth of its range -- jitter at 0.05,
    /// shake at 0.06, wave at 0.15 -- and on a linear track that put them all inside
    /// about twenty pixels. Squaring the response spreads them across the first two
    /// fifths instead, and keeps getting finer as values get smaller.
    ///
    /// Both ends are hard limits. An earlier version let the track stretch to follow a
    /// value typed past its end, which fed back on itself: dragging to the end raised
    /// the value, which raised the end, which let the next drag raise it again, so the
    /// handle stuck at the far right while the number climbed forever.
    /// </remarks>
    public static class CurvedSlider
    {
        private const float FieldWidth = 56f;
        private const float Gap = 4f;

        /// <summary>
        /// How sharply the track bends. 2 squares the response: the midpoint of the
        /// track sits at a quarter of the range, the first fifth covers the first
        /// twenty-fifth.
        /// </summary>
        public const float Response = 2f;

        /// <summary>The slider position a value sits at, 0..1 along the track.</summary>
        public static float ToPosition(float value, float min, float max, float response)
        {
            if (max - min < Mathf.Epsilon)
                return 0f;

            value = Mathf.Clamp(value, min, max);

            if (min >= 0f)
                return Mathf.Pow(Mathf.InverseLerp(min, max, value), 1f / response);

            // A track that reaches into negatives curves outward from zero in both
            // directions, so the fine end stays around zero rather than sitting at the
            // most negative value nobody is aiming for.
            float zeroAt = Mathf.InverseLerp(min, max, 0f);
            float side = value >= 0f ? max : -min;
            float curved = side < Mathf.Epsilon ? 0f : Mathf.Pow(Mathf.Abs(value) / side, 1f / response);

            return value >= 0f ? Mathf.Lerp(zeroAt, 1f, curved) : Mathf.Lerp(zeroAt, 0f, curved);
        }

        /// <summary>The value at a slider position. The exact inverse of <see cref="ToPosition"/>.</summary>
        public static float ToValue(float position, float min, float max, float response)
        {
            float p = Mathf.Clamp01(position);

            if (min >= 0f)
                return Mathf.Lerp(min, max, Mathf.Pow(p, response));

            float zeroAt = Mathf.InverseLerp(min, max, 0f);

            if (p >= zeroAt)
            {
                float curved = zeroAt >= 1f ? 0f : (p - zeroAt) / (1f - zeroAt);
                return Mathf.Pow(curved, response) * max;
            }

            float negative = zeroAt <= 0f ? 0f : (zeroAt - p) / zeroAt;
            return -Mathf.Pow(negative, response) * -min;
        }

        /// <summary>Draws the slider and its field, and returns the new value.</summary>
        public static float Draw(GUIContent label, float value, float min, float max)
        {
            value = Mathf.Clamp(value, min, max);

            Rect line = EditorGUILayout.GetControlRect();
            Rect content = EditorGUI.PrefixLabel(line, label);

            var sliderRect = new Rect(content.x, content.y,
                Mathf.Max(0f, content.width - FieldWidth - Gap), content.height);
            var fieldRect = new Rect(content.xMax - FieldWidth, content.y, FieldWidth, content.height);

            // Only take the slider's value when it was actually dragged, or every repaint
            // would quantise the number to the track's pixel resolution.
            EditorGUI.BeginChangeCheck();
            float position = GUI.HorizontalSlider(sliderRect, ToPosition(value, min, max, Response), 0f, 1f);
            if (EditorGUI.EndChangeCheck())
                value = ToValue(position, min, max, Response);

            return Mathf.Clamp(EditorGUI.FloatField(fieldRect, value), min, max);
        }

        /// <summary>Draws the slider for a serialized float, keeping undo intact.</summary>
        public static void Draw(SerializedProperty property, GUIContent label, float min, float max)
        {
            EditorGUI.BeginChangeCheck();
            float value = Draw(label, property.floatValue, min, max);
            if (EditorGUI.EndChangeCheck())
                property.floatValue = value;
        }
    }
}
