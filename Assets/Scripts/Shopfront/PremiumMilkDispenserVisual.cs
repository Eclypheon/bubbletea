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
            if (DayManager.Instance == null) return;
            bool isVisible = DayManager.Instance.CurrentDay > 2;

            var graphics = GetComponentsInChildren<Graphic>(true);
            foreach (var g in graphics)
            {
                g.enabled = isVisible;
            }
        }
    }
}
