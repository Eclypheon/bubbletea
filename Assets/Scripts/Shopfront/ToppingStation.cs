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
