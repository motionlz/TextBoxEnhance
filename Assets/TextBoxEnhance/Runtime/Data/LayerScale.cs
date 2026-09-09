namespace TextBoxEnhance.Data
{
    /// <summary>
    /// The multipliers a tag can dial onto a data-driven effect, so one asset covers
    /// a whole family of intensities: <c>&lt;sparkle&gt;</c>, <c>&lt;sparkle a=2&gt;</c>,
    /// <c>&lt;sparkle a=0.5 f=3&gt;</c>.
    /// </summary>
    /// <remarks>
    /// These scale every layer at once. Reaching a single layer from a tag would mean
    /// the asset had to name its parameters, which is exactly the kind of bookkeeping
    /// the visual editor exists to avoid.
    /// </remarks>
    public readonly struct LayerScale
    {
        /// <summary>Leaves the asset's own values untouched.</summary>
        public static readonly LayerScale None = new LayerScale(1f, 1f, 1f);

        /// <summary>Multiplies how far each layer travels.</summary>
        public readonly float Amount;

        /// <summary>Multiplies how fast each layer moves.</summary>
        public readonly float Speed;

        /// <summary>Multiplies the delay between neighbouring characters.</summary>
        public readonly float Spread;

        public LayerScale(float amount, float speed, float spread)
        {
            Amount = amount;
            Speed = speed;
            Spread = spread;
        }

        /// <summary>Reads the multipliers a tag wrote, falling back to no scaling.</summary>
        public static LayerScale FromTag(TagParams parameters)
        {
            if (parameters == null)
                return None;

            return new LayerScale(
                parameters.GetPrimaryFloat(1f, "a", "amp", "amount"),
                parameters.GetFloat(1f, "f", "freq", "speed"),
                parameters.GetFloat(1f, "w", "wave", "spread"));
        }
    }
}
