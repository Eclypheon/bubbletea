using UnityEngine;
using UnityEngine.UI;

namespace BubbleTeaShop
{
    public class MilkDispenser : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private MilkType milkType = MilkType.FreshMilk;
        [SerializeField] private Button dispenseButton;

        [Header("Audio (Optional)")]
        [SerializeField] private AudioClip milkPourSound;

        private void Start()
        {
            if (dispenseButton != null)
            {
                dispenseButton.onClick.AddListener(DispenseMilk);
            }
            UpdateVisibility();
        }

        private void Update()
        {
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
            if (milkType == MilkType.FreshMilk)
            {
                // Fresh milk is always available on countertop
                SetVisible(true);
                return;
            }

            int stock = InventoryManager.Instance != null ? InventoryManager.Instance.GetMilkStock(milkType) : 0;
            bool isDayUnlocked = DayManager.Instance != null && DayManager.Instance.CurrentDay > 2;

            bool isVisible = stock > 0 || isDayUnlocked;
            SetVisible(isVisible);
        }

        private void SetVisible(bool isVisible)
        {
            if (dispenseButton != null)
            {
                dispenseButton.gameObject.SetActive(isVisible);
            }
            else
            {
                var graphics = GetComponentsInChildren<Graphic>(true);
                foreach (var g in graphics) g.enabled = isVisible;
            }
        }

        public void DispenseMilk()
        {
            if (CupStation.Instance == null || !CupStation.Instance.CurrentCup.hasCup) return;

            if (CupStation.Instance.CurrentCup.isSealed)
            {
                HUDController.Instance?.ShowNotification("Cup is already sealed!");
                return;
            }

            if (CupStation.Instance.CurrentCup.milk != MilkType.None)
            {
                HUDController.Instance?.ShowNotification("Cup already has milk! Trash the cup to start over.");
                return;
            }

            if (InventoryManager.Instance != null)
            {
                if (!InventoryManager.Instance.ConsumeStock($"Milk_{milkType}", 1))
                {
                    Debug.LogWarning($"Out of {milkType}!");
                    HUDController.Instance?.ShowNotification($"Out of {milkType}! Buy more at night.");
                    return;
                }
            }

            if (milkPourSound != null)
            {
                AudioManager.Instance?.PlaySFX(milkPourSound);
            }

            CupStation.Instance?.AddMilk(milkType);
        }
    }
}
