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

        private void HandleStateChanged(GameState state)
        {
            bool isNight = (state == GameState.NightPhase);
            if (nightPanelRoot != null) nightPanelRoot.SetActive(isNight);

            if (isNight)
            {
                ForagingManager.Instance?.ResetNightForaging();
                if (foragingLogText != null) foragingLogText.text = "Select a region to forage wild ingredients tonight.";
                UpdateLedger();
                SwitchTab(3); // Start on Ledger
            }
        }

        public void SwitchTab(int tabIndex)
        {
            if (marketTabPanel != null) marketTabPanel.SetActive(tabIndex == 0);
            if (foragingTabPanel != null) foragingTabPanel.SetActive(tabIndex == 1);
            if (upgradesTabPanel != null) upgradesTabPanel.SetActive(tabIndex == 2);
            if (ledgerTabPanel != null) ledgerTabPanel.SetActive(tabIndex == 3);
        }

        private void UpdateLedger()
        {
            int day = DayManager.Instance.CurrentDay;
            int daysLeft = EconomyManager.Instance.GetDaysUntilRent(day);
            float totalRent = EconomyManager.Instance.GetTotalRentDue(day);
            float baseRent = EconomyManager.Instance.GetRentDueForDay(day);
            float accumulated = EconomyManager.Instance.AccumulatedRentOwed;

            if (ledgerSummaryText != null)
            {
                ledgerSummaryText.text = $"<b>Day {day} Summary:</b>\n• Customers Served: {DayManager.Instance.CustomersServedToday}/{DayManager.Instance.TotalCustomersToday}\n• Sales Revenue: ${DayManager.Instance.DailySalesTotal:F2}\n• Tips Earned: ${DayManager.Instance.DailyTipsTotal:F2}\n• Total Balance: ${EconomyManager.Instance.CurrentCash:F2}";
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
