using System;
using System.Collections.Generic;
using TextBoxEnhance.Data;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace TextBoxEnhance.EditorTools
{
    /// <summary>
    /// Renders animated sample text into an editor rect, using a throwaway TextMeshPro
    /// object in an off-screen preview scene.
    /// </summary>
    /// <remarks>
    /// It animates through <see cref="TextMeshAnimator"/>, the same code the runtime
    /// component uses, so a preview cannot quietly disagree with the game. The effect
    /// being edited is applied directly rather than looked up by tag, which is what lets
    /// an unsaved draft animate.
    /// </remarks>
    internal sealed class EffectPreviewRenderer : IDisposable
    {
        private PreviewRenderUtility m_Preview;
        private GameObject m_TextObject;
        private TextMeshPro m_Text;
        private TMP_MeshInfo[] m_Cache;

        private readonly ParsedText m_Parsed = new ParsedText();
        private readonly List<EffectRange> m_Ranges = new List<EffectRange>();

        private string m_BuiltText;
        private float m_BuiltSize;
        private bool m_LayoutDirty = true;

        public bool IsReady => m_Text != null;

        private void EnsureCreated()
        {
            if (m_Preview != null)
                return;

            m_Preview = new PreviewRenderUtility();
            m_Preview.camera.orthographic = true;
            m_Preview.camera.nearClipPlane = 0.1f;
            m_Preview.camera.farClipPlane = 100f;
            m_Preview.camera.clearFlags = CameraClearFlags.SolidColor;
            m_Preview.camera.transform.position = new Vector3(0f, 0f, -10f);
            m_Preview.camera.transform.rotation = Quaternion.identity;

            m_TextObject = new GameObject("TextBoxEnhance Preview")
            {
                hideFlags = HideFlags.HideAndDontSave,
            };

            m_Text = m_TextObject.AddComponent<TextMeshPro>();
            m_Text.alignment = TextAlignmentOptions.Center;
            m_Text.enableAutoSizing = false;
            m_Text.color = Color.white;

            m_Preview.AddSingleGO(m_TextObject);
        }

        /// <summary>Sets the sample string and its point size. Cheap to call every frame.</summary>
        public void SetSample(string text, float fontSize)
        {
            if (m_BuiltText == text && Mathf.Approximately(m_BuiltSize, fontSize))
                return;

            m_BuiltText = text;
            m_BuiltSize = fontSize;
            m_LayoutDirty = true;
        }

        /// <summary>Forces the next draw to re-lay out the text, e.g. after the font changes.</summary>
        public void MarkDirty()
        {
            m_LayoutDirty = true;
        }

        private void RebuildLayout()
        {
            TextEffectParser.Parse(m_BuiltText ?? string.Empty, m_Parsed);

            m_Text.fontSize = Mathf.Max(1f, m_BuiltSize);
            m_Text.text = m_Parsed.Text;
            m_Text.ForceMeshUpdate();

            m_Cache = TextMeshAnimator.Cache(m_Text);
            m_LayoutDirty = false;
        }

        /// <summary>
        /// Builds the effect list for one frame: whatever the sample text asked for by
        /// tag, plus the draft effect across every character.
        /// </summary>
        private List<EffectRange> RangesFor(TextEffectAsset draft)
        {
            m_Ranges.Clear();
            m_Ranges.AddRange(m_Parsed.Effects);

            int charCount = m_Text.textInfo != null ? m_Text.textInfo.characterCount : 0;
            if (draft != null && charCount > 0)
            {
                m_Ranges.Add(new EffectRange
                {
                    Effect = new DataTextEffect(draft),
                    Parameters = TagParams.Empty,
                    Start = 0,
                    End = charCount,
                });
            }

            return m_Ranges;
        }

        /// <summary>Draws one animated frame of the sample into <paramref name="rect"/>.</summary>
        public void Draw(Rect rect, TextEffectAsset draft, float time, Color background)
        {
            if (rect.width < 4f || rect.height < 4f || Event.current.type != EventType.Repaint)
                return;

            EnsureCreated();

            if (m_LayoutDirty || m_Cache == null)
                RebuildLayout();

            TextMeshAnimator.Apply(m_Text, m_Cache, RangesFor(draft), time, 1f / 60f,
                FullyRevealed.Instance, RevealSettings.None);

            FrameCamera(rect);

            m_Preview.camera.backgroundColor = background;
            m_Preview.BeginPreview(rect, GUIStyle.none);
            m_Preview.camera.Render();
            Texture rendered = m_Preview.EndPreview();
            GUI.DrawTexture(rect, rendered, ScaleMode.StretchToFill, false);
        }

        /// <summary>
        /// Fits the laid-out text in view with room to spare, so a character that swings
        /// or scales out does not clip against the edge and read as a bug.
        /// </summary>
        private void FrameCamera(Rect rect)
        {
            Bounds bounds = m_Text.bounds;
            float aspect = rect.width / Mathf.Max(1f, rect.height);

            const float headroom = 1.45f;
            float halfHeight = Mathf.Max(bounds.extents.y, bounds.extents.x / Mathf.Max(0.01f, aspect));

            m_Preview.camera.aspect = aspect;
            m_Preview.camera.orthographicSize = Mathf.Max(0.5f, halfHeight * headroom);
            m_Preview.camera.transform.position = new Vector3(bounds.center.x, bounds.center.y, -10f);
        }

        public void Dispose()
        {
            if (m_TextObject != null)
            {
                UnityEngine.Object.DestroyImmediate(m_TextObject);
                m_TextObject = null;
                m_Text = null;
            }

            m_Preview?.Cleanup();
            m_Preview = null;
            m_Cache = null;
        }
    }
}
