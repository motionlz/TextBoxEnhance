using TextBoxEnhance.Data;
using UnityEditor;
using UnityEngine;

namespace TextBoxEnhance.EditorTools
{
    /// <summary>
    /// Builds text effects by stacking layers, with the result animating live above the
    /// controls. The point is the feedback loop: finding an amplitude that feels right
    /// takes a dozen tries, and a dozen script recompiles is not a loop anyone completes.
    /// </summary>
    public sealed class TextEffectEditorWindow : EditorWindow
    {
        private const float PreviewHeight = 170f;
        private const string SamplePrefKey = "TextBoxEnhance.EffectEditor.Sample";
        private const string SizePrefKey = "TextBoxEnhance.EffectEditor.Size";
        private const string AdvancedPrefKey = "TextBoxEnhance.EffectEditor.Advanced";

        [SerializeField] private TextEffectAsset m_Asset;
        [SerializeField] private string m_Sample = "Animate this text";
        [SerializeField] private float m_FontSize = 36f;
        [SerializeField] private bool m_Playing = true;
        [SerializeField] private bool m_Advanced;
        [SerializeField] private float m_Time;

        private SerializedObject m_Serialized;
        private EffectPreviewRenderer m_Renderer;
        private double m_LastTick;
        private Vector2 m_Scroll;

        [MenuItem("Tools/TextBox Enhance/Text Effect Editor", priority = 0)]
        public static void Open()
        {
            var window = GetWindow<TextEffectEditorWindow>("Text Effects");
            window.minSize = new Vector2(360f, 480f);
            window.Show();
        }

        /// <summary>Opens the window already editing <paramref name="asset"/>.</summary>
        public static void Open(TextEffectAsset asset)
        {
            Open();
            GetWindow<TextEffectEditorWindow>().Select(asset);
        }

        private void OnEnable()
        {
            m_Sample = EditorPrefs.GetString(SamplePrefKey, m_Sample);
            m_FontSize = EditorPrefs.GetFloat(SizePrefKey, m_FontSize);
            m_Advanced = EditorPrefs.GetBool(AdvancedPrefKey, false);

            m_Renderer = new EffectPreviewRenderer();
            m_LastTick = EditorApplication.timeSinceStartup;
            EditorApplication.update += Tick;

            Select(m_Asset);
        }

        private void OnDisable()
        {
            EditorApplication.update -= Tick;
            m_Renderer?.Dispose();
            m_Renderer = null;

            EditorPrefs.SetString(SamplePrefKey, m_Sample);
            EditorPrefs.SetFloat(SizePrefKey, m_FontSize);
            EditorPrefs.SetBool(AdvancedPrefKey, m_Advanced);
        }

        private void Tick()
        {
            double now = EditorApplication.timeSinceStartup;
            float delta = (float)(now - m_LastTick);
            m_LastTick = now;

            if (!m_Playing)
                return;

            // Clamped so a stall in another part of the editor does not jump the preview.
            m_Time += Mathf.Clamp(delta, 0f, 0.25f);
            Repaint();
        }

        private void Select(TextEffectAsset asset)
        {
            m_Asset = asset;
            m_Serialized = asset != null ? new SerializedObject(asset) : null;
        }

        private void OnGUI()
        {
            DrawToolbar();

            if (m_Asset == null)
            {
                DrawEmptyState();
                return;
            }

            m_Serialized.Update();

            DrawPreview();
            DrawPreviewControls();
            EditorGUILayout.Space(4f);
            DrawIdentity();

            m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll);
            DrawLayers();
            EditorGUILayout.Space(6f);
            DrawPresetBar("Add a layer from");
            EditorGUILayout.EndScrollView();

            m_Serialized.ApplyModifiedProperties();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                var picked = (TextEffectAsset)EditorGUILayout.ObjectField(
                    m_Asset, typeof(TextEffectAsset), false, GUILayout.MinWidth(120f));

                if (picked != m_Asset)
                    Select(picked);

                if (GUILayout.Button("New", EditorStyles.toolbarButton, GUILayout.Width(44f)))
                    CreateAsset();

                using (new EditorGUI.DisabledScope(m_Asset == null))
                {
                    if (GUILayout.Button("Add to Library", EditorStyles.toolbarButton, GUILayout.Width(96f)))
                        AddToLibrary();
                }

