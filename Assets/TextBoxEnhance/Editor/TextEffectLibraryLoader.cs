using System.Collections.Generic;
using System.Linq;
using TextBoxEnhance.Data;
using UnityEditor;
using UnityEngine;

namespace TextBoxEnhance.EditorTools
{
    /// <summary>
    /// Gets the effect library loaded in both places it has to be alive: the editor,
    /// where nothing would otherwise open the asset, and the player, where an asset
    /// nothing references is stripped from the build.
    /// </summary>
    [InitializeOnLoad]
    public static class TextEffectLibraryLoader
    {
        static TextEffectLibraryLoader()
        {
            // The asset database is not reliably queryable while the domain is still
            // loading, so ask on the next editor tick instead.
            EditorApplication.delayCall += () => LoadLibrary();
        }

        /// <summary>Finds the project's library and touches it so its OnEnable registers.</summary>
        public static TextEffectLibrary LoadLibrary()
        {
            string[] found = AssetDatabase.FindAssets("t:" + nameof(TextEffectLibrary));
            if (found.Length == 0)
                return null;

            if (found.Length > 1)
            {
                string paths = string.Join(", ", found.Select(AssetDatabase.GUIDToAssetPath));
                Debug.LogWarning($"[TextBoxEnhance] More than one effect library exists ({paths}). " +
                                 "Only the first is registered; merge them or delete the spares.");
            }

            var library = AssetDatabase.LoadAssetAtPath<TextEffectLibrary>(
                AssetDatabase.GUIDToAssetPath(found[0]));

            library?.Reregister();
            return library;
        }

        /// <summary>
        /// Adds the library to the preloaded assets list, which is what carries it into
        /// a build. Without this the effects work in the editor and vanish in the player
        /// -- the single most confusing way this could fail.
        /// </summary>
        public static void EnsurePreloaded(TextEffectLibrary library)
        {
            if (library == null)
                return;

            List<Object> preloaded = PlayerSettings.GetPreloadedAssets().ToList();
            if (preloaded.Contains(library))
                return;

            preloaded.RemoveAll(asset => asset == null);
            preloaded.Add(library);
            PlayerSettings.SetPreloadedAssets(preloaded.ToArray());

            Debug.Log($"[TextBoxEnhance] Added '{library.name}' to preloaded assets so it reaches builds.");
        }

        [MenuItem("Tools/TextBox Enhance/Create Effect Library", priority = 140)]
        private static void CreateLibraryMenu()
        {
            TextEffectLibrary library = FindOrCreateLibrary();
            Selection.activeObject = library;
            EditorGUIUtility.PingObject(library);
        }

        /// <summary>Returns the project's library, creating it the first time it is needed.</summary>
        public static TextEffectLibrary FindOrCreateLibrary()
        {
            TextEffectLibrary existing = LoadLibrary();
            if (existing != null)
            {
                EnsurePreloaded(existing);
                return existing;
            }

            // Deliberately outside the package folder. Installed from a git URL a package
            // is read-only, so a library living inside it could never be added to -- the
            // first press of Add to Library would fail and there would be no way to fix
            // it short of copying the package into Assets.
            const string folder = "Assets/TextBoxEnhance Effects";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets", "TextBoxEnhance Effects");

            var library = ScriptableObject.CreateInstance<TextEffectLibrary>();
            AssetDatabase.CreateAsset(library, folder + "/TextEffectLibrary.asset");
            AssetDatabase.SaveAssets();

            EnsurePreloaded(library);
            return library;
        }
    }
}
