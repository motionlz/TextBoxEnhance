using System.Globalization;
using TMPro;

namespace TextBoxEnhance
{
    /// <summary>
    /// Which cluster each laid-out character belongs to, and which character leads it.
    /// </summary>
    /// <remarks>
    /// The default value is a map where every character stands alone, which is exactly
    /// right for text with no combining marks in it.
    /// </remarks>
    public readonly struct ClusterMap
    {
        private readonly int[] m_ClusterOf;
        private readonly int[] m_BaseOf;

        /// <summary>How many clusters the text contains, or 0 for an unmapped text.</summary>
        public readonly int Count;

        public ClusterMap(int[] clusterOf, int[] baseOf, int count)
        {
            m_ClusterOf = clusterOf;
            m_BaseOf = baseOf;
            Count = count;
        }

        /// <summary>Every character is its own cluster.</summary>
        public static ClusterMap OnePerCharacter => default;

        /// <summary>The cluster a character belongs to.</summary>
        public int ClusterFor(int charIndex)
        {
            return m_ClusterOf != null && charIndex >= 0 && charIndex < m_ClusterOf.Length
                ? m_ClusterOf[charIndex]
                : charIndex;
        }

        /// <summary>
        /// The character that leads a character's cluster: itself, unless it is a mark
        /// hanging off the one before it.
        /// </summary>
        public int BaseFor(int charIndex)
        {
            return m_BaseOf != null && charIndex >= 0 && charIndex < m_BaseOf.Length
                ? m_BaseOf[charIndex]
                : charIndex;
        }
    }

    /// <summary>
    /// Groups the characters TextMeshPro lays out into the units a reader sees as single
    /// letters.
    /// </summary>
    /// <remarks>
    /// In Thai a syllable such as "กิ" is two code points: the consonant and a vowel that
    /// hangs above it with no width of its own. Animating them as separate characters
    /// gives each its own phase and its own random offset, so the vowel drifts off the
    /// consonant it belongs to -- and a typewriter reveals a bare consonant for a moment
    /// before its vowel arrives. Neither is text anyone would accept.
    ///
    /// The same applies to Devanagari matras, Arabic and Hebrew vowel points, and
    /// combining accents on Latin letters, so the rule is Unicode's rather than Thai's:
    /// a non-spacing mark joins the character before it.
    /// </remarks>
    public static class TextClusters
    {
        /// <summary>
        /// Works out which cluster each laid-out character belongs to.
        /// </summary>
        /// <param name="textInfo">Laid-out text to read characters from.</param>
        /// <param name="clusterOf">
        /// Filled with each character's 0-based cluster number. Grown if too small.
        /// </param>
        /// <param name="baseOf">
        /// Filled with the character index that leads each character's cluster. Grown if
        /// too small.
        /// </param>
        /// <returns>How many clusters the text contains.</returns>
        public static int Build(TMP_TextInfo textInfo, ref int[] clusterOf, ref int[] baseOf)
        {
            int count = textInfo != null ? textInfo.characterCount : 0;
            Grow(ref clusterOf, count);
            Grow(ref baseOf, count);

            int clusters = 0;
            int currentBase = 0;

            for (int i = 0; i < count; i++)
            {
                // The first character cannot join anything, whatever it is.
                bool joinsPrevious = i > 0 && IsCombining(textInfo.characterInfo[i].character);

                if (!joinsPrevious)
                {
                    currentBase = i;
                    clusters++;
                }

                clusterOf[i] = clusters - 1;
                baseOf[i] = currentBase;
            }

            return clusters;
        }

        /// <summary>
        /// True for a mark that renders on top of the character before it rather than
        /// beside it: Thai vowels and tone marks, Latin combining accents, and the rest
        /// of Unicode's non-spacing marks.
        /// </summary>
        public static bool IsCombining(char character)
        {
            switch (CharUnicodeInfo.GetUnicodeCategory(character))
            {
                case UnicodeCategory.NonSpacingMark:
                case UnicodeCategory.EnclosingMark:
                    return true;

                default:
                    return false;
            }
        }

        private static void Grow(ref int[] array, int count)
        {
            if (array == null || array.Length < count)
                array = new int[count > 16 ? count : 16];
        }
    }
}