                GUILayout.FlexibleSpace();
                m_Advanced = GUILayout.Toggle(m_Advanced, "Advanced", EditorStyles.toolbarButton, GUILayout.Width(70f));
            }
        }

        private void DrawEmptyState()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.HelpBox(
                "Pick an effect to edit, or start a new one from a preset below.\n\n" +
                "An effect is a stack of layers. Each layer moves one thing -- position, " +
                "rotation, size, opacity or colour -- in one way.",
                MessageType.Info);

            EditorGUILayout.Space(6f);
            DrawPresetBar("Start from");
        }

        private void DrawPreview()
        {
            Rect rect = GUILayoutUtility.GetRect(10f, PreviewHeight, GUILayout.ExpandWidth(true));

            Color background = EditorGUIUtility.isProSkin
                ? new Color(0.13f, 0.14f, 0.17f)
                : new Color(0.22f, 0.23f, 0.27f);

            m_Renderer.SetSample(m_Sample, m_FontSize);
            m_Renderer.Draw(rect, m_Asset, m_Time, background);
        }

        private void DrawPreviewControls()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                m_Playing = GUILayout.Toggle(m_Playing, m_Playing ? "Pause" : "Play",
                    EditorStyles.miniButton, GUILayout.Width(52f));

                if (GUILayout.Button("Restart", EditorStyles.miniButton, GUILayout.Width(56f)))
                    m_Time = 0f;

                using (new EditorGUI.DisabledScope(m_Playing))
                    m_Time = EditorGUILayout.Slider(m_Time, 0f, 10f);
            }

            m_Sample = EditorGUILayout.TextField("Sample text", m_Sample);
            m_FontSize = EditorGUILayout.Slider("Text size", m_FontSize, 8f, 96f);
        }

        private void DrawIdentity()
        {
            SerializedProperty tag = m_Serialized.FindProperty("m_Tag");
            EditorGUILayout.PropertyField(tag, new GUIContent("Tag",
                "The word you write in angle brackets to use this effect."));

            string tagValue = tag.stringValue;
            if (string.IsNullOrWhiteSpace(tagValue))
            {
                EditorGUILayout.HelpBox("Give the effect a tag or nothing can call it.", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.LabelField(" ", $"Write  <{tagValue}>your text</{tagValue}>", EditorStyles.miniLabel);

                if (TextEffectRegistry.IsBuiltIn(tagValue))
                {
                    EditorGUILayout.HelpBox($"<{tagValue}> is already a built-in effect. " +
                                            "This one will replace it everywhere.", MessageType.Warning);
                }
            }

            if (m_Advanced)
                EditorGUILayout.PropertyField(m_Serialized.FindProperty("m_Description"));
        }

        private void DrawLayers()
        {
            SerializedProperty layers = m_Serialized.FindProperty("m_Layers");

            if (layers.arraySize == 0)
            {
                EditorGUILayout.HelpBox("No layers yet. Add one from a preset below.", MessageType.None);
            }

            int removeAt = -1;
            for (int i = 0; i < layers.arraySize; i++)
            {
                if (EffectLayerGui.Draw(layers.GetArrayElementAtIndex(i), i, m_Advanced))
                    removeAt = i;
            }

            if (removeAt >= 0)
                layers.DeleteArrayElementAtIndex(removeAt);

            if (GUILayout.Button("Add empty layer"))
            {
                layers.InsertArrayElementAtIndex(layers.arraySize);
                m_Serialized.ApplyModifiedProperties();
                ResetLayer(m_Asset.Layers[m_Asset.Layers.Count - 1]);
                m_Serialized.Update();
            }
        }

        /// <summary>
        /// A freshly inserted array element copies the one before it, which is rarely
        /// what someone adding a layer wants. Start it from a plain wave instead.
        /// </summary>
        private static void ResetLayer(EffectLayer layer)
        {
            if (layer == null)
                return;

            EffectLayer template = EffectPresets.Wave()[0];
            layer.Channel = template.Channel;
            layer.Motion = template.Motion;
            layer.Timebase = template.Timebase;
            layer.Min = template.Min;
            layer.Max = template.Max;
            layer.Speed = template.Speed;
            layer.Spread = template.Spread;
            layer.Phase = 0f;
            layer.Duty = 0.5f;
            layer.Salt = 0;
            layer.ColourMode = ColourMode.Solid;
            layer.Colour = Color.white;
            layer.Weight = 1f;
        }

        private void DrawPresetBar(string heading)
        {
            EditorGUILayout.LabelField(heading, EditorStyles.miniBoldLabel);

            float width = EditorGUIUtility.currentViewWidth - 24f;
            int perRow = Mathf.Max(1, Mathf.FloorToInt(width / 84f));
            int index = 0;

            while (index < EffectPresets.All.Count)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (int column = 0; column < perRow && index < EffectPresets.All.Count; column++, index++)
                    {
                        EffectPresets.Preset preset = EffectPresets.All[index];
                        if (GUILayout.Button(preset.Name, EditorStyles.miniButton))
                            ApplyPreset(preset);
                    }
                }
            }
        }

        private void ApplyPreset(EffectPresets.Preset preset)
        {
            if (m_Asset == null)
            {
                CreateAsset(preset);
                return;
            }

            Undo.RecordObject(m_Asset, "Add " + preset.Name + " layers");
            m_Asset.Layers.AddRange(preset.Build());
            EditorUtility.SetDirty(m_Asset);
            m_Serialized.Update();
        }

        private void CreateAsset()
        {
            CreateAsset(default);
        }

        private void CreateAsset(EffectPresets.Preset preset)
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "New Text Effect", "NewTextEffect", "asset",
                "Where should the effect asset live?");

            if (string.IsNullOrEmpty(path))
                return;

            var asset = ScriptableObject.CreateInstance<TextEffectAsset>();
            asset.Layers.AddRange(preset.Build != null ? preset.Build() : EffectPresets.Wave());

            AssetDatabase.CreateAsset(asset, path);

            // The file name is the obvious first guess at a tag, and lowercase because
            // that is how tags are written even though matching ignores case.
            var serialized = new SerializedObject(asset);
            serialized.FindProperty("m_Tag").stringValue =
                System.IO.Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
            serialized.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.SaveAssets();
            Select(asset);
            AddToLibrary();
        }

        /// <summary>
        /// Puts the effect in the project library, which is what makes its tag work in
        /// a text box. Without this step the effect exists but nothing can call it.
        /// </summary>
        private void AddToLibrary()
        {
            if (m_Asset == null)
                return;

            TextEffectLibrary library = TextEffectLibraryLoader.FindOrCreateLibrary();
            if (library == null)
                return;

            if (!library.Effects.Contains(m_Asset))
            {
                Undo.RecordObject(library, "Add effect to library");
                library.Effects.Add(m_Asset);
                EditorUtility.SetDirty(library);
                AssetDatabase.SaveAssets();
            }

            TextEffectLibraryLoader.EnsurePreloaded(library);
            library.Reregister();
        }
    }
}
