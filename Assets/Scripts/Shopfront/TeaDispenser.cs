using UnityEngine;
using UnityEngine.UI;

namespace BubbleTeaShop
{
    public class TeaDispenser : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private TeaBase teaType = TeaBase.BlackTea;
        [SerializeField] private Button dispenseButton;

        [Header("Audio (Optional)")]
        [SerializeField] private AudioClip teaPourSound;

        private void Start()
        {
            if (dispenseButton != null)
            {
                dispenseButton.onClick.AddListener(DispenseTea);
            }
        }

        public void DispenseTea()
        {
            if (CupStation.Instance == null || !CupStation.Instance.CurrentCup.hasCup) return;

            if (CupStation.Instance.CurrentCup.isSealed)
            {
                HUDController.Instance?.ShowNotification("Cup is already sealed!");
                return;
            }

            if (CupStation.Instance.CurrentCup.tea != TeaBase.None)
            {
                HUDController.Instance?.ShowNotification("Cup already has tea! Trash the cup to start over.");
                return;
            }

            if (teaPourSound != null)
            {
                AudioManager.Instance?.PlaySFX(teaPourSound);
            }

            CupStation.Instance?.AddTea(teaType);
        }
    }
}
