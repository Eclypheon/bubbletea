using UnityEditor;
using UnityEngine;

namespace BubbleTeaShop.Editor
{
    [CustomEditor(typeof(SpriteManager))]
    public class SpriteManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            SpriteManager manager = (SpriteManager)target;

            EditorGUILayout.Space(12);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Single Source of Truth", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("All UI controllers and game subviews can read sprites from here instead of dragging them into multiple individual inspectors.", EditorStyles.wordWrappedLabel);
                EditorGUILayout.Space(6);

                if (GUILayout.Button("Reload / Auto-Populate All Sprites from Project", GUILayout.Height(30)))
                {
                    manager.EnsureAssetsLoaded();
                    EditorUtility.SetDirty(manager);
                }
            }
        }
    }
}
