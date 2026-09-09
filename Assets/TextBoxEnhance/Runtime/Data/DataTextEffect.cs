namespace TextBoxEnhance.Data
{
    /// <summary>
    /// Bridges a <see cref="TextEffectAsset"/> into the same registry the code effects
    /// live in, so a tag behaves identically whether it came from a class or an asset.
    /// </summary>
    public sealed class DataTextEffect : TextEffect
    {
        private readonly TextEffectAsset m_Asset;

        public DataTextEffect(TextEffectAsset asset)
        {
            m_Asset = asset;
        }

        /// <summary>The asset driving this effect.</summary>
        public TextEffectAsset Asset => m_Asset;

        public override void Apply(in TextEffectContext context, TagParams parameters, ref CharacterMod mod)
        {
            if (m_Asset == null)
                return;

            m_Asset.Apply(in context, LayerScale.FromTag(parameters), ref mod);
        }
    }
}
