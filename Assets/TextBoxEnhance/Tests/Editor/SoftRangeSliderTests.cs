using NUnit.Framework;
using TextBoxEnhance.EditorTools;

namespace TextBoxEnhance.Tests
{
    /// <summary>
    /// The slider's whole reason for existing is that its range bends: short enough to
    /// be precise for everyday values, but never a ceiling. These pin down that bend.
    /// </summary>
    public sealed class SoftRangeSliderTests
    {
        [Test]
        public void EverydayValuesGetExactlyTheSoftRange()
        {
            SoftRangeSlider.Range(0.15f, 0f, 0.4f, out float low, out float high);

            Assert.AreEqual(0f, low, 1e-5f);
            Assert.AreEqual(0.4f, high, 1e-5f);
        }

        [Test]
        public void TrackStretchesForAValueTypedPastTheSoftMaximum()
        {
            SoftRangeSlider.Range(0.8f, 0f, 0.4f, out float low, out float high);

            Assert.AreEqual(0f, low, 1e-5f);
            Assert.Greater(high, 0.8f, "the handle would be pinned to the end of the track");
        }

        [Test]
        public void TrackStretchesBelowZeroForANegativeValue()
        {
            // Negative speed runs an effect backwards, so the track has to follow it
            // there rather than clamping the value away.
            SoftRangeSlider.Range(-2f, 0f, 4f, out float low, out float high);

            Assert.Less(low, -2f, "the handle would be pinned to the start of the track");
            Assert.AreEqual(4f, high, 1e-5f);
        }

        [Test]
        public void ValueAtTheSoftMaximumStillLeavesTheHandleOnTheTrack()
        {
            SoftRangeSlider.Range(0.4f, 0f, 0.4f, out _, out float high);

            Assert.Greater(high, 0.4f);
        }

        [Test]
        public void ACollapsedSoftRangeStillHasWidth()
        {
            SoftRangeSlider.Range(0f, 0f, 0f, out float low, out float high);

            Assert.Greater(high, low, "a zero-width track would make the handle meaningless");
        }
    }
}
