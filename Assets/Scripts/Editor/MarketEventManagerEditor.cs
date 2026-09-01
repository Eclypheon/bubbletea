using UnityEditor;
using UnityEngine;

namespace BubbleTeaShop.Editor
{
    [CustomEditor(typeof(MarketEventManager))]
    public class MarketEventManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            MarketEventManager manager = (MarketEventManager)target;

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("Live Event Testing & Preview", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Select any Market Event from the dropdown above to immediately test and preview its icon and HUD badge in-game.", EditorStyles.wordWrappedLabel);
                EditorGUILayout.Space(6);

                if (GUILayout.Button("Apply Selected Event (Live Preview)", GUILayout.Height(30)))
                {
                    manager.SetEventByType(manager.TestEventSelection);
                }

                if (GUILayout.Button("Clear Active Event", GUILayout.Height(24)))
                {
                    manager.ClearActiveEvent();
                }
            }

            if (manager.ActiveEvent != null)
            {
                EditorGUILayout.Space(8);
                EditorGUILayout.HelpBox($"Active Event: {manager.ActiveEvent.title}\n" +
                                       $"Event ID: {manager.ActiveEvent.eventId}\n" +
                                       $"Affected Key: {manager.ActiveEvent.affectedKey}\n" +
                                       $"Days Remaining: {manager.ActiveEvent.daysRemaining} / {manager.ActiveEvent.totalDurationDays}\n" +
                                       $"Price Mult: {manager.ActiveEvent.priceMultiplier:F2}x | Demand Mult: {manager.ActiveEvent.demandMultiplier:F2}x",
                                       MessageType.Info);
            }
        }
    }
}
