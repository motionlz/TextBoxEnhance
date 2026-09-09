using System.Collections.Generic;
using NUnit.Framework;
using TextBoxEnhance.Data;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace TextBoxEnhance.Tests
{
    /// <summary>
    /// The effect editor's preview stands on one assumption worth pinning down: that a
    /// TextMeshPro object living in a PreviewRenderUtility scene lays text out normally
    /// and lets its vertices be rewritten. If that ever stops holding, the preview goes
    /// blank or stops animating, and the failure looks like a bug in whatever effect
    /// happened to be open at the time.
    /// </summary>
    public sealed class PreviewRenderingTests
    {
        private PreviewRenderUtility m_Preview;
        private GameObject m_Object;
        private TextMeshPro m_Text;

        [SetUp]
        public void SetUp()
        {
            m_Preview = new PreviewRenderUtility();
            m_Object = new GameObject("Preview Text") { hideFlags = HideFlags.HideAndDontSave };
            m_Text = m_Object.AddComponent<TextMeshPro>();
            m_Preview.AddSingleGO(m_Object);
        }

        [TearDown]
        public void TearDown()
        {
            if (m_Object != null)
                Object.DestroyImmediate(m_Object);

            m_Preview?.Cleanup();
            m_Preview = null;
        }

        private void Lay(string text)
        {
            m_Text.fontSize = 36f;
            m_Text.text = text;
            m_Text.ForceMeshUpdate();
        }

        private float VertexY(int charIndex)
        {
            TMP_CharacterInfo character = m_Text.textInfo.characterInfo[charIndex];
            return m_Text.textInfo.meshInfo[character.materialReferenceIndex]
                .vertices[character.vertexIndex].y;
        }

        [Test]
        public void TextLaysOutInsideAPreviewScene()
        {
            Lay("AAAA");

            Assert.AreEqual(4, m_Text.textInfo.characterCount);
            Assert.IsTrue(m_Text.textInfo.characterInfo[0].isVisible);
            Assert.Greater(m_Text.bounds.size.x, 0f, "the text has no width, so nothing would render");
        }

        [Test]
        public void GeometryCanBeCachedAndRestored()
        {
            Lay("AAAA");

            TMP_MeshInfo[] cache = TextMeshAnimator.Cache(m_Text);
            Assert.IsNotNull(cache);
            Assert.IsTrue(TextMeshAnimator.Restore(m_Text, cache), "the cache did not match the layout");
        }

        [Test]
        public void ADraftEffectMovesTheVerticesOfPreviewText()
        {
            Lay("AAAA");

            TMP_MeshInfo[] cache = TextMeshAnimator.Cache(m_Text);
            var asset = ScriptableObject.CreateInstance<TextEffectAsset>();

            try
            {
                asset.Layers.AddRange(EffectPresets.Wave());

                var ranges = new List<EffectRange>
                {
                    new EffectRange
                    {
                        Effect = new DataTextEffect(asset),
                        Parameters = TagParams.Empty,
                        Start = 0,
                        End = m_Text.textInfo.characterCount,
                    },
                };

                // A quarter through the cycle is the peak, so the displacement is at its
                // largest and cannot be mistaken for rounding.
                float peak = 0.25f / asset.Layers[0].Speed;

                Assert.IsTrue(TextMeshAnimator.Apply(m_Text, cache, ranges, 0f, 1f / 60f,
                    FullyRevealed.Instance, RevealSettings.None));
                float resting = VertexY(0);

                Assert.IsTrue(TextMeshAnimator.Apply(m_Text, cache, ranges, peak, 1f / 60f,
                    FullyRevealed.Instance, RevealSettings.None));
                float lifted = VertexY(0);

                // 0.15 em at 36pt is 5.4 units, and the preset peaks at exactly that.
                Assert.AreEqual(5.4f, lifted - resting, 0.2f,
                    "the preview did not move the text by the amount the effect asks for");
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void ApplyingTwiceFromTheSameCacheDoesNotAccumulate()
        {
            Lay("AAAA");

            TMP_MeshInfo[] cache = TextMeshAnimator.Cache(m_Text);
            var asset = ScriptableObject.CreateInstance<TextEffectAsset>();

            try
            {
                asset.Layers.AddRange(EffectPresets.Wave());

                var ranges = new List<EffectRange>
                {
                    new EffectRange
                    {
                        Effect = new DataTextEffect(asset),
                        Parameters = TagParams.Empty,
                        Start = 0,
                        End = m_Text.textInfo.characterCount,
                    },
                };

                float peak = 0.25f / asset.Layers[0].Speed;

                TextMeshAnimator.Apply(m_Text, cache, ranges, peak, 1f / 60f, FullyRevealed.Instance, RevealSettings.None);
                float once = VertexY(0);

                TextMeshAnimator.Apply(m_Text, cache, ranges, peak, 1f / 60f, FullyRevealed.Instance, RevealSettings.None);
                float twice = VertexY(0);

                Assert.AreEqual(once, twice, 1e-3f, "the same moment drew differently the second time");
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }
    }
}
