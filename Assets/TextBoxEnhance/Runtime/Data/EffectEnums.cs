namespace TextBoxEnhance.Data
{
    /// <summary>What a layer moves on the character.</summary>
    public enum EffectChannel
    {
        /// <summary>Horizontal offset, in em.</summary>
        OffsetX = 0,

        /// <summary>Vertical offset, in em.</summary>
        OffsetY = 1,

        /// <summary>Rotation around the baseline, in degrees.</summary>
        Rotation = 2,

        /// <summary>Horizontal scale, where 1 is unchanged.</summary>
        ScaleX = 3,

        /// <summary>Vertical scale, where 1 is unchanged.</summary>
        ScaleY = 4,

        /// <summary>Both scale axes at once.</summary>
        Scale = 5,

        /// <summary>Opacity multiplier, 0 to 1.</summary>
        Alpha = 6,

        /// <summary>Colour, blended over whatever the character already is.</summary>
        Colour = 7,
    }

    /// <summary>
    /// How a layer's value moves over time. Every motion is normalised to 0..1 so the
    /// layer can map it onto any channel with a single Min/Max pair.
    /// </summary>
    public enum EffectMotion
    {
        /// <summary>Smooth back and forth. The everyday wave.</summary>
        Sine = 0,

        /// <summary>Sine folded at zero, so it only travels one way. Hops.</summary>
        Bounce = 1,

        /// <summary>Smooth drift from Perlin noise. Drunk, underwater, breathing.</summary>
        Drift = 2,

        /// <summary>Stepped random, re-rolled at a fixed rate. A rattle.</summary>
        Shake = 3,

        /// <summary>Random every frame. Frantic.</summary>
        Jitter = 4,

        /// <summary>Sawtooth 0 to 1, then snaps back. Cycles a gradient.</summary>
        Ramp = 5,

        /// <summary>Hard on/off, with a duty cycle.</summary>
        Blink = 6,

        /// <summary>A hand-drawn curve, looped.</summary>
        Curve = 7,

        /// <summary>No movement at all. Holds at the layer's Max.</summary>
        Constant = 8,
    }

    /// <summary>
    /// Which part of a letter a layer moves, for scripts that write a letter as a base
    /// plus marks that sit on it.
    /// </summary>
    /// <remarks>
    /// A Thai reader parses a syllable as one shape, so a tone mark drifting off its
    /// consonant reads as broken text rather than as style -- and a mark that wanders
    /// towards the next consonant can be read as belonging to that one instead. Worth
    /// reaching for on a single word, a title or a damage number; not on body text.
    /// </remarks>
    public enum LayerTarget
    {
        /// <summary>The base and its marks move as one shape. The readable default.</summary>
        WholeLetter = 0,

        /// <summary>Only the base moves, sliding out from under its marks.</summary>
        BaseLetterOnly = 1,

        /// <summary>
        /// Only the marks move, each with a phase of its own, over a base that stays put.
        /// </summary>
        MarksOnly = 2,
    }

    /// <summary>What drives a layer's motion.</summary>
    public enum EffectTimebase
    {
        /// <summary>Seconds since the text box started. The usual choice.</summary>
        Time = 0,

        /// <summary>
        /// The character's 0..1 entrance progress, so the layer plays once as the
        /// typewriter arrives and then settles.
        /// </summary>
        RevealProgress = 1,
    }

    /// <summary>How a <see cref="EffectChannel.Colour"/> layer decides its colour.</summary>
    public enum ColourMode
    {
        /// <summary>One flat colour.</summary>
        Solid = 0,

        /// <summary>Samples a gradient with the motion's 0..1 value.</summary>
        Gradient = 1,

        /// <summary>Sweeps hue with the motion's 0..1 value. The rainbow.</summary>
        HueSweep = 2,
    }
}
