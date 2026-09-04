using System;
using UnityEngine;

namespace BubbleTeaShop
{
    public class DayManager : MonoBehaviour
    {
        public static DayManager Instance { get; private set; }

        [Header("Day Progression")]
        [SerializeField] private int currentDay = 1;
        [SerializeField] private int lastCompletedDay = 1;
        [SerializeField] private int minCustomersPerDay = 3;
        [SerializeField] private int maxCustomersPerDay = 7;

        [Header("Runtime Daily Stats")]
        [SerializeField] private int totalCustomersToday;
        [SerializeField] private int currentCustomerIndex;
        [SerializeField] private int customersServedToday;
        [SerializeField] private int customersSkippedToday;
        [SerializeField] private float dailySalesTotal;
        [SerializeField] private float dailyTipsTotal;
        [SerializeField] private bool hadNightActivityLastNight = false;

        public int CurrentDay => currentDay;
        public int LastCompletedDay => lastCompletedDay;
        public int TotalCustomersToday => totalCustomersToday;
        public int CurrentCustomerIndex => currentCustomerIndex;
        public int CustomersServedToday => customersServedToday;
        public int CustomersSkippedToday => customersSkippedToday;
        public int ProcessedCustomersToday => customersServedToday + customersSkippedToday;
        public int CustomersRemainingToday => Mathf.Max(0, totalCustomersToday - ProcessedCustomersToday);
        public float DailySalesTotal => dailySalesTotal;
        public float DailyTipsTotal => dailyTipsTotal;
        public bool HadNightActivityLastNight => hadNightActivityLastNight;
        public bool IsDayFinished => (GameManager.Instance != null && GameManager.Instance.IsCasualMode) ? false : (ProcessedCustomersToday >= totalCustomersToday);

        public event Action<int> OnDayStarted;
        public event Action<int, float, float> OnDayCompleted; // day, sales, tips
        public event Action<int, int> OnCustomerProgressUpdated; // currentCustomerIndex, total

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void RecordNightActivity()
        {
            hadNightActivityLastNight = true;
            Debug.Log("[DayManager] Night activity recorded! Tomorrow's customer count will be reduced by 1 due to late opening.");
        }

        public void StartNewDay()
        {
            currentCustomerIndex = 0;
            customersServedToday = 0;
            customersSkippedToday = 0;
            dailySalesTotal = 0f;
            dailyTipsTotal = 0f;

            // Check if upgrades increase customer traffic
            int min = minCustomersPerDay;
            int max = maxCustomersPerDay;

            if (UpgradeManager.Instance != null)
            {
                if (UpgradeManager.Instance.HasUpgrade(UpgradeType.StorefrontBeautification)) min += 1;
                if (UpgradeManager.Instance.HasUpgrade(UpgradeType.Advertisements)) max += 1;
                if (UpgradeManager.Instance.HasUpgrade(UpgradeType.StorefrontSign))
                {
                    min = Mathf.Max(min, 5);
                    max = Mathf.Max(max, 8);
                }
            }

            int rolled = UnityEngine.Random.Range(min, max + 1);

            // Night activity penalty: reduce customer traffic by 1 due to late morning opening
            if (hadNightActivityLastNight)
            {
                rolled = Mathf.Max(1, rolled - 1);
                Debug.Log($"[DayManager] Late opening penalty applied from last night's activity! Total customers today: {rolled}");
                hadNightActivityLastNight = false;
            }

            totalCustomersToday = rolled;
            
            OnDayStarted?.Invoke(currentDay);
            OnCustomerProgressUpdated?.Invoke(currentCustomerIndex, totalCustomersToday);
        }

        public void AdvanceCustomerIndex()
        {
            currentCustomerIndex++;
            OnCustomerProgressUpdated?.Invoke(currentCustomerIndex, totalCustomersToday);
        }

        public void RecordCustomerServed(float sales, float tip)
        {
            customersServedToday++;
            dailySalesTotal += sales;
            dailyTipsTotal += tip;
        }

        public void RecordCustomerSkipped()
        {
            customersSkippedToday++;
        }

        public void CompleteDay()
        {
            lastCompletedDay = currentDay;
            OnDayCompleted?.Invoke(currentDay, dailySalesTotal, dailyTipsTotal);
            currentDay++;
        }

        // Persistence API
        public void RestoreDay(int day, int lastCompleted, bool hadNightActivity)
        {
            currentDay = Mathf.Max(1, day);
            lastCompletedDay = Mathf.Max(1, lastCompleted);
            hadNightActivityLastNight = hadNightActivity;
            currentCustomerIndex = 0;
            customersServedToday = 0;
            customersSkippedToday = 0;
            dailySalesTotal = 0f;
            dailyTipsTotal = 0f;
        }

        public void ResetDays()
        {
            currentDay = 1;
            lastCompletedDay = 1;
            hadNightActivityLastNight = false;
            currentCustomerIndex = 0;
            customersServedToday = 0;
            customersSkippedToday = 0;
            dailySalesTotal = 0f;
            dailyTipsTotal = 0f;
        }
    }
}
