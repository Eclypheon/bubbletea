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

        public MilkType Type => milkType;

        private void Start()
        {
            if (dispenseButton != null)
            {
                dispenseButton.onClick.AddListener(DispenseMilk);
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
