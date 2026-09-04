using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace TextBoxEnhance
{
    /// <summary>A span of characters that one effect tag applies to.</summary>
    public struct EffectRange
    {
        public TextEffect Effect;
        public TagParams Parameters;

        /// <summary>First character index the tag covers, inclusive.</summary>
        public int Start;

        /// <summary>One past the last character the tag covers.</summary>
        public int End;

        public bool Covers(int charIndex) => charIndex >= Start && charIndex < End;
    }

    /// <summary>A span of characters the typewriter runs through at a different rate.</summary>
    public struct SpeedRange
    {
        public int Start;
        public int End;
        public float Multiplier;
    }

    /// <summary>A hold inserted before the character at <see cref="CharIndex"/> is revealed.</summary>
    public struct PauseMarker
    {
        public int CharIndex;
        public float Seconds;
    }

    /// <summary>The stripped text plus everything the tags asked for.</summary>
    public sealed class ParsedText
    {
        /// <summary>Source text with this package's tags removed; TextMeshPro tags survive.</summary>
        public string Text = string.Empty;

        public readonly List<EffectRange> Effects = new List<EffectRange>();
        public readonly List<SpeedRange> Speeds = new List<SpeedRange>();
        public readonly List<PauseMarker> Pauses = new List<PauseMarker>();

        public void Clear()
        {
            Text = string.Empty;
            Effects.Clear();
            Speeds.Clear();
            Pauses.Clear();
        }
    }
}
