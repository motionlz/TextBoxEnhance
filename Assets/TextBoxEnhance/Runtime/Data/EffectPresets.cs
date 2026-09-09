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

            /// <summary>
            /// The coded effect this preset reproduces, or null for one that has no
            /// single-tag equivalent -- a combination only the layer system can express.
            /// </summary>
            public readonly string BuiltInTag;

            /// <summary>True for a preset that stands in for a built-in tag.</summary>
            public bool ReproducesABuiltIn => !string.IsNullOrEmpty(BuiltInTag);

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

            // No single tag does these. They are the argument for layers: two or three
            // simple motions on different channels read as one deliberate effect.
            new Preset("Float", null, Float),
            new Preset("Heartbeat", null, Heartbeat),
            new Preset("Fire", null, Fire),
            new Preset("Glitch", null, Glitch),
            new Preset("Whisper", null, Whisper),
            new Preset("Rise", null, Rise),
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
                Min = -0.03f,
                Max = 0.03f,
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
                Min = -0.05f,
                Max = 0.05f,
                Speed = 6f / Tau,
                Spread = 0.6f / Tau,
            });
        }

        public static List<EffectLayer> Shake()
        {
            return Pair(EffectMotion.Shake, -0.03f, 0.03f, speed: 25f, spread: 0f, saltX: 0, saltY: 1);
        }

        public static List<EffectLayer> Wobble()
        {
            return Pair(EffectMotion.Drift, -0.033f, 0.033f, speed: 1.5f, spread: 0f, saltX: 0, saltY: 1);
        }

        public static List<EffectLayer> Jitter()
        {
            return Pair(EffectMotion.Jitter, -0.03f, 0.03f, speed: 1f, spread: 0f, saltX: 2, saltY: 3);
        }

        public static List<EffectLayer> Bounce()
        {
            return One(new EffectLayer
            {
                Channel = EffectChannel.OffsetY,
                Motion = EffectMotion.Bounce,
                Min = 0f,
                Max = 0.083f,
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
                Min = -4f,
                Max = 4f,
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
                Min = 0.95f,
                Max = 1.05f,
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

        /// <summary>Idle drift: never still, never going anywhere.</summary>
        public static List<EffectLayer> Float()
        {
            return new List<EffectLayer>
            {
                new EffectLayer
                {
                    Channel = EffectChannel.OffsetY, Motion = EffectMotion.Drift,
                    Min = -0.04f, Max = 0.04f, Speed = 0.6f, Salt = 0,
                },
                new EffectLayer
                {
                    Channel = EffectChannel.Rotation, Motion = EffectMotion.Drift,
                    Min = -3f, Max = 3f, Speed = 0.4f, Salt = 5,
                },
            };
        }

        /// <summary>Two thumps and a rest, which is what a curve is for.</summary>
        public static List<EffectLayer> Heartbeat()
        {
            var beat = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.10f, 1f),
                new Keyframe(0.22f, 0f),
                new Keyframe(0.32f, 0.55f),
                new Keyframe(0.45f, 0f),
                new Keyframe(1f, 0f));

            return One(new EffectLayer
            {
                Channel = EffectChannel.Scale, Motion = EffectMotion.Curve,
                Min = 1f, Max = 1.12f, Speed = 0.8f, Curve = beat,
            });
        }

        /// <summary>Heat: colour running through the flame, and the letters lifting with it.</summary>
        public static List<EffectLayer> Fire()
        {
            var flame = new Gradient();
            flame.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.25f, 0.05f), 0f),
                    new GradientColorKey(new Color(1f, 0.55f, 0.10f), 0.45f),
                    new GradientColorKey(new Color(1f, 0.90f, 0.35f), 0.75f),
                    new GradientColorKey(new Color(1f, 0.35f, 0.05f), 1f),
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });

            return new List<EffectLayer>
            {
                new EffectLayer
                {
                    Channel = EffectChannel.Colour, Motion = EffectMotion.Ramp,
                    ColourMode = ColourMode.Gradient, Gradient = flame,
                    Min = 0f, Max = 1f, Speed = 0.9f, Spread = 0.12f, Weight = 1f,
                },
                new EffectLayer
                {
                    Channel = EffectChannel.OffsetY, Motion = EffectMotion.Drift,
                    Min = -0.02f, Max = 0.05f, Speed = 2.5f, Salt = 3,
                },
            };
        }

        /// <summary>A broken signal: position, visibility and colour all misbehaving.</summary>
        public static List<EffectLayer> Glitch()
        {
            return new List<EffectLayer>
            {
                new EffectLayer
                {
                    Channel = EffectChannel.OffsetX, Motion = EffectMotion.Shake,
                    Min = -0.05f, Max = 0.05f, Speed = 14f, Salt = 0,
                },
                new EffectLayer
                {
                    Channel = EffectChannel.Alpha, Motion = EffectMotion.Blink,
                    Min = 0.35f, Max = 1f, Speed = 7.5f, Duty = 0.85f,
                },
                new EffectLayer
                {
                    Channel = EffectChannel.Colour, Motion = EffectMotion.Shake,
                    ColourMode = ColourMode.HueSweep, Min = 0.45f, Max = 0.6f,
                    Speed = 11f, Saturation = 0.7f, Value = 1f, Weight = 0.5f, Salt = 7,
                },
            };
        }

        /// <summary>Barely there, and slowly moving. For something half heard.</summary>
        public static List<EffectLayer> Whisper()
        {
            return new List<EffectLayer>
            {
                new EffectLayer
                {
                    Channel = EffectChannel.Alpha, Motion = EffectMotion.Sine,
                    Min = 0.25f, Max = 0.6f, Speed = 0.35f, Spread = 0.05f,
                },
                new EffectLayer
                {
                    Channel = EffectChannel.OffsetY, Motion = EffectMotion.Drift,
                    Min = -0.03f, Max = 0.03f, Speed = 0.5f, Salt = 2,
                },
            };
        }

        /// <summary>
        /// Plays once as the typewriter reaches each letter rather than looping: the
        /// letter climbs into place and stays there. Timebase is what makes that
        /// possible, and nothing with a single tag can do it.
        /// </summary>
        public static List<EffectLayer> Rise()
        {
            var settle = new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(0.6f, 0.1f),
                new Keyframe(1f, 0f));

            return new List<EffectLayer>
            {
                new EffectLayer
                {
                    Channel = EffectChannel.OffsetY, Motion = EffectMotion.Curve,
                    Timebase = EffectTimebase.RevealProgress,
                    Min = 0f, Max = 0.35f, Speed = 1f, Curve = settle,
                },
            };
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
