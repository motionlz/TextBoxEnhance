using NUnit.Framework;
using TextBoxEnhance.Data;

namespace TextBoxEnhance.Tests
{
    /// <summary>
    /// Splitting a letter from the marks that sit on it, which is the one thing a script
    /// like Thai can do that Latin cannot: tone marks rattling over a word that holds
    /// still, or a word sliding out from under them.
    /// </summary>
    public sealed class LayerTargetTests
    {
        private static TextEffectContext Context(int letter, int character, bool isMark, float time)
        {
            return new TextEffectContext(null, letter, 0, 1, time, 1f / 60f, 1f, character, isMark);
        }

        private static EffectLayer Wave(LayerTarget target)
        {
            EffectLayer layer = EffectPresets.Wave()[0];
            layer.Target = target;
            return layer;
        }

        private static float OffsetY(EffectLayer layer, in TextEffectContext context)
        {
            CharacterMod mod = CharacterMod.Identity;
            layer.Apply(context, LayerScale.None, ref mod);
            return mod.Offset.y;
        }

        [Test]
        public void WholeLetterMovesBothTheBaseAndItsMarks()
        {
            EffectLayer layer = Wave(LayerTarget.WholeLetter);

            float onBase = OffsetY(layer, Context(letter: 0, character: 0, isMark: false, time: 0.3f));
            float onMark = OffsetY(layer, Context(letter: 0, character: 1, isMark: true, time: 0.3f));

            Assert.AreNotEqual(0f, onBase);
            Assert.AreEqual(onBase, onMark, 1e-5f, "a mark must travel with the letter it sits on");
        }

        [Test]
        public void BaseOnlyLeavesTheMarksWhereTheyAre()
        {
            EffectLayer layer = Wave(LayerTarget.BaseLetterOnly);

            Assert.AreNotEqual(0f, OffsetY(layer, Context(0, 0, false, 0.3f)));
            Assert.AreEqual(0f, OffsetY(layer, Context(0, 1, true, 0.3f)), 1e-6f);
        }

        [Test]
        public void MarksOnlyLeavesTheBaseWhereItIs()
        {
            EffectLayer layer = Wave(LayerTarget.MarksOnly);

            Assert.AreEqual(0f, OffsetY(layer, Context(0, 0, false, 0.3f)), 1e-6f);
            Assert.AreNotEqual(0f, OffsetY(layer, Context(0, 1, true, 0.3f)));
        }

        [Test]
        public void MarksOnTheSameLetterMoveIndependently()
        {
            // Two marks stacked on one consonant, a vowel and a tone mark. Sharing the
            // letter's phase would move them in lockstep with the base they are supposed
            // to be coming away from, which is not what asking for them separately means.
            EffectLayer layer = Wave(LayerTarget.MarksOnly);

            float first = OffsetY(layer, Context(letter: 0, character: 1, isMark: true, time: 0.3f));
            float second = OffsetY(layer, Context(letter: 0, character: 2, isMark: true, time: 0.3f));

            Assert.AreNotEqual(first, second, "both marks moved as one");
        }

        [Test]
        public void ShakeGivesEachMarkItsOwnRandomness()
        {
            EffectLayer layer = EffectPresets.Shake()[0];
            layer.Target = LayerTarget.MarksOnly;

            CharacterMod first = CharacterMod.Identity;
            CharacterMod second = CharacterMod.Identity;
            layer.Apply(Context(0, 1, true, 1f), LayerScale.None, ref first);
            layer.Apply(Context(0, 2, true, 1f), LayerScale.None, ref second);

            Assert.AreNotEqual(first.Offset.x, second.Offset.x, "the marks rattle in step");
        }

        [Test]
        public void LayersDefaultToMovingTheWholeLetter()
        {
            Assert.AreEqual(LayerTarget.WholeLetter, new EffectLayer().Target);
            Assert.AreEqual(LayerTarget.WholeLetter, EffectPresets.NewLayer().Target);

            foreach (EffectPresets.Preset preset in EffectPresets.All)
            {
                foreach (EffectLayer layer in preset.Build())
                    Assert.AreEqual(LayerTarget.WholeLetter, layer.Target, preset.Name + " splits letters");
            }
        }

        [Test]
        public void TagFlagsAimAnEffectAtPartOfALetter()
        {
            var parsed = new ParsedText();

            TextEffectParser.Parse("<shake marks>x</shake>", parsed);
            Assert.AreEqual(LayerTarget.MarksOnly, parsed.Effects[0].Target);

            TextEffectParser.Parse("<shake base>x</shake>", parsed);
            Assert.AreEqual(LayerTarget.BaseLetterOnly, parsed.Effects[0].Target);

            TextEffectParser.Parse("<shake>x</shake>", parsed);
            Assert.AreEqual(LayerTarget.WholeLetter, parsed.Effects[0].Target);
        }

        [Test]
        public void AimingFlagsDoNotDisturbTheOtherAttributes()
        {
            var parsed = new ParsedText();
            TextEffectParser.Parse("<shake marks a=0.2 f=30>x</shake>", parsed);

            Assert.AreEqual(LayerTarget.MarksOnly, parsed.Effects[0].Target);
            Assert.AreEqual(0.2f, parsed.Effects[0].Parameters.GetPrimaryFloat(0f, "a"), 1e-5f);
            Assert.AreEqual(30f, parsed.Effects[0].Parameters.GetFloat(0f, "f"), 1e-5f);
        }
    }
}
