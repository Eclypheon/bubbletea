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
        }

        public void DispenseMilk()
        {
            if (InventoryManager.Instance != null)
            {
                if (!InventoryManager.Instance.ConsumeStock($"Milk_{milkType}", 1))
                {
                    Debug.LogWarning($"Out of {milkType}!");
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
