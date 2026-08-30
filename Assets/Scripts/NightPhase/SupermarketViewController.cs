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

        [Header("Ingredient Icons (Optional)")]
        [SerializeField] private Sprite freshMilkIcon;
        [SerializeField] private Sprite oatMilkIcon;
        [SerializeField] private Sprite coconutMilkIcon;
        [SerializeField] private Sprite condensedMilkIcon;
        [SerializeField] private Sprite tapiocaIcon;
        [SerializeField] private Sprite poppingBobaIcon;
        [SerializeField] private Sprite grassJellyIcon;
        [SerializeField] private Sprite eggPuddingIcon;
        [SerializeField] private Sprite coconutJellyIcon;
        [SerializeField] private Sprite cheeseFoamIcon;
        [SerializeField] private Sprite goldenHoneyPearlsIcon;

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

        public Sprite GetIngredientIcon(string key)
        {
            Sprite icon = key switch
            {
                "Milk_FreshMilk" => freshMilkIcon,
                "Milk_OatMilk" => oatMilkIcon,
                "Milk_CoconutMilk" => coconutMilkIcon,
                "Milk_CondensedMilk" => condensedMilkIcon,
                "Topping_TapiocaPearls" => tapiocaIcon,
                "Topping_PoppingBoba" => poppingBobaIcon,
                "Topping_GrassJelly" => grassJellyIcon,
                "Topping_EggPudding" => eggPuddingIcon,
                "Topping_CoconutJelly" => coconutJellyIcon,
                "Topping_CheeseFoam" => cheeseFoamIcon,
                "Topping_GoldenHoneyPearls" => goldenHoneyPearlsIcon,
                _ => null
            };

            if (icon == null && CashRegisterInventoryUI.Instance != null)
            {
                icon = CashRegisterInventoryUI.Instance.GetIngredientIcon(key);
            }
            else if (icon == null && CupStation.Instance != null)
            {
                icon = key switch
                {
                    "Topping_TapiocaPearls" => CupStation.Instance.TapiocaSprite,
                    "Topping_PoppingBoba" => CupStation.Instance.PoppingBobaSprite,
                    "Topping_GrassJelly" => CupStation.Instance.GrassJellySprite,
                    "Topping_EggPudding" => CupStation.Instance.EggPuddingSprite,
                    "Topping_CoconutJelly" => CupStation.Instance.CoconutJellySprite,
                    "Topping_CheeseFoam" => CupStation.Instance.CheeseFoamSprite,
                    "Topping_GoldenHoneyPearls" => CupStation.Instance.GoldenHoneyPearlsSprite,
                    _ => null
                };
            }

            return icon;
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
                marketAisleTitleText.text = $"Wholesale Supermarket — Day {dayNumber}";
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

        private string FormatStockCount(int count)
        {
            string colorHex = count == 0 ? "#FF4444" : (count <= 6 ? "#F1C40F" : "#2ECC71");
            return $"<color={colorHex}>x {count:D2}</color>";
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
            if (catalog.Count == 0) return;

            RectTransform containerRt = marketCatalogContainer as RectTransform;
            float totalWidth = containerRt != null && containerRt.rect.width > 200 ? containerRt.rect.width : 960f;
            float totalHeight = containerRt != null && containerRt.rect.height > 100 ? containerRt.rect.height : 450f;

            int cols = totalWidth > 800 ? 3 : 2;
            int totalRows = Mathf.CeilToInt((float)catalog.Count / cols);

            float paddingX = 16f;
            float paddingY = 16f;
            float spacingX = 20f;
            float spacingY = 16f;

            float cardWidth = (totalWidth - (paddingX * 2) - (spacingX * (cols - 1))) / cols;
            float cardHeight = Mathf.Clamp((totalHeight - (paddingY * 2) - (spacingY * (totalRows - 1))) / Mathf.Max(1, totalRows), 90f, 120f);

            float startX = -totalWidth * 0.5f + paddingX + (cardWidth * 0.5f);
            float startY = totalHeight * 0.5f - paddingY - (cardHeight * 0.5f);

            for (int i = 0; i < catalog.Count; i++)
            {
                var item = catalog[i];
                int currentStock = InventoryManager.Instance != null ? InventoryManager.Instance.GetStock(item.stockKey) : 0;
                bool canAfford = EconomyManager.Instance != null && EconomyManager.Instance.CanAfford(item.price);
                Sprite itemIcon = GetIngredientIcon(item.stockKey);

                int row = i / cols;
                int col = i % cols;
                Vector2 pos = new Vector2(startX + col * (cardWidth + spacingX), startY - row * (cardHeight + spacingY));

                // Container card
                GameObject cardObj = new GameObject($"Card_{item.stockKey}", typeof(RectTransform), typeof(Image));
                cardObj.transform.SetParent(marketCatalogContainer, false);
                var rt = cardObj.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(cardWidth, cardHeight);
                rt.anchoredPosition = pos;

                var img = cardObj.GetComponent<Image>();
                img.color = new Color(0.12f, 0.16f, 0.24f, 0.95f);

                // Left: Ingredient Icon
                float leftOffset = 14f;
                if (itemIcon != null)
                {
                    GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                    iconObj.transform.SetParent(cardObj.transform, false);
                    var iconRt = iconObj.GetComponent<RectTransform>();
                    iconRt.anchorMin = new Vector2(0, 0.5f);
                    iconRt.anchorMax = new Vector2(0, 0.5f);
                    iconRt.pivot = new Vector2(0, 0.5f);
                    float iconSize = Mathf.Min(cardHeight - 16f, 68f);
                    iconRt.sizeDelta = new Vector2(iconSize, iconSize);
                    iconRt.anchoredPosition = new Vector2(12, 0);

                    var iconImg = iconObj.GetComponent<Image>();
                    iconImg.sprite = itemIcon;
                    iconImg.preserveAspect = true;
                    leftOffset = iconSize + 24f;
                }

                // Right: Dedicated Buy Button (Generous width to fit large text without wrapping)
                float buyButtonWidth = 120f;
                GameObject buyBtnObj = new GameObject("BuyButton", typeof(RectTransform), typeof(Image), typeof(Button));
                buyBtnObj.transform.SetParent(cardObj.transform, false);
                var buyRt = buyBtnObj.GetComponent<RectTransform>();
                buyRt.anchorMin = new Vector2(1, 0.5f);
                buyRt.anchorMax = new Vector2(1, 0.5f);
                buyRt.pivot = new Vector2(1, 0.5f);
                buyRt.sizeDelta = new Vector2(buyButtonWidth, cardHeight - 18f);
                buyRt.anchoredPosition = new Vector2(-12, 0);

                var buyImg = buyBtnObj.GetComponent<Image>();
                buyImg.color = canAfford ? new Color(0.18f, 0.55f, 0.34f, 1f) : new Color(0.35f, 0.35f, 0.35f, 0.65f);

                var buyBtn = buyBtnObj.GetComponent<Button>();
                buyBtn.interactable = canAfford;

                GameObject buyTextObj = new GameObject("BuyText", typeof(RectTransform), typeof(TextMeshProUGUI));
                buyTextObj.transform.SetParent(buyBtnObj.transform, false);
                var buyTextRt = buyTextObj.GetComponent<RectTransform>();
                buyTextRt.anchorMin = Vector2.zero;
                buyTextRt.anchorMax = Vector2.one;
                buyTextRt.offsetMin = new Vector2(4, 2);
                buyTextRt.offsetMax = new Vector2(-4, -2);

                var buyTmp = buyTextObj.GetComponent<TextMeshProUGUI>();
                buyTmp.fontSize = 20f;
                buyTmp.alignment = TextAlignmentOptions.Center;
                buyTmp.enableWordWrapping = false;
                buyTmp.lineSpacing = -10f;
                buyTmp.text = $"<b>BUY</b>\n<size=17>${item.price:F2}</size>";

                // Middle: Info Text (Item Name + Pack Quantity + In Store count)
                GameObject infoTextObj = new GameObject("InfoText", typeof(RectTransform), typeof(TextMeshProUGUI));
                infoTextObj.transform.SetParent(cardObj.transform, false);
                var infoRt = infoTextObj.GetComponent<RectTransform>();
                infoRt.anchorMin = new Vector2(0, 0);
                infoRt.anchorMax = new Vector2(1, 1);
                infoRt.offsetMin = new Vector2(leftOffset, 4);
                infoRt.offsetMax = new Vector2(-(buyButtonWidth + 20f), -4);

                var infoTmp = infoTextObj.GetComponent<TextMeshProUGUI>();
                infoTmp.fontSize = 22f;
                infoTmp.alignment = TextAlignmentOptions.MidlineLeft;
                infoTmp.enableWordWrapping = true;
                infoTmp.text = $"<b>{item.displayName}</b>\n" +
                               $"<size=17><color=#BDC3C7>Pack of {item.bundleQuantity}</color>  |  In Store: {FormatStockCount(currentStock)}</size>";

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
