using NUnit.Framework;
using TMPro;

namespace TextBoxEnhance.Tests
{
    /// <summary>
    /// Thai writes a syllable as a consonant plus marks that hang above or below it with
    /// no width of their own, and TextMeshPro lays each one out as its own character.
    /// Animating them separately pulls a vowel off the letter it belongs to, so these
    /// pin down the grouping that stops it.
    /// </summary>
    public sealed class TextClusterTests
    {
        // Written as escapes rather than glyphs so the test cannot be broken by a file
        // being saved in the wrong encoding.
        private const char ThaiKoKai = '\u0E01';        // consonant
        private const char ThaiSaraI = '\u0E34';        // vowel, sits above
        private const char ThaiNoNu = '\u0E19';         // consonant
        private const char ThaiMaiEk = '\u0E48';        // tone mark, sits above
        private const char CombiningAcute = '\u0301';   // Latin accent

        private static TMP_TextInfo TextInfoOf(params char[] characters)
        {
            var info = new TMP_TextInfo
            {
                characterInfo = new TMP_CharacterInfo[characters.Length],
                characterCount = characters.Length,
            };

            for (int i = 0; i < characters.Length; i++)
                info.characterInfo[i].character = characters[i];

            return info;
        }

        private static int Build(TMP_TextInfo info, out int[] clusterOf, out int[] baseOf)
        {
            clusterOf = null;
            baseOf = null;
            return TextClusters.Build(info, ref clusterOf, ref baseOf);
        }

        [Test]
        public void ThaiVowelsAndToneMarksAreCombining()
        {
            Assert.IsTrue(TextClusters.IsCombining(ThaiSaraI));
            Assert.IsTrue(TextClusters.IsCombining(ThaiMaiEk));
            Assert.IsTrue(TextClusters.IsCombining(CombiningAcute));
        }

        [Test]
        public void ConsonantsAndLatinLettersAreNot()
        {
            Assert.IsFalse(TextClusters.IsCombining(ThaiKoKai));
            Assert.IsFalse(TextClusters.IsCombining(ThaiNoNu));
            Assert.IsFalse(TextClusters.IsCombining('a'));
            Assert.IsFalse(TextClusters.IsCombining(' '));
        }

        [Test]
        public void AMarkJoinsTheLetterBeforeIt()
        {
            // "กิน" is three code points and two letters.
            int clusters = Build(TextInfoOf(ThaiKoKai, ThaiSaraI, ThaiNoNu),
                out int[] clusterOf, out int[] baseOf);

            Assert.AreEqual(2, clusters);
            Assert.AreEqual(new[] { 0, 0, 1 }, new[] { clusterOf[0], clusterOf[1], clusterOf[2] });
            Assert.AreEqual(new[] { 0, 0, 2 }, new[] { baseOf[0], baseOf[1], baseOf[2] });
        }

        [Test]
        public void SeveralMarksStackOntoTheSameLetter()
        {
            // A vowel and a tone mark both sit on one consonant.
            int clusters = Build(TextInfoOf(ThaiKoKai, ThaiSaraI, ThaiMaiEk),
                out int[] clusterOf, out int[] baseOf);

            Assert.AreEqual(1, clusters);
            Assert.AreEqual(0, clusterOf[2]);
            Assert.AreEqual(0, baseOf[2]);
        }

        [Test]
        public void PlainTextIsOneClusterPerCharacter()
        {
            int clusters = Build(TextInfoOf('a', 'b', 'c'), out int[] clusterOf, out int[] baseOf);

            Assert.AreEqual(3, clusters);
            Assert.AreEqual(2, clusterOf[2]);
            Assert.AreEqual(2, baseOf[2]);
        }

        [Test]
        public void ALeadingMarkStandsOnItsOwn()
        {
            // Malformed text, but it must not walk off the front of the array.
            int clusters = Build(TextInfoOf(ThaiSaraI, ThaiKoKai), out int[] clusterOf, out int[] baseOf);

            Assert.AreEqual(2, clusters);
            Assert.AreEqual(0, baseOf[0]);
            Assert.AreEqual(1, baseOf[1]);
        }

        [Test]
        public void EmptyTextHasNoClusters()
        {
            Assert.AreEqual(0, Build(TextInfoOf(), out _, out _));
        }

        [Test]
        public void AnUnmappedMapLeavesEveryCharacterAlone()
        {
            ClusterMap map = ClusterMap.OnePerCharacter;

            Assert.AreEqual(7, map.ClusterFor(7));
            Assert.AreEqual(7, map.BaseFor(7));
        }

        [Test]
        public void TheMapReportsWhatBuildFound()
        {
            int clusters = Build(TextInfoOf(ThaiKoKai, ThaiSaraI, ThaiNoNu),
                out int[] clusterOf, out int[] baseOf);
            var map = new ClusterMap(clusterOf, baseOf, clusters);

            Assert.AreEqual(2, map.Count);
            Assert.AreEqual(0, map.ClusterFor(1), "the vowel belongs to the first letter");
            Assert.AreEqual(0, map.BaseFor(1), "and pivots on the consonant");
            Assert.AreEqual(1, map.ClusterFor(2));
        }
    }
}
