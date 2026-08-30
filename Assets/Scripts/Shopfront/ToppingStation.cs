using UnityEngine;
using UnityEngine.UI;

namespace BubbleTeaShop
{
    public class ToppingStation : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private ToppingType toppingType = ToppingType.TapiocaPearls;
        [SerializeField] private Button scoopButton;

        [Header("Audio (Optional)")]
        [SerializeField] private AudioClip scoopSound;

        private void Start()
        {
            if (scoopButton != null)
            {
                scoopButton.onClick.AddListener(ScoopTopping);
            }
            UpdateVisibility();
        }

        private void OnEnable()
        {
            UpdateVisibility();
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnInventoryUpdated += UpdateVisibility;
            }
        }

        private void OnDisable()
        {
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnInventoryUpdated -= UpdateVisibility;
            }
        }

        public void UpdateVisibility()
        {
            if (toppingType == ToppingType.TapiocaPearls)
            {
                // Tapioca Pearls jar is always available on countertop
                if (scoopButton != null) scoopButton.gameObject.SetActive(true);
                return;
            }

            int day = DayManager.Instance != null ? DayManager.Instance.CurrentDay : 1;
            int stock = InventoryManager.Instance != null ? InventoryManager.Instance.GetToppingStock(toppingType) : 0;

            bool unlockedBySchedule = (day >= 3 && (toppingType == ToppingType.PoppingBoba || toppingType == ToppingType.GrassJelly)) ||
                                     (day >= 8 && (toppingType == ToppingType.EggPudding || toppingType == ToppingType.CoconutJelly)) ||
                                     (day >= 15 && (toppingType == ToppingType.CheeseFoam || toppingType == ToppingType.GoldenHoneyPearls));

            bool isVisible = unlockedBySchedule || stock > 0;
            if (scoopButton != null)
            {
                scoopButton.gameObject.SetActive(isVisible);
            }
            else
            {
                var graphics = GetComponentsInChildren<Graphic>(true);
                foreach (var g in graphics) g.enabled = isVisible;
            }
        }

        public void ScoopTopping()
        {
            if (InventoryManager.Instance != null)
            {
                if (!InventoryManager.Instance.ConsumeStock($"Topping_{toppingType}", 1))
                {
                    Debug.LogWarning($"Out of {toppingType}! Harvest pearls or buy at market.");
                    return;
                }
            }

            if (scoopSound != null)
            {
                AudioManager.Instance?.PlaySFX(scoopSound);
            }

            CupStation.Instance?.AddTopping(toppingType);
        }
    }
}
