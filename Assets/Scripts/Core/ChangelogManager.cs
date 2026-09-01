using System;
using System.Collections.Generic;
using UnityEngine;

namespace BubbleTeaShop
{
    [Serializable]
    public struct VersionEntry
    {
        public string version;
        public string releaseDate;
        public string summaryTitle;
        [TextArea(5, 12)]
        public string notes;
    }

    public class ChangelogManager : MonoBehaviour
    {
        public static ChangelogManager Instance { get; private set; }

        public const string CurrentVersion = "v1.2.0";

        [Header("Version History")]
        [SerializeField] private List<VersionEntry> history = new List<VersionEntry>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitializeHistoryIfEmpty();
        }

        private void InitializeHistoryIfEmpty()
        {
            if (history.Count > 0) return;

            history.Add(new VersionEntry
            {
                version = "v1.2.0",
                releaseDate = "2026-09-01",
                summaryTitle = "Topping Visuals & Dismissal Safety",
                notes = "<b>• Multi-Topping Layering:</b> Multiple toppings now dynamically stack inside the cup without vertical squishing.\n" +
                        "<b>• Calibrated Cheese Foam:</b> Placed across the top rim of the drink with tuned width & thickness.\n" +
                        "<b>• Bell Dismissal Safety:</b> Ringing the bell while an unserved customer is waiting prompts for a 2nd confirmation ring.\n" +
                        "<b>• $1,500 Buyout Goal:</b> Recalibrated default store buyout goal for a balanced 4-week story arc.\n" +
                        "<b>• Misty Mountains & Centrifuge:</b> Foraging minigame and kitchen centrifuge station."
            });
        }

        public string GetLatestVersion() => CurrentVersion;

        public VersionEntry GetLatestEntry()
        {
            InitializeHistoryIfEmpty();
            return history.Count > 0 ? history[0] : default;
        }

        public string GetFormattedLatestChangelog()
        {
            var entry = GetLatestEntry();
            return $"<b><size=120%>{entry.version} ({entry.releaseDate})</size></b>\n" +
                   $"<i>{entry.summaryTitle}</i>\n\n" +
                   $"{entry.notes}";
        }

        public List<VersionEntry> GetAllEntries()
        {
            InitializeHistoryIfEmpty();
            return history;
        }
    }
}
