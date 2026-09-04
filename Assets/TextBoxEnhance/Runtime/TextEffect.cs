using System;
using TMPro;
using UnityEngine;

namespace TextBoxEnhance
{
    /// <summary>
    /// Everything an effect needs to know about the character it is animating.
    /// </summary>
    public readonly struct TextEffectContext
    {
        /// <summary>Laid-out text being animated. Read-only as far as effects are concerned.</summary>
        public readonly TMP_TextInfo TextInfo;

        /// <summary>Index of this character within the whole label.</summary>
        public readonly int CharIndex;

        /// <summary>Index of this character within the tag's range, starting at 0.</summary>
        public readonly int IndexInRange;

        /// <summary>How many characters the tag's range covers.</summary>
        public readonly int RangeLength;

        /// <summary>Seconds the text box has been animating.</summary>
        public readonly float Time;

        /// <summary>Seconds since the previous frame, matching the box's time scale.</summary>
        public readonly float DeltaTime;

        /// <summary>
        /// How far this character is through its reveal animation, 0..1.
        /// Always 1 once the typewriter has finished with it, and 1 everywhere when
        /// the box is not using a typewriter at all.
        /// </summary>
        public readonly float RevealProgress;

        public TextEffectContext(TMP_TextInfo textInfo, int charIndex, int indexInRange, int rangeLength,
            float time, float deltaTime, float revealProgress)
        {
            TextInfo = textInfo;
            CharIndex = charIndex;
            IndexInRange = indexInRange;
            RangeLength = rangeLength;
            Time = time;
            DeltaTime = deltaTime;
            RevealProgress = revealProgress;
        }

        /// <summary>Character info for the character being animated.</summary>
        public TMP_CharacterInfo Character => TextInfo.characterInfo[CharIndex];

        /// <summary>
        /// A stable per-character pseudo-random value in 0..1, handy for de-syncing
        /// noise so neighbouring characters do not move in lockstep.
        /// </summary>
        public float Seed => Hash01(CharIndex);

        internal static float Hash01(int value)
        {
            // Cheap integer hash (xorshift-ish) so the result is stable across frames
            // and platforms, unlike UnityEngine.Random.
            unchecked
            {
                uint x = (uint)value * 2654435761u;
                x ^= x >> 15;
                x *= 2246822519u;
                x ^= x >> 13;
                return (x & 0xFFFFFF) / (float)0x1000000;
            }
        }
    }

    /// <summary>
    /// Base class for a continuous per-character animation driven by a rich-text tag.
    /// Implementations must be stateless — one instance serves every text box in the
    /// project, so all per-character state comes in through <see cref="TextEffectContext"/>.
    /// </summary>
    public abstract class TextEffect
    {
        /// <summary>
        /// Adds this effect's contribution to <paramref name="mod"/> for one character.
        /// </summary>
        public abstract void Apply(in TextEffectContext context, TagParams parameters, ref CharacterMod mod);
    }

    /// <summary>
    /// Registers a <see cref="TextEffect"/> under one or more rich-text tag names, so
    /// writing <c>&lt;wave&gt;hello&lt;/wave&gt;</c> in a text box just works. Tag names are
    /// case-insensitive and must not collide with TextMeshPro's own tags.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class TextEffectTagAttribute : Attribute
    {
        public string[] Tags { get; }

        public TextEffectTagAttribute(params string[] tags)
        {
            Tags = tags;
        }
    }
}
