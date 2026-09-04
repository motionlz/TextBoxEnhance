using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TextBoxEnhance.Samples.EditorTools
{
    /// <summary>
    /// Builds the demo scene from code rather than shipping a hand-authored one, so it
    /// cannot rot against the component's serialized fields.
    /// </summary>
    public static class DemoSceneBuilder
    {
        private const string ScenePath = "Assets/TextBoxEnhance/Samples/TextBoxEnhanceDemo.unity";

        private static readonly Color Background = new Color(0.07f, 0.08f, 0.11f);
        private static readonly Color Panel = new Color(0.13f, 0.15f, 0.20f, 0.95f);

        [MenuItem("Tools/TextBox Enhance/Create Demo Scene", priority = 120)]
        public static void CreateDemoScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildCamera();
            Canvas canvas = BuildCanvas();
            RectTransform panel = BuildPanel(canvas);
            AnimatedTextBox box = BuildTextBox(panel);
            BuildAdvanceButton(canvas, box.GetComponent<DialogueSequence>());

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();

            Debug.Log("[TextBoxEnhance] Demo scene written to " + ScenePath);
        }

        private static void BuildCamera()
        {
            var cameraObject = new GameObject("Main Camera", typeof(Camera));
            cameraObject.tag = "MainCamera";

            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Background;
            camera.orthographic = true;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        }

        private static Canvas BuildCanvas()
        {
            var canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            return canvas;
        }

        private static RectTransform BuildPanel(Canvas canvas)
        {
            var panelObject = new GameObject("Dialogue Panel", typeof(RectTransform), typeof(Image));
            var rect = panelObject.GetComponent<RectTransform>();
            rect.SetParent(canvas.transform, false);
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 80f);
            rect.sizeDelta = new Vector2(1500f, 380f);

            panelObject.GetComponent<Image>().color = Panel;
            return rect;
        }

        private static AnimatedTextBox BuildTextBox(RectTransform panel)
        {
            var labelObject = new GameObject("Animated Text Box", typeof(RectTransform));
            var rect = labelObject.GetComponent<RectTransform>();
            rect.SetParent(panel, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(56f, 56f);
            rect.offsetMax = new Vector2(-56f, -40f);

            var label = labelObject.AddComponent<TextMeshProUGUI>();
            label.fontSize = 46f;
            label.alignment = TextAlignmentOptions.TopLeft;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.color = Color.white;

            AnimatedTextBox box = labelObject.AddComponent<AnimatedTextBox>();
            labelObject.AddComponent<DialogueSequence>();
            return box;
        }

        private static void BuildAdvanceButton(Canvas canvas, DialogueSequence sequence)
        {
            var buttonObject = new GameObject("Advance Button", typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.SetParent(canvas.transform, false);
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(620f, 20f);
            rect.sizeDelta = new Vector2(260f, 64f);

            buttonObject.GetComponent<Image>().color = new Color(0.24f, 0.42f, 0.75f);

            var labelObject = new GameObject("Label", typeof(RectTransform));
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.SetParent(rect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var label = labelObject.AddComponent<TextMeshProUGUI>();
            label.text = "Continue";
            label.fontSize = 28f;
            label.alignment = TextAlignmentOptions.Center;

            // Wired here rather than by hand so the demo works the moment it is created.
            UnityEventTools.AddPersistentListener(
                buttonObject.GetComponent<Button>().onClick, sequence.Advance);
        }
    }
}
