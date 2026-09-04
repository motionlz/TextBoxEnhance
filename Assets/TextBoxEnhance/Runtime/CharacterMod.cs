using UnityEngine;

namespace TextBoxEnhance
{
    /// <summary>
    /// The per-character transform and colour delta that every effect covering a
    /// character writes into. One <see cref="CharacterMod"/> is accumulated per
    /// character per frame, then baked into the TextMeshPro mesh in one pass.
    /// </summary>
    /// <remarks>
    /// <see cref="Offset"/> is measured in <c>em</c> — 1 means "one font size" — so
    /// an effect looks the same whether the label is 12pt or 120pt.
    /// </remarks>
    public struct CharacterMod
    {
        /// <summary>Positional offset in em (1 = the character's point size).</summary>
        public Vector2 Offset;

        /// <summary>Rotation in degrees around the character's baseline centre.</summary>
        public float Rotation;

        /// <summary>Multiplicative scale around the character's baseline centre.</summary>
        public Vector2 Scale;

        /// <summary>Multiplied into the character's vertex colour.</summary>
        public Color ColorMultiply;

        /// <summary>Colour the character is lerped towards, by <see cref="ColorOverrideWeight"/>.</summary>
        public Color ColorOverride;

        /// <summary>How strongly <see cref="ColorOverride"/> replaces the vertex colour, 0..1.</summary>
        public float ColorOverrideWeight;

        public static CharacterMod Identity => new CharacterMod
        {
            Offset = Vector2.zero,
            Rotation = 0f,
            Scale = Vector2.one,
            ColorMultiply = Color.white,
            ColorOverride = Color.white,
            ColorOverrideWeight = 0f,
        };

        /// <summary>Blends <paramref name="color"/> over whatever colour the character already has.</summary>
        public void Tint(Color color, float weight = 1f)
        {
            if (weight <= 0f)
                return;

            // Later tints win, but earlier ones still show through partial weights.
            ColorOverride = ColorOverrideWeight > 0f
                ? Color.Lerp(ColorOverride, color, weight)
                : color;
            ColorOverrideWeight = Mathf.Clamp01(ColorOverrideWeight + weight * (1f - ColorOverrideWeight));
        }

        /// <summary>Multiplies the character's alpha.</summary>
        public void MultiplyAlpha(float alpha)
        {
            ColorMultiply.a *= alpha;
        }
    }
}
