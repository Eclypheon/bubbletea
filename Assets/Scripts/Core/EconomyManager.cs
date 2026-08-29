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
        [SerializeField] private float buyoutGoal = 5000.00f;

        [Header("Runtime State")]
        [SerializeField] private float currentCash;
        [SerializeField] private int rentCycleDays = 7;

        public float CurrentCash => currentCash;
        public float BuyoutGoal => buyoutGoal;

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

        public int GetDaysUntilRent(int currentDay)
        {
            int mod = currentDay % rentCycleDays;
            return mod == 0 ? 0 : (rentCycleDays - mod);
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
            float rent = GetRentDueForDay(day);
            if (SpendCash(rent, $"Weekly Rent (Day {day})"))
            {
                OnRentPaid?.Invoke(rent, day);
                return true;
            }
            return false;
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
