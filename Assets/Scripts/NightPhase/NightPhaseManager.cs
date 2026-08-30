using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BubbleTeaShop
{
    public class NightPhaseManager : MonoBehaviour
    {
        [Header("Canvas & Panels")]
        [SerializeField] private GameObject nightPanelRoot;
        [SerializeField] private GameObject marketTabPanel;
        [SerializeField] private GameObject foragingTabPanel;
        [SerializeField] private GameObject upgradesTabPanel;
        [SerializeField] private GameObject ledgerTabPanel;

        [Header("Navigation Buttons")]
        [SerializeField] private Button tabMarketButton;
        [SerializeField] private Button tabForagingButton;
        [SerializeField] private Button tabUpgradesButton;
        [SerializeField] private Button tabLedgerButton;
        [SerializeField] private Button sleepButton;

        [Header("Ledger Info")]
        [SerializeField] private TextMeshProUGUI ledgerSummaryText;
        [SerializeField] private TextMeshProUGUI rentStatusText;
        [SerializeField] private Button buyoutShopButton;
        [SerializeField] private TextMeshProUGUI buyoutButtonText;

        [Header("Foraging Buttons")]
        [SerializeField] private Button forageBambooBtn;
        [SerializeField] private Button forageHoneyBtn;
        [SerializeField] private Button forageMountainBtn;
        [SerializeField] private TextMeshProUGUI foragingLogText;

        private void Start()
        {
            if (tabMarketButton != null) tabMarketButton.onClick.AddListener(() => SwitchTab(0));
            if (tabForagingButton != null) tabForagingButton.onClick.AddListener(() => SwitchTab(1));
            if (tabUpgradesButton != null) tabUpgradesButton.onClick.AddListener(() => SwitchTab(2));
            if (tabLedgerButton != null) tabLedgerButton.onClick.AddListener(() => SwitchTab(3));
            if (sleepButton != null) sleepButton.onClick.AddListener(OnSleepClicked);

            if (forageBambooBtn != null) forageBambooBtn.onClick.AddListener(() => ForagingManager.Instance?.GoForaging("BambooGrove"));
            if (forageHoneyBtn != null) forageHoneyBtn.onClick.AddListener(() => ForagingManager.Instance?.GoForaging("HoneyMeadow"));
            if (forageMountainBtn != null) forageMountainBtn.onClick.AddListener(() => ForagingManager.Instance?.GoForaging("MistMountain"));
            if (buyoutShopButton != null) buyoutShopButton.onClick.AddListener(OnBuyoutClicked);

            if (ForagingManager.Instance != null)
            {
                ForagingManager.Instance.OnForagingResult += msg =>
                {
                    if (foragingLogText != null) foragingLogText.text = msg;
                };
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged += HandleStateChanged;
            }

            nightPanelRoot.SetActive(false);
        }

        private int lastDeductedDay = 0;

        private void HandleStateChanged(GameState state)
        {
            bool isNight = (state == GameState.NightPhase);
            if (nightPanelRoot != null) nightPanelRoot.SetActive(isNight);

            if (isNight)
            {
                int completedDay = DayManager.Instance != null ? DayManager.Instance.LastCompletedDay : 1;
                int currentDay = DayManager.Instance != null ? DayManager.Instance.CurrentDay : 1;

                if (lastDeductedDay != completedDay)
                {
                    lastDeductedDay = completedDay;
                    EconomyManager.Instance?.DeductDailySupplies(completedDay);
                }

                ForagingManager.Instance?.ResetNightForaging();
                UpdateTabsState(currentDay);
                UpdateForagingButtons(currentDay);
                UpdateMarketTab(currentDay);
                UpdateLedger();
                SwitchTab(3); // Start on Ledger
            }
        }

        private void UpdateTabsState(int day)
        {
            // 1. Market Tab: Unlocks on Day 2
            bool marketUnlocked = (day >= 2);
            if (tabMarketButton != null)
            {
                tabMarketButton.interactable = marketUnlocked;
                var t = tabMarketButton.GetComponentInChildren<TextMeshProUGUI>();
                if (t != null) t.text = marketUnlocked ? "Wholesale Market" : "Market (Day 2)";
            }

            // 2. Foraging Tab: Unlocks on Day 5
            bool foragingUnlocked = (day >= 5);
            if (tabForagingButton != null)
            {
                tabForagingButton.interactable = foragingUnlocked;
                var t = tabForagingButton.GetComponentInChildren<TextMeshProUGUI>();
                if (t != null) t.text = foragingUnlocked ? "Foraging Expedition" : "Foraging (Day 5)";
            }

            // 3. Upgrades Tab: Unlocks on Day 8 (Week 2)
            bool upgradesUnlocked = (day >= 8);
            if (tabUpgradesButton != null)
            {
                tabUpgradesButton.interactable = upgradesUnlocked;
                var t = tabUpgradesButton.GetComponentInChildren<TextMeshProUGUI>();
                if (t != null) t.text = upgradesUnlocked ? "Shop Upgrades" : "Upgrades (Day 8)";
            }
        }

        private void UpdateForagingButtons(int day)
        {
            if (ForagingManager.Instance == null) return;

            if (forageBambooBtn != null)
            {
                bool u = ForagingManager.Instance.IsZoneUnlocked("BambooGrove", day);
                forageBambooBtn.interactable = u;
                var t = forageBambooBtn.GetComponentInChildren<TextMeshProUGUI>();
                if (t != null) t.text = u ? "Bamboo Grove" : "Bamboo Grove (Day 5)";
            }

            if (forageHoneyBtn != null)
            {
                bool u = ForagingManager.Instance.IsZoneUnlocked("HoneyMeadow", day);
                forageHoneyBtn.interactable = u;
                var t = forageHoneyBtn.GetComponentInChildren<TextMeshProUGUI>();
                if (t != null) t.text = u ? "Honey Meadow" : "Honey Meadow (Day 11)";
            }

            if (forageMountainBtn != null)
            {
                bool u = ForagingManager.Instance.IsZoneUnlocked("MistMountain", day);
                forageMountainBtn.interactable = u;
                var t = forageMountainBtn.GetComponentInChildren<TextMeshProUGUI>();
                if (t != null) t.text = u ? "Mist Peak Mountain" : "Mist Mountain (Day 18)";
            }

            if (foragingLogText != null)
            {
                foragingLogText.text = "Select an unlocked region to forage wild ingredients tonight.";
            }
        }

        private void UpdateMarketTab(int day)
        {
            if (marketTabPanel == null || MarketManager.Instance == null) return;

            // Clear old market buttons if any
            for (int i = marketTabPanel.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(marketTabPanel.transform.GetChild(i).gameObject);
            }

            var catalog = MarketManager.Instance.GetAvailableCatalog(day);
            int cols = 3;
            float startX = -280f;
            float startY = 120f;
            float spacingX = 280f;
            float spacingY = 70f;

            for (int i = 0; i < catalog.Count; i++)
            {
                var item = catalog[i];
                int row = i / cols;
                int col = i % cols;
                Vector2 pos = new Vector2(startX + col * spacingX, startY - row * spacingY);

                GameObject btnObj = new GameObject($"Buy_{item.stockKey}", typeof(RectTransform), typeof(Image), typeof(Button));
                btnObj.transform.SetParent(marketTabPanel.transform, false);
                var rt = btnObj.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(260, 58);
                rt.anchoredPosition = pos;

                var img = btnObj.GetComponent<Image>();
                img.color = new Color(0.2f, 0.25f, 0.35f, 1f);

                GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                textObj.transform.SetParent(btnObj.transform, false);
                var trt = textObj.GetComponent<RectTransform>();
                trt.sizeDelta = rt.sizeDelta;
                var tmp = textObj.GetComponent<TextMeshProUGUI>();
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontSize = 15;
                tmp.color = Color.white;
                tmp.text = $"<b>{item.displayName}</b> (+{item.bundleQuantity})\n<color=#2ECC71>${item.price:F2}</color>";

                var btn = btnObj.GetComponent<Button>();
                btn.onClick.AddListener(() =>
                {
                    if (MarketManager.Instance.BuyItem(item))
                    {
                        UpdateLedger();
                    }
                });
            }
        }

        public void SwitchTab(int tabIndex)
        {
            int day = DayManager.Instance != null ? DayManager.Instance.CurrentDay : 1;

            if (tabIndex == 0 && day < 2)
            {
                HUDController.Instance?.ShowNotification("Wholesale Supermarket unlocks on Day 2!");
                return;
            }
            if (tabIndex == 1 && day < 5)
            {
                HUDController.Instance?.ShowNotification("Foraging Expeditions unlock on Day 5!");
                return;
            }
            if (tabIndex == 2 && day < 8)
            {
                HUDController.Instance?.ShowNotification("Shop Upgrades unlock on Day 8 (Week 2)!");
                return;
            }

            if (tabIndex == 0 && SupermarketViewController.Instance != null)
            {
                SupermarketViewController.Instance.OpenSupermarketView(day);
                return;
            }

            if (marketTabPanel != null) marketTabPanel.SetActive(tabIndex == 0);
            if (foragingTabPanel != null) foragingTabPanel.SetActive(tabIndex == 1);
            if (upgradesTabPanel != null) upgradesTabPanel.SetActive(tabIndex == 2);
            if (ledgerTabPanel != null) ledgerTabPanel.SetActive(tabIndex == 3);
        }

        private void UpdateLedger()
        {
            if (DayManager.Instance == null || EconomyManager.Instance == null) return;

            int completedDay = DayManager.Instance.LastCompletedDay;
            int currentDay = DayManager.Instance.CurrentDay;
            int daysLeft = EconomyManager.Instance.GetDaysUntilRent(currentDay);
            float totalRent = EconomyManager.Instance.GetTotalRentDue(currentDay);
            float baseRent = EconomyManager.Instance.GetRentDueForDay(currentDay);
            float accumulated = EconomyManager.Instance.AccumulatedRentOwed;

            float sales = DayManager.Instance.DailySalesTotal;
            float tips = DayManager.Instance.DailyTipsTotal;
            float suppliesExpense = EconomyManager.DailySuppliesExpense;
            float netProfit = sales + tips - suppliesExpense;
            string netProfitFormatted = netProfit >= 0
                ? $"<color=#2ECC71>+${netProfit:F2}</color>"
                : $"<color=#FF4444>-${Mathf.Abs(netProfit):F2}</color>";

            if (ledgerSummaryText != null)
            {
                ledgerSummaryText.text = $"<b>Day {completedDay} Summary:</b>\n" +
                                         $"• Customers Served: {DayManager.Instance.CustomersServedToday}/{DayManager.Instance.TotalCustomersToday}\n" +
                                         $"• Sales Revenue: <color=#2ECC71>+${sales:F2}</color>\n" +
                                         $"• Tips Earned: <color=#2ECC71>+${tips:F2}</color>\n" +
                                         $"• Daily Supplies & Utilities: <color=#FF4444>-${suppliesExpense:F2}</color> <i>(Tea, Cups, Ice, Sugar)</i>\n" +
                                         $"• Net Daily Profit: {netProfitFormatted}\n" +
                                         $"• Total Shop Balance: <color=#2ECC71>${EconomyManager.Instance.CurrentCash:F2}</color>";
            }

            if (rentStatusText != null)
            {
                if (accumulated > 0)
                {
                    rentStatusText.text = $"Weekly Rent: ${baseRent:F2} + <color=#FF4444>Overdue: ${accumulated:F2}</color> (Total: ${totalRent:F2}) | <color=#FFAA00>Extensions: 1/1 used</color>";
                }
                else
                {
                    string dueNotice = daysLeft == 0 ? "Due Today at Closing" : $"Due in {daysLeft} days";
                    rentStatusText.text = $"Weekly Rent: ${totalRent:F2} ({dueNotice})";
                }
            }

            if (buyoutButtonText != null)
            {
                buyoutButtonText.text = $"Buy Out Location (${EconomyManager.Instance.BuyoutGoal:N0})";
            }

            if (buyoutShopButton != null)
            {
                buyoutShopButton.interactable = EconomyManager.Instance.CanAfford(EconomyManager.Instance.BuyoutGoal);
            }

            // Check 4-Week Lease Victory (Day 28 completion)
            if (completedDay >= 28 && GameManager.Instance != null && EconomyManager.Instance != null && EconomyManager.Instance.AccumulatedRentOwed <= 0)
            {
                HUDController.Instance?.ShowNotification("🏆 Incredible! You have successfully completed the 4-week lease!", 5f);
            }
        }

        private void OnBuyoutClicked()
        {
            EconomyManager.Instance?.TryBuyoutShop();
        }

        private void OnSleepClicked()
        {
            GameManager.Instance?.EndNightAndSleep();
        }
    }
}
