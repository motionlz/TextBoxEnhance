using TextBoxEnhance.Data;

namespace TextBoxEnhance.EditorTools
{
    /// <summary>
    /// Where each slider's track ends, and what a channel reads as "unchanged".
    /// Kept in one place so the window and the tests that guard it cannot drift apart.
    /// </summary>
    public static class EffectLimits
    {
        /// <summary>
        /// Motions that jump between values rather than travelling between them.
        /// They need far smaller amplitudes to stay readable: a fifth of an em of
        /// smooth travel is a wave, while the same distance jumped twice a second is
        /// text nobody can read.
        /// </summary>
        public static bool IsInstant(EffectMotion motion)
        {
            switch (motion)
            {
                case EffectMotion.Shake:
                case EffectMotion.Jitter:
                case EffectMotion.Blink:
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>The end of the amount slider's track.</summary>
        public static float Amount(EffectChannel channel, EffectMotion motion)
        {
            // Opacity and colour are edited as their own 0..1 range rather than as a
            // distance from a resting value, so the motion has no say in their limits.
            if (channel == EffectChannel.Alpha || channel == EffectChannel.Colour)
                return 1f;

            // Scale is measured around 1, and its track has to reach past that or the
            // scale can never cross zero -- which would leave Allow flipping switched on
            // and doing nothing, because the value it guards is unreachable.
            if (IsScale(channel))
                return 1.5f;

            // Half a turn was never wanted for text and left a four degree swing pinned
            // against the left edge; a quarter turn is more than any legible effect needs.
            if (channel == EffectChannel.Rotation)
                return IsInstant(motion) ? 30f : 90f;

            return IsInstant(motion) ? 0.15f : 0.6f;
        }

        /// <summary>
        /// The end of the speed slider's track. Speed means different things to
        /// different motions: a rattle counts re-rolls per second and runs at 25, a
        /// wave counts cycles and runs near 1. One track for both would pin the wave
        /// against the left edge.
        /// </summary>
        public static float Speed(EffectMotion motion)
        {
            return motion == EffectMotion.Shake || motion == EffectMotion.Jitter ? 60f : 8f;
        }

        /// <summary>The end of the offset-per-letter track, which runs both ways.</summary>
        public const float Spread = 1f;

        /// <summary>
        /// What the channel reads as untouched. Scale is the odd one: a resting value
        /// of zero collapses the character to nothing, where every other channel treats
        /// zero as "leave it alone".
        /// </summary>
        public static float Neutral(EffectChannel channel)
        {
            switch (channel)
            {
                case EffectChannel.ScaleX:
                case EffectChannel.ScaleY:
                case EffectChannel.Scale:
                    return 1f;

                default:
                    return 0f;
            }
        }

        /// <summary>True for the channels measured around 1 rather than around 0.</summary>
        public static bool IsScale(EffectChannel channel)
        {
            switch (channel)
            {
                case EffectChannel.ScaleX:
                case EffectChannel.ScaleY:
                case EffectChannel.Scale:
                    return true;

                default:
                    return false;
            }
        }
    }
}
