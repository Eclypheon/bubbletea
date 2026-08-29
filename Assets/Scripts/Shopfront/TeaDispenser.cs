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
            if (InventoryManager.Instance != null)
            {
                if (!InventoryManager.Instance.ConsumeStock($"Tea_{teaType}", 1))
                {
                    Debug.LogWarning($"Out of {teaType}! Buy more at the night market.");
                    return;
                }
            }

            if (teaPourSound != null)
            {
                AudioManager.Instance?.PlaySFX(teaPourSound);
            }

            CupStation.Instance?.AddTea(teaType);
        }
    }
}
