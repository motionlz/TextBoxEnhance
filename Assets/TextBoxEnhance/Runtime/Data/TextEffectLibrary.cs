using System.Collections.Generic;
using UnityEngine;

namespace TextBoxEnhance.Data
{
    /// <summary>
    /// The list of <see cref="TextEffectAsset"/>s a project ships. Holding them here
    /// rather than scanning a Resources folder means the set that reaches a build is
    /// something you can see and edit, not something that depends on file placement.
    /// </summary>
    /// <remarks>
    /// The library registers its effects from <see cref="OnEnable"/>. In a build that
    /// happens because the asset is in PlayerSettings' preloaded assets; in the editor
    /// a startup hook loads it. Both paths are set up automatically when the library is
    /// created from the Tools menu.
    /// </remarks>
    [CreateAssetMenu(menuName = "TextBox Enhance/Text Effect Library", fileName = "TextEffectLibrary")]
    public sealed class TextEffectLibrary : ScriptableObject
    {
        [Tooltip("Effects to register. Each one claims the tag written on it.")]
        [SerializeField]
        private List<TextEffectAsset> m_Effects = new List<TextEffectAsset>();

        private static TextEffectLibrary s_Active;

        /// <summary>The library currently registered, if any.</summary>
        public static TextEffectLibrary Active => s_Active;

        public List<TextEffectAsset> Effects => m_Effects;

        private void OnEnable()
        {
            s_Active = this;
            RegisterAll();
        }

        private void OnValidate()
        {
            // Renaming a tag leaves the old one registered, so start from a clean slate.
            if (s_Active == this)
                Reregister();
        }

        /// <summary>Drops every registration and puts this library's effects back.</summary>
        public void Reregister()
        {
            TextEffectRegistry.Reset();
            RegisterAll();
        }

        private void RegisterAll()
        {
            var claimed = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < m_Effects.Count; i++)
            {
                TextEffectAsset asset = m_Effects[i];
                if (asset == null)
                    continue;

                string tag = asset.Tag;
                if (string.IsNullOrWhiteSpace(tag))
                {
                    Debug.LogWarning($"[TextBoxEnhance] '{asset.name}' has no tag, so nothing can call it.", asset);
                    continue;
                }

                if (!claimed.Add(tag))
                {
                    Debug.LogWarning($"[TextBoxEnhance] Two effects in '{name}' both claim <{tag}>. " +
                                     "Only the first one runs.", asset);
                    continue;
                }

                if (TextEffectRegistry.IsBuiltIn(tag))
                {
                    Debug.LogWarning($"[TextBoxEnhance] '{asset.name}' claims <{tag}>, which is already a " +
                                     "built-in effect. The asset wins; rename it if that was not deliberate.", asset);
                }

                TextEffectRegistry.Register(tag, new DataTextEffect(asset));
            }
        }
    }
}
