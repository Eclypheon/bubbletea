using UnityEngine;
using UnityEngine.UI;

namespace BubbleTeaShop
{
    public class PremiumMilkDispenserVisual : MonoBehaviour
    {
        private void Start()
        {
            CheckVisibility();
        }

        private void Update()
        {
            CheckVisibility();
        }

        private void CheckVisibility()
        {
            bool isBlitzOrCasual = GameManager.Instance != null && (GameManager.Instance.IsBlitzMode || GameManager.Instance.IsCasualMode);
            bool isVisible = isBlitzOrCasual || (InventoryManager.Instance != null && InventoryManager.Instance.HasPremiumMilkDispenser) || (DayManager.Instance != null && DayManager.Instance.CurrentDay > 2);

            var graphics = GetComponentsInChildren<Graphic>(true);
            foreach (var g in graphics)
            {
                g.enabled = isVisible;
            }

            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var b in buttons)
            {
                b.interactable = isVisible;
            }
        }
    }
}
