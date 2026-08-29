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

        private int[] iceLevels = new int[] { 0, 30, 50, 100 };
        private int currentLevelIndex = 0;

        private void Start()
        {
            if (iceButton != null)
            {
                iceButton.onClick.AddListener(CycleIce);
            }
            UpdateDisplay();
        }

        public void CycleIce()
        {
            currentLevelIndex = (currentLevelIndex + 1) % iceLevels.Length;
            int targetIce = iceLevels[currentLevelIndex];
            CupStation.Instance?.SetIce(targetIce);
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (iceLevelText != null)
            {
                iceLevelText.text = $"Ice: {iceLevels[currentLevelIndex]}%";
            }
        }
    }
}
