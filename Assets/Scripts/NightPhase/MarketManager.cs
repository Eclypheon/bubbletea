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

        [SerializeField] private List<MarketItem> marketCatalog = new List<MarketItem>();
        public List<MarketItem> Catalog => marketCatalog;

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
            marketCatalog.Clear();
            marketCatalog.Add(new MarketItem { stockKey = "Cup", displayName = "Cups & Straws Pack", price = 8.00f, bundleQuantity = 10 });
            marketCatalog.Add(new MarketItem { stockKey = "Tea_BlackTea", displayName = "Black Tea Leaves", price = 10.00f, bundleQuantity = 8 });
            marketCatalog.Add(new MarketItem { stockKey = "Tea_GreenTea", displayName = "Jasmine Green Tea", price = 10.00f, bundleQuantity = 8 });
            marketCatalog.Add(new MarketItem { stockKey = "Tea_OolongTea", displayName = "High Mountain Oolong", price = 14.00f, bundleQuantity = 8 });
            marketCatalog.Add(new MarketItem { stockKey = "Tea_ThaiTea", displayName = "Thai Spiced Tea", price = 12.00f, bundleQuantity = 8 });
            marketCatalog.Add(new MarketItem { stockKey = "Tea_TaroTea", displayName = "Sweet Taro Powder", price = 12.00f, bundleQuantity = 8 });
            marketCatalog.Add(new MarketItem { stockKey = "Milk_FreshMilk", displayName = "Fresh Whole Milk", price = 6.00f, bundleQuantity = 8 });
            marketCatalog.Add(new MarketItem { stockKey = "Milk_OatMilk", displayName = "Barista Oat Milk", price = 8.00f, bundleQuantity = 8 });
            marketCatalog.Add(new MarketItem { stockKey = "Topping_TapiocaPearls", displayName = "Raw Tapioca Pearls", price = 7.50f, bundleQuantity = 10 });
            marketCatalog.Add(new MarketItem { stockKey = "Topping_PoppingBoba", displayName = "Mango Popping Boba", price = 9.00f, bundleQuantity = 8 });
            marketCatalog.Add(new MarketItem { stockKey = "Topping_GrassJelly", displayName = "Herbal Grass Jelly", price = 7.00f, bundleQuantity = 8 });
            marketCatalog.Add(new MarketItem { stockKey = "Topping_EggPudding", displayName = "Silky Egg Custard", price = 8.50f, bundleQuantity = 8 });
        }

        public bool BuyItem(MarketItem item)
        {
            if (EconomyManager.Instance.SpendCash(item.price, $"Wholesale: {item.displayName}"))
            {
                InventoryManager.Instance.AddStock(item.stockKey, item.bundleQuantity);
                return true;
            }
            return false;
        }
    }
}
