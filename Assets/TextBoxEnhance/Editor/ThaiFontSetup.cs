using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace TextBoxEnhance.EditorTools
{
    /// <summary>
    /// Turns a font file already in the project into a TextMeshPro font asset that can
    /// render any script, Thai included.
    /// </summary>
    /// <remarks>
    /// The font TextMeshPro ships with covers Latin and nothing else, so Thai text comes
    /// out as a row of empty boxes -- which reads as the animation being broken rather
    /// than the font being wrong.
    ///
    /// The asset is created with a dynamic atlas: glyphs are rendered as they are first
    /// used, so one asset covers whatever the font itself covers without anyone picking
    /// character ranges up front.
    ///
    /// No font is bundled here on purpose. Fonts are licensed separately from code, and
    /// which one ends up in a project is a decision for whoever ships it.
    /// </remarks>
    public static class ThaiFontSetup
    {
        private const int SamplingPointSize = 90;
        private const int AtlasPadding = 9;
        private const int AtlasSize = 1024;

        [MenuItem("Assets/TextBox Enhance/Create TMP Font Asset", true)]
        private static bool ValidateCreateFontAsset()
        {
            return Selection.activeObject is Font;
        }

        [MenuItem("Assets/TextBox Enhance/Create TMP Font Asset", false, 2000)]
        private static void CreateFontAssetFromSelection()
        {
            var font = Selection.activeObject as Font;
            if (font == null)
                return;

            TMP_FontAsset asset = Create(font);
            if (asset == null)
                return;

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        [MenuItem("Tools/TextBox Enhance/Create TMP Font Asset...", priority = 160)]
        private static void CreateFontAssetFromMenu()
        {
            EditorUtility.DisplayDialog(
                "Create a TMP font asset",
                "Put a .ttf or .otf that covers the script you need into the project, " +
                "select it, then use Assets > TextBox Enhance > Create TMP Font Asset.\n\n" +
                "For Thai, any font with Thai glyphs works. Noto Sans Thai is a common " +
                "choice and is licensed for redistribution; the Windows system fonts are " +
                "not, so copying one into a project you ship needs checking first.",
                "Got it");
        }

        /// <summary>
        /// Creates a dynamic font asset next to <paramref name="font"/> and returns it.
        /// </summary>
        public static TMP_FontAsset Create(Font font)
        {
            string sourcePath = AssetDatabase.GetAssetPath(font);
            if (string.IsNullOrEmpty(sourcePath))
            {
                Debug.LogError("[TextBoxEnhance] That font is not an asset in this project. " +
                               "Copy the font file into Assets first.");
                return null;
            }

            TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(
                font, SamplingPointSize, AtlasPadding, GlyphRenderMode.SDFAA,
                AtlasSize, AtlasSize, AtlasPopulationMode.Dynamic);

            if (asset == null)
            {
                Debug.LogError($"[TextBoxEnhance] TextMeshPro could not build a font asset from '{font.name}'.");
                return null;
            }

            // Built with a forward slash rather than Path.Combine: the asset database
            // wants Unity-style paths whatever the platform separator happens to be.
            string folder = (Path.GetDirectoryName(sourcePath) ?? "Assets")
                .Replace(Path.DirectorySeparatorChar, '/');

            string path = string.Concat(folder, "/",
                Path.GetFileNameWithoutExtension(sourcePath), " SDF.asset");

            asset.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(asset, AssetDatabase.GenerateUniqueAssetPath(path));

            // The atlas texture and material live inside the asset rather than beside it.
            asset.atlasTextures[0].name = asset.name + " Atlas";
            AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
            AssetDatabase.AddObjectToAsset(asset.material, asset);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[TextBoxEnhance] Created '{asset.name}'. Assign it to your text, or set it as the " +
                      "default in Project Settings > TextMesh Pro > Settings.", asset);

            return asset;
        }

        /// <summary>
        /// The characters in <paramref name="text"/> that a font asset cannot draw.
        /// Empty when everything can be rendered.
        /// </summary>
        public static bool TryFindMissingCharacters(TMP_FontAsset font, string text, out List<char> missing)
        {
            missing = null;

            if (font == null || string.IsNullOrEmpty(text))
                return false;

            return !font.HasCharacters(text, out missing) && missing != null && missing.Count > 0;
        }
    }
}
