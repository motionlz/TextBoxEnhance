using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace TextBoxEnhance.EditorTools
{
    /// <summary>
    /// Inspector for <see cref="AnimatedTextBox"/>: grouped settings that hide what does
    /// nothing, transport buttons, a live scene-view preview outside play mode, and a
    /// list of the tags this project actually has.
    /// </summary>
    [CustomEditor(typeof(AnimatedTextBox))]
    [CanEditMultipleObjects]
    public sealed class AnimatedTextBoxEditor : Editor
    {
        private const string PreviewPrefKey = "TextBoxEnhance.PreviewInEditMode";

        private static bool s_ShowTagReference;
        private static bool s_ShowEvents;
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

            DrawText();
            DrawTypewriter();
            DrawEntrance();
            DrawTiming();
            DrawEvents();

            serializedObject.ApplyModifiedProperties();

            EditorUi.Section("Preview");
            DrawTransport();

            EditorGUILayout.Space(4f);
            DrawTagReference();
        }

        private SerializedProperty Find(string name)
        {
            return serializedObject.FindProperty(name);
        }

        /// <summary>
        /// No heading over these two: they sit at the top of the component, where a
        /// heading saying "Text" above a field labelled "Text" is a word wasted.
        /// </summary>
        private void DrawText()
        {
            EditorGUILayout.PropertyField(Find("m_Text"));
            EditorGUILayout.PropertyField(Find("m_Target"));
        }

        private void DrawTypewriter()
        {
            EditorUi.Section("Typewriter");

            SerializedProperty typewriter = Find("m_Typewriter");
            EditorGUILayout.PropertyField(typewriter, new GUIContent("Reveal one at a time"));

            // Speed and play-on-enable mean nothing with the typewriter off, and a field
            // that does nothing is worse than one that is missing.
            if (!typewriter.boolValue)
                return;

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(Find("m_CharactersPerSecond"), new GUIContent("Letters per second"));
                EditorGUILayout.PropertyField(Find("m_PlayOnEnable"), new GUIContent("Start on enable"));
            }
        }

        private void DrawEntrance()
        {
            EditorUi.Section("Character entrance");

            SerializedProperty style = Find("m_RevealStyle");
            EditorGUILayout.PropertyField(style, new GUIContent("Style"));

            if (style.hasMultipleDifferentValues)
                return;

            var styleValue = (RevealStyle)style.enumValueIndex;
            if (styleValue == RevealStyle.None)
                return;

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(Find("m_RevealDuration"), new GUIContent("Seconds each"));
                EditorGUILayout.PropertyField(Find("m_RevealEase"), new GUIContent("Shape"));

                if (Slides(styleValue))
                    EditorGUILayout.PropertyField(Find("m_RevealDistance"), new GUIContent("Travel (em)"));

                if (styleValue == RevealStyle.Spin)
                    EditorGUILayout.PropertyField(Find("m_RevealSpins"), new GUIContent("Turns"));
            }
        }

        private static bool Slides(RevealStyle style)
        {
            switch (style)
            {
                case RevealStyle.SlideUp:
                case RevealStyle.SlideDown:
                case RevealStyle.SlideLeft:
                case RevealStyle.SlideRight:
                    return true;

                default:
                    return false;
            }
        }

        private void DrawTiming()
        {
            EditorUi.Section("Timing");
            EditorGUILayout.PropertyField(Find("m_UseUnscaledTime"), new GUIContent("Ignore time scale",
                "Keeps text moving while the game is paused, which is what a pause menu wants."));
        }

        private void DrawEvents()
        {
            EditorGUILayout.Space(6f);
            s_ShowEvents = EditorGUILayout.Foldout(s_ShowEvents, "Events", true, EditorStyles.foldoutHeader);
            if (!s_ShowEvents)
                return;

            EditorGUILayout.PropertyField(Find("OnRevealStarted"));
            EditorGUILayout.PropertyField(Find("OnCharacterRevealed"));
            EditorGUILayout.PropertyField(Find("OnRevealCompleted"));
        }

        private void DrawTransport()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Play", GUILayout.Height(22f)))
                    ForEachTarget(box => box.Play());

                if (GUILayout.Button("Skip", GUILayout.Height(22f)))
                    ForEachTarget(box => box.SkipToEnd());

                if (GUILayout.Button("Rewind", GUILayout.Height(22f)))
                    ForEachTarget(box => box.Rewind());
            }

            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                bool preview = EditorGUILayout.ToggleLeft(
                    new GUIContent("Animate in the scene view",
                        "Pumps the player loop while this component is selected, so effects move " +
                        "outside play mode."),
                    PreviewInEditMode);

                if (preview != PreviewInEditMode)
                    PreviewInEditMode = preview;
            }

            var box = target as AnimatedTextBox;
            if (box == null || !Application.isPlaying)
                return;

            int total = box.TotalCharacters;
            float done = total > 0 ? box.RevealedCharacters / (float)total : 0f;
            EditorUi.ProgressBar("Revealed", done, $"{box.RevealedCharacters} / {total}");
        }

        private void DrawTagReference()
        {
            s_ShowTagReference = EditorGUILayout.Foldout(s_ShowTagReference, "Tags you can write", true,
                EditorStyles.foldoutHeader);

            if (!s_ShowTagReference)
                return;

            var builtIn = new List<string>();
            var fromAssets = new List<string>();

            foreach (string tag in TextEffectRegistry.TagNames)
                (TextEffectRegistry.IsBuiltIn(tag) ? builtIn : fromAssets).Add(tag);

            DrawTagRow("Built in", builtIn);

            // Effects authored in this project, which is worth showing separately: it is
            // the only place someone can check whether their own effect registered.
            if (fromAssets.Count > 0)
                DrawTagRow("This project", fromAssets);

            DrawTagRow("Timing", new List<string> { "speed=2", "pause=0.4" });

            EditorGUILayout.Space(2f);
            EditorGUILayout.LabelField(
                "Attributes: <wave=0.3> or <wave a=0.3 f=8 w=0.6>.  Thai: add \"marks\" or \"base\" to " +
                "aim at part of a letter.  TextMeshPro's own tags still work.",
                EditorStyles.wordWrappedMiniLabel);
        }

        private static void DrawTagRow(string heading, List<string> tags)
        {
            if (tags.Count == 0)
                return;

            var builder = new StringBuilder();
            for (int i = 0; i < tags.Count; i++)
            {
                if (i > 0)
                    builder.Append("   ");
                builder.Append('<').Append(tags[i]).Append('>');
            }

            EditorGUILayout.LabelField(heading, builder.ToString(), EditorStyles.wordWrappedMiniLabel);
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
