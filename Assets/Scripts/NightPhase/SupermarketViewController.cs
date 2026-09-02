using System;
using System.Collections;
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
        [SerializeField] private TextMeshProUGUI cashDeductionDeltaText;
        [SerializeField] private TextMeshProUGUI marketAisleTitleText;
        [SerializeField] private Button returnToNightHubButton;

        public TextMeshProUGUI CashDeductionDeltaText
        {
            get => cashDeductionDeltaText;
            set => cashDeductionDeltaText = value;
        }

        private Coroutine cashDeductionRoutine;
        private Vector2 cashDeductionOriginalPos;
        private bool hasCapturedCashDeductionPos = false;

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

            EnsureFallbackAssets();
            EnsureSupermarketPanelHierarchy();
        }

        private void Start()
        {
            if (returnToNightHubButton != null)
            {
                returnToNightHubButton.onClick.RemoveListener(CloseSupermarketView);
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

        private void EnsureFallbackAssets()
        {
#if UNITY_EDITOR
            if (supermarketInteriorSprite == null)
            {
                supermarketInteriorSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Sprites2/Sprites 3/market.png");
            }
#endif
            if (supermarketInteriorSprite == null)
            {
                var allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
                for (int i = 0; i < allSprites.Length; i++)
                {
                    var s = allSprites[i];
                    if (s == null) continue;
                    if (supermarketInteriorSprite == null && (s.name.ToLower().Contains("market") || s.name.ToLower().Contains("supermarket")))
                    {
                        supermarketInteriorSprite = s;
                        break;
                    }
                }
            }
        }

        private void EnsureSupermarketPanelHierarchy()
        {
            // 1. Resolve Root Panel
            if (supermarketPanelRoot == null)
            {
                supermarketPanelRoot = gameObject;
            }

            // 2. Resolve Background Image
            if (supermarketBackgroundImage == null)
            {
                supermarketBackgroundImage = GetComponent<Image>();
                if (supermarketBackgroundImage == null)
                {
                    var bgChild = transform.Find("SupermarketBg");
                    if (bgChild != null) supermarketBackgroundImage = bgChild.GetComponent<Image>();
                }
            }

            // 3. Resolve Header Title
            if (marketAisleTitleText == null)
            {
                var titleChild = transform.Find("MarketAisleTitleText") ?? transform.Find("TitleText");
                if (titleChild != null) marketAisleTitleText = titleChild.GetComponent<TextMeshProUGUI>();
            }

            // 4. Resolve Cash Balance Text
            if (cashBalanceText == null)
            {
                var cashChild = transform.Find("CashBalanceText") ?? transform.Find("CashBalanceText (1)") ?? transform.Find("CashText");
                if (cashChild != null) cashBalanceText = cashChild.GetComponent<TextMeshProUGUI>();
            }

            if (cashBalanceText != null)
            {
                cashBalanceText.raycastTarget = false;
            }

            // 4b. Resolve Cash Deduction Delta Text
            EnsureCashDeductionUI();

            // 5. Resolve Return / Exit Button
            if (returnToNightHubButton == null)
            {
                var retChild = transform.Find("ReturnToNightHubButton") ?? transform.Find("ReturnShopButton") ?? transform.Find("ExitButton") ?? transform.Find("BackButton");
                if (retChild != null) returnToNightHubButton = retChild.GetComponent<Button>();
            }

            if (returnToNightHubButton != null)
            {
                returnToNightHubButton.onClick.RemoveListener(CloseSupermarketView);
                returnToNightHubButton.onClick.AddListener(CloseSupermarketView);
                returnToNightHubButton.transform.SetAsLastSibling();

                var btnText = returnToNightHubButton.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null) btnText.raycastTarget = false;
            }

            // 6. Resolve Catalog Container
            if (marketCatalogContainer == null)
            {
                var catChild = transform.Find("MarketCatalogContainer");
                if (catChild != null)
                {
                    marketCatalogContainer = catChild;
                }
                else
                {
                    GameObject catalogObj = new GameObject("MarketCatalogContainer", typeof(RectTransform));
                    catalogObj.transform.SetParent(transform, false);
                    var catalogRt = catalogObj.GetComponent<RectTransform>();
                    catalogRt.anchorMin = new Vector2(0.5f, 0.5f);
                    catalogRt.anchorMax = new Vector2(0.5f, 0.5f);
                    catalogRt.pivot = new Vector2(0.5f, 0.5f);
                    catalogRt.anchoredPosition = new Vector2(0f, -30f);
                    catalogRt.sizeDelta = new Vector2(980f, 460f);
                    marketCatalogContainer = catalogObj.transform;
                }
            }
        }

        public Sprite GetIngredientIcon(string key)
        {
            if (SpriteManager.Instance != null)
            {
                var sp = SpriteManager.Instance.GetSprite(key);
                if (sp != null) return sp;
            }

            if (CupStation.Instance != null)
            {
                var sp = CupStation.Instance.GetToppingSprite(key);
                if (sp != null) return sp;
            }

            return null;
        }

        public void OpenSupermarketView(int dayNumber)
        {
            if (supermarketPanelRoot != null)
            {
                supermarketPanelRoot.SetActive(true);
            }

            HUDController.Instance?.SetSubscreenMode(true, "Wholesale Supermarket: Tap items to purchase supplies for tomorrow.");

            if (supermarketBackgroundImage != null && supermarketInteriorSprite != null)
            {
                supermarketBackgroundImage.sprite = supermarketInteriorSprite;
            }

            if (marketAisleTitleText != null)
            {
                marketAisleTitleText.text = $"Wholesale Supermarket - Day {dayNumber}";
            }

            UpdateSupermarketDisplay(dayNumber);
        }

        public void CloseSupermarketView()
        {
            if (supermarketPanelRoot != null)
            {
                supermarketPanelRoot.SetActive(false);
            }
            HUDController.Instance?.SetSubscreenMode(false);
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

            // Use 2 columns for a spacious, clean layout with ample room for titles and stats
            int cols = totalWidth >= 1250f ? 3 : 2;
            int totalRows = Mathf.CeilToInt((float)catalog.Count / cols);

            float paddingX = 16f;
            float paddingY = 12f;
            float spacingX = 20f;
            float spacingY = 12f;

            float cardWidth = (totalWidth - (paddingX * 2) - (spacingX * (cols - 1))) / cols;
            float cardHeight = Mathf.Clamp((totalHeight - (paddingY * 2) - (spacingY * (totalRows - 1))) / Mathf.Max(1, totalRows), 76f, 88f);

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
                    float iconSize = Mathf.Min(cardHeight - 16f, 54f);
                    iconRt.sizeDelta = new Vector2(iconSize, iconSize);
                    iconRt.anchoredPosition = new Vector2(10, 0);

                    var iconImg = iconObj.GetComponent<Image>();
                    iconImg.sprite = itemIcon;
                    iconImg.preserveAspect = true;
                    leftOffset = iconSize + 20f;
                }

                // Right: Dedicated Buy Button
                float buyButtonWidth = 96f;
                GameObject buyBtnObj = new GameObject("BuyButton", typeof(RectTransform), typeof(Image), typeof(Button));
                buyBtnObj.transform.SetParent(cardObj.transform, false);
                var buyRt = buyBtnObj.GetComponent<RectTransform>();
                buyRt.anchorMin = new Vector2(1, 0.5f);
                buyRt.anchorMax = new Vector2(1, 0.5f);
                buyRt.pivot = new Vector2(1, 0.5f);
                buyRt.sizeDelta = new Vector2(buyButtonWidth, cardHeight - 16f);
                buyRt.anchoredPosition = new Vector2(-10, 0);

                var buyImg = buyBtnObj.GetComponent<Image>();
                buyImg.color = canAfford ? new Color(0.18f, 0.55f, 0.34f, 1f) : new Color(0.35f, 0.35f, 0.35f, 0.65f);

                var buyBtn = buyBtnObj.GetComponent<Button>();
                buyBtn.interactable = canAfford;

                GameObject buyTextObj = new GameObject("BuyText", typeof(RectTransform), typeof(TextMeshProUGUI));
                buyTextObj.transform.SetParent(buyBtnObj.transform, false);
                var buyTextRt = buyTextObj.GetComponent<RectTransform>();
                buyTextRt.anchorMin = Vector2.zero;
                buyTextRt.anchorMax = Vector2.one;
                buyTextRt.offsetMin = new Vector2(2, 2);
                buyTextRt.offsetMax = new Vector2(-2, -2);

                var buyTmp = buyTextObj.GetComponent<TextMeshProUGUI>();
                buyTmp.fontSize = 17f;
                buyTmp.alignment = TextAlignmentOptions.Center;
                buyTmp.textWrappingMode = TextWrappingModes.NoWrap;
                buyTmp.lineSpacing = -4f;
                buyTmp.text = $"<b>BUY</b>\n<size=14>${item.price:F2}</size>";

                // Middle: Info Text (Item Name + Pack Quantity + In Store count)
                GameObject infoTextObj = new GameObject("InfoText", typeof(RectTransform), typeof(TextMeshProUGUI));
                infoTextObj.transform.SetParent(cardObj.transform, false);
                var infoRt = infoTextObj.GetComponent<RectTransform>();
                infoRt.anchorMin = new Vector2(0, 0);
                infoRt.anchorMax = new Vector2(1, 1);
                infoRt.offsetMin = new Vector2(leftOffset, 0);
                infoRt.offsetMax = new Vector2(-(buyButtonWidth + 18f), 0);

                var infoTmp = infoTextObj.GetComponent<TextMeshProUGUI>();
                infoTmp.fontSize = 18f;
                infoTmp.lineSpacing = 2f;
                infoTmp.alignment = TextAlignmentOptions.MidlineLeft;
                infoTmp.textWrappingMode = TextWrappingModes.Normal;
                infoTmp.text = $"<b>{item.displayName}</b>\n" +
                               $"<size=14><color=#BDC3C7>Pack of {item.bundleQuantity}</color>   In Store: {FormatStockCount(currentStock)}</size>";

                buyBtn.onClick.AddListener(() =>
                {
                    if (MarketManager.Instance.BuyItem(item))
                    {
                        if (purchaseChimeSound != null)
                        {
                            AudioManager.Instance?.PlaySFX(purchaseChimeSound);
                        }
                        ShowFloatingCashDeduction(item.price);
                        UpdateSupermarketDisplay(dayNumber);
                    }
                });
            }
        }

        public void EnsureCashDeductionUI()
        {
            if (cashDeductionDeltaText != null)
            {
                if (!hasCapturedCashDeductionPos)
                {
                    cashDeductionOriginalPos = cashDeductionDeltaText.rectTransform.anchoredPosition;
                    hasCapturedCashDeductionPos = true;
                }
                return;
            }

            Transform targetParent = (cashBalanceText != null) ? cashBalanceText.transform.parent : transform;
            Transform existing = targetParent.Find("CashDeductionDeltaText");
            if (existing != null)
            {
                cashDeductionDeltaText = existing.GetComponent<TextMeshProUGUI>();
                if (cashDeductionDeltaText != null)
                {
                    cashDeductionOriginalPos = cashDeductionDeltaText.rectTransform.anchoredPosition;
                    hasCapturedCashDeductionPos = true;
                    return;
                }
            }

            GameObject deltaObj = new GameObject("CashDeductionDeltaText", typeof(RectTransform), typeof(TextMeshProUGUI));
            deltaObj.transform.SetParent(targetParent, false);

            var rt = deltaObj.GetComponent<RectTransform>();
            if (cashBalanceText != null)
            {
                rt.anchorMin = cashBalanceText.rectTransform.anchorMin;
                rt.anchorMax = cashBalanceText.rectTransform.anchorMax;
                rt.pivot = cashBalanceText.rectTransform.pivot;
                Vector2 basePos = cashBalanceText.rectTransform.anchoredPosition + new Vector2(170f, 0f);
                rt.anchoredPosition = basePos;
                cashDeductionOriginalPos = basePos;
            }
            else
            {
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                Vector2 basePos = new Vector2(100f, -40f);
                rt.anchoredPosition = basePos;
                cashDeductionOriginalPos = basePos;
            }

            rt.sizeDelta = new Vector2(160f, 40f);
            hasCapturedCashDeductionPos = true;

            cashDeductionDeltaText = deltaObj.GetComponent<TextMeshProUGUI>();
            cashDeductionDeltaText.fontSize = 24;
            cashDeductionDeltaText.fontStyle = FontStyles.Bold;
            cashDeductionDeltaText.color = new Color(1f, 0.35f, 0.35f, 1f); // #FF5555 Red
            cashDeductionDeltaText.alignment = TextAlignmentOptions.Left;
            cashDeductionDeltaText.raycastTarget = false;

            deltaObj.SetActive(false);
        }

        public void ShowFloatingCashDeduction(float amount)
        {
            if (amount <= 0) return;

            EnsureCashDeductionUI();
            if (cashDeductionDeltaText == null) return;

            if (cashDeductionRoutine != null)
            {
                StopCoroutine(cashDeductionRoutine);
            }
            cashDeductionRoutine = StartCoroutine(CashDeductionFloatRoutine(amount));
        }

        private IEnumerator CashDeductionFloatRoutine(float amount)
        {
            cashDeductionDeltaText.gameObject.SetActive(true);
            cashDeductionDeltaText.text = $"-${amount:F2}";
            cashDeductionDeltaText.color = new Color(1f, 0.35f, 0.35f, 1f);

            RectTransform rt = cashDeductionDeltaText.rectTransform;
            Vector2 startPos = cashDeductionOriginalPos;
            Vector2 targetPos = startPos + new Vector2(0f, -18f);

            float duration = 1.5f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Smooth downward drift
                rt.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);

                // Pop scale in first 0.15s
                if (t < 0.15f)
                {
                    float pop = Mathf.Lerp(1.35f, 1.0f, t / 0.15f);
                    rt.localScale = new Vector3(pop, pop, 1f);
                }
                else
                {
                    rt.localScale = Vector3.one;
                }

                // Smooth fade out in last 0.5s
                if (t > 0.65f)
                {
                    float alpha = Mathf.Lerp(1f, 0f, (t - 0.65f) / 0.35f);
                    cashDeductionDeltaText.color = new Color(1f, 0.35f, 0.35f, alpha);
                }

                yield return null;
            }

            rt.anchoredPosition = startPos;
            rt.localScale = Vector3.one;
            cashDeductionDeltaText.gameObject.SetActive(false);
            cashDeductionRoutine = null;
        }
    }
}
