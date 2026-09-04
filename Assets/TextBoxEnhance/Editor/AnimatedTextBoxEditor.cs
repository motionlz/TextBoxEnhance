using System.Collections.Generic;
using System.Text;
using TextBoxEnhance;
using UnityEditor;
using UnityEngine;

namespace TextBoxEnhance.EditorTools
{
    /// <summary>
    /// Inspector for <see cref="AnimatedTextBox"/>: transport buttons, a live scene-view
    /// preview outside play mode, and a cheat sheet of the tags currently registered.
    /// </summary>
    [CustomEditor(typeof(AnimatedTextBox))]
    [CanEditMultipleObjects]
    public sealed class AnimatedTextBoxEditor : Editor
    {
        private const string PreviewPrefKey = "TextBoxEnhance.PreviewInEditMode";

        private static bool s_ShowTagReference;
        private bool m_DrivingPreview;

        private bool PreviewInEditMode
        {
            get => EditorPrefs.GetBool(PreviewPrefKey, true);
            set => EditorPrefs.SetBool(PreviewPrefKey, value);
        }

        private void OnEnable()
        {
            EditorApplication.update += DrivePreview;
            m_DrivingPreview = true;
        }

        private void OnDisable()
        {
            if (m_DrivingPreview)
            {
                EditorApplication.update -= DrivePreview;
                m_DrivingPreview = false;
            }
        }

        /// <summary>
        /// Edit mode has no game loop, so the preview needs pumping by hand for the
        /// effects to move while the component is selected.
        /// </summary>
        private void DrivePreview()
        {
            if (Application.isPlaying || !PreviewInEditMode || target == null)
                return;

            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script");
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            DrawTransport();

            EditorGUILayout.Space();
            DrawTagReference();
        }

        private void DrawTransport()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Play"))
                    ForEachTarget(box => box.Play());

                if (GUILayout.Button("Skip"))
                    ForEachTarget(box => box.SkipToEnd());

                if (GUILayout.Button("Rewind"))
                    ForEachTarget(box => box.Rewind());
            }

            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                bool preview = EditorGUILayout.ToggleLeft(
                    new GUIContent("Preview in edit mode",
                        "Pumps the player loop while this component is selected so the effects animate in the scene view."),
                    PreviewInEditMode);

                if (preview != PreviewInEditMode)
                    PreviewInEditMode = preview;
            }

            var box = target as AnimatedTextBox;
            if (box != null && Application.isPlaying)
                EditorGUILayout.LabelField("Revealed", $"{box.RevealedCharacters} / {box.TotalCharacters}");
        }

        private void DrawTagReference()
        {
            s_ShowTagReference = EditorGUILayout.Foldout(s_ShowTagReference, "Available tags", true);
            if (!s_ShowTagReference)
                return;

            var builder = new StringBuilder();
            builder.Append("Effects: ");

            bool first = true;
            foreach (string tag in TextEffectRegistry.TagNames)
            {
                if (!first)
                    builder.Append(", ");
                builder.Append('<').Append(tag).Append('>');
                first = false;
            }

            builder.AppendLine().AppendLine();
            builder.AppendLine("Timing: <speed=2>faster</speed>, <pause=0.4>");
            builder.AppendLine();
            builder.AppendLine("Attributes take either form: <wave=0.3> or <wave a=0.3 f=8 w=0.6>.");
            builder.Append("TextMeshPro's own tags (<b>, <color>, <size>, <sprite>) still work alongside these.");

            EditorGUILayout.HelpBox(builder.ToString(), MessageType.None);
        }

        private void ForEachTarget(System.Action<AnimatedTextBox> action)
        {
            foreach (Object each in targets)
            {
                if (each is AnimatedTextBox box)
                    action(box);
            }
        }
    }
}
