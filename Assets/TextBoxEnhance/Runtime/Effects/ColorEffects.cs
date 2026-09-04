using UnityEngine;

namespace TextBoxEnhance.Effects
{
    /// <summary>
    /// Cycles the hue along the text. <c>&lt;rainbow f=0.35 w=0.06 s=1 v=1&gt;</c>
    /// - cycles per second, hue shift per character, saturation, value.
    /// </summary>
    [TextEffectTag("rainbow")]
    public sealed class RainbowEffect : TextEffect
    {
        public override void Apply(in TextEffectContext context, TagParams parameters, ref CharacterMod mod)
        {
            float frequency = parameters.GetPrimaryFloat(0.35f, "f", "freq", "speed");
            float spread = parameters.GetFloat(0.06f, "w", "wave", "spread");
            float saturation = parameters.GetFloat(1f, "s", "sat", "saturation");
            float value = parameters.GetFloat(1f, "v", "val", "value");

            float hue = Mathf.Repeat(context.Time * frequency + context.CharIndex * spread, 1f);
            mod.Tint(Color.HSVToRGB(hue, Mathf.Clamp01(saturation), Mathf.Clamp01(value)));
        }
    }

    /// <summary>
    /// Paints a span a flat colour without disturbing the typewriter's alpha.
    /// <c>&lt;tint=#FF8800&gt;</c> or <c>&lt;tint c=red w=0.5&gt;</c>
    /// </summary>
    [TextEffectTag("tint")]
    public sealed class TintEffect : TextEffect
    {
        public override void Apply(in TextEffectContext context, TagParams parameters, ref CharacterMod mod)
        {
            Color color = parameters.GetPrimaryColor("c", Color.white);
            float weight = Mathf.Clamp01(parameters.GetFloat(1f, "w", "weight"));
            mod.Tint(color, weight);
        }
    }

    /// <summary>
    /// Breathes opacity up and down. <c>&lt;fade min=0.25 max=1 f=2 w=0.4&gt;</c>
    /// </summary>
    [TextEffectTag("fade")]
    public sealed class FadeEffect : TextEffect
    {
        public override void Apply(in TextEffectContext context, TagParams parameters, ref CharacterMod mod)
        {
            float min = parameters.GetPrimaryFloat(0.25f, "min", "lo");
            float max = parameters.GetFloat(1f, "max", "hi");
            float frequency = parameters.GetFloat(2f, "f", "freq", "frequency");
            float waveLength = parameters.GetFloat(0.4f, "w", "wave", "length");

            float phase = context.Time * frequency + context.CharIndex * waveLength;
            float t = (Mathf.Sin(phase) + 1f) * 0.5f;
            mod.MultiplyAlpha(Mathf.Lerp(min, max, t));
        }
    }

    /// <summary>
    /// Hard on/off blinking. <c>&lt;blink f=3 duty=0.5&gt;</c> - blinks per second,
    /// fraction of each cycle spent visible.
    /// </summary>
    [TextEffectTag("blink")]
    public sealed class BlinkEffect : TextEffect
    {
        public override void Apply(in TextEffectContext context, TagParams parameters, ref CharacterMod mod)
        {
            float frequency = parameters.GetPrimaryFloat(3f, "f", "freq", "frequency");
            float duty = Mathf.Clamp01(parameters.GetFloat(0.5f, "duty", "d"));
            float offAlpha = parameters.GetFloat(0f, "min", "off");

            float cycle = Mathf.Repeat(context.Time * frequency, 1f);
            mod.MultiplyAlpha(cycle < duty ? 1f : offAlpha);
        }
    }
}
