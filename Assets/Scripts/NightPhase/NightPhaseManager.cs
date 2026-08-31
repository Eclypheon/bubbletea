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
        [SerializeField] private Button prepAreaButton;
        [SerializeField] private Button sleepButton;

        [Header("Ledger Info")]
        [SerializeField] private TextMeshProUGUI ledgerSummaryText;
        [SerializeField] private TextMeshProUGUI rentStatusText;
        [SerializeField] private Button buyoutShopButton;
        [SerializeField] private TextMeshProUGUI buyoutButtonText;
        [SerializeField] private Transform ledgerInventoryContainer;

        [Header("Foraging Buttons")]
        [SerializeField] private Button forageBambooBtn;
        [SerializeField] private Button forageHoneyBtn;
        [SerializeField] private Button forageMountainBtn;
        [SerializeField] private TextMeshProUGUI foragingLogText;

        public static NightPhaseManager Instance { get; private set; }

        public enum NightActivityType { None, Market, Foraging }

        [Header("Night Activity Limits")]
        [SerializeField] private NightActivityType performedActivityTonight = NightActivityType.None;

        public NightActivityType PerformedActivityTonight => performedActivityTonight;
        public bool HasPerformedNightActivityTonight => performedActivityTonight != NightActivityType.None;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (tabMarketButton != null) tabMarketButton.onClick.AddListener(() => SwitchTab(0));
            if (tabForagingButton != null) tabForagingButton.onClick.AddListener(() => SwitchTab(1));
            if (tabUpgradesButton != null) tabUpgradesButton.onClick.AddListener(() => SwitchTab(2));
            if (tabLedgerButton != null) tabLedgerButton.onClick.AddListener(() => SwitchTab(3));
            if (prepAreaButton != null) prepAreaButton.onClick.AddListener(OpenPrepArea);
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

            if (SupermarketViewController.Instance != null)
            {
                SupermarketViewController.Instance.OnSupermarketClosed += () =>
                {
                    int day = DayManager.Instance != null ? DayManager.Instance.CurrentDay : 1;
                    UpdateTabsState(day);
                    UpdateForagingButtons(day);
                };
            }

            if (PrepAreaViewController.Instance != null)
            {
                PrepAreaViewController.Instance.OnPrepAreaClosed += () =>
                {
                    int day = DayManager.Instance != null ? DayManager.Instance.CurrentDay : 1;
                    UpdateTabsState(day);
                    UpdateLedger();
                };
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged += HandleStateChanged;
            }

            nightPanelRoot.SetActive(false);
        }

        private int lastDeductedDay = 0;

        public void RecordActivity(NightActivityType activity)
        {
            performedActivityTonight = activity;
            DayManager.Instance?.RecordNightActivity();
            int day = DayManager.Instance != null ? DayManager.Instance.CurrentDay : 1;
            UpdateTabsState(day);
            UpdateForagingButtons(day);
        }

        private void HandleStateChanged(GameState state)
        {
            bool isNight = (state == GameState.NightPhase);
            if (nightPanelRoot != null) nightPanelRoot.SetActive(isNight);

            if (isNight)
            {
                performedActivityTonight = NightActivityType.None;
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

        public void UpdateTabsState(int day)
        {
            // 1. Market Tab: Unlocks on Day 2
            bool marketUnlocked = (day >= 2);
            if (tabMarketButton != null)
            {
                var t = tabMarketButton.GetComponentInChildren<TextMeshProUGUI>();
                if (!marketUnlocked)
                {
                    tabMarketButton.interactable = false;
                    if (t != null) t.text = "Market (Day 2)";
                }
                else if (performedActivityTonight == NightActivityType.Foraging)
                {
                    tabMarketButton.interactable = false;
                    if (t != null) t.text = "Market (Exhausted)";
                }
                else
                {
                    tabMarketButton.interactable = true;
                    if (t != null) t.text = "Wholesale Market";
                }
            }

            // 2. Foraging Tab: Unlocks on Day 5
            bool foragingUnlocked = (day >= 5);
            if (tabForagingButton != null)
            {
                var t = tabForagingButton.GetComponentInChildren<TextMeshProUGUI>();
                if (!foragingUnlocked)
                {
                    tabForagingButton.interactable = false;
                    if (t != null) t.text = "Foraging (Day 5)";
                }
                else if (performedActivityTonight == NightActivityType.Market)
                {
                    tabForagingButton.interactable = false;
                    if (t != null) t.text = "Foraging (Exhausted)";
                }
                else if (performedActivityTonight == NightActivityType.Foraging)
                {
                    tabForagingButton.interactable = true;
                    if (t != null) t.text = "Foraging (Completed)";
                }
                else
                {
                    tabForagingButton.interactable = true;
                    if (t != null) t.text = "Foraging Expedition";
                }
            }

            // 3. Upgrades Tab: Unlocks on Day 8 (Week 2)
            bool upgradesUnlocked = (day >= 8);
            if (tabUpgradesButton != null)
            {
                tabUpgradesButton.interactable = upgradesUnlocked;
                var t = tabUpgradesButton.GetComponentInChildren<TextMeshProUGUI>();
                if (t != null) t.text = upgradesUnlocked ? "Shop Upgrades" : "Upgrades (Day 8)";
            }

            // 4. Kitchen Prep Area Button: Unlocks on Day 5+
            if (prepAreaButton != null)
            {
                bool prepUnlocked = (day >= 5);
                prepAreaButton.interactable = prepUnlocked;
                var t = prepAreaButton.GetComponentInChildren<TextMeshProUGUI>();
                if (t != null) t.text = prepUnlocked ? "Kitchen Prep Area →" : "Prep Area (Day 5)";
            }
        }

        public void UpdateForagingButtons(int day)
        {
            if (ForagingManager.Instance == null) return;

            bool canForageTonight = (performedActivityTonight != NightActivityType.Market && !ForagingManager.Instance.HasForagedTonight);

            if (forageBambooBtn != null)
            {
                bool u = ForagingManager.Instance.IsZoneUnlocked("BambooGrove", day);
                forageBambooBtn.interactable = u && canForageTonight;
                var t = forageBambooBtn.GetComponentInChildren<TextMeshProUGUI>();
                if (t != null) t.text = u ? (canForageTonight ? "Bamboo Grove" : "Bamboo Grove (Exhausted)") : "Bamboo Grove (Day 5)";
            }

            if (forageHoneyBtn != null)
            {
                bool u = ForagingManager.Instance.IsZoneUnlocked("HoneyMeadow", day);
                forageHoneyBtn.interactable = u && canForageTonight;
                var t = forageHoneyBtn.GetComponentInChildren<TextMeshProUGUI>();
                if (t != null) t.text = u ? (canForageTonight ? "Honey Meadow" : "Honey Meadow (Exhausted)") : "Honey Meadow (Day 11)";
            }

            if (forageMountainBtn != null)
            {
                bool u = ForagingManager.Instance.IsZoneUnlocked("MistMountain", day);
                forageMountainBtn.interactable = u && canForageTonight;
                var t = forageMountainBtn.GetComponentInChildren<TextMeshProUGUI>();
                if (t != null) t.text = u ? (canForageTonight ? "Mist Peak Mountain" : "Mist Mountain (Exhausted)") : "Mist Mountain (Day 18)";
            }

            if (foragingLogText != null)
            {
                if (performedActivityTonight == NightActivityType.Market)
                {
                    foragingLogText.text = "<color=#FFAA00>Exhausted from Market trip! Only 1 night activity allowed per night.</color>";
                }
                else if (ForagingManager.Instance.HasForagedTonight)
                {
                    foragingLogText.text = "<color=#2ECC71>Foraging expedition completed for tonight. Rest up for tomorrow!</color>";
                }
                else
                {
                    foragingLogText.text = "Select an unlocked region to forage wild ingredients tonight.\n<i>(Note: Embarking will cause late opening tomorrow: -1 customer)</i>";
                }
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

            if (tabIndex == 0) // Market
            {
                if (day < 2)
                {
                    HUDController.Instance?.ShowNotification("Wholesale Supermarket unlocks on Day 2!");
                    return;
                }

                if (performedActivityTonight == NightActivityType.Foraging)
                {
                    HUDController.Instance?.ShowNotification("You are exhausted from tonight's Foraging expedition! Only 1 night activity allowed per night.", 4.5f);
                    return;
                }

                RecordActivity(NightActivityType.Market);

                if (SupermarketViewController.Instance != null)
                {
                    SupermarketViewController.Instance.OpenSupermarketView(day);
                    return;
                }
            }

            if (tabIndex == 1) // Foraging
            {
                if (day < 5)
                {
                    HUDController.Instance?.ShowNotification("Foraging Expeditions unlock on Day 5!");
                    return;
                }

                if (performedActivityTonight == NightActivityType.Market)
                {
                    HUDController.Instance?.ShowNotification("You are exhausted from visiting the Supermarket! Only 1 night activity allowed per night.", 4.5f);
                    return;
                }
            }

            if (tabIndex == 2 && day < 8)
            {
                HUDController.Instance?.ShowNotification("Shop Upgrades unlock on Day 8 (Week 2)!");
                return;
            }

            if (marketTabPanel != null) marketTabPanel.SetActive(tabIndex == 0);
            if (foragingTabPanel != null) foragingTabPanel.SetActive(tabIndex == 1);
            if (upgradesTabPanel != null) upgradesTabPanel.SetActive(tabIndex == 2);
            if (ledgerTabPanel != null) ledgerTabPanel.SetActive(tabIndex == 3);
        }

        public void OpenPrepArea()
        {
            int day = DayManager.Instance != null ? DayManager.Instance.CurrentDay : 1;
            if (PrepAreaViewController.Instance != null)
            {
                PrepAreaViewController.Instance.OpenPrepAreaView(day);
            }
            else
            {
                HUDController.Instance?.ShowNotification("Kitchen Prep Area is not available.", 3f);
            }
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
                var rt = ledgerSummaryText.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(-280f, 75f);
                    rt.sizeDelta = new Vector2(460f, 290f);
                    ledgerSummaryText.fontSize = 20;
                }

                ledgerSummaryText.text = $"<b>Day {completedDay} Summary:</b>\n" +
                                         $"• Customers Served: {DayManager.Instance.CustomersServedToday}/{DayManager.Instance.TotalCustomersToday}\n" +
                                         $"• Sales Revenue: <color=#2ECC71>+${sales:F2}</color>\n" +
                                         $"• Tips Earned: <color=#2ECC71>+${tips:F2}</color>\n" +
                                         $"• Daily Supplies: <color=#FF4444>-${suppliesExpense:F2}</color>\n" +
                                         $"• Net Daily Profit: {netProfitFormatted}\n" +
                                         $"• Total Shop Balance: <color=#2ECC71>${EconomyManager.Instance.CurrentCash:F2}</color>";
            }

            if (rentStatusText != null)
            {
                var rt = rentStatusText.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(-280f, -105f);
                    rt.sizeDelta = new Vector2(460f, 65f);
                }

                if (accumulated > 0)
                {
                    rentStatusText.text = $"Weekly Rent: ${baseRent:F2} + <color=#FF4444>Overdue: ${accumulated:F2}</color> (Total: ${totalRent:F2})\n<color=#FFAA00>Extensions: 1/1 used</color>";
                }
                else
                {
                    string dueNotice = daysLeft == 0 ? "Due Today at Closing" : $"Due in {daysLeft} days";
                    rentStatusText.text = $"Weekly Rent: ${totalRent:F2}\n({dueNotice})";
                }
            }

            if (buyoutButtonText != null)
            {
                buyoutButtonText.text = $"Buy Out Location (${EconomyManager.Instance.BuyoutGoal:N0})";
            }

            if (buyoutShopButton != null)
            {
                var rt = buyoutShopButton.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(-280f, -175f);
                    rt.sizeDelta = new Vector2(340f, 48f);
                }
                buyoutShopButton.interactable = EconomyManager.Instance.CanAfford(EconomyManager.Instance.BuyoutGoal);
            }

            // Check 4-Week Lease Victory (Day 28 completion)
            if (completedDay >= 28 && GameManager.Instance != null && EconomyManager.Instance != null && EconomyManager.Instance.AccumulatedRentOwed <= 0)
            {
                HUDController.Instance?.ShowNotification("🏆 Incredible! You have successfully completed the 4-week lease!", 5f);
            }

            // Populate visual inventory cards in Ledger Tab
            EnsureLedgerInventoryContainer();
            if (ledgerInventoryContainer != null)
            {
                PopulateLedgerInventoryCards(ledgerInventoryContainer);
            }
        }

        private void EnsureLedgerInventoryContainer()
        {
            if (ledgerInventoryContainer == null && ledgerTabPanel != null)
            {
                Transform existing = ledgerTabPanel.transform.Find("LedgerInventoryContainer");
                if (existing != null)
                {
                    ledgerInventoryContainer = existing;
                }
                else
                {
                    GameObject invObj = new GameObject("LedgerInventoryContainer", typeof(RectTransform));
                    invObj.transform.SetParent(ledgerTabPanel.transform, false);
                    var rt = invObj.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(250f, -15f);
                    rt.sizeDelta = new Vector2(560f, 490f);
                    ledgerInventoryContainer = invObj.transform;
                }
            }
        }

        private void PopulateLedgerInventoryCards(Transform container)
        {
            if (container == null || InventoryManager.Instance == null) return;

            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Destroy(container.GetChild(i).gameObject);
            }

            var milks = new (string key, string name, int count)[]
            {
                ("Milk_FreshMilk", "Fresh Milk", InventoryManager.Instance.GetMilkStock(MilkType.FreshMilk)),
                ("Milk_OatMilk", "Oat Milk", InventoryManager.Instance.GetMilkStock(MilkType.OatMilk)),
                ("Milk_CoconutMilk", "Coconut Milk", InventoryManager.Instance.GetMilkStock(MilkType.CoconutMilk)),
                ("Milk_CondensedMilk", "Condensed Milk", InventoryManager.Instance.GetMilkStock(MilkType.CondensedMilk))
            };

            var toppings = new (string key, string name, int count)[]
            {
                ("Topping_TapiocaPearls", "Tapioca Pearls", InventoryManager.Instance.GetToppingStock(ToppingType.TapiocaPearls)),
                ("Topping_PoppingBoba", "Popping Boba", InventoryManager.Instance.GetToppingStock(ToppingType.PoppingBoba)),
                ("Topping_GrassJelly", "Grass Jelly", InventoryManager.Instance.GetToppingStock(ToppingType.GrassJelly)),
                ("Topping_EggPudding", "Egg Pudding", InventoryManager.Instance.GetToppingStock(ToppingType.EggPudding)),
                ("Topping_CoconutJelly", "Coconut Jelly", InventoryManager.Instance.GetToppingStock(ToppingType.CoconutJelly)),
                ("Topping_CheeseFoam", "Cheese Foam", InventoryManager.Instance.GetToppingStock(ToppingType.CheeseFoam)),
                ("Topping_GoldenHoneyPearls", "Honey Pearls", InventoryManager.Instance.GetToppingStock(ToppingType.GoldenHoneyPearls))
            };

            // Left column: Milks (width ~260)
            GameObject milkCol = new GameObject("MilksCol", typeof(RectTransform));
            milkCol.transform.SetParent(container, false);
            var mRt = milkCol.GetComponent<RectTransform>();
            mRt.anchorMin = new Vector2(0, 0);
            mRt.anchorMax = new Vector2(0.48f, 1);
            mRt.offsetMin = Vector2.zero;
            mRt.offsetMax = Vector2.zero;
            PopulateLedgerCardList(milkCol.transform, milks, "MILKS");

            // Right column: Toppings (width ~280)
            GameObject topCol = new GameObject("ToppingsCol", typeof(RectTransform));
            topCol.transform.SetParent(container, false);
            var tRt = topCol.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0.52f, 0);
            tRt.anchorMax = new Vector2(1, 1);
            tRt.offsetMin = Vector2.zero;
            tRt.offsetMax = Vector2.zero;
            PopulateLedgerCardList(topCol.transform, toppings, "TOPPINGS");
        }

        private void PopulateLedgerCardList(Transform colTransform, (string key, string name, int count)[] items, string sectionTitle)
        {
            float cardHeight = 48f;
            float spacingY = 6f;
            float headerHeight = 28f;

            // Header
            GameObject headerObj = new GameObject($"Header_{sectionTitle}", typeof(RectTransform), typeof(TextMeshProUGUI));
            headerObj.transform.SetParent(colTransform, false);
            var headerRt = headerObj.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0, 1);
            headerRt.anchorMax = new Vector2(1, 1);
            headerRt.pivot = new Vector2(0.5f, 1);
            headerRt.sizeDelta = new Vector2(0, headerHeight);
            headerRt.anchoredPosition = new Vector2(0, 0);

            var headerTmp = headerObj.GetComponent<TextMeshProUGUI>();
            headerTmp.text = $"<b>{sectionTitle}</b>";
            headerTmp.fontSize = 20;
            headerTmp.alignment = TextAlignmentOptions.MidlineLeft;
            headerTmp.color = new Color(0.9f, 0.92f, 1f, 1f);

            // Cards
            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                Sprite icon = GetIngredientIcon(item.key);

                GameObject cardObj = new GameObject($"Card_{item.key}", typeof(RectTransform), typeof(Image));
                cardObj.transform.SetParent(colTransform, false);
                var rt = cardObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(1, 1);
                rt.pivot = new Vector2(0.5f, 1);
                rt.sizeDelta = new Vector2(0, cardHeight);
                rt.anchoredPosition = new Vector2(0, -(headerHeight + 4f) - (i * (cardHeight + spacingY)));

                var cardImg = cardObj.GetComponent<Image>();
                cardImg.color = new Color(0.12f, 0.16f, 0.24f, 0.92f);

                // Left Icon
                float leftOffset = 10f;
                if (icon != null)
                {
                    GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                    iconObj.transform.SetParent(cardObj.transform, false);
                    var iconRt = iconObj.GetComponent<RectTransform>();
                    iconRt.anchorMin = new Vector2(0, 0.5f);
                    iconRt.anchorMax = new Vector2(0, 0.5f);
                    iconRt.pivot = new Vector2(0.5f, 0.5f);
                    iconRt.sizeDelta = new Vector2(38, 38);
                    iconRt.anchoredPosition = new Vector2(6, 0);

                    var img = iconObj.GetComponent<Image>();
                    img.sprite = icon;
                    img.preserveAspect = true;
                    leftOffset = 48f;
                }

                // Right Count Pill
                GameObject pillObj = new GameObject("CountPill", typeof(RectTransform), typeof(Image));
                pillObj.transform.SetParent(cardObj.transform, false);
                var pillRt = pillObj.GetComponent<RectTransform>();
                pillRt.anchorMin = new Vector2(1, 0.5f);
                pillRt.anchorMax = new Vector2(1, 0.5f);
                pillRt.pivot = new Vector2(1, 0.5f);
                pillRt.sizeDelta = new Vector2(74, 32);
                pillRt.anchoredPosition = new Vector2(-6, 0);

                var pillImg = pillObj.GetComponent<Image>();
                pillImg.color = new Color(0.18f, 0.24f, 0.36f, 0.90f);

                GameObject countTextObj = new GameObject("CountText", typeof(RectTransform), typeof(TextMeshProUGUI));
                countTextObj.transform.SetParent(pillObj.transform, false);
                var countTextRt = countTextObj.GetComponent<RectTransform>();
                countTextRt.anchorMin = Vector2.zero;
                countTextRt.anchorMax = Vector2.one;
                countTextRt.offsetMin = Vector2.zero;
                countTextRt.offsetMax = Vector2.zero;

                var countTmp = countTextObj.GetComponent<TextMeshProUGUI>();
                countTmp.text = FormatStockCount(item.count);
                countTmp.fontSize = 17;
                countTmp.alignment = TextAlignmentOptions.Center;

                // Middle Name
                GameObject nameObj = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
                nameObj.transform.SetParent(cardObj.transform, false);
                var nameRt = nameObj.GetComponent<RectTransform>();
                nameRt.anchorMin = new Vector2(0, 0);
                nameRt.anchorMax = new Vector2(1, 1);
                nameRt.offsetMin = new Vector2(leftOffset, 0);
                nameRt.offsetMax = new Vector2(-82, 0);

                var nameTmp = nameObj.GetComponent<TextMeshProUGUI>();
                nameTmp.text = $"<b>{item.name}</b>";
                nameTmp.fontSize = 17;
                nameTmp.alignment = TextAlignmentOptions.MidlineLeft;
                nameTmp.color = Color.white;
            }
        }

        private string FormatStockCount(int count)
        {
            string colorHex = count == 0 ? "#FF4444" : (count <= 6 ? "#F1C40F" : "#2ECC71");
            return $"<color={colorHex}>x {count:D2}</color>";
        }

        private Sprite GetIngredientIcon(string key)
        {
            if (CashRegisterInventoryUI.Instance != null)
            {
                var icon = CashRegisterInventoryUI.Instance.GetIngredientIcon(key);
                if (icon != null) return icon;
            }

            if (CupStation.Instance != null)
            {
                return key switch
                {
                    "Topping_TapiocaPearls" => CupStation.Instance.TapiocaSprite,
                    "Topping_PoppingBoba" => CupStation.Instance.PoppingBobaSprite,
                    "Topping_GrassJelly" => CupStation.Instance.GrassJellySprite,
                    "Topping_EggPudding" => CupStation.Instance.EggPuddingSprite,
                    "Topping_CoconutJelly" => CupStation.Instance.CoconutJellySprite,
                    "Topping_CheeseFoam" => CupStation.Instance.CheeseFoamSprite,
                    "Topping_GoldenHoneyPearls" => CupStation.Instance.GoldenHoneyPearlsSprite,
                    _ => null
                };
            }

            return null;
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
