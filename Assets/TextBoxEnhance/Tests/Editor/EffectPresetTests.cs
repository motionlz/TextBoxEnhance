using System.Collections.Generic;
using NUnit.Framework;
using TextBoxEnhance.Data;
using UnityEngine;

namespace TextBoxEnhance.Tests
{
    /// <summary>
    /// The load-bearing test for the data-driven effects. Layers are a vocabulary, and
    /// a vocabulary is only worth building an editor on top of if it can already say
    /// everything the hand-written effects say. Each preset is checked against the code
    /// effect it stands in for, across a spread of characters and moments.
    /// </summary>
    public sealed class EffectPresetTests
    {
        private readonly List<TextEffectAsset> m_Created = new List<TextEffectAsset>();

        [TearDown]
        public void TearDown()
        {
            foreach (TextEffectAsset asset in m_Created)
                Object.DestroyImmediate(asset);

            m_Created.Clear();
        }

        private TextEffectAsset AssetFrom(List<EffectLayer> layers)
        {
            var asset = ScriptableObject.CreateInstance<TextEffectAsset>();
            asset.Layers.Clear();
            asset.Layers.AddRange(layers);
            m_Created.Add(asset);
            return asset;
        }

        private static TextEffectContext Context(int charIndex, float time)
        {
            return new TextEffectContext(null, charIndex, 0, 1, time, 1f / 60f, 1f);
        }

        private static void AssertSameMod(CharacterMod expected, CharacterMod actual, string what)
        {
            const float tolerance = 1e-4f;

            Assert.AreEqual(expected.Offset.x, actual.Offset.x, tolerance, what + " offset X");
            Assert.AreEqual(expected.Offset.y, actual.Offset.y, tolerance, what + " offset Y");
            Assert.AreEqual(expected.Rotation, actual.Rotation, tolerance, what + " rotation");
            Assert.AreEqual(expected.Scale.x, actual.Scale.x, tolerance, what + " scale X");
            Assert.AreEqual(expected.Scale.y, actual.Scale.y, tolerance, what + " scale Y");
            Assert.AreEqual(expected.ColorMultiply.a, actual.ColorMultiply.a, tolerance, what + " alpha");
            Assert.AreEqual(expected.ColorOverrideWeight, actual.ColorOverrideWeight, tolerance, what + " tint weight");
            Assert.AreEqual(expected.ColorOverride.r, actual.ColorOverride.r, tolerance, what + " tint red");
            Assert.AreEqual(expected.ColorOverride.g, actual.ColorOverride.g, tolerance, what + " tint green");
            Assert.AreEqual(expected.ColorOverride.b, actual.ColorOverride.b, tolerance, what + " tint blue");
        }

        [Test]
        public void EveryPresetMatchesTheBuiltInItStandsFor(
            [ValueSource(nameof(PresetNames))] string presetName)
        {
            EffectPresets.Preset preset = FindPreset(presetName);
            Assert.IsTrue(TextEffectRegistry.TryGet(preset.BuiltInTag, out TextEffect builtIn),
                $"<{preset.BuiltInTag}> is not registered");

            TextEffectAsset asset = AssetFrom(preset.Build());

            // A spread of characters and moments, so a preset cannot pass by being right
            // only at t=0 or only for the first letter.
            foreach (int charIndex in new[] { 0, 1, 5, 23 })
            {
                foreach (float time in new[] { 0f, 0.07f, 0.5f, 1.3f, 4.75f })
                {
                    TextEffectContext context = Context(charIndex, time);

                    CharacterMod expected = CharacterMod.Identity;
                    builtIn.Apply(in context, TagParams.Empty, ref expected);

                    CharacterMod actual = CharacterMod.Identity;
                    asset.Apply(in context, LayerScale.None, ref actual);

                    AssertSameMod(expected, actual, $"<{preset.BuiltInTag}> char {charIndex} at t={time}");
                }
            }
        }

        public static IEnumerable<string> PresetNames()
        {
            foreach (EffectPresets.Preset preset in EffectPresets.All)
                yield return preset.Name;
        }

        private static EffectPresets.Preset FindPreset(string name)
        {
            foreach (EffectPresets.Preset preset in EffectPresets.All)
            {
                if (preset.Name == name)
                    return preset;
            }

            throw new AssertionException("No preset named " + name);
        }

        [Test]
        public void PresetsCoverEveryBuiltInEffect()
        {
            var covered = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            foreach (EffectPresets.Preset preset in EffectPresets.All)
                covered.Add(preset.BuiltInTag);

            // Aliases point at the same effect instance, so one preset covers both.
            var aliases = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
            {
                "wiggle", "rotate",
            };

            foreach (string tag in TextEffectRegistry.TagNames)
            {
                if (aliases.Contains(tag))
                    continue;

                Assert.IsTrue(covered.Contains(tag),
                    $"<{tag}> has no preset, so the effect editor cannot offer it as a starting point");
            }
        }

        [Test]
        public void BuildReturnsAFreshStackEachTime()
        {
            List<EffectLayer> first = EffectPresets.Wave();
            List<EffectLayer> second = EffectPresets.Wave();

            first[0].Max = 99f;

            Assert.AreNotEqual(99f, second[0].Max, "presets must not hand out shared layer instances");
        }
    }
}
