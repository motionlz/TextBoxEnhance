using UnityEditor;
using UnityEngine;

namespace TextBoxEnhance.EditorTools
{
    /// <summary>
    /// A slider whose range is a suggestion rather than a limit: the track covers the
    /// values people reach for every day, and the number field beside it accepts
    /// anything at all.
    /// </summary>
    /// <remarks>
    /// Unity's own <c>EditorGUILayout.Slider</c> clamps typed input to the track, so a
    /// track short enough to be precise is also a ceiling. Every effect preset lives in
    /// the bottom sixth of a 0..1 track, which put the useful values inside about
    /// twenty pixels; widening the track is not an option and shortening it must not
    /// cost the ability to go further.
    /// </remarks>
    public static class SoftRangeSlider
    {
        private const float FieldWidth = 56f;
        private const float Gap = 4f;

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

        /// <summary>Draws the slider and its unclamped field, and returns the new value.</summary>
        public static float Draw(GUIContent label, float value, float softMin, float softMax)
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
            float dragged = GUI.HorizontalSlider(sliderRect, value, low, high);
            if (EditorGUI.EndChangeCheck())
                value = dragged;

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
