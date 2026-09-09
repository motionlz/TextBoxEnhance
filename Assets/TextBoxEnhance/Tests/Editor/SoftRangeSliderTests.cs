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
        public void PositionAndValueAreExactInverses()
        {
            foreach (float value in new[] { 0f, 0.02f, 0.06f, 0.15f, 0.5f, 1f })
            {
                float position = SoftRangeSlider.ToPosition(value, 0f, 1f, 2f);
                float back = SoftRangeSlider.ToValue(position, 0f, 1f, 2f);

                Assert.AreEqual(value, back, 1e-4f, $"round trip failed for {value}");
            }
        }

        [Test]
        public void HalfTheTrackCoversAQuarterOfTheRange()
        {
            // What squaring the response buys: the small values get the long half.
            Assert.AreEqual(0.25f, SoftRangeSlider.ToValue(0.5f, 0f, 1f, 2f), 1e-4f);
        }

        [Test]
        public void PresetValuesLandWellClearOfTheLeftEdge()
        {
            // The complaint that prompted the curve: every preset sat inside the first
            // sixth of the track, where a pixel of travel was most of the useful range.
            var presets = new (string Name, float Value, float Limit)[]
            {
                ("jitter", 0.05f, 1f),
                ("shake", 0.06f, 1f),
                ("swing", 12f, 180f),
                ("wave", 0.15f, 1f),
            };

            foreach ((string name, float value, float limit) in presets)
            {
                float position = SoftRangeSlider.ToPosition(value, 0f, limit, 2f);

                Assert.Greater(position, 0.2f, $"{name} is still bunched against the left edge");
                Assert.Less(position, 0.6f, $"{name} has been pushed too far along the track");
            }
        }

        [Test]
        public void ATrackSpanningZeroStaysLinear()
        {
            // Bending a track that reaches into negatives would put its fine end at the
            // most negative value instead of around zero.
            float position = SoftRangeSlider.ToPosition(0f, -4f, 4f, 2f);

            Assert.AreEqual(0.5f, position, 1e-4f, "zero should sit at the middle of a symmetric track");
        }

        [Test]
        public void ACollapsedSoftRangeStillHasWidth()
        {
            SoftRangeSlider.Range(0f, 0f, 0f, out float low, out float high);

            Assert.Greater(high, low, "a zero-width track would make the handle meaningless");
        }
    }
}
