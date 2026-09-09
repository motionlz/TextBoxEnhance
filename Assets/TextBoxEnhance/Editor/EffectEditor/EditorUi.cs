using UnityEditor;
using UnityEngine;

namespace TextBoxEnhance.EditorTools
{
    /// <summary>
    /// Small pieces of shared layout, so the inspector and the effect window group
    /// things the same way and a reader moving between them is not relearning where to
    /// look.
    /// </summary>
    internal static class EditorUi
    {
        /// <summary>What a layer's row asked to happen to it.</summary>
        internal enum RowAction
        {
            None,
            Remove,
            MoveUp,
            MoveDown,
            Duplicate,
        }

        /// <summary>A titled break between groups of fields.</summary>
        public static void Section(string title)
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            Divider();
        }

        /// <summary>A hairline the width of the inspector.</summary>
        public static void Divider()
        {
            Rect line = EditorGUILayout.GetControlRect(false, 1f);
            line.height = 1f;
            EditorGUI.DrawRect(line, EditorGUIUtility.isProSkin
                ? new Color(1f, 1f, 1f, 0.08f)
                : new Color(0f, 0f, 0f, 0.12f));
            EditorGUILayout.Space(2f);
        }

        /// <summary>
        /// Draws a value the user cannot edit, for things worth seeing but not touching.
        /// </summary>
        public static void ReadOnlyField(string label, string value)
        {
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.TextField(label, value);
        }

        /// <summary>A labelled 0..1 bar, for showing how far along something is.</summary>
        public static void ProgressBar(string label, float fill, string overlay)
        {
            Rect rect = EditorGUILayout.GetControlRect(true, 18f);
            rect = EditorGUI.PrefixLabel(rect, new GUIContent(label));
            EditorGUI.ProgressBar(rect, Mathf.Clamp01(fill), overlay);
        }
    }
}
