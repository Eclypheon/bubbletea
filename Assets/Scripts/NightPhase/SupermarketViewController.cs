using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BubbleTeaShop
{
    public class SupermarketViewController : MonoBehaviour
    {
        public static SupermarketViewController Instance { get; private set; }

        [Header("Root & Background")]
        [SerializeField] private GameObject supermarketPanelRoot;
        [SerializeField] private Image supermarketBackgroundImage;
        [SerializeField] private Sprite supermarketInteriorSprite;

        [Header("Header & Navigation")]
        [SerializeField] private TextMeshProUGUI cashBalanceText;
        [SerializeField] private TextMeshProUGUI marketAisleTitleText;
        [SerializeField] private Button returnToNightHubButton;

        [Header("Catalog Container")]
        [SerializeField] private Transform marketCatalogContainer;

        [Header("Audio (Optional)")]
        [SerializeField] private AudioClip purchaseChimeSound;

        public event Action OnSupermarketClosed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (returnToNightHubButton != null)
            {
                returnToNightHubButton.onClick.AddListener(CloseSupermarketView);
            }

            if (supermarketBackgroundImage != null && supermarketInteriorSprite != null)
            {
                supermarketBackgroundImage.sprite = supermarketInteriorSprite;
            }

            if (supermarketPanelRoot != null)
            {
                supermarketPanelRoot.SetActive(false);
            }
        }

        public void OpenSupermarketView(int dayNumber)
        {
            if (supermarketPanelRoot != null)
            {
                supermarketPanelRoot.SetActive(true);
            }

            if (supermarketBackgroundImage != null && supermarketInteriorSprite != null)
            {
                supermarketBackgroundImage.sprite = supermarketInteriorSprite;
            }

            if (marketAisleTitleText != null)
            {
                marketAisleTitleText.text = $"🛒 <b>Wholesale Supermarket</b> — Day {dayNumber}";
            }

            UpdateSupermarketDisplay(dayNumber);
        }

        public void CloseSupermarketView()
        {
            if (supermarketPanelRoot != null)
            {
                supermarketPanelRoot.SetActive(false);
            }
            OnSupermarketClosed?.Invoke();
        }

        public void UpdateSupermarketDisplay(int dayNumber = -1)
        {
            if (dayNumber <= 0 && DayManager.Instance != null)
            {
                dayNumber = DayManager.Instance.CurrentDay;
            }

            if (EconomyManager.Instance != null && cashBalanceText != null)
            {
                cashBalanceText.text = $"Wallet: <color=#2ECC71>${EconomyManager.Instance.CurrentCash:F2}</color>";
            }

            if (marketCatalogContainer == null || MarketManager.Instance == null) return;

            // Clear previous item cards
            for (int i = marketCatalogContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(marketCatalogContainer.GetChild(i).gameObject);
            }

            var catalog = MarketManager.Instance.GetAvailableCatalog(dayNumber);
            int cols = 3;
            float startX = -290f;
            float startY = 110f;
            float spacingX = 290f;
            float spacingY = 85f;

            for (int i = 0; i < catalog.Count; i++)
            {
                var item = catalog[i];
                int currentStock = InventoryManager.Instance != null ? InventoryManager.Instance.GetStock(item.stockKey) : 0;
                bool canAfford = EconomyManager.Instance != null && EconomyManager.Instance.CanAfford(item.price);

                int row = i / cols;
                int col = i % cols;
                Vector2 pos = new Vector2(startX + col * spacingX, startY - row * spacingY);

                // Container card
                GameObject cardObj = new GameObject($"Card_{item.stockKey}", typeof(RectTransform), typeof(Image));
                cardObj.transform.SetParent(marketCatalogContainer, false);
                var rt = cardObj.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(275, 76);
                rt.anchoredPosition = pos;

                var img = cardObj.GetComponent<Image>();
                img.color = new Color(0.12f, 0.16f, 0.24f, 0.92f);

                // Info Text (Item Name + Pack quantity + In Stock count)
                GameObject infoTextObj = new GameObject("InfoText", typeof(RectTransform), typeof(TextMeshProUGUI));
                infoTextObj.transform.SetParent(cardObj.transform, false);
                var infoRt = infoTextObj.GetComponent<RectTransform>();
                infoRt.anchorMin = new Vector2(0, 0);
                infoRt.anchorMax = new Vector2(0.65f, 1);
                infoRt.offsetMin = new Vector2(10, 6);
                infoRt.offsetMax = new Vector2(0, -6);
                var infoTmp = infoTextObj.GetComponent<TextMeshProUGUI>();
                infoTmp.fontSize = 13;
                infoTmp.alignment = TextAlignmentOptions.MidlineLeft;
                infoTmp.text = $"<b>{item.displayName}</b>\n" +
                               $"<color=#BDC3C7>Pack of {item.bundleQuantity}</color> | In Bag: <color=#F1C40F>{currentStock:D2}</color>";

                // Buy Button
                GameObject buyBtnObj = new GameObject("BuyButton", typeof(RectTransform), typeof(Image), typeof(Button));
                buyBtnObj.transform.SetParent(cardObj.transform, false);
                var buyRt = buyBtnObj.GetComponent<RectTransform>();
                buyRt.anchorMin = new Vector2(0.66f, 0.15f);
                buyRt.anchorMax = new Vector2(0.96f, 0.85f);
                buyRt.offsetMin = Vector2.zero;
                buyRt.offsetMax = Vector2.zero;

                var buyImg = buyBtnObj.GetComponent<Image>();
                buyImg.color = canAfford ? new Color(0.18f, 0.55f, 0.34f, 1f) : new Color(0.4f, 0.4f, 0.4f, 0.7f);

                var buyBtn = buyBtnObj.GetComponent<Button>();
                buyBtn.interactable = canAfford;

                GameObject buyTextObj = new GameObject("BuyText", typeof(RectTransform), typeof(TextMeshProUGUI));
                buyTextObj.transform.SetParent(buyBtnObj.transform, false);
                var buyTextRt = buyTextObj.GetComponent<RectTransform>();
                buyTextRt.sizeDelta = buyRt.sizeDelta;
                var buyTmp = buyTextObj.GetComponent<TextMeshProUGUI>();
                buyTmp.fontSize = 13;
                buyTmp.alignment = TextAlignmentOptions.Center;
                buyTmp.text = $"<b>Buy</b>\n${item.price:F2}";

                buyBtn.onClick.AddListener(() =>
                {
                    if (MarketManager.Instance.BuyItem(item))
                    {
                        if (purchaseChimeSound != null)
                        {
                            AudioManager.Instance?.PlaySFX(purchaseChimeSound);
                        }
                        UpdateSupermarketDisplay(dayNumber);
                    }
                });
            }
        }
    }
}
