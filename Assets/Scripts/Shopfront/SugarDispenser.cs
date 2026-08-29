using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BubbleTeaShop
{
    public class SugarDispenser : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Manual Mode Elements")]
        [SerializeField] private GameObject manualContainer;
        [SerializeField] private Slider sugarFillSlider;
        [SerializeField] private TextMeshProUGUI sugarPercentText;
        [SerializeField] private float fillSpeed = 50f; // % per second

        [Header("Digital Upgrade Elements")]
        [SerializeField] private GameObject digitalContainer;
        [SerializeField] private Button btn0;
        [SerializeField] private Button btn25;
        [SerializeField] private Button btn50;
        [SerializeField] private Button btn75;
        [SerializeField] private Button btn100;

        private bool isHolding = false;
        private float currentSugarAmount = 0f;

        private void Start()
        {
            if (btn0 != null) btn0.onClick.AddListener(() => SetDigitalSugar(0));
            if (btn25 != null) btn25.onClick.AddListener(() => SetDigitalSugar(25));
            if (btn50 != null) btn50.onClick.AddListener(() => SetDigitalSugar(50));
            if (btn75 != null) btn75.onClick.AddListener(() => SetDigitalSugar(75));
            if (btn100 != null) btn100.onClick.AddListener(() => SetDigitalSugar(100));

            UpdateUpgradeMode();
        }

        private void Update()
        {
            if (isHolding)
            {
                currentSugarAmount += fillSpeed * Time.deltaTime;
                if (currentSugarAmount > 100f) currentSugarAmount = 100f;
                UpdateManualDisplay();
            }
        }

        public void UpdateUpgradeMode()
        {
            bool hasDigital = UpgradeManager.Instance != null && UpgradeManager.Instance.HasUpgrade(UpgradeType.DigitalSugarMeter);
            if (manualContainer != null) manualContainer.SetActive(!hasDigital);
            if (digitalContainer != null) digitalContainer.SetActive(hasDigital);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            isHolding = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isHolding = false;
            int finalPercent = Mathf.RoundToInt(currentSugarAmount);
            CupStation.Instance?.SetSugar(finalPercent);
            currentSugarAmount = 0f;
            UpdateManualDisplay();
        }

        private void SetDigitalSugar(int percent)
        {
            CupStation.Instance?.SetSugar(percent);
        }

        private void UpdateManualDisplay()
        {
            if (sugarFillSlider != null) sugarFillSlider.value = currentSugarAmount / 100f;
            if (sugarPercentText != null) sugarPercentText.text = $"{Mathf.RoundToInt(currentSugarAmount)}%";
        }
    }
}
