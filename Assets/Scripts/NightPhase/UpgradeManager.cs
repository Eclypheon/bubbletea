using System;
using System.Collections.Generic;
using UnityEngine;

namespace BubbleTeaShop
{
    public class UpgradeManager : MonoBehaviour
    {
        public static UpgradeManager Instance { get; private set; }

        [SerializeField] private List<ShopUpgrade> availableUpgrades = new List<ShopUpgrade>();

        public List<ShopUpgrade> Upgrades => availableUpgrades;

        public event Action<UpgradeType> OnUpgradePurchased;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitializeUpgrades();
        }

        private void InitializeUpgrades()
        {
            availableUpgrades.Clear();
            availableUpgrades.Add(new ShopUpgrade(UpgradeType.PearlFarm, "Pearl Hydroponic Farm", "Cultivates 12 fresh Tapioca Pearls automatically every morning in the backroom.", 120f));
            availableUpgrades.Add(new ShopUpgrade(UpgradeType.DigitalSugarMeter, "Digital Sugar Dispenser", "Adds precision 0%, 25%, 50%, 75%, 100% one-touch preset buttons.", 85f));
            availableUpgrades.Add(new ShopUpgrade(UpgradeType.AutoSealer, "Pneumatic Auto-Sealer", "Instantly seals cups with airtight precision and straw.", 110f));
            availableUpgrades.Add(new ShopUpgrade(UpgradeType.CozyDecor, "Cozy Shop Ambiance", "Fairy lights and lo-fi radio increase customer patience by 20%.", 95f));
            availableUpgrades.Add(new ShopUpgrade(UpgradeType.StorefrontSign, "Neon Boba Sign", "Attracts 5-8 customers per day instead of 3-7.", 150f));
        }

        public bool HasUpgrade(UpgradeType type)
        {
            var u = availableUpgrades.Find(x => x.type == type);
            return u != null && u.isPurchased;
        }

        public bool TryPurchaseUpgrade(UpgradeType type)
        {
            var u = availableUpgrades.Find(x => x.type == type);
            if (u == null || u.isPurchased) return false;

            if (EconomyManager.Instance.SpendCash(u.cost, $"Purchased Upgrade: {u.title}"))
            {
                u.isPurchased = true;
                u.currentLevel = 1;
                OnUpgradePurchased?.Invoke(type);
                return true;
            }

            return false;
        }
    }
}
