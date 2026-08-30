using UnityEngine;

namespace BubbleTeaShop
{
    public class PremiumMilkDispenserVisual : MonoBehaviour
    {
        [Tooltip("Optional: If assigned, toggles this target GameObject. If left empty, toggles the GameObject this script is attached to.")]
        [SerializeField] private GameObject visualRoot;

        private void Start()
        {
            if (visualRoot == null) visualRoot = gameObject;
            UpdateVisibility();
        }

        private void OnEnable()
        {
            if (visualRoot == null) visualRoot = gameObject;
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
            bool isUnlocked = InventoryManager.Instance != null && InventoryManager.Instance.HasPremiumMilkDispenser;
            if (visualRoot != null)
            {
                visualRoot.SetActive(isUnlocked);
            }
        }
    }
}
