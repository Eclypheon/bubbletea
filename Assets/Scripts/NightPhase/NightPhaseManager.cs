using System.Collections;
using System.Collections.Generic;
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
            EnsureNightSubviews();
        }

        private void EnsureNightSubviews()
        {
            Transform parentCanvas = (transform.parent != null) ? transform.parent : transform;

            if (BambooGroveViewController.Instance == null)
            {
                var existing = FindFirstObjectByType<BambooGroveViewController>(FindObjectsInactive.Include);
                if (existing == null)
                {
                    var go = new GameObject("BambooGroveViewController", typeof(RectTransform));
                    go.transform.SetParent(parentCanvas, false);
                    go.AddComponent<BambooGroveViewController>();
                }
            }

            if (HoneyMeadowViewController.Instance == null)
            {
                var existing = FindFirstObjectByType<HoneyMeadowViewController>(FindObjectsInactive.Include);
                if (existing == null)
                {
                    var go = new GameObject("HoneyMeadowViewController", typeof(RectTransform));
                    go.transform.SetParent(parentCanvas, false);
                    go.AddComponent<HoneyMeadowViewController>();
                }
            }

            if (MistMountainViewController.Instance == null)
            {
                var existing = FindFirstObjectByType<MistMountainViewController>(FindObjectsInactive.Include);
                if (existing == null)
                {
                    var go = new GameObject("MistMountainViewController", typeof(RectTransform));
                    go.transform.SetParent(parentCanvas, false);
                    go.AddComponent<MistMountainViewController>();
                }
            }

            if (PrepAreaViewController.Instance == null)
            {
                var existing = FindFirstObjectByType<PrepAreaViewController>(FindObjectsInactive.Include);
                if (existing == null)
                {
                    var go = new GameObject("PrepAreaViewController", typeof(RectTransform));
                    go.transform.SetParent(parentCanvas, false);
                    go.AddComponent<PrepAreaViewController>();
                }
            }

            if (SupermarketViewController.Instance == null)
            {
                var existing = FindFirstObjectByType<SupermarketViewController>(FindObjectsInactive.Include);
                if (existing == null)
                {
                    var go = new GameObject("SupermarketViewController", typeof(RectTransform));
                    go.transform.SetParent(parentCanvas, false);
                    go.AddComponent<SupermarketViewController>();
                }
            }
        }

        private void Start()
        {
            EnsureNightSubviews();

            if (tabMarketButton != null) tabMarketButton.onClick.AddListener(() => SwitchTab(0));
            if (tabForagingButton != null) tabForagingButton.onClick.AddListener(() => SwitchTab(1));
            if (tabUpgradesButton != null) tabUpgradesButton.onClick.AddListener(() => SwitchTab(2));
            if (tabLedgerButton != null) tabLedgerButton.onClick.AddListener(() => SwitchTab(3));
            if (prepAreaButton != null) prepAreaButton.onClick.AddListener(OpenPrepArea);
            if (sleepButton != null) sleepButton.onClick.AddListener(OnSleepClicked);

            if (forageBambooBtn != null) forageBambooBtn.onClick.AddListener(OnForageBambooClicked);
            if (forageHoneyBtn != null) forageHoneyBtn.onClick.AddListener(OnForageHoneyClicked);
            if (forageMountainBtn != null) forageMountainBtn.onClick.AddListener(OnForageMountainClicked);
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
                    StopPrepAreaButtonPulse();
                    int day = DayManager.Instance != null ? DayManager.Instance.CurrentDay : 1;
                    UpdateTabsState(day);
                    UpdateForagingButtons(day);
                };
            }

            if (PrepAreaViewController.Instance != null)
            {
                PrepAreaViewController.Instance.OnPrepAreaClosed += () =>
                {
                    StopPrepAreaButtonPulse();
                    int day = DayManager.Instance != null ? DayManager.Instance.CurrentDay : 1;
                    UpdateTabsState(day);
                    UpdateLedger();
                };
            }

            if (BambooGroveViewController.Instance != null)
            {
                BambooGroveViewController.Instance.OnBambooGroveClosed += () =>
                {
                    int day = DayManager.Instance != null ? DayManager.Instance.CurrentDay : 1;
                    UpdateTabsState(day);
                    UpdateForagingButtons(day);
                    UpdateLedger();
                    StartPrepAreaButtonPulse();
                };
            }

            if (HoneyMeadowViewController.Instance != null)
            {
                HoneyMeadowViewController.Instance.OnHoneyMeadowClosed += () =>
                {
                    int day = DayManager.Instance != null ? DayManager.Instance.CurrentDay : 1;
                    UpdateTabsState(day);
                    UpdateForagingButtons(day);
                    UpdateLedger();
                    StartPrepAreaButtonPulse();
                };
            }

            if (MistMountainViewController.Instance != null)
            {
                MistMountainViewController.Instance.OnMistMountainClosed += () =>
                {
                    int day = DayManager.Instance != null ? DayManager.Instance.CurrentDay : 1;
                    UpdateTabsState(day);
                    UpdateForagingButtons(day);
                    UpdateLedger();
                    StartPrepAreaButtonPulse();
                };
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged += HandleStateChanged;
            }

            nightPanelRoot.SetActive(false);
        }

        private int lastDeductedDay = 0;

        public void RecordActivity(NightActivityType activity, string subZone = "")
        {
            performedActivityTonight = activity;
            
            bool waivePenalty = false;
            if (UpgradeManager.Instance != null)
            {
                if (activity == NightActivityType.Market && UpgradeManager.Instance.HasUpgrade(UpgradeType.NightChauffeur))
                {
                    waivePenalty = true;
                }
                else if (activity == NightActivityType.Foraging)
                {
                    if (subZone == "BambooGrove" && UpgradeManager.Instance.HasUpgrade(UpgradeType.BambooGroveTrailMap)) waivePenalty = true;
                    else if (subZone == "HoneyMeadow" && UpgradeManager.Instance.HasUpgrade(UpgradeType.HoneyMeadowsTrailMap)) waivePenalty = true;
                    else if (subZone == "MistMountain" && UpgradeManager.Instance.HasUpgrade(UpgradeType.MistyMountainsTrailMap)) waivePenalty = true;
                    else if (string.IsNullOrEmpty(subZone) && UpgradeManager.Instance.HasUpgrade(UpgradeType.BambooGroveTrailMap)) waivePenalty = true;
                }
            }

            if (!waivePenalty)
            {
                DayManager.Instance?.RecordNightActivity();
            }
            else
            {
                Debug.Log($"[NightPhaseManager] Late opening penalty waived by upgrade for {activity} ({subZone})!");
            }

            int day = DayManager.Instance != null ? DayManager.Instance.CurrentDay : 1;
            UpdateTabsState(day);
            UpdateForagingButtons(day);
        }

        private void HandleStateChanged(GameState state)
        {
            bool isNight = (state == GameState.NightPhase);
            if (nightPanelRoot != null) nightPanelRoot.SetActive(isNight);

            if (!isNight)
            {
                StopPrepAreaButtonPulse();
            }

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
                UpdateUpgradesTab();
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
                if (t != null) t.text = prepUnlocked ? "Kitchen Prep Area ->" : "Prep Area (Day 5)";
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

            if (tabIndex == 2)
            {
                if (day < 8)
                {
                    HUDController.Instance?.ShowNotification("Shop Upgrades unlock on Day 8 (Week 2)!");
                    return;
                }
                UpdateUpgradesTab();
            }

            if (marketTabPanel != null) marketTabPanel.SetActive(tabIndex == 0);
            if (foragingTabPanel != null) foragingTabPanel.SetActive(tabIndex == 1);
            if (upgradesTabPanel != null) upgradesTabPanel.SetActive(tabIndex == 2);
            if (ledgerTabPanel != null) ledgerTabPanel.SetActive(tabIndex == 3);
        }

        private Coroutine prepAreaPulseRoutine;
        private Vector3 prepAreaBaseScale = Vector3.one;
        private Color prepAreaBaseColor = Color.white;
        private bool hasCapturedPrepAreaBaseProps = false;

        public void StartPrepAreaButtonPulse()
        {
            if (prepAreaButton == null || !prepAreaButton.interactable) return;

            if (!hasCapturedPrepAreaBaseProps)
            {
                prepAreaBaseScale = prepAreaButton.transform.localScale;
                if (prepAreaButton.image != null)
                {
                    prepAreaBaseColor = prepAreaButton.image.color;
                }
                hasCapturedPrepAreaBaseProps = true;
            }

            if (prepAreaPulseRoutine != null) StopCoroutine(prepAreaPulseRoutine);
            prepAreaPulseRoutine = StartCoroutine(PrepAreaButtonPulseRoutine());
        }

        public void StopPrepAreaButtonPulse()
        {
            if (prepAreaPulseRoutine != null)
            {
                StopCoroutine(prepAreaPulseRoutine);
                prepAreaPulseRoutine = null;
            }
            if (prepAreaButton != null && hasCapturedPrepAreaBaseProps)
            {
                prepAreaButton.transform.localScale = prepAreaBaseScale;
                if (prepAreaButton.image != null)
                {
                    prepAreaButton.image.color = prepAreaBaseColor;
                }
            }
        }

        private IEnumerator PrepAreaButtonPulseRoutine()
        {
            if (prepAreaButton == null) yield break;

            Image btnImage = prepAreaButton.image;
            // Vibrant glowing gold / amber highlight to draw player attention
            Color brightPulseColor = new Color(1f, 0.88f, 0.30f, 1f);

            while (prepAreaButton != null && prepAreaButton.gameObject.activeInHierarchy)
            {
                float t = Time.time * 4.0f;
                float pulse = (Mathf.Sin(t) + 1f) * 0.5f; // 0 to 1

                prepAreaButton.transform.localScale = prepAreaBaseScale * (1f + (pulse * 0.09f));

                if (btnImage != null)
                {
                    btnImage.color = Color.Lerp(prepAreaBaseColor, brightPulseColor, pulse * 0.9f);
                }

                yield return null;
            }

            if (prepAreaButton != null)
            {
                prepAreaButton.transform.localScale = prepAreaBaseScale;
                if (btnImage != null)
                {
                    btnImage.color = prepAreaBaseColor;
                }
            }
            prepAreaPulseRoutine = null;
        }

        public void OpenPrepArea()
        {
            StopPrepAreaButtonPulse();
            EnsureNightSubviews();
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

        private void OnForageBambooClicked()
        {
            int day = DayManager.Instance != null ? DayManager.Instance.CurrentDay : 1;
            if (performedActivityTonight == NightActivityType.Market)
            {
                HUDController.Instance?.ShowNotification("You are exhausted from visiting the Supermarket! Only 1 night activity allowed per night.", 4.5f);
                return;
            }
            if (ForagingManager.Instance != null && ForagingManager.Instance.HasForagedTonight)
            {
                HUDController.Instance?.ShowNotification("You are exhausted from tonight's foraging expedition! Rest up for tomorrow.", 4.5f);
                return;
            }

            RecordActivity(NightActivityType.Foraging, "BambooGrove");
            ForagingManager.Instance?.SetForagedTonight();
            EnsureNightSubviews();

            if (BambooGroveViewController.Instance != null)
            {
                BambooGroveViewController.Instance.OpenBambooGroveView(day);
            }
            else
            {
                ForagingManager.Instance?.GoForaging("BambooGrove");
            }
        }

        private void OnForageHoneyClicked()
        {
            int day = DayManager.Instance != null ? DayManager.Instance.CurrentDay : 1;
            if (performedActivityTonight == NightActivityType.Market)
            {
                HUDController.Instance?.ShowNotification("You are exhausted from visiting the Supermarket! Only 1 night activity allowed per night.", 4.5f);
                return;
            }
            if (ForagingManager.Instance != null && ForagingManager.Instance.HasForagedTonight)
            {
                HUDController.Instance?.ShowNotification("You are exhausted from tonight's foraging expedition! Rest up for tomorrow.", 4.5f);
                return;
            }

            RecordActivity(NightActivityType.Foraging, "HoneyMeadow");
            ForagingManager.Instance?.SetForagedTonight();
            EnsureNightSubviews();

            if (HoneyMeadowViewController.Instance != null)
            {
                HoneyMeadowViewController.Instance.OpenHoneyMeadowView(day);
            }
            else
            {
                ForagingManager.Instance?.GoForaging("HoneyMeadow");
            }
        }

        private void OnForageMountainClicked()
        {
            int day = DayManager.Instance != null ? DayManager.Instance.CurrentDay : 1;
            if (performedActivityTonight == NightActivityType.Market)
            {
                HUDController.Instance?.ShowNotification("You are exhausted from visiting the Supermarket! Only 1 night activity allowed per night.", 4.5f);
                return;
            }
            if (ForagingManager.Instance != null && ForagingManager.Instance.HasForagedTonight)
            {
                HUDController.Instance?.ShowNotification("You are exhausted from tonight's foraging expedition! Rest up for tomorrow.", 4.5f);
                return;
            }

            RecordActivity(NightActivityType.Foraging, "MistMountain");
            ForagingManager.Instance?.SetForagedTonight();
            EnsureNightSubviews();

            if (MistMountainViewController.Instance != null)
            {
                MistMountainViewController.Instance.OpenMistMountainView(day);
            }
            else
            {
                ForagingManager.Instance?.GoForaging("MistMountain");
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
            float suppliesExpense = EconomyManager.Instance.CurrentDailySuppliesExpense;
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
                                         $"- Customers Served: {DayManager.Instance.CustomersServedToday}/{DayManager.Instance.TotalCustomersToday}\n" +
                                         $"- Sales Revenue: <color=#2ECC71>+${sales:F2}</color>\n" +
                                         $"- Tips Earned: <color=#2ECC71>+${tips:F2}</color>\n" +
                                         $"- Daily Supplies: <color=#FF4444>-${suppliesExpense:F2}</color>\n" +
                                         $"- Net Daily Profit: {netProfitFormatted}\n" +
                                         $"- Total Shop Balance: <color=#2ECC71>${EconomyManager.Instance.CurrentCash:F2}</color>";
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
                    rt.anchoredPosition = new Vector2(0f, 260f);
                    rt.sizeDelta = new Vector2(340f, 48f);
                }
                buyoutShopButton.interactable = EconomyManager.Instance.CanAfford(EconomyManager.Instance.BuyoutGoal);
            }

            // Check 4-Week Lease Victory (Day 28 completion)
            if (completedDay >= 28 && GameManager.Instance != null && EconomyManager.Instance != null && EconomyManager.Instance.AccumulatedRentOwed <= 0)
            {
                HUDController.Instance?.ShowNotification("Incredible! You have successfully completed the 4-week lease!", 5f);
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

            var allMilks = new (string key, string name, int count)[]
            {
                ("Milk_FreshMilk", "Fresh Milk", InventoryManager.Instance.GetMilkStock(MilkType.FreshMilk)),
                ("Milk_OatMilk", "Oat Milk", InventoryManager.Instance.GetMilkStock(MilkType.OatMilk)),
                ("Milk_CoconutMilk", "Coconut Milk", InventoryManager.Instance.GetMilkStock(MilkType.CoconutMilk)),
                ("Milk_CondensedMilk", "Condensed Milk", InventoryManager.Instance.GetMilkStock(MilkType.CondensedMilk))
            };
            var milksList = new List<(string key, string name, int count)>();
            foreach (var m in allMilks)
            {
                if (InventoryManager.Instance.HasEverHadStock(m.key))
                {
                    milksList.Add(m);
                }
            }
            var milks = milksList.ToArray();

            var allToppings = new (string key, string name, int count)[]
            {
                ("Topping_TapiocaPearls", "Tapioca Pearls", InventoryManager.Instance.GetToppingStock(ToppingType.TapiocaPearls)),
                ("Topping_PoppingBoba", "Popping Boba", InventoryManager.Instance.GetToppingStock(ToppingType.PoppingBoba)),
                ("Topping_GrassJelly", "Grass Jelly", InventoryManager.Instance.GetToppingStock(ToppingType.GrassJelly)),
                ("Topping_EggPudding", "Egg Pudding", InventoryManager.Instance.GetToppingStock(ToppingType.EggPudding)),
                ("Topping_CoconutJelly", "Coconut Jelly", InventoryManager.Instance.GetToppingStock(ToppingType.CoconutJelly)),
                ("Topping_CheeseFoam", "Cheese Foam", InventoryManager.Instance.GetToppingStock(ToppingType.CheeseFoam)),
                ("Topping_GoldenHoneyPearls", "Honey Pearls", InventoryManager.Instance.GetToppingStock(ToppingType.GoldenHoneyPearls))
            };
            var toppingsList = new List<(string key, string name, int count)>();
            foreach (var t in allToppings)
            {
                if (InventoryManager.Instance.HasEverHadStock(t.key))
                {
                    toppingsList.Add(t);
                }
            }
            var toppings = toppingsList.ToArray();

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

        private Transform upgradesScrollContainer;
        private TextMeshProUGUI upgradesWalletText;

        public void UpdateUpgradesTab()
        {
            if (upgradesTabPanel == null || UpgradeManager.Instance == null) return;

            EnsureUpgradesTabUI();
            PopulateUpgradesCards();
        }

        private void EnsureUpgradesTabUI()
        {
            if (upgradesScrollContainer != null) return;

            // Clear any old placeholder children
            for (int i = upgradesTabPanel.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(upgradesTabPanel.transform.GetChild(i).gameObject);
            }

            // Top Header (Wallet & Title)
            GameObject headerObj = new GameObject("UpgradesHeader", typeof(RectTransform), typeof(TextMeshProUGUI));
            headerObj.transform.SetParent(upgradesTabPanel.transform, false);
            var headerRt = headerObj.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0.5f, 1f);
            headerRt.anchorMax = new Vector2(0.5f, 1f);
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.anchoredPosition = new Vector2(0, -6f);
            headerRt.sizeDelta = new Vector2(980f, 32f);

            upgradesWalletText = headerObj.GetComponent<TextMeshProUGUI>();
            upgradesWalletText.fontSize = 20f;
            upgradesWalletText.alignment = TextAlignmentOptions.MidlineLeft;

            // Scroll View Root
            GameObject scrollObj = new GameObject("UpgradesScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollObj.transform.SetParent(upgradesTabPanel.transform, false);
            var scrollRt = scrollObj.GetComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0.5f, 0.5f);
            scrollRt.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRt.pivot = new Vector2(0.5f, 0.5f);
            scrollRt.anchoredPosition = new Vector2(0, -25f);
            scrollRt.sizeDelta = new Vector2(1000f, 440f);

            var scrollImg = scrollObj.GetComponent<Image>();
            scrollImg.color = new Color(0, 0, 0, 0.01f);

            // Viewport
            GameObject viewportObj = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportObj.transform.SetParent(scrollObj.transform, false);
            var viewRt = viewportObj.GetComponent<RectTransform>();
            viewRt.anchorMin = Vector2.zero;
            viewRt.anchorMax = Vector2.one;
            viewRt.offsetMin = Vector2.zero;
            viewRt.offsetMax = Vector2.zero;

            var mask = viewportObj.GetComponent<Mask>();
            mask.showMaskGraphic = false;

            // Content
            GameObject contentObj = new GameObject("Content", typeof(RectTransform));
            contentObj.transform.SetParent(viewportObj.transform, false);
            var contentRt = contentObj.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0.5f, 1f);
            contentRt.anchorMax = new Vector2(0.5f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;

            var scrollRect = scrollObj.GetComponent<ScrollRect>();
            scrollRect.content = contentRt;
            scrollRect.viewport = viewRt;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 35f;

            upgradesScrollContainer = contentObj.transform;
        }

        private void PopulateUpgradesCards()
        {
            if (upgradesScrollContainer == null || UpgradeManager.Instance == null) return;

            var upgrades = UpgradeManager.Instance.Upgrades;
            int purchasedCount = 0;
            for (int i = 0; i < upgrades.Count; i++)
            {
                if (upgrades[i].isPurchased) purchasedCount++;
            }

            if (upgradesWalletText != null && EconomyManager.Instance != null)
            {
                upgradesWalletText.text = $"<b>SHOP UPGRADES</b>  |  Wallet: <color=#2ECC71>${EconomyManager.Instance.CurrentCash:F2}</color>  |  <color=#FFAA00>{purchasedCount}/{upgrades.Count} Active</color>";
            }

            // Clear old cards
            for (int i = upgradesScrollContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(upgradesScrollContainer.GetChild(i).gameObject);
            }

            int cols = 2;
            int totalRows = Mathf.CeilToInt((float)upgrades.Count / cols);

            float totalWidth = 980f;
            float paddingX = 10f;
            float paddingY = 12f;
            float spacingX = 20f;
            float spacingY = 14f;

            float cardWidth = (totalWidth - (paddingX * 2) - spacingX) / cols; // ~470f
            float cardHeight = 120f;

            float totalHeight = (paddingY * 2) + (totalRows * cardHeight) + ((totalRows - 1) * spacingY);
            RectTransform contentRt = upgradesScrollContainer as RectTransform;
            if (contentRt != null)
            {
                contentRt.sizeDelta = new Vector2(totalWidth, totalHeight);
                contentRt.anchoredPosition = Vector2.zero;
            }

            float startX = -totalWidth * 0.5f + paddingX + (cardWidth * 0.5f);
            float startY = -paddingY - (cardHeight * 0.5f);

            for (int i = 0; i < upgrades.Count; i++)
            {
                var u = upgrades[i];
                bool isOwned = u.isPurchased;
                bool canAfford = EconomyManager.Instance != null && EconomyManager.Instance.CanAfford(u.cost);

                int row = i / cols;
                int col = i % cols;
                Vector2 pos = new Vector2(startX + col * (cardWidth + spacingX), startY - row * (cardHeight + spacingY));

                // Container card
                GameObject cardObj = new GameObject($"Card_{u.type}", typeof(RectTransform), typeof(Image));
                cardObj.transform.SetParent(upgradesScrollContainer, false);
                var rt = cardObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(cardWidth, cardHeight);
                rt.anchoredPosition = pos;

                var img = cardObj.GetComponent<Image>();
                img.color = isOwned ? new Color(0.10f, 0.20f, 0.16f, 0.95f) : new Color(0.12f, 0.16f, 0.24f, 0.95f);

                // Right Buy Button
                float buyButtonWidth = 115f;
                GameObject buyBtnObj = new GameObject("BuyButton", typeof(RectTransform), typeof(Image), typeof(Button));
                buyBtnObj.transform.SetParent(cardObj.transform, false);
                var buyRt = buyBtnObj.GetComponent<RectTransform>();
                buyRt.anchorMin = new Vector2(1, 0.5f);
                buyRt.anchorMax = new Vector2(1, 0.5f);
                buyRt.pivot = new Vector2(1, 0.5f);
                buyRt.sizeDelta = new Vector2(buyButtonWidth, cardHeight - 18f);
                buyRt.anchoredPosition = new Vector2(-10, 0);

                var buyImg = buyBtnObj.GetComponent<Image>();
                var buyBtn = buyBtnObj.GetComponent<Button>();

                if (isOwned)
                {
                    buyImg.color = new Color(0.15f, 0.28f, 0.20f, 0.85f);
                    buyBtn.interactable = false;
                }
                else
                {
                    buyImg.color = canAfford ? new Color(0.18f, 0.55f, 0.34f, 1f) : new Color(0.35f, 0.35f, 0.35f, 0.65f);
                    buyBtn.interactable = canAfford;
                }

                GameObject buyTextObj = new GameObject("BuyText", typeof(RectTransform), typeof(TextMeshProUGUI));
                buyTextObj.transform.SetParent(buyBtnObj.transform, false);
                var buyTextRt = buyTextObj.GetComponent<RectTransform>();
                buyTextRt.anchorMin = Vector2.zero;
                buyTextRt.anchorMax = Vector2.one;
                buyTextRt.offsetMin = new Vector2(4, 2);
                buyTextRt.offsetMax = new Vector2(-4, -2);

                var buyTmp = buyTextObj.GetComponent<TextMeshProUGUI>();
                buyTmp.fontSize = 18f;
                buyTmp.alignment = TextAlignmentOptions.Center;
                buyTmp.enableWordWrapping = false;
                buyTmp.lineSpacing = -8f;

                if (isOwned)
                {
                    buyTmp.text = "<color=#2ECC71><b>OWNED</b></color>\n<size=15><color=#A8D5BA>Active</color></size>";
                }
                else
                {
                    buyTmp.text = $"<b>BUY</b>\n<size=16>${u.cost:F2}</size>";
                }

                // Left: 3 Fields (Name, Description, Effect)
                GameObject infoTextObj = new GameObject("InfoText", typeof(RectTransform), typeof(TextMeshProUGUI));
                infoTextObj.transform.SetParent(cardObj.transform, false);
                var infoRt = infoTextObj.GetComponent<RectTransform>();
                infoRt.anchorMin = new Vector2(0, 0);
                infoRt.anchorMax = new Vector2(1, 1);
                infoRt.offsetMin = new Vector2(14f, 6f);
                infoRt.offsetMax = new Vector2(-(buyButtonWidth + 18f), -6f);

                var infoTmp = infoTextObj.GetComponent<TextMeshProUGUI>();
                infoTmp.fontSize = 16f;
                infoTmp.alignment = TextAlignmentOptions.MidlineLeft;
                infoTmp.enableWordWrapping = true;
                infoTmp.lineSpacing = -2f;

                string titleHeader = isOwned
                    ? $"<color=#2ECC71><b>{u.title}</b></color> <size=13><color=#A8D5BA>[OWNED]</color></size>"
                    : $"<b>{u.title}</b>";

                infoTmp.text = $"{titleHeader}\n" +
                               $"<size=14><color=#BDC3C7>{u.description}</color></size>\n" +
                               $"<size=14><color=#FFAA00><b>Effect:</b> {u.effect}</color></size>";

                if (!isOwned && canAfford)
                {
                    UpgradeType upgType = u.type;
                    string upgTitle = u.title;
                    buyBtn.onClick.AddListener(() =>
                    {
                        if (UpgradeManager.Instance.TryPurchaseUpgrade(upgType))
                        {
                            PopulateUpgradesCards();
                            UpdateLedger();
                            HUDController.Instance?.ShowNotification($"Purchased {upgTitle}!", 3f);
                        }
                    });
                }
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
