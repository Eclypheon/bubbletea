using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BubbleTeaShop
{
    public class IceDispenser : MonoBehaviour
    {
        [Header("Elements")]
        [SerializeField] private Button iceButton;
        [SerializeField] private TextMeshProUGUI iceLevelText;

        private void Start()
        {
            if (iceButton != null)
            {
                iceButton.onClick.AddListener(AddIce);
            }

            if (CupStation.Instance != null)
            {
                CupStation.Instance.OnCupUpdated += UpdateDisplay;
            }

            UpdateDisplay();
        }

        private void OnDestroy()
        {
            if (CupStation.Instance != null)
            {
                CupStation.Instance.OnCupUpdated -= UpdateDisplay;
            }
        }

        public void AddIce()
        {
            if (CupStation.Instance == null || !CupStation.Instance.CurrentCup.hasCup) return;
            if (CupStation.Instance.CurrentCup.isSealed) return;

            int currentIce = CupStation.Instance.CurrentCup.icePercent;

            if (currentIce < 50)
            {
                CupStation.Instance.SetIce(50);
            }
            else if (currentIce < 100)
            {
                CupStation.Instance.SetIce(100);
            }
            else
            {
                Debug.LogWarning("Cup already has maximum 100% ice! Trash the cup to remake if you wanted less ice.");
            }

            UpdateDisplay();
        }

        public void UpdateDisplay()
        {
            if (iceLevelText != null)
            {
                int currentIce = CupStation.Instance != null && CupStation.Instance.CurrentCup.hasCup
                    ? CupStation.Instance.CurrentCup.icePercent
                    : 0;

                iceLevelText.text = $"Ice: {currentIce}%";
            }
        }
    }
}
