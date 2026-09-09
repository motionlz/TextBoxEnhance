using System;
using UnityEngine;

namespace TextBoxEnhance.Data
{
    /// <summary>
    /// One movement written by a data-driven effect: a channel, a motion that drives it,
    /// and the range that motion is mapped onto.
    /// </summary>
    /// <remarks>
    /// Every motion is normalised to 0..1, so a layer is always "lerp between Min and
    /// Max by however far the motion has travelled". That one rule is what lets a single
    /// inspector serve a wave, a rattle and a colour sweep alike.
    /// </remarks>
    [Serializable]
    public sealed class EffectLayer
    {
        private const float Tau = Mathf.PI * 2f;

        [Tooltip("What this layer moves.")]
        public EffectChannel Channel = EffectChannel.OffsetY;

        [Tooltip("How it moves.")]
        public EffectMotion Motion = EffectMotion.Sine;

        [Tooltip("Whether the motion runs off the clock or off the character's entrance.")]
        public EffectTimebase Timebase = EffectTimebase.Time;

        [Tooltip("Value when the motion is at 0.")]
        public float Min = -0.15f;

        [Tooltip("Value when the motion is at 1.")]
        public float Max = 0.15f;

        [Tooltip("Cycles, or re-rolls, per second.")]
        public float Speed = 1f;

        [Tooltip("How far each character lags the one before it. This is what makes a wave travel.")]
        public float Spread;

        [Tooltip("Shifts the whole motion along its cycle, 0 to 1.")]
        public float Phase;

        [Tooltip("Fraction of each Blink cycle spent on.")]
        [Range(0f, 1f)]
        public float Duty = 0.5f;

        [Tooltip("Separates the randomness of two layers that would otherwise move together.")]
        public int Salt;

        [Tooltip("Shape for the Curve motion, sampled across one cycle.")]
        public AnimationCurve Curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("Lets a scale layer pass through zero and turn the character inside out. " +
                 "Off keeps it the right way round however far the scale swings.")]
        public bool AllowFlip;

        [Header("Colour")]
        public ColourMode ColourMode = ColourMode.Solid;

        public Color Colour = Color.white;

        public Gradient Gradient = new Gradient();

        [Range(0f, 1f)]
        public float Saturation = 1f;

        [Range(0f, 1f)]
        public float Value = 1f;

        [Tooltip("How strongly the colour replaces what the character already is.")]
        [Range(0f, 1f)]
        public float Weight = 1f;

        /// <summary>
        /// Adds this layer's contribution to <paramref name="mod"/>.
        /// </summary>
        /// <param name="scale">
        /// Multipliers from the tag that invoked the effect, letting one asset be
        /// dialled up or down per use: <c>&lt;sparkle a=2 f=0.5&gt;</c>.
        /// </param>
        public void Apply(in TextEffectContext context, in LayerScale scale, ref CharacterMod mod)
        {
            float time = Timebase == EffectTimebase.Time ? context.Time : context.RevealProgress;
            float normalised = EvaluateMotion(time, context.CharIndex, context.Seed, scale);

            // Amount scales around the layer's midpoint, so turning a wave up makes it
            // taller rather than sliding the whole line off its baseline.
            float middle = (Min + Max) * 0.5f;
            float reach = (Max - Min) * 0.5f * scale.Amount;
            float value = Mathf.LerpUnclamped(middle - reach, middle + reach, normalised);

            switch (Channel)
            {
                case EffectChannel.OffsetX:
                    mod.Offset.x += value;
                    break;

                case EffectChannel.OffsetY:
                    mod.Offset.y += value;
                    break;

                case EffectChannel.Rotation:
                    mod.Rotation += value;
                    break;

                case EffectChannel.ScaleX:
                    mod.Scale.x *= Scaled(value);
                    break;

                case EffectChannel.ScaleY:
                    mod.Scale.y *= Scaled(value);
                    break;

                case EffectChannel.Scale:
                {
                    float uniform = Scaled(value);
                    mod.Scale.x *= uniform;
                    mod.Scale.y *= uniform;
                    break;
                }

                case EffectChannel.Alpha:
                    mod.MultiplyAlpha(value);
                    break;

                case EffectChannel.Colour:
                    mod.Tint(ResolveColour(normalised, value), Weight);
                    break;
            }
        }

        /// <summary>The layer's motion at a moment, always in 0..1.</summary>
        public float EvaluateMotion(float time, int charIndex, float characterSeed)
        {
            return EvaluateMotion(time, charIndex, characterSeed, LayerScale.None);
        }

        /// <summary>The layer's motion at a moment, with the calling tag's multipliers.</summary>
        public float EvaluateMotion(float time, int charIndex, float characterSeed, in LayerScale scale)
        {
            float speed = Speed * scale.Speed;
            float travelled = time * speed + charIndex * Spread * scale.Spread + Phase;

            switch (Motion)
            {
                case EffectMotion.Sine:
                    return (Mathf.Sin(travelled * Tau) + 1f) * 0.5f;

                case EffectMotion.Bounce:
                    return Mathf.Abs(Mathf.Sin(travelled * Mathf.PI));

                case EffectMotion.Drift:
                    return Mathf.PerlinNoise(characterSeed * 100f + Salt * 37.4f, time * speed);

                case EffectMotion.Shake:
                    return Random01(charIndex, Mathf.FloorToInt(time * speed), Salt);

                case EffectMotion.Jitter:
                    return Random01(charIndex, Time.frameCount, Salt);

                case EffectMotion.Ramp:
                    return Mathf.Repeat(travelled, 1f);

                case EffectMotion.Blink:
                    return Mathf.Repeat(time * speed + Phase, 1f) < Duty ? 1f : 0f;

                case EffectMotion.Curve:
                    return Curve?.Evaluate(Mathf.Repeat(travelled, 1f)) ?? 0f;

                case EffectMotion.Constant:
                    return 1f;

                default:
                    return 0f;
            }
        }

        private Color ResolveColour(float normalised, float value)
        {
            switch (ColourMode)
            {
                case ColourMode.Gradient:
                    return Gradient != null ? Gradient.Evaluate(Mathf.Clamp01(normalised)) : Colour;

                case ColourMode.HueSweep:
                    return Color.HSVToRGB(Mathf.Repeat(value, 1f), Saturation, Value);

                default:
                    return Colour;
            }
        }

        /// <summary>
        /// A scale on its way through zero would mirror the character, which reads as a
        /// glitch far more often than it reads as an effect, so it is opt-in.
        /// </summary>
        private float Scaled(float value)
        {
            return AllowFlip ? value : Mathf.Max(0f, value);
        }

        /// <summary>
        /// Stable pseudo-random in 0..1. Deliberately not UnityEngine.Random: the same
        /// character at the same step must land in the same place every frame, or the
        /// text would boil even when nothing is meant to be moving.
        /// </summary>
        internal static float Random01(int charIndex, int step, int salt)
        {
            return TextEffectContext.Hash01(charIndex * 73856093 ^ step * 19349663 ^ salt * 83492791);
        }
    }
}
