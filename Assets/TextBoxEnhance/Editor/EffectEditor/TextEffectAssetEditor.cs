using TextBoxEnhance.Data;
using UnityEditor;
using UnityEngine;

namespace TextBoxEnhance.EditorTools
{
    /// <summary>
    /// Inspector for an effect asset. The raw layer list is still here for anyone who
    /// wants it, but the button comes first: layers are hard to judge without watching
    /// them move.
    /// </summary>
    [CustomEditor(typeof(TextEffectAsset))]
    public sealed class TextEffectAssetEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Open in Effect Editor", GUILayout.Height(26f)))
                TextEffectEditorWindow.Open((TextEffectAsset)target);

            EditorGUILayout.Space(4f);
            DrawDefaultInspector();
        }
    }
}
