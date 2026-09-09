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

        private int[] m_ClusterOf;
        private int[] m_ClusterBase;
        private ClusterMap m_Clusters;

        private string m_BuiltText;
        private float m_BuiltSize;
        private bool m_LayoutDirty = true;

        public bool IsReady => m_Text != null;

        /// <summary>The font asset the preview is drawing with, or null before its first draw.</summary>
        public TMP_FontAsset Font => m_Text != null ? m_Text.font : null;


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

            // A 3D TextMeshPro shrinks its geometry by ten unless this is on, while a
            // TextMeshProUGUI turns it on for itself. Leaving it off would draw the
            // preview at a tenth of the size the game does, and make every effect look
            // ten times stronger in here than it is.
            m_Text.isOrthographic = true;

            // Its container is sized for that tenth-scale geometry, a fifth of a unit
            // across, so full-size text wrapped after two or three letters. A preview
            // wants one line anyway: wrapping tells you nothing about an effect and
            // hides half the sample.
            m_Text.textWrappingMode = TextWrappingModes.NoWrap;
            m_Text.overflowMode = TextOverflowModes.Overflow;
            m_TextObject.GetComponent<RectTransform>().sizeDelta = new Vector2(4000f, 400f);

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

        /// <summary>
        /// Draws with a particular font, or with the project default when null. Worth
        /// setting to whatever the game ships: an effect that looks right on Latin can
        /// sit badly on a script with marks above and below the line.
        /// </summary>
        public void SetFont(TMP_FontAsset font)
        {
            EnsureCreated();

            if (m_Text.font == font || (font == null && m_Text.font == TMP_Settings.defaultFontAsset))
                return;

            m_Text.font = font != null ? font : TMP_Settings.defaultFontAsset;
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

            // Thai sample text is worth previewing correctly too: without this the
            // vowels animate off the consonants they belong to.
            int clusters = TextClusters.Build(m_Text.textInfo, ref m_ClusterOf, ref m_ClusterBase);
            m_Clusters = new ClusterMap(m_ClusterOf, m_ClusterBase, clusters);

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
        public void Draw(Rect rect, TextEffectAsset draft, float time, Color background, float zoom = 1f)
        {
            if (rect.width < 4f || rect.height < 4f || Event.current.type != EventType.Repaint)
                return;

            EnsureCreated();

            if (m_LayoutDirty || m_Cache == null)
                RebuildLayout();

            TextMeshAnimator.Apply(m_Text, m_Cache, RangesFor(draft), time, 1f / 60f,
                FullyRevealed.Instance, RevealSettings.None, m_Clusters);

            FrameCamera(rect, zoom);

            m_Preview.camera.backgroundColor = background;
            m_Preview.BeginPreview(rect, GUIStyle.none);
            m_Preview.camera.Render();
            Texture rendered = m_Preview.EndPreview();
            GUI.DrawTexture(rect, rendered, ScaleMode.StretchToFill, false);
        }

        /// <summary>
        /// Shows the text at actual size: one point of the preview is one point of text,
        /// so a 36pt sample is drawn 36 points tall.
        /// </summary>
        /// <remarks>
        /// This used to zoom to fit, which quietly rescaled every effect. A short sample
        /// filled the preview whatever its point size, so a travel of a fraction of a
        /// pixel was blown up until it looked right -- and then did almost nothing once
        /// the same effect ran at the size text is actually read at. Whatever the preview
        /// shows has to be the thing that ships, even when that means an effect looking
        /// smaller in here than anyone would like.
        /// </remarks>
        private void FrameCamera(Rect rect, float zoom)
        {
            Bounds bounds = m_Text.bounds;

            m_Preview.camera.aspect = rect.width / Mathf.Max(1f, rect.height);
            m_Preview.camera.orthographicSize =
                Mathf.Max(1f, rect.height * 0.5f / Mathf.Max(0.1f, zoom));
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
