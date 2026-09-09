using UnityEditor;
using UnityEngine;

namespace TextBoxEnhance.EditorTools
{
    /// <summary>
    /// A slider that gives most of its track to the small end of the range, paired with
    /// a number field that accepts anything at all.
    /// </summary>
    /// <remarks>
    /// Every effect preset lives in the bottom sixth of its range -- jitter at 0.05,
    /// shake at 0.06, wave at 0.15 -- so a linear track spends five sixths of its
    /// length on values nobody uses and crams the rest into twenty pixels. Squaring the
    /// response moves those presets out to between a quarter and two fifths of the
    /// track without shortening it, so nothing becomes unreachable.
    ///
    /// The range still stretches to follow a value typed outside it, because Unity's
    /// own <c>EditorGUILayout.Slider</c> clamps typed input and a slider that argues
    /// with its own number field is worse than either alone.
    /// </remarks>
    public static class SoftRangeSlider
    {
        private const float FieldWidth = 56f;
        private const float Gap = 4f;

        /// <summary>
        /// How sharply the track bends. 2 squares the response: the midpoint of the
        /// track sits at a quarter of the range, the first fifth covers the first
        /// twenty-fifth.
        /// </summary>
        public const float DefaultResponse = 2f;

        /// <summary>How much of the track a value past the soft range is given.</summary>
        private const float Headroom = 1.25f;

        /// <summary>
        /// The track a value should be drawn on: the soft range, stretched to keep the
        /// handle in view when the value has been typed outside it.
        /// </summary>
        public static void Range(float value, float softMin, float softMax, out float low, out float high)
        {
            low = Mathf.Min(softMin, value * Headroom);
            high = Mathf.Max(softMax, value * Headroom);

            // A soft range of zero width would make the handle meaningless.
            if (high - low < Mathf.Epsilon)
                high = low + 1f;
        }

        /// <summary>
        /// The slider position a value sits at, 0..1 along the track.
        /// </summary>
        /// <remarks>
        /// The curve only applies to a track that starts at or above zero. One spanning
        /// zero -- which only happens when a negative speed or spread has been typed --
        /// stays linear, because bending it would put the fine end at the most negative
        /// value rather than around zero where the interesting values are.
        /// </remarks>
        public static float ToPosition(float value, float low, float high, float response)
        {
            if (high - low < Mathf.Epsilon)
                return 0f;

            float t = Mathf.InverseLerp(low, high, value);
            return low < 0f ? t : Mathf.Pow(t, 1f / response);
        }

        /// <summary>The value at a slider position. The exact inverse of <see cref="ToPosition"/>.</summary>
        public static float ToValue(float position, float low, float high, float response)
        {
            float p = Mathf.Clamp01(position);
            float t = low < 0f ? p : Mathf.Pow(p, response);
            return Mathf.Lerp(low, high, t);
        }

        /// <summary>Draws the slider and its unclamped field, and returns the new value.</summary>
        public static float Draw(GUIContent label, float value, float softMin, float softMax)
        {
            return Draw(label, value, softMin, softMax, DefaultResponse);
        }

        /// <summary>Draws the slider with a chosen response curve.</summary>
        public static float Draw(GUIContent label, float value, float softMin, float softMax, float response)
        {
            Range(value, softMin, softMax, out float low, out float high);

            Rect line = EditorGUILayout.GetControlRect();
            Rect content = EditorGUI.PrefixLabel(line, label);

            var sliderRect = new Rect(content.x, content.y,
                Mathf.Max(0f, content.width - FieldWidth - Gap), content.height);
            var fieldRect = new Rect(content.xMax - FieldWidth, content.y, FieldWidth, content.height);

            // Only take the slider's value when it was actually dragged, or every repaint
            // would quantise the number to the track's pixel resolution.
            EditorGUI.BeginChangeCheck();
            float position = GUI.HorizontalSlider(sliderRect, ToPosition(value, low, high, response), 0f, 1f);
            if (EditorGUI.EndChangeCheck())
                value = ToValue(position, low, high, response);

            return EditorGUI.FloatField(fieldRect, value);
        }

        /// <summary>Draws the slider for a serialized float, keeping undo intact.</summary>
        public static void Draw(SerializedProperty property, GUIContent label, float softMin, float softMax)
        {
            EditorGUI.BeginChangeCheck();
            float value = Draw(label, property.floatValue, softMin, softMax);
            if (EditorGUI.EndChangeCheck())
                property.floatValue = value;
        }
    }
}
