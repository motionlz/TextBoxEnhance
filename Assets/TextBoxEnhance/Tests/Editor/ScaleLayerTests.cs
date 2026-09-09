using NUnit.Framework;
using TextBoxEnhance.Data;
using TextBoxEnhance.EditorTools;

namespace TextBoxEnhance.Tests
{
    /// <summary>
    /// Scale is the one channel measured around 1 rather than 0, and both of its
    /// failure modes look like a broken effect rather than a setting to change: a
    /// character mirrored halfway through a pulse, or one flattened out of existence
    /// because its resting value came from an offset layer.
    /// </summary>
    public sealed class ScaleLayerTests
    {
        private static TextEffectContext Context(float time)
        {
            return new TextEffectContext(null, 0, 0, 1, time, 1f / 60f, 1f);
        }

        private static EffectLayer SwingingThroughZero(bool allowFlip)
        {
            return new EffectLayer
            {
                Channel = EffectChannel.Scale,
                Motion = EffectMotion.Sine,
                Min = -0.5f,
                Max = 1.5f,
                Speed = 1f,
                AllowFlip = allowFlip,
            };
        }

        /// <summary>Three quarters through a sine cycle the motion sits at its minimum.</summary>
        private static CharacterMod AtTrough(EffectLayer layer)
        {
            CharacterMod mod = CharacterMod.Identity;
            layer.Apply(Context(0.75f), LayerScale.None, ref mod);
            return mod;
        }

        [Test]
        public void ScaleStopsAtZeroRatherThanMirroring()
        {
            CharacterMod mod = AtTrough(SwingingThroughZero(false));

            Assert.GreaterOrEqual(mod.Scale.x, 0f, "the character turned inside out");
            Assert.GreaterOrEqual(mod.Scale.y, 0f);
        }

        [Test]
        public void AllowFlipLetsTheScalePassThroughZero()
        {
            CharacterMod mod = AtTrough(SwingingThroughZero(true));

            Assert.Less(mod.Scale.x, 0f, "flipping was asked for and did not happen");
        }

        [Test]
        public void FlippingIsOffOnANewLayer()
        {
            Assert.IsFalse(new EffectLayer().AllowFlip);
        }

        [Test]
        public void ScaleRestsAtOneWhileEveryOtherChannelRestsAtZero()
        {
            Assert.AreEqual(1f, EffectLimits.Neutral(EffectChannel.Scale), 1e-5f);
            Assert.AreEqual(1f, EffectLimits.Neutral(EffectChannel.ScaleX), 1e-5f);
            Assert.AreEqual(0f, EffectLimits.Neutral(EffectChannel.OffsetY), 1e-5f);
            Assert.AreEqual(0f, EffectLimits.Neutral(EffectChannel.Rotation), 1e-5f);
        }

        [Test]
        public void AScaleLayerRestingAtOneLeavesTheCharacterAloneWithNoAmount()
        {
            // What someone expects from an amount of zero: text at its normal size.
            var layer = new EffectLayer
            {
                Channel = EffectChannel.Scale,
                Motion = EffectMotion.Sine,
                Min = 1f,
                Max = 1f,
                Speed = 1f,
            };

            CharacterMod mod = CharacterMod.Identity;
            layer.Apply(Context(0.3f), LayerScale.None, ref mod);

            Assert.AreEqual(1f, mod.Scale.x, 1e-5f);
            Assert.AreEqual(1f, mod.Scale.y, 1e-5f);
        }

        [Test]
        public void FlippingIsReachableFromTheScaleSlider()
        {
            // The bug this catches: with the amount track capped below 1, a scale layer
            // could never reach zero, so Allow flipping sat there switched on and doing
            // nothing at all. A toggle that cannot fire is worse than no toggle.
            float lowest = EffectLimits.Neutral(EffectChannel.Scale)
                           - EffectLimits.Amount(EffectChannel.Scale, EffectMotion.Sine);

            Assert.Less(lowest, 0f, "the scale can never cross zero, so flipping cannot happen");
        }

        [Test]
        public void AnAddedLayerIsGentlerThanEveryPreset()
        {
            // A layer lands next to whatever is already there. Arriving at full preset
            // strength swamps the effect being built instead of adding to it.
            EffectLayer added = EffectPresets.NewLayer();
            float addedReach = (added.Max - added.Min) * 0.5f;

            Assert.Greater(addedReach, 0f, "an added layer that does nothing looks broken too");

            // Measured against the wave, which is the ordinary case. Shake and jitter are
            // deliberately tiny, and being quieter than those would mean being invisible.
            EffectLayer wave = EffectPresets.Wave()[0];
            Assert.LessOrEqual(addedReach, (wave.Max - wave.Min) * 0.5f,
                "an added layer hits harder than the wave preset");
        }

        [Test]
        public void ThePulsePresetNeverMirrorsAcrossAWholeCycle()
        {
            EffectLayer layer = EffectPresets.Pulse()[0];

            for (int step = 0; step <= 60; step++)
            {
                CharacterMod mod = CharacterMod.Identity;
                layer.Apply(Context(step / 60f), LayerScale.None, ref mod);

                Assert.Greater(mod.Scale.x, 0f, $"pulse mirrored the character at step {step}");
            }
        }
    }
}
