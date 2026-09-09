using System.Collections.Generic;
using UnityEngine;

namespace TextBoxEnhance.Data
{
    /// <summary>
    /// An effect built from data rather than code. Each asset claims a rich-text tag
    /// and stacks however many <see cref="EffectLayer"/>s it needs.
    /// </summary>
    /// <remarks>
    /// The point of these is iteration: layers can be dragged around with the text
    /// animating in front of you, where a code effect costs a domain reload per tweak.
    /// </remarks>
    [CreateAssetMenu(menuName = "TextBox Enhance/Text Effect", fileName = "NewTextEffect")]
    public sealed class TextEffectAsset : ScriptableObject
    {
        [Tooltip("The rich-text tag that runs this effect. <sparkle> means a tag of \"sparkle\".")]
        [SerializeField]
        private string m_Tag = "myeffect";

        [Tooltip("Shown in the tag list on the AnimatedTextBox inspector.")]
        [SerializeField, TextArea(1, 3)]
        private string m_Description;

        [SerializeField]
        private List<EffectLayer> m_Layers = new List<EffectLayer>();

        /// <summary>The rich-text tag this asset answers to.</summary>
        public string Tag => m_Tag;

        public string Description => m_Description;

        public List<EffectLayer> Layers => m_Layers;

        /// <summary>Adds every layer's contribution for one character.</summary>
        public void Apply(in TextEffectContext context, in LayerScale scale, ref CharacterMod mod)
        {
            for (int i = 0; i < m_Layers.Count; i++)
                m_Layers[i]?.Apply(in context, in scale, ref mod);
        }

        private void OnValidate()
        {
            m_Tag = m_Tag?.Trim();
        }
    }
}
