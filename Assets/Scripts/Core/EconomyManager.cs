using System;
using UnityEngine;

namespace BubbleTeaShop
{
    public class EconomyManager : MonoBehaviour
    {
        public static EconomyManager Instance { get; private set; }

        [Header("Economy Settings")]
        [SerializeField] private float startingCash = 50.00f;
        [SerializeField] private float baseRentAmount = 150.00f;
        [SerializeField] private float rentIncreasePerWeek = 50.00f;
        [SerializeField] private float buyoutGoal = 1500.00f;

        public const float DailySuppliesExpense = 10.00f;

        [Header("Runtime State")]
        [SerializeField] private float currentCash;
        [SerializeField] private int rentCycleDays = 7;
        [SerializeField] private float accumulatedRentOwed = 0f;
        [SerializeField] private int rentSkipsUsed = 0; // Max allowed skips = 1

        public float CurrentCash => currentCash;
        public float BuyoutGoal => buyoutGoal;
        public float AccumulatedRentOwed => accumulatedRentOwed;
        public int RentSkipsUsed => rentSkipsUsed;
        public int RentCycleDays => rentCycleDays;

        public event Action<float> OnCashChanged;
        public event Action<float, string> OnTransactionOccurred; // amount, description
        public event Action<float, int> OnRentPaid;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            currentCash = startingCash;
        }

        private void Start()
        {
            OnCashChanged?.Invoke(currentCash);
        }

        public float GetRentDueForDay(int currentDay)
        {
            int weekNumber = Mathf.Max(1, Mathf.CeilToInt((float)currentDay / rentCycleDays));
            return baseRentAmount + (weekNumber - 1) * rentIncreasePerWeek;
        }

        public float GetTotalRentDue(int currentDay)
        {
            return GetRentDueForDay(currentDay) + accumulatedRentOwed;
        }

        public bool CanSkipRent()
        {
            return rentSkipsUsed < 1;
        }

        public void SkipRent(int currentDay)
        {
            accumulatedRentOwed += GetRentDueForDay(currentDay);
            rentSkipsUsed++;
            Debug.Log($"[Economy] Rent skipped! Total accumulated rent owed: ${accumulatedRentOwed:F2} (Skips used: {rentSkipsUsed}/1)");
        }

        public bool PayTotalRent(int currentDay)
        {
            float total = GetTotalRentDue(currentDay);
            return PaySpecificRent(total, currentDay);
        }

        public bool PaySpecificRent(float amount, int currentDay)
        {
            if (SpendCash(amount, $"Weekly Rent Settlement (Day {currentDay})"))
            {
                accumulatedRentOwed = 0f;
                rentSkipsUsed = 0; // Reset skip strike once caught up
                OnRentPaid?.Invoke(amount, currentDay);
                Debug.Log($"[Economy] Paid rent: ${amount:F2}");
                return true;
            }
            return false;
        }

        public int GetDaysUntilRent(int currentDay)
        {
            int mod = currentDay % rentCycleDays;
            return mod == 0 ? 0 : (rentCycleDays - mod);
        }

        public float CurrentDailySuppliesExpense => (UpgradeManager.Instance != null && UpgradeManager.Instance.HasUpgrade(UpgradeType.SwitchSupplyContract)) ? 3.00f : DailySuppliesExpense;

        public void DeductDailySupplies(int dayNumber)
        {
            float expense = CurrentDailySuppliesExpense;
            currentCash -= expense;
            currentCash = (float)Math.Round(currentCash, 2);
            OnCashChanged?.Invoke(currentCash);
            OnTransactionOccurred?.Invoke(-expense, $"Daily Supplies & Utilities (Day {dayNumber})");
            Debug.Log($"[Economy] Deducted ${expense:F2} for daily supplies on Day {dayNumber}. Remaining cash: ${currentCash:F2}");
        }

        public void AddCash(float amount, string reason = "Sale")
        {
            if (amount <= 0) return;
            currentCash += amount;
            currentCash = (float)Math.Round(currentCash, 2);
            OnCashChanged?.Invoke(currentCash);
            OnTransactionOccurred?.Invoke(amount, reason);
        }

        public bool SpendCash(float amount, string reason = "Purchase")
        {
            if (amount <= 0) return true;
            if (currentCash < amount)
            {
                return false;
            }

            currentCash -= amount;
            currentCash = (float)Math.Round(currentCash, 2);
            OnCashChanged?.Invoke(currentCash);
            OnTransactionOccurred?.Invoke(-amount, reason);
            return true;
        }

        public bool CanAfford(float amount)
        {
            return currentCash >= amount;
        }

        public bool TryPayRent(int day)
        {
            return PayTotalRent(day);
        }

        public bool TryBuyoutShop()
        {
            if (SpendCash(buyoutGoal, "Bought Out Shop Location!"))
            {
                GameManager.Instance?.TriggerGameWon();
                return true;
            }
            return false;
        }
    }
}
