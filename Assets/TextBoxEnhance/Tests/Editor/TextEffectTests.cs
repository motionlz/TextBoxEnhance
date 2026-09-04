using NUnit.Framework;
using TextBoxEnhance;
using TextBoxEnhance.Effects;
using UnityEngine;

namespace TextBoxEnhance.Tests
{
    /// <summary>
    /// Effects are pure functions of their context, which is what makes them testable
    /// without a scene: none of the built-ins reads TextInfo, so a null one is fine.
    /// </summary>
    public sealed class TextEffectTests
    {
        private static TextEffectContext Context(int charIndex = 0, float time = 0f, float revealProgress = 1f)
        {
            return new TextEffectContext(null, charIndex, 0, 1, time, 1f / 60f, revealProgress);
        }

        [Test]
        public void WaveIsAtRestAtTimeZeroAndMovesLater()
        {
            var effect = new WaveEffect();

            CharacterMod atRest = CharacterMod.Identity;
            effect.Apply(Context(time: 0f), TagParams.Empty, ref atRest);
            Assert.AreEqual(0f, atRest.Offset.y, 1e-5f);

            CharacterMod moved = CharacterMod.Identity;
            effect.Apply(Context(time: 0.25f), TagParams.Empty, ref moved);
            Assert.Greater(Mathf.Abs(moved.Offset.y), 0.01f);
        }

        [Test]
        public void WaveAmplitudeIsBounded()
        {
            var effect = new WaveEffect();
            var parameters = new ParsedText();
            TextEffectParser.Parse("<wave a=0.2>x</wave>", parameters);
            TagParams tag = parameters.Effects[0].Parameters;

            for (int step = 0; step < 200; step++)
            {
                CharacterMod mod = CharacterMod.Identity;
                effect.Apply(Context(time: step * 0.01f), tag, ref mod);
                Assert.LessOrEqual(Mathf.Abs(mod.Offset.y), 0.2f + 1e-4f);
            }
        }

        [Test]
        public void ShakeIsStableForTheSameCharacterAndTime()
        {
            var effect = new ShakeEffect();

            CharacterMod first = CharacterMod.Identity;
            CharacterMod second = CharacterMod.Identity;
            effect.Apply(Context(charIndex: 7, time: 1.234f), TagParams.Empty, ref first);
            effect.Apply(Context(charIndex: 7, time: 1.234f), TagParams.Empty, ref second);

            Assert.AreEqual(first.Offset.x, second.Offset.x, 1e-6f, "same input must give the same offset");
            Assert.AreEqual(first.Offset.y, second.Offset.y, 1e-6f);
        }

        [Test]
        public void ShakeMovesNeighbouringCharactersDifferently()
        {
            var effect = new ShakeEffect();

            CharacterMod a = CharacterMod.Identity;
            CharacterMod b = CharacterMod.Identity;
            effect.Apply(Context(charIndex: 3, time: 1f), TagParams.Empty, ref a);
            effect.Apply(Context(charIndex: 4, time: 1f), TagParams.Empty, ref b);

            Assert.AreNotEqual(a.Offset.x, b.Offset.x, "characters must not rattle in lockstep");
        }

        [Test]
        public void RainbowSetsHueWithoutTouchingAlpha()
        {
            var effect = new RainbowEffect();
            CharacterMod mod = CharacterMod.Identity;
            effect.Apply(Context(time: 0.5f), TagParams.Empty, ref mod);

            Assert.Greater(mod.ColorOverrideWeight, 0f, "a hue should have been written");
            Assert.AreEqual(1f, mod.ColorMultiply.a, 1e-5f, "alpha belongs to the typewriter");
        }

        [Test]
        public void FadeStaysWithinItsRange()
        {
            var effect = new FadeEffect();
            var parsed = new ParsedText();
            TextEffectParser.Parse("<fade min=0.3 max=0.9>x</fade>", parsed);
            TagParams tag = parsed.Effects[0].Parameters;

            for (int step = 0; step < 200; step++)
            {
                CharacterMod mod = CharacterMod.Identity;
                effect.Apply(Context(time: step * 0.02f), tag, ref mod);
                Assert.GreaterOrEqual(mod.ColorMultiply.a, 0.3f - 1e-4f);
                Assert.LessOrEqual(mod.ColorMultiply.a, 0.9f + 1e-4f);
            }
        }

        [Test]
        public void TintReadsTheShorthandColour()
        {
            var parsed = new ParsedText();
            TextEffectParser.Parse("<tint=#FF8800>x</tint>", parsed);

            CharacterMod mod = CharacterMod.Identity;
            new TintEffect().Apply(Context(), parsed.Effects[0].Parameters, ref mod);

            Assert.AreEqual(1f, mod.ColorOverride.r, 0.01f);
            Assert.AreEqual(0f, mod.ColorOverride.b, 0.01f);
        }

        [Test]
        public void EveryDocumentedTagResolvesToAnEffect()
        {
            string[] tags =
            {
                "wave", "shake", "wobble", "wiggle", "jitter", "bounce", "swing", "rotate",
                "pulse", "rainbow", "tint", "fade", "blink",
            };

            foreach (string tag in tags)
                Assert.IsTrue(TextEffectRegistry.TryGet(tag, out _), $"<{tag}> is not registered");
        }

        [Test]
        public void ControlTagsAreKnownButAreNotEffects()
        {
            Assert.IsTrue(TextEffectRegistry.IsKnownTag("speed"));
            Assert.IsTrue(TextEffectRegistry.IsKnownTag("pause"));
            Assert.IsFalse(TextEffectRegistry.TryGet("speed", out _));
        }

        [Test]
        public void UnknownTagIsNotClaimed()
        {
            Assert.IsFalse(TextEffectRegistry.IsKnownTag("b"));
            Assert.IsFalse(TextEffectRegistry.IsKnownTag("color"));
            Assert.IsFalse(TextEffectRegistry.IsKnownTag("sprite"));
        }

        [Test]
        public void RevealEntranceIsNeutralOnceSettled()
        {
            CharacterMod mod = CharacterMod.Identity;
            RevealAnimator.Apply(RevealStyle.Pop, 1f, 0.5f, 1f, ref mod);

            Assert.AreEqual(Vector2.one, mod.Scale);
            Assert.AreEqual(1f, mod.ColorMultiply.a, 1e-5f);
            Assert.AreEqual(0f, mod.Rotation, 1e-5f);
        }

        [Test]
        public void RevealEntranceIsInvisibleAtTheStart()
        {
            CharacterMod mod = CharacterMod.Identity;
            RevealAnimator.Apply(RevealStyle.Fade, 0f, 0.5f, 1f, ref mod);

            Assert.AreEqual(0f, mod.ColorMultiply.a, 1e-5f);
        }
    }
}
