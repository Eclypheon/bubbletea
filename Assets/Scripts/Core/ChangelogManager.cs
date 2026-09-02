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
        [TextArea(5, 15)]
        public string notes;
    }

    public class ChangelogManager : MonoBehaviour
    {
        public static ChangelogManager Instance { get; private set; }

        public const string CurrentVersion = "v1.4.0";

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
                version = "v1.4.0",
                releaseDate = "2026-09-02",
                summaryTitle = "Simplified Ratings, Real-Time Payout HUD & Supermarket Feedback",
                notes = "<b>• Simplified 1-Star Deductions:</b> Deducts exactly 1 star per mistake (minimum 1 star floor).\n" +
                        "<b>• Slowness Star Deductions:</b> Deducts 1 star per 5% below 20% patience.\n" +
                        "<b>• 90% Speed Bonus:</b> Full +30% tip speed bonus when patience >= 90%.\n" +
                        "<b>• Real-Time Payout Display:</b> Min, Current (dynamic gradient), and Max payout HUD indicator.\n" +
                        "<b>• Animated Cash Feedback:</b> Floating green (+$X.XX) cash gain on drink service and red (-$X.XX) deduction on market purchases.\n" +
                        "<b>• Terminology & Lore Alignment:</b> Unified Egg Pudding, Bamboo Grove, Honey Meadows, and Misty Mountains naming."
            });

            history.Add(new VersionEntry
            {
                version = "v1.3.0",
                releaseDate = "2026-09-01",
                summaryTitle = "Topping Visuals, Bell Dismissal Safety, Auto-Sealer & Balanced Buyout",
                notes = "<b>• Multi-Topping Stacking:</b> Cups dynamically stack multiple topping layers without vertical compression.\n" +
                        "<b>• Calibrated Cheese Foam:</b> Tuned top-rim positioning, scale, and width.\n" +
                        "<b>• Bell Dismissal Safety:</b> Ringing the bell while an unserved customer is waiting prompts for a 2nd confirmation ring.\n" +
                        "<b>• Auto-Sealer Upgrade:</b> Added Commercial Auto-Sealer to automatically seal drinks on serve.\n" +
                        "<b>• $1,500 Buyout Goal:</b> Calibrated default store buyout goal for a balanced 4-week story arc.\n" +
                        "<b>• Legacy Asset Cleanup:</b> Completely removed non-existent Wild Mountain Tea references."
            });

            history.Add(new VersionEntry
            {
                version = "v1.2.0",
                releaseDate = "2026-08-28",
                summaryTitle = "Foraging Expeditions, Kitchen Prep & Shop Upgrades",
                notes = "<b>• Foraging Expeditions:</b> Playable expeditions across Bamboo Grove, Honey Meadows, and Misty Mountains.\n" +
                        "<b>• Kitchen Prep Area:</b> Added Blender & Sieve, Chopping Board, and High-Speed Centrifuge stations.\n" +
                        "<b>• Shop Upgrades:</b> Permanent upgrades including Storefront Sign, Ads, and Supply Contracts.\n" +
                        "<b>• Mentor Dialogue Skip:</b> Added buttons to skip Mentor dialogues.\n" +
                        "<b>• Ingredient Rebalance:</b> Rebalanced purchase costs and menu sell prices."
            });

            history.Add(new VersionEntry
            {
                version = "v1.1.0",
                releaseDate = "2026-08-20",
                summaryTitle = "Wholesale Market, Inventory & Mentor Guidance",
                notes = "<b>• Wholesale Market:</b> Night phase market to buy bulk cups, milks, and ingredients.\n" +
                        "<b>• Inventory Management:</b> Counter cash register inspection and full stock tracking.\n" +
                        "<b>• Market Events:</b> Dynamic daily events and market price fluctuations.\n" +
                        "<b>• Mentor Guidance:</b> Morning briefings and tutorial milestones.\n" +
                        "<b>• Order Ticket UI:</b> Physical clipped order tickets for customer preferences."
            });

            history.Add(new VersionEntry
            {
                version = "v1.0.0",
                releaseDate = "2026-08-10",
                summaryTitle = "Initial Release",
                notes = "<b>• Core Tea Brewing:</b> Tea base dispensers, sweetness/ice sliders, milk, toppings, and cup sealer.\n" +
                        "<b>• Customer Archetypes:</b> Neurodivergent customer personalities and unique patience mechanics.\n" +
                        "<b>• Cafe Loop:</b> Daily service shift, evaluation ratings, tips, and weekly rent cycles."
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
