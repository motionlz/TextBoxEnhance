using UnityEngine;

namespace TextBoxEnhance.Effects
{
    /// <summary>Shared helpers for the built-in motion effects.</summary>
    internal static class EffectMath
    {
        /// <summary>Stable 0..1 noise for a character at a discrete time step.</summary>
        internal static float Random01(int charIndex, int step, int salt = 0)
        {
            return TextEffectContext.Hash01(charIndex * 73856093 ^ step * 19349663 ^ salt * 83492791);
        }
    }

    /// <summary>
    /// A sine wave travelling along the text.
    /// <c>&lt;wave a=0.15 f=6 w=0.6&gt;</c> - amplitude in em, speed, radians per character.
    /// </summary>
    [TextEffectTag("wave")]
    public sealed class WaveEffect : TextEffect
    {
        public override void Apply(in TextEffectContext context, TagParams parameters, ref CharacterMod mod)
        {
            float amplitude = parameters.GetPrimaryFloat(0.15f, "a", "amp", "amplitude");
            float frequency = parameters.GetFloat(6f, "f", "freq", "frequency");
            float waveLength = parameters.GetFloat(0.6f, "w", "wave", "length");

            float phase = context.Time * frequency + context.CharIndex * waveLength;
            mod.Offset.y += Mathf.Sin(phase) * amplitude;
        }
    }

    /// <summary>
    /// A hard, stepped rattle - the classic "shouting" text.
    /// <c>&lt;shake a=0.06 f=25&gt;</c> - amplitude in em, steps per second.
    /// </summary>
    [TextEffectTag("shake")]
    public sealed class ShakeEffect : TextEffect
    {
        public override void Apply(in TextEffectContext context, TagParams parameters, ref CharacterMod mod)
        {
            float amplitude = parameters.GetPrimaryFloat(0.06f, "a", "amp", "amplitude");
            float frequency = parameters.GetFloat(25f, "f", "freq", "frequency");

            // Quantising time is what makes this read as a rattle rather than a drift.
            int step = Mathf.FloorToInt(context.Time * frequency);
            mod.Offset.x += (EffectMath.Random01(context.CharIndex, step) - 0.5f) * 2f * amplitude;
            mod.Offset.y += (EffectMath.Random01(context.CharIndex, step, 1) - 0.5f) * 2f * amplitude;
        }
    }

    /// <summary>
    /// A smooth, drunken drift. <c>&lt;wobble a=0.1 f=1.5&gt;</c>
    /// </summary>
    [TextEffectTag("wobble", "wiggle")]
    public sealed class WobbleEffect : TextEffect
    {
        public override void Apply(in TextEffectContext context, TagParams parameters, ref CharacterMod mod)
        {
            float amplitude = parameters.GetPrimaryFloat(0.1f, "a", "amp", "amplitude");
            float frequency = parameters.GetFloat(1.5f, "f", "freq", "frequency");

            float seed = context.Seed * 100f;
            float t = context.Time * frequency;
            mod.Offset.x += (Mathf.PerlinNoise(seed, t) - 0.5f) * 2f * amplitude;
            mod.Offset.y += (Mathf.PerlinNoise(seed + 37.4f, t) - 0.5f) * 2f * amplitude;
        }
    }

    /// <summary>
    /// Re-randomised every frame - noisier and more frantic than <c>&lt;shake&gt;</c>.
    /// <c>&lt;jitter a=0.05&gt;</c>
    /// </summary>
    [TextEffectTag("jitter")]
    public sealed class JitterEffect : TextEffect
    {
        public override void Apply(in TextEffectContext context, TagParams parameters, ref CharacterMod mod)
        {
            float amplitude = parameters.GetPrimaryFloat(0.05f, "a", "amp", "amplitude");

            int step = Time.frameCount;
            mod.Offset.x += (EffectMath.Random01(context.CharIndex, step, 2) - 0.5f) * 2f * amplitude;
            mod.Offset.y += (EffectMath.Random01(context.CharIndex, step, 3) - 0.5f) * 2f * amplitude;
        }
    }

    /// <summary>
    /// Characters hop upward in sequence. <c>&lt;bounce a=0.25 f=6 w=0.6&gt;</c>
    /// </summary>
    [TextEffectTag("bounce")]
    public sealed class BounceEffect : TextEffect
    {
        public override void Apply(in TextEffectContext context, TagParams parameters, ref CharacterMod mod)
        {
            float amplitude = parameters.GetPrimaryFloat(0.25f, "a", "amp", "amplitude");
            float frequency = parameters.GetFloat(6f, "f", "freq", "frequency");
            float waveLength = parameters.GetFloat(0.6f, "w", "wave", "length");

            float phase = context.Time * frequency + context.CharIndex * waveLength;
            mod.Offset.y += Mathf.Abs(Mathf.Sin(phase)) * amplitude;
        }
    }

    /// <summary>
    /// Rocks each character around its baseline. <c>&lt;swing a=12 f=4 w=0.5&gt;</c> - degrees.
    /// </summary>
    [TextEffectTag("swing", "rotate")]
    public sealed class SwingEffect : TextEffect
    {
        public override void Apply(in TextEffectContext context, TagParams parameters, ref CharacterMod mod)
        {
            float angle = parameters.GetPrimaryFloat(12f, "a", "angle", "amplitude");
            float frequency = parameters.GetFloat(4f, "f", "freq", "frequency");
            float waveLength = parameters.GetFloat(0.5f, "w", "wave", "length");

            float phase = context.Time * frequency + context.CharIndex * waveLength;
            mod.Rotation += Mathf.Sin(phase) * angle;
        }
    }

    /// <summary>
    /// Breathes each character in and out. <c>&lt;pulse a=0.15 f=5 w=0.4&gt;</c>
    /// </summary>
    [TextEffectTag("pulse")]
    public sealed class PulseEffect : TextEffect
    {
        public override void Apply(in TextEffectContext context, TagParams parameters, ref CharacterMod mod)
        {
            float amount = parameters.GetPrimaryFloat(0.15f, "a", "amp", "amount");
            float frequency = parameters.GetFloat(5f, "f", "freq", "frequency");
            float waveLength = parameters.GetFloat(0.4f, "w", "wave", "length");

            float phase = context.Time * frequency + context.CharIndex * waveLength;
            float scale = 1f + Mathf.Sin(phase) * amount;
            mod.Scale.x *= scale;
            mod.Scale.y *= scale;
        }
    }
}
