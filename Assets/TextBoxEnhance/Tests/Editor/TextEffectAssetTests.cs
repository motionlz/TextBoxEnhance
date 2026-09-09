using NUnit.Framework;
using TextBoxEnhance.Data;
using UnityEngine;

namespace TextBoxEnhance.Tests
{
    /// <summary>
    /// How a data-driven effect responds to the tag that called it, and how a library
    /// gets its effects into the registry.
    /// </summary>
    public sealed class TextEffectAssetTests
    {
        private TextEffectAsset m_Asset;
        private TextEffectLibrary m_Library;

        [TearDown]
        public void TearDown()
        {
            if (m_Library != null)
                Object.DestroyImmediate(m_Library);

            if (m_Asset != null)
                Object.DestroyImmediate(m_Asset);

            m_Library = null;
            m_Asset = null;

            // The library writes to global state, so put the registry back for the next test.
            TextEffectRegistry.Reset();
        }

        private TextEffectAsset MakeAsset(string tag)
        {
            m_Asset = ScriptableObject.CreateInstance<TextEffectAsset>();
            m_Asset.name = tag;
            m_Asset.Layers.Clear();
            m_Asset.Layers.AddRange(EffectPresets.Wave());

            SerializedTagOf(m_Asset, tag);
            return m_Asset;
        }

        /// <summary>The tag is serialized and private, so tests set it the way the inspector would.</summary>
        private static void SerializedTagOf(TextEffectAsset asset, string tag)
        {
            var serialized = new UnityEditor.SerializedObject(asset);
            serialized.FindProperty("m_Tag").stringValue = tag;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static TextEffectContext Context(int charIndex, float time)
        {
            return new TextEffectContext(null, charIndex, 0, 1, time, 1f / 60f, 1f);
        }

        private static float OffsetYAt(TextEffectAsset asset, LayerScale scale, float time)
        {
            CharacterMod mod = CharacterMod.Identity;
            asset.Apply(Context(0, time), scale, ref mod);
            return mod.Offset.y;
        }

        [Test]
        public void AmountScaleWidensTheTravelWithoutShiftingIt()
        {
            TextEffectAsset asset = MakeAsset("scaled");

            // A quarter into the sine is its peak, which is where doubling shows cleanly.
            float peakTime = 0.25f / asset.Layers[0].Speed;

            float normal = OffsetYAt(asset, LayerScale.None, peakTime);
            float doubled = OffsetYAt(asset, new LayerScale(2f, 1f, 1f), peakTime);

            Assert.AreEqual(normal * 2f, doubled, 1e-4f, "amount should double the reach");

            // The midpoint is what the character sits at when the motion is halfway, and
            // scaling must leave it alone or the text drifts off its baseline.
            float restingNormal = OffsetYAt(asset, LayerScale.None, 0f);
            float restingDoubled = OffsetYAt(asset, new LayerScale(2f, 1f, 1f), 0f);

            Assert.AreEqual(restingNormal, restingDoubled, 1e-4f, "the resting position must not move");
        }

        [Test]
        public void SpeedScaleAdvancesTheMotionFaster()
        {
            TextEffectAsset asset = MakeAsset("scaled");

            float halfTime = 0.5f;
            float single = OffsetYAt(asset, LayerScale.None, halfTime);
            float doubled = OffsetYAt(asset, new LayerScale(1f, 2f, 1f), halfTime);
            float singleAtDoubleTime = OffsetYAt(asset, LayerScale.None, halfTime * 2f);

            Assert.AreEqual(singleAtDoubleTime, doubled, 1e-4f,
                "twice the speed at t should match once the speed at 2t");
            Assert.AreNotEqual(single, doubled, "speed had no effect");
        }

        [Test]
        public void TagAttributesFeedTheScale()
        {
            var parsed = new ParsedText();
            TextEffectParser.Parse("<wave a=2 f=3 w=4>x</wave>", parsed);

            LayerScale scale = LayerScale.FromTag(parsed.Effects[0].Parameters);

            Assert.AreEqual(2f, scale.Amount, 1e-5f);
            Assert.AreEqual(3f, scale.Speed, 1e-5f);
            Assert.AreEqual(4f, scale.Spread, 1e-5f);
        }

        [Test]
        public void ShorthandSetsAmountOnly()
        {
            var parsed = new ParsedText();
            TextEffectParser.Parse("<wave=2>x</wave>", parsed);

            LayerScale scale = LayerScale.FromTag(parsed.Effects[0].Parameters);

            Assert.AreEqual(2f, scale.Amount, 1e-5f);
            Assert.AreEqual(1f, scale.Speed, 1e-5f, "the shorthand must not touch speed");
            Assert.AreEqual(1f, scale.Spread, 1e-5f);
        }

        [Test]
        public void LibraryRegistersItsEffectsUnderTheirTags()
        {
            TextEffectAsset asset = MakeAsset("sparkle");

            m_Library = ScriptableObject.CreateInstance<TextEffectLibrary>();
            m_Library.Effects.Add(asset);
            m_Library.Reregister();

            Assert.IsTrue(TextEffectRegistry.TryGet("sparkle", out TextEffect effect));
            Assert.IsInstanceOf<DataTextEffect>(effect);
            Assert.AreSame(asset, ((DataTextEffect)effect).Asset);
        }

        [Test]
        public void RegisteredAssetTagIsRecognisedByTheParser()
        {
            TextEffectAsset asset = MakeAsset("sparkle");

            m_Library = ScriptableObject.CreateInstance<TextEffectLibrary>();
            m_Library.Effects.Add(asset);
            m_Library.Reregister();

            var parsed = new ParsedText();
            TextEffectParser.Parse("ab<sparkle>cd</sparkle>", parsed);

            Assert.AreEqual("abcd", parsed.Text, "the custom tag should be stripped like any other");
            Assert.AreEqual(1, parsed.Effects.Count);
            Assert.AreEqual(2, parsed.Effects[0].Start);
            Assert.AreEqual(4, parsed.Effects[0].End);
        }

        [Test]
        public void ResetRestoresTheBuiltInsAndDropsAssets()
        {
            TextEffectAsset asset = MakeAsset("sparkle");

            m_Library = ScriptableObject.CreateInstance<TextEffectLibrary>();
            m_Library.Effects.Add(asset);
            m_Library.Reregister();

            TextEffectRegistry.Reset();

            Assert.IsFalse(TextEffectRegistry.TryGet("sparkle", out _), "asset effects should be gone");
            Assert.IsTrue(TextEffectRegistry.TryGet("wave", out _), "built-ins should come back");
        }

        [Test]
        public void BuiltInTagsAreReportedAsBuiltIn()
        {
            Assert.IsTrue(TextEffectRegistry.IsBuiltIn("wave"));
            Assert.IsFalse(TextEffectRegistry.IsBuiltIn("sparkle"));
        }
    }
}
