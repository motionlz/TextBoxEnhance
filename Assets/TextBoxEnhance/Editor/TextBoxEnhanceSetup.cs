using TMPro;
using TMPro.EditorUtilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TextBoxEnhance.EditorTools
{
    /// <summary>
    /// Menu commands for getting a text box on screen: importing the TextMeshPro
    /// resources a fresh project lacks, and spawning a ready-to-use label.
    /// </summary>
    public static class TextBoxEnhanceSetup
    {
        private const string EssentialResourcesPath = "/Package Resources/TMP Essential Resources.unitypackage";

        /// <summary>
        /// True once TMP's settings asset and default font exist. A fresh project has
        /// neither, and every TMP label renders blank until they are imported.
        /// </summary>
        public static bool TextMeshProResourcesInstalled => AssetDatabase.FindAssets("t:TMP_Settings").Length > 0;

        [MenuItem("Tools/TextBox Enhance/Import TextMeshPro Resources", priority = 100)]
        public static void ImportTextMeshProResources()
        {
            if (TextMeshProResourcesInstalled)
            {
                Debug.Log("[TextBoxEnhance] TextMeshPro resources are already installed.");
                return;
            }

            // Unity's own menu command opens a confirmation dialog; import silently instead.
            AssetDatabase.ImportPackage(TMP_EditorUtility.packageFullPath + EssentialResourcesPath, false);
        }

        [MenuItem("GameObject/UI/Animated Text Box", priority = 2030)]
        public static void CreateAnimatedTextBox(MenuCommand command)
        {
            if (!TextMeshProResourcesInstalled)
                ImportTextMeshProResources();

            Canvas canvas = FindOrCreateCanvas(command.context as GameObject);

            var go = new GameObject("Animated Text Box", typeof(RectTransform));
            GameObjectUtility.SetParentAndAlign(go, canvas.gameObject);

            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(600f, 200f);
            rect.anchoredPosition = Vector2.zero;

            var label = go.AddComponent<TextMeshProUGUI>();
            label.fontSize = 40f;
            label.alignment = TextAlignmentOptions.TopLeft;
            label.textWrappingMode = TextWrappingModes.Normal;

            var box = go.AddComponent<AnimatedTextBox>();
            box.Text = "The <wave>sea</wave> was <rainbow>calm</rainbow>, " +
                       "<pause=0.4>and then it <shake a=0.08>wasn't</shake>.";

            Undo.RegisterCreatedObjectUndo(go, "Create Animated Text Box");
            Selection.activeGameObject = go;
        }

        private static Canvas FindOrCreateCanvas(GameObject context)
        {
            var existing = context != null ? context.GetComponentInParent<Canvas>() : null;
            if (existing != null)
                return existing;

            existing = Object.FindFirstObjectByType<Canvas>();
            if (existing != null)
                return existing;

            var canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            Undo.RegisterCreatedObjectUndo(canvasObject, "Create Canvas");
            return canvas;
        }
    }
}
