using System;
using UnityEngine;

namespace BubbleTeaShop
{
    public class DayManager : MonoBehaviour
    {
        public static DayManager Instance { get; private set; }

        [Header("Day Progression")]
        [SerializeField] private int currentDay = 1;
        [SerializeField] private int minCustomersPerDay = 3;
        [SerializeField] private int maxCustomersPerDay = 7;

        [Header("Runtime Daily Stats")]
        [SerializeField] private int totalCustomersToday;
        [SerializeField] private int customersServedToday;
        [SerializeField] private float dailySalesTotal;
        [SerializeField] private float dailyTipsTotal;

        public int CurrentDay => currentDay;
        public int TotalCustomersToday => totalCustomersToday;
        public int CustomersServedToday => customersServedToday;
        public int CustomersRemainingToday => Mathf.Max(0, totalCustomersToday - customersServedToday);
        public float DailySalesTotal => dailySalesTotal;
        public float DailyTipsTotal => dailyTipsTotal;
        public bool IsDayFinished => customersServedToday >= totalCustomersToday;

        public event Action<int> OnDayStarted;
        public event Action<int, float, float> OnDayCompleted; // day, sales, tips
        public event Action<int, int> OnCustomerProgressUpdated; // served, total

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void StartNewDay()
        {
            customersServedToday = 0;
            dailySalesTotal = 0f;
            dailyTipsTotal = 0f;

            // Check if storefront sign upgrade increases customer traffic
            bool hasSign = UpgradeManager.Instance != null && UpgradeManager.Instance.HasUpgrade(UpgradeType.StorefrontSign);
            int min = hasSign ? 5 : minCustomersPerDay;
            int max = hasSign ? 8 : maxCustomersPerDay;

            totalCustomersToday = UnityEngine.Random.Range(min, max + 1);
            
            OnDayStarted?.Invoke(currentDay);
            OnCustomerProgressUpdated?.Invoke(customersServedToday, totalCustomersToday);
        }

        public void RecordCustomerServed(float sales, float tip)
        {
            customersServedToday++;
            dailySalesTotal += sales;
            dailyTipsTotal += tip;
            OnCustomerProgressUpdated?.Invoke(customersServedToday, totalCustomersToday);
        }

        public void CompleteDay()
        {
            OnDayCompleted?.Invoke(currentDay, dailySalesTotal, dailyTipsTotal);
            currentDay++;
        }
    }
}
