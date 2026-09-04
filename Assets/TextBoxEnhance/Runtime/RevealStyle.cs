using UnityEngine;

namespace TextBoxEnhance
{
    /// <summary>How each character animates in as the typewriter reaches it.</summary>
    public enum RevealStyle
    {
        /// <summary>Characters simply pop into existence.</summary>
        None = 0,

        /// <summary>Opacity ramps from 0 to 1.</summary>
        Fade = 1,

        /// <summary>Scales up from nothing with a slight overshoot, and fades in.</summary>
        Pop = 2,

        /// <summary>Scales up from nothing without an overshoot.</summary>
        Grow = 3,

        /// <summary>Rises into place from below while fading in.</summary>
        SlideUp = 4,

        /// <summary>Drops into place from above while fading in.</summary>
        SlideDown = 5,

        /// <summary>Enters from the right while fading in.</summary>
        SlideLeft = 6,

        /// <summary>Enters from the left while fading in.</summary>
        SlideRight = 7,

        /// <summary>Spins into place while scaling and fading in.</summary>
        Spin = 8,
    }

    /// <summary>Turns a <see cref="RevealStyle"/> and a 0..1 progress into a character delta.</summary>
    public static class RevealAnimator
    {
        /// <summary>
        /// Applies the entrance animation for one character.
        /// </summary>
        /// <param name="style">Which entrance to play.</param>
        /// <param name="t">Eased progress, 0 at the start of the entrance and 1 when settled.</param>
        /// <param name="distance">Travel distance for the sliding styles, in em.</param>
        /// <param name="spins">Full turns for <see cref="RevealStyle.Spin"/>.</param>
        /// <param name="mod">Accumulator the entrance is added to.</param>
        public static void Apply(RevealStyle style, float t, float distance, float spins, ref CharacterMod mod)
        {
            if (t >= 1f || style == RevealStyle.None)
                return;

            float remaining = 1f - t;

            switch (style)
            {
                case RevealStyle.Fade:
                    mod.MultiplyAlpha(t);
                    break;

                case RevealStyle.Pop:
                {
                    // A touch of overshoot around 70% of the way in reads as "springy".
                    float scale = t < 1f ? t + Mathf.Sin(t * Mathf.PI) * 0.35f : 1f;
                    mod.Scale *= scale;
                    mod.MultiplyAlpha(t);
                    break;
                }

                case RevealStyle.Grow:
                    mod.Scale *= t;
                    mod.MultiplyAlpha(t);
                    break;

                case RevealStyle.SlideUp:
                    mod.Offset.y -= remaining * distance;
                    mod.MultiplyAlpha(t);
                    break;

                case RevealStyle.SlideDown:
                    mod.Offset.y += remaining * distance;
                    mod.MultiplyAlpha(t);
                    break;

                case RevealStyle.SlideLeft:
                    mod.Offset.x += remaining * distance;
                    mod.MultiplyAlpha(t);
                    break;

                case RevealStyle.SlideRight:
                    mod.Offset.x -= remaining * distance;
                    mod.MultiplyAlpha(t);
                    break;

                case RevealStyle.Spin:
                    mod.Rotation += remaining * 360f * spins;
                    mod.Scale *= t;
                    mod.MultiplyAlpha(t);
                    break;
            }
        }
    }
}
