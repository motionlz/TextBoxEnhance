using UnityEngine;

namespace TextBoxEnhance
{
    /// <summary>Everything needed to play a character's entrance.</summary>
    public readonly struct RevealSettings
    {
        /// <summary>Entrances are off; characters simply appear.</summary>
        public static readonly RevealSettings None = new RevealSettings(RevealStyle.None, null, 0f, 0f);

        public readonly RevealStyle Style;
        public readonly AnimationCurve Ease;
        public readonly float Distance;
        public readonly float Spins;

        public RevealSettings(RevealStyle style, AnimationCurve ease, float distance, float spins)
        {
            Style = style;
            Ease = ease;
            Distance = distance;
            Spins = spins;
        }

        /// <summary>
        /// Shapes raw progress through the easing curve, falling back to linear when the
        /// curve has been cleared -- an empty curve evaluates to zero, which would leave
        /// every character stuck invisible.
        /// </summary>
        public float Shape(float progress)
        {
            return Ease != null && Ease.length > 0 ? Ease.Evaluate(progress) : progress;
        }
    }

    /// <summary>
    /// Supplies how far each character is through its entrance. The text box answers
    /// from its typewriter; the effect editor's preview answers 1 for everything.
    /// </summary>
    public interface ICharacterRevealSource
    {
        /// <summary>0 while the character is still hidden, 1 once it has settled.</summary>
        float RevealProgressOf(int charIndex);
    }

    /// <summary>A reveal source that considers every character already on screen.</summary>
    public sealed class FullyRevealed : ICharacterRevealSource
    {
        public static readonly FullyRevealed Instance = new FullyRevealed();

        public float RevealProgressOf(int charIndex) => 1f;
    }
}
