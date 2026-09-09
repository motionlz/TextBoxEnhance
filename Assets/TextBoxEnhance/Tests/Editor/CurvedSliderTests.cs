using NUnit.Framework;
using TextBoxEnhance.EditorTools;
using UnityEngine;

namespace TextBoxEnhance.Tests
{
    /// <summary>
    /// The slider's whole reason for existing is where it puts the small values, and
    /// its whole failure mode is a track that argues with the number on it. Both are
    /// pinned down here.
    /// </summary>
    public sealed class CurvedSliderTests
    {
        private const float Response = CurvedSlider.Response;

        [Test]
        public void HalfTheTrackCoversAQuarterOfTheRange()
        {
            // What squaring the response buys: the small values get the long half.
            Assert.AreEqual(0.25f, CurvedSlider.ToValue(0.5f, 0f, 1f, Response), 1e-4f);
        }

        [Test]
        public void PositionAndValueAreExactInverses()
        {
            foreach (float value in new[] { 0f, 0.02f, 0.06f, 0.15f, 0.5f, 1f })
            {
                float position = CurvedSlider.ToPosition(value, 0f, 1f, Response);
                Assert.AreEqual(value, CurvedSlider.ToValue(position, 0f, 1f, Response), 1e-4f,
                    $"round trip failed for {value}");
            }
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
                float position = CurvedSlider.ToPosition(value, 0f, limit, Response);

                Assert.Greater(position, 0.2f, $"{name} is still bunched against the left edge");
                Assert.Less(position, 0.6f, $"{name} has been pushed too far along the track");
            }
        }

        [Test]
        public void TheEndOfTheTrackIsTheLimitAndStaysThere()
        {
            // The bug this replaced: dragging to the end raised the value, which raised
            // the end, which let the next drag raise it again -- for ever.
            float atEnd = CurvedSlider.ToValue(1f, 0f, 1f, Response);
            Assert.AreEqual(1f, atEnd, 1e-4f);

            float stillAtEnd = CurvedSlider.ToPosition(atEnd, 0f, 1f, Response);
            Assert.AreEqual(1f, stillAtEnd, 1e-4f, "the handle moved even though the value did not");
        }

        [Test]
        public void ValuesBeyondTheLimitAreBroughtBack()
        {
            Assert.AreEqual(1f, CurvedSlider.ToPosition(5f, 0f, 1f, Response), 1e-4f);
            Assert.AreEqual(0f, CurvedSlider.ToPosition(-5f, 0f, 1f, Response), 1e-4f);
        }

        [Test]
        public void ZeroSitsAtTheMiddleOfASymmetricTrack()
        {
            Assert.AreEqual(0.5f, CurvedSlider.ToPosition(0f, -8f, 8f, Response), 1e-4f);
            Assert.AreEqual(0f, CurvedSlider.ToValue(0.5f, -8f, 8f, Response), 1e-4f);
        }

        [Test]
        public void ASymmetricTrackCurvesOutwardFromZeroBothWays()
        {
            // Negative speed runs an effect backwards, and deserves the same fine
            // control near zero that the positive side gets.
            float positive = CurvedSlider.ToValue(0.75f, -8f, 8f, Response);
            float negative = CurvedSlider.ToValue(0.25f, -8f, 8f, Response);

            Assert.AreEqual(2f, positive, 1e-3f, "half of the positive side should be a quarter of it");
            Assert.AreEqual(-2f, negative, 1e-3f, "the negative side should mirror the positive one");
        }

        [Test]
        public void SymmetricPositionAndValueAreExactInverses()
        {
            foreach (float value in new[] { -8f, -3f, -0.2f, 0f, 0.2f, 3f, 8f })
            {
                float position = CurvedSlider.ToPosition(value, -8f, 8f, Response);
                Assert.AreEqual(value, CurvedSlider.ToValue(position, -8f, 8f, Response), 1e-3f,
                    $"round trip failed for {value}");
            }
        }

        [Test]
        public void ACollapsedRangeDoesNotDivideByZero()
        {
            Assert.AreEqual(0f, CurvedSlider.ToPosition(0f, 0f, 0f, Response), 1e-4f);
            Assert.IsFalse(float.IsNaN(CurvedSlider.ToValue(0.5f, 0f, 0f, Response)));
        }

        [Test]
        public void EveryPresetFitsInsideItsOwnTrack()
        {
            // A limit that clipped a preset would quietly change what that preset does,
            // and the clipping would only show up as an effect that looks wrong.
            foreach (Data.EffectPresets.Preset preset in Data.EffectPresets.All)
            {
                foreach (Data.EffectLayer layer in preset.Build())
                {
                    Assert.LessOrEqual(Mathf.Abs(layer.Speed), EffectLimits.Speed(layer.Motion),
                        $"{preset.Name} speed {layer.Speed} does not fit its track");

                    Assert.LessOrEqual(Mathf.Abs(layer.Spread), EffectLimits.Spread,
                        $"{preset.Name} offset per letter does not fit its track");

                    Assert.LessOrEqual((layer.Max - layer.Min) * 0.5f,
                        EffectLimits.Amount(layer.Channel, layer.Motion),
                        $"{preset.Name} amount does not fit its track");
                }
            }
        }

        [Test]
        public void InstantMotionsGetAShorterAmountTrack()
        {
            // A fifth of an em travelled smoothly is a wave; the same distance jumped
            // twice a second is text nobody can read, so the track has to be shorter.
            float smooth = EffectLimits.Amount(Data.EffectChannel.OffsetY, Data.EffectMotion.Sine);
            float instant = EffectLimits.Amount(Data.EffectChannel.OffsetY, Data.EffectMotion.Blink);

            Assert.Less(instant, smooth);
        }

        [Test]
        public void SmallOffsetsForInstantMotionsSpreadAcrossTheTrack()
        {
            // The readable range for a jumping offset is roughly 0 to 0.03, and it needs
            // enough of the track to be dialled rather than guessed at.
            float limit = EffectLimits.Amount(Data.EffectChannel.OffsetX, Data.EffectMotion.Blink);

            Assert.Greater(CurvedSlider.ToPosition(0.03f, 0f, limit, Response), 0.4f,
                "the readable range is still crammed against the left edge");
        }
    }
}
