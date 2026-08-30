using System;
using System.Collections.Generic;
using UnityEngine;

namespace BubbleTeaShop
{
    public class ForagingManager : MonoBehaviour
    {
        public static ForagingManager Instance { get; private set; }

        private bool hasForagedTonight = false;
        public bool HasForagedTonight => hasForagedTonight;

        public event Action<string> OnForagingResult;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void ResetNightForaging()
        {
            hasForagedTonight = false;
        }

        public bool IsZoneUnlocked(string zone, int day)
        {
            return zone switch
            {
                "BambooGrove" => day >= 5,
                "HoneyMeadow" => day >= 11,
                "MistMountain" => day >= 18,
                _ => false
            };
        }

        public bool GoForaging(string zone)
        {
            if (hasForagedTonight)
            {
                OnForagingResult?.Invoke("You are too exhausted to forage again tonight! Rest up for tomorrow.");
                return false;
            }

            hasForagedTonight = true;
            string resultMessage = "";

            if (zone == "BambooGrove")
            {
                InventoryManager.Instance.AddTeaStock(TeaBase.GreenTea, 6);
                InventoryManager.Instance.AddToppingStock(ToppingType.GrassJelly, 4);
                resultMessage = "Bamboo Grove: Foraged 6x Fresh Jasmine Green Leaves and 4x Herbal Grass Jelly herbs!";
            }
            else if (zone == "HoneyMeadow")
            {
                InventoryManager.Instance.AddToppingStock(ToppingType.GoldenHoneyPearls, 6);
                EconomyManager.Instance.AddCash(15f, "Foraged Wild Honey Sale");
                resultMessage = "Honey Meadow: Discovered 6x Rare Golden Honey Pearls and sold extra wild honeycomb for +$15.00!";
            }
            else if (zone == "MistMountain")
            {
                InventoryManager.Instance.AddTeaStock(TeaBase.WildMountainTea, 6);
                InventoryManager.Instance.AddToppingStock(ToppingType.TapiocaPearls, 6);
                resultMessage = "Mist Mountain: Scaled the peak and harvested 6x Legendary Wild Mountain Tea Leaves!";
            }

            OnForagingResult?.Invoke(resultMessage);
            return true;
        }
    }
}
