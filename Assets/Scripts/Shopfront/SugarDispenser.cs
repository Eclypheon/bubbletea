using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BubbleTeaShop
{
    public class SugarDispenser : MonoBehaviour
    {
        [Header("Single Incremental Button Mode")]
        [Tooltip("Clicking this adds sugar upward (e.g. 0% -> 25% -> 50% -> 75% -> 100%)")]
        [SerializeField] private Button addSugarButton;
        [SerializeField] private TextMeshProUGUI sugarLevelText;
        [SerializeField] private int sugarStepPercent = 25; // +25% per click

        [Header("Audio (Optional)")]
        [SerializeField] private AudioClip sugarDispenseSound;

        [Header("Preset Buttons Mode (Optional)")]
        [SerializeField] private Button btn0;
        [SerializeField] private Button btn25;
        [SerializeField] private Button btn50;
        [SerializeField] private Button btn75;
        [SerializeField] private Button btn100;

        private void Start()
        {
            if (addSugarButton != null)
            {
                addSugarButton.onClick.AddListener(AddSugar);
            }

            if (btn0 != null) btn0.onClick.AddListener(() => SetSugar(0));
            if (btn25 != null) btn25.onClick.AddListener(() => SetSugar(25));
            if (btn50 != null) btn50.onClick.AddListener(() => SetSugar(50));
            if (btn75 != null) btn75.onClick.AddListener(() => SetSugar(75));
            if (btn100 != null) btn100.onClick.AddListener(() => SetSugar(100));

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

        public void AddSugar()
        {
            if (CupStation.Instance == null || !CupStation.Instance.CurrentCup.hasCup) return;
            if (CupStation.Instance.CurrentCup.isSealed) return;

            int currentSugar = CupStation.Instance.CurrentCup.sweetnessPercent;

            if (currentSugar < 100)
            {
                int newSugar = Mathf.Min(100, currentSugar + sugarStepPercent);
                CupStation.Instance.SetSugar(newSugar);
                if (sugarDispenseSound != null) AudioManager.Instance?.PlaySFX(sugarDispenseSound);
            }
            else
            {
                Debug.LogWarning("Cup already has maximum 100% sugar! Trash the cup to remake if you wanted less sugar.");
            }

            UpdateDisplay();
        }

        public void SetSugar(int percent)
        {
            if (CupStation.Instance == null || !CupStation.Instance.CurrentCup.hasCup) return;
            if (CupStation.Instance.CurrentCup.isSealed) return;

            CupStation.Instance.SetSugar(percent);
            if (sugarDispenseSound != null) AudioManager.Instance?.PlaySFX(sugarDispenseSound);
            UpdateDisplay();
        }

        public void UpdateUpgradeMode()
        {
            UpdateDisplay();
        }

        public void UpdateDisplay()
        {
            if (sugarLevelText != null)
            {
                int currentSugar = CupStation.Instance != null && CupStation.Instance.CurrentCup.hasCup
                    ? CupStation.Instance.CurrentCup.sweetnessPercent
                    : 0;

                sugarLevelText.text = $"{currentSugar}%";
            }
        }
    }
}
