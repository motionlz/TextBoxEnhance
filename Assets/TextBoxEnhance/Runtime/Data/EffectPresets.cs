using System;
using System.Collections.Generic;
using UnityEngine;

namespace TextBoxEnhance.Data
{
    /// <summary>
    /// Layer stacks that reproduce each built-in effect exactly. They are the starting
    /// points offered in the effect editor, and the proof that the layer vocabulary is
    /// expressive enough: a test asserts each one matches its coded counterpart.
    /// </summary>
    /// <remarks>
    /// The built-ins measure speed in radians per second because that is what reads
    /// naturally in a sine call. Layers measure it in cycles per second because that is
    /// what reads naturally on a slider, so the conversions below carry a /Tau or /Pi.
    /// </remarks>
    public static class EffectPresets
    {
        private const float Tau = Mathf.PI * 2f;

        /// <summary>A named starting point offered in the effect editor.</summary>
        public readonly struct Preset
        {
            /// <summary>Label shown on the button.</summary>
            public readonly string Name;

            /// <summary>Tag the built-in equivalent answers to.</summary>
            public readonly string BuiltInTag;

            /// <summary>Builds a fresh layer stack. Never returns a shared instance.</summary>
            public readonly Func<List<EffectLayer>> Build;

            public Preset(string name, string builtInTag, Func<List<EffectLayer>> build)
            {
                Name = name;
                BuiltInTag = builtInTag;
                Build = build;
            }
        }

        private static readonly Preset[] s_All =
        {
            new Preset("Wave", "wave", Wave),
            new Preset("Shake", "shake", Shake),
            new Preset("Wobble", "wobble", Wobble),
            new Preset("Jitter", "jitter", Jitter),
            new Preset("Bounce", "bounce", Bounce),
            new Preset("Swing", "swing", Swing),
            new Preset("Pulse", "pulse", Pulse),
            new Preset("Rainbow", "rainbow", Rainbow),
            new Preset("Tint", "tint", Tint),
            new Preset("Fade", "fade", Fade),
            new Preset("Blink", "blink", Blink),
        };

        public static IReadOnlyList<Preset> All => s_All;

        /// <summary>
        /// What an added layer starts as. Deliberately gentler than any preset: a layer
        /// appears next to whatever is already there, and one arriving at full strength
        /// swamps the effect being built rather than adding to it.
        /// </summary>
        public static EffectLayer NewLayer()
        {
            return new EffectLayer
            {
                Channel = EffectChannel.OffsetY,
                Motion = EffectMotion.Sine,
                Timebase = EffectTimebase.Time,
                Min = -0.05f,
                Max = 0.05f,
                Speed = 1f,
                Spread = 0.1f,
                Duty = 0.5f,
                Weight = 1f,
                Colour = Color.white,
            };
        }

        public static List<EffectLayer> Wave()
        {
            return One(new EffectLayer
            {
                Channel = EffectChannel.OffsetY,
                Motion = EffectMotion.Sine,
                Min = -0.15f,
                Max = 0.15f,
                Speed = 6f / Tau,
                Spread = 0.6f / Tau,
            });
        }

        public static List<EffectLayer> Shake()
        {
            return Pair(EffectMotion.Shake, -0.06f, 0.06f, speed: 25f, spread: 0f, saltX: 0, saltY: 1);
        }

        public static List<EffectLayer> Wobble()
        {
            return Pair(EffectMotion.Drift, -0.1f, 0.1f, speed: 1.5f, spread: 0f, saltX: 0, saltY: 1);
        }

        public static List<EffectLayer> Jitter()
        {
            return Pair(EffectMotion.Jitter, -0.05f, 0.05f, speed: 1f, spread: 0f, saltX: 2, saltY: 3);
        }

        public static List<EffectLayer> Bounce()
        {
            return One(new EffectLayer
            {
                Channel = EffectChannel.OffsetY,
                Motion = EffectMotion.Bounce,
                Min = 0f,
                Max = 0.25f,
                Speed = 6f / Mathf.PI,
                Spread = 0.6f / Mathf.PI,
            });
        }

        public static List<EffectLayer> Swing()
        {
            return One(new EffectLayer
            {
                Channel = EffectChannel.Rotation,
                Motion = EffectMotion.Sine,
                Min = -12f,
                Max = 12f,
                Speed = 4f / Tau,
                Spread = 0.5f / Tau,
            });
        }

        public static List<EffectLayer> Pulse()
        {
            return One(new EffectLayer
            {
                Channel = EffectChannel.Scale,
                Motion = EffectMotion.Sine,
                Min = 0.85f,
                Max = 1.15f,
                Speed = 5f / Tau,
                Spread = 0.4f / Tau,
            });
        }

        public static List<EffectLayer> Rainbow()
        {
            return One(new EffectLayer
            {
                Channel = EffectChannel.Colour,
                Motion = EffectMotion.Ramp,
                ColourMode = ColourMode.HueSweep,
                Min = 0f,
                Max = 1f,
                Speed = 0.35f,
                Spread = 0.06f,
                Saturation = 1f,
                Value = 1f,
                Weight = 1f,
            });
        }

        public static List<EffectLayer> Tint()
        {
            return One(new EffectLayer
            {
                Channel = EffectChannel.Colour,
                Motion = EffectMotion.Constant,
                ColourMode = ColourMode.Solid,
                Colour = Color.white,
                Min = 0f,
                Max = 1f,
                Weight = 1f,
            });
        }

        public static List<EffectLayer> Fade()
        {
            return One(new EffectLayer
            {
                Channel = EffectChannel.Alpha,
                Motion = EffectMotion.Sine,
                Min = 0.25f,
                Max = 1f,
                Speed = 2f / Tau,
                Spread = 0.4f / Tau,
            });
        }

        public static List<EffectLayer> Blink()
        {
            return One(new EffectLayer
            {
                Channel = EffectChannel.Alpha,
                Motion = EffectMotion.Blink,
                Min = 0f,
                Max = 1f,
                Speed = 3f,
                Duty = 0.5f,
            });
        }

        private static List<EffectLayer> One(EffectLayer layer)
        {
            return new List<EffectLayer> { layer };
        }

        /// <summary>
        /// Two layers driving X and Y from the same motion. The salts keep the axes
        /// independent -- sharing one would make every character move on a diagonal.
        /// </summary>
        private static List<EffectLayer> Pair(EffectMotion motion, float min, float max,
            float speed, float spread, int saltX, int saltY)
        {
            return new List<EffectLayer>
            {
                new EffectLayer
                {
                    Channel = EffectChannel.OffsetX, Motion = motion,
                    Min = min, Max = max, Speed = speed, Spread = spread, Salt = saltX,
                },
                new EffectLayer
                {
                    Channel = EffectChannel.OffsetY, Motion = motion,
                    Min = min, Max = max, Speed = speed, Spread = spread, Salt = saltY,
                },
            };
        }
    }
}
