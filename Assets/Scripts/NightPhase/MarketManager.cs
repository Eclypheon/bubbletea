using System;
using System.Collections.Generic;
using UnityEngine;

namespace BubbleTeaShop
{
    [System.Serializable]
    public class MarketItem
    {
        public string stockKey;
        public string displayName;
        public float price;
        public int bundleQuantity = 5;
    }

    public class MarketManager : MonoBehaviour
    {
        public static MarketManager Instance { get; private set; }

        [SerializeField] private List<MarketItem> allMarketItems = new List<MarketItem>();

        public List<MarketItem> GetAvailableCatalog(int dayNumber)
        {
            List<MarketItem> available = new List<MarketItem>();

            foreach (var item in allMarketItems)
            {
                if (IsItemUnlockedOnDay(item.stockKey, dayNumber))
                {
                    // Dynamically calculate pack price and bundle size based on MarketPriceManager and active events
                    if (MarketPriceManager.Instance != null)
                    {
                        item.price = MarketPriceManager.Instance.GetMarketPackPrice(item.stockKey);
                        item.bundleQuantity = MarketPriceManager.Instance.GetMarketPackQuantity(item.stockKey);
                    }
                    available.Add(item);
                }
            }

            return available;
        }

        private bool IsItemUnlockedOnDay(string stockKey, int dayNumber)
        {
            // Week 1 (Days 2 to 7): Fresh Milk, Oat Milk, Tapioca, Popping Boba, Grass Jelly
            if (stockKey == "Milk_FreshMilk" || stockKey == "Milk_OatMilk" ||
                stockKey == "Topping_TapiocaPearls" || stockKey == "Topping_PoppingBoba" || stockKey == "Topping_GrassJelly")
            {
                return true;
            }

            // Week 2 (Days 8 to 14): Coconut Milk, Egg Pudding, Coconut Jelly
            if (dayNumber >= 8)
            {
                if (stockKey == "Milk_CoconutMilk" || stockKey == "Topping_EggPudding" || stockKey == "Topping_CoconutJelly")
                {
                    return true;
                }
            }

            // Week 3 (Days 15 to 21): Condensed Milk, Cheese Foam, Golden Honey Pearls
            if (dayNumber >= 15)
            {
                if (stockKey == "Milk_CondensedMilk" || stockKey == "Topping_CheeseFoam" || stockKey == "Topping_GoldenHoneyPearls")
                {
                    return true;
                }
            }

            return false;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            InitializeCatalog();
        }

        private void InitializeCatalog()
        {
            allMarketItems.Clear();

            // Week 1 (Days 2-7)
            allMarketItems.Add(new MarketItem { stockKey = "Milk_FreshMilk", displayName = "Fresh Whole Milk", price = 6.00f, bundleQuantity = 8 });
            allMarketItems.Add(new MarketItem { stockKey = "Milk_OatMilk", displayName = "Barista Oat Milk", price = 9.00f, bundleQuantity = 8 });
            allMarketItems.Add(new MarketItem { stockKey = "Topping_TapiocaPearls", displayName = "Raw Tapioca Pearls", price = 5.60f, bundleQuantity = 10 });
            allMarketItems.Add(new MarketItem { stockKey = "Topping_PoppingBoba", displayName = "Mango Popping Boba", price = 7.00f, bundleQuantity = 8 });
            allMarketItems.Add(new MarketItem { stockKey = "Topping_GrassJelly", displayName = "Herbal Grass Jelly", price = 9.00f, bundleQuantity = 8 });

            // Week 2 (Days 8-14)
            allMarketItems.Add(new MarketItem { stockKey = "Milk_CoconutMilk", displayName = "Organic Coconut Milk", price = 10.80f, bundleQuantity = 8 });
            allMarketItems.Add(new MarketItem { stockKey = "Topping_CoconutJelly", displayName = "Sweet Coconut Jelly", price = 11.50f, bundleQuantity = 8 });
            allMarketItems.Add(new MarketItem { stockKey = "Topping_EggPudding", displayName = "Silky Egg Pudding", price = 14.50f, bundleQuantity = 8 });

            // Week 3 (Days 15-21)
            allMarketItems.Add(new MarketItem { stockKey = "Milk_CondensedMilk", displayName = "Sweet Condensed Milk", price = 12.00f, bundleQuantity = 8 });
            allMarketItems.Add(new MarketItem { stockKey = "Topping_CheeseFoam", displayName = "Salted Cheese Foam Powder", price = 18.50f, bundleQuantity = 8 });
            allMarketItems.Add(new MarketItem { stockKey = "Topping_GoldenHoneyPearls", displayName = "Golden Honey Pearls", price = 23.50f, bundleQuantity = 8 });
        }

        public bool BuyItem(MarketItem item)
        {
            if (EconomyManager.Instance.SpendCash(item.price, $"Wholesale: {item.displayName}"))
            {
                InventoryManager.Instance.AddStock(item.stockKey, item.bundleQuantity);
                HUDController.Instance?.ShowNotification($"Purchased {item.displayName} (+{item.bundleQuantity})!", 2.5f);
                return true;
            }
            HUDController.Instance?.ShowNotification("Not enough cash for wholesale order!", 2.5f);
            return false;
        }
    }
}
