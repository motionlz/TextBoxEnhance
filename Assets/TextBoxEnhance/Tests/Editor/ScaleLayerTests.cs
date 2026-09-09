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
