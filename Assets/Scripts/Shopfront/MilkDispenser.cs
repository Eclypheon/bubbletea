using UnityEngine;
using UnityEngine.UI;

namespace BubbleTeaShop
{
    public class MilkDispenser : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private MilkType milkType = MilkType.FreshMilk;
        [SerializeField] private Button dispenseButton;

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

            CupStation.Instance?.AddMilk(milkType);
        }
    }
}
