using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace TextBoxEnhance.Tests
{
    /// <summary>
    /// End-to-end checks against a real TextMeshPro label: the typewriter has to run
    /// off the game loop and the effects have to actually move vertices, neither of
    /// which the pure unit tests can see.
    /// </summary>
    public sealed class AnimatedTextBoxPlayTests
    {
        private GameObject m_Root;
        private TextMeshProUGUI m_Label;
        private AnimatedTextBox m_Box;

        [SetUp]
        public void SetUp()
        {
            m_Root = new GameObject("Canvas", typeof(Canvas));
            var canvas = m_Root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var labelObject = new GameObject("Label", typeof(RectTransform));
            labelObject.transform.SetParent(m_Root.transform, false);
            labelObject.GetComponent<RectTransform>().sizeDelta = new Vector2(800f, 400f);

            m_Label = labelObject.AddComponent<TextMeshProUGUI>();
            m_Label.fontSize = 36f;

            m_Box = labelObject.AddComponent<AnimatedTextBox>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(m_Root);
        }

        private static float VertexY(TMP_TextInfo textInfo, int charIndex)
        {
            TMP_CharacterInfo character = textInfo.characterInfo[charIndex];
            return textInfo.meshInfo[character.materialReferenceIndex].vertices[character.vertexIndex].y;
        }

        private static byte VertexAlpha(TMP_TextInfo textInfo, int charIndex)
        {
            TMP_CharacterInfo character = textInfo.characterInfo[charIndex];
            return textInfo.meshInfo[character.materialReferenceIndex].colors32[character.vertexIndex].a;
        }

        [UnityTest]
        public IEnumerator TypewriterRevealsCharactersOverTime()
        {
            m_Box.CharactersPerSecond = 20f;
            m_Box.Text = "abcdefghij";
            yield return null;

            Assert.AreEqual(10, m_Box.TotalCharacters);
            Assert.IsTrue(m_Box.IsRevealing);

            yield return new WaitForSeconds(0.25f);

            Assert.Greater(m_Box.RevealedCharacters, 0, "nothing was revealed");
            Assert.Less(m_Box.RevealedCharacters, 10, "the whole line appeared at once");
        }

        [UnityTest]
        public IEnumerator RevealCompletesAndRaisesItsEvent()
        {
            bool completed = false;
            m_Box.OnRevealCompleted.AddListener(() => completed = true);

            m_Box.CharactersPerSecond = 100f;
            m_Box.Text = "abcdefghij";

            yield return new WaitForSeconds(1f);

            Assert.AreEqual(10, m_Box.RevealedCharacters);
            Assert.IsFalse(m_Box.IsRevealing);
            Assert.IsTrue(completed, "OnRevealCompleted never fired");
        }

        [UnityTest]
        public IEnumerator SkipToEndRevealsEverythingImmediately()
        {
            m_Box.CharactersPerSecond = 2f;
            m_Box.Text = "abcdefghij";
            yield return null;

            m_Box.SkipToEnd();
            yield return null;

            Assert.AreEqual(10, m_Box.RevealedCharacters);
            Assert.IsFalse(m_Box.IsRevealing);
        }

        [UnityTest]
        public IEnumerator UnrevealedCharactersAreTransparent()
        {
            m_Box.CharactersPerSecond = 2f;
            m_Box.Text = "abcdefghij";
            yield return null;
            yield return null;

            Assert.AreEqual(0, VertexAlpha(m_Label.textInfo, 9), "the last character should still be hidden");
        }

        [UnityTest]
        public IEnumerator RewindHidesEverythingAgain()
        {
            m_Box.CharactersPerSecond = 100f;
            m_Box.Text = "abcdefghij";
            yield return new WaitForSeconds(0.5f);

            m_Box.Rewind();
            yield return null;

            Assert.AreEqual(0, m_Box.RevealedCharacters);
            Assert.AreEqual(0, VertexAlpha(m_Label.textInfo, 0));
        }

        [UnityTest]
        public IEnumerator WaveMovesVerticesWithoutMovingUntaggedText()
        {
            m_Box.CharactersPerSecond = 0f;
            m_Box.Text = "AA<wave a=0.4>AA</wave>";
            yield return null;

            float plainBefore = VertexY(m_Label.textInfo, 0);
            float wavedBefore = VertexY(m_Label.textInfo, 3);

            yield return new WaitForSeconds(0.2f);

            Assert.AreEqual(plainBefore, VertexY(m_Label.textInfo, 0), 1e-3f,
                "characters outside the tag must not move");
            Assert.AreNotEqual(wavedBefore, VertexY(m_Label.textInfo, 3),
                "the tagged characters did not move");
        }

        [UnityTest]
        public IEnumerator EffectsOscillateRatherThanDrift()
        {
            // The component restores TMP's own geometry every frame. If it did not, each
            // frame's offset would be applied to the previous frame's result and the
            // character would wander off instead of swinging around its layout position.
            const float amplitudeEm = 0.15f;
            const float fontSize = 36f;
            float peakToPeak = 2f * amplitudeEm * fontSize;

            m_Box.CharactersPerSecond = 0f;
            m_Box.Text = "<wave>AAAA</wave>";
            yield return null;

            float lowest = float.MaxValue;
            float highest = float.MinValue;

            // Sampled over a wall-clock window rather than a frame count: batch mode
            // runs frames far faster than a build, and the wave is driven by time.
            // The default frequency of 6 rad/s puts one cycle at just over a second.
            float deadline = Time.unscaledTime + 1.5f;
            while (Time.unscaledTime < deadline)
            {
                float y = VertexY(m_Label.textInfo, 0);
                lowest = Mathf.Min(lowest, y);
                highest = Mathf.Max(highest, y);
                yield return null;
            }

            Assert.Less(highest - lowest, peakToPeak * 2f,
                "the character drifted instead of oscillating around its layout position");
            Assert.Greater(highest - lowest, peakToPeak * 0.5f, "the wave barely moved");
        }

        [UnityTest]
        public IEnumerator TextMeshProTagsSurviveTheParser()
        {
            m_Box.CharactersPerSecond = 0f;
            m_Box.Text = "<b>ab</b><wave>cd</wave>";
            yield return null;

            Assert.AreEqual(4, m_Box.TotalCharacters, "rich text tags should not count as characters");
            Assert.AreEqual("<b>ab</b>cd", m_Label.text);
        }

        [UnityTest]
        public IEnumerator PauseTagDelaysTheReveal()
        {
            m_Box.CharactersPerSecond = 50f;
            m_Box.Text = "ab<pause=5>cdefghij";
            yield return new WaitForSeconds(0.5f);

            Assert.AreEqual(2, m_Box.RevealedCharacters, "the pause should still be holding");
        }

        [UnityTest]
        public IEnumerator CharacterRevealedEventReportsAscendingIndices()
        {
            var seen = new System.Collections.Generic.List<int>();
            m_Box.OnCharacterRevealed.AddListener(seen.Add);

            m_Box.CharactersPerSecond = 60f;
            m_Box.Text = "abcde";

            yield return new WaitForSeconds(0.6f);

            Assert.AreEqual(new[] { 0, 1, 2, 3, 4 }, seen);
        }
    }
}
