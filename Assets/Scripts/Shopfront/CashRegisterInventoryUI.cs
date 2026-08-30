using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BubbleTeaShop
{
    public class CashRegisterInventoryUI : MonoBehaviour
    {
        public static CashRegisterInventoryUI Instance { get; private set; }

        [Header("UI Panels")]
        [SerializeField] private GameObject inventoryModalPanel;
        [SerializeField] private Button cashRegisterButton;
        [SerializeField] private Button closeButton;

        [Header("Display Text (Fallbacks)")]
        [SerializeField] private TextMeshProUGUI cashBalanceText;
        [SerializeField] private TextMeshProUGUI milkStockText;
        [SerializeField] private TextMeshProUGUI toppingStockText;
        [SerializeField] private TextMeshProUGUI marketNewsText;

        [Header("Card Containers (Visual Item Cards)")]
        [SerializeField] private Transform inventoryCardsContainer;
        [SerializeField] private Transform milkTableContainer;
        [SerializeField] private Transform toppingTableContainer;

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

        [Header("Audio")]
        [SerializeField] private AudioClip registerChimeSound;

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
            if (cashRegisterButton != null)
            {
                cashRegisterButton.onClick.AddListener(OpenInventoryModal);
            }
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(CloseInventoryModal);
            }

            if (inventoryModalPanel != null)
            {
                inventoryModalPanel.SetActive(false);
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

            if (icon == null && CupStation.Instance != null)
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

        public void TriggerAttentionPulse(float duration = 2.5f)
        {
            if (cashRegisterButton == null) return;
            var helper = cashRegisterButton.GetComponent<CashRegisterAttentionHelper>();
            if (helper == null)
            {
                helper = cashRegisterButton.gameObject.AddComponent<CashRegisterAttentionHelper>();
            }
            helper.TriggerPulse(duration);
        }

        public void OpenInventoryModal()
        {
            if (cashRegisterButton != null)
            {
                var helper = cashRegisterButton.GetComponent<CashRegisterAttentionHelper>();
                if (helper != null) helper.StopPulse();
            }

            if (registerChimeSound != null)
            {
                AudioManager.Instance?.PlaySFX(registerChimeSound);
            }

            UpdateInventoryDisplay();
            if (inventoryModalPanel != null)
            {
                inventoryModalPanel.SetActive(true);
            }
        }

        public void CloseInventoryModal()
        {
            if (inventoryModalPanel != null)
            {
                inventoryModalPanel.SetActive(false);
            }
        }

        public void UpdateInventoryDisplay()
        {
            if (EconomyManager.Instance != null && cashBalanceText != null)
            {
                cashBalanceText.text = $"Shop Balance: <color=#2ECC71>${EconomyManager.Instance.CurrentCash:F2}</color>";
            }

            if (InventoryManager.Instance == null) return;

            // Milks Data
            var milks = new (string key, string name, int count)[]
            {
                ("Milk_FreshMilk", "Fresh Whole Milk", InventoryManager.Instance.GetMilkStock(MilkType.FreshMilk)),
                ("Milk_OatMilk", "Barista Oat Milk", InventoryManager.Instance.GetMilkStock(MilkType.OatMilk)),
                ("Milk_CoconutMilk", "Organic Coconut Milk", InventoryManager.Instance.GetMilkStock(MilkType.CoconutMilk)),
                ("Milk_CondensedMilk", "Sweet Condensed Milk", InventoryManager.Instance.GetMilkStock(MilkType.CondensedMilk))
            };

            // Toppings Data
            var toppings = new (string key, string name, int count)[]
            {
                ("Topping_TapiocaPearls", "Raw Tapioca Pearls", InventoryManager.Instance.GetToppingStock(ToppingType.TapiocaPearls)),
                ("Topping_PoppingBoba", "Mango Popping Boba", InventoryManager.Instance.GetToppingStock(ToppingType.PoppingBoba)),
                ("Topping_GrassJelly", "Herbal Grass Jelly", InventoryManager.Instance.GetToppingStock(ToppingType.GrassJelly)),
                ("Topping_EggPudding", "Silky Egg Custard", InventoryManager.Instance.GetToppingStock(ToppingType.EggPudding)),
                ("Topping_CoconutJelly", "Sweet Coconut Jelly", InventoryManager.Instance.GetToppingStock(ToppingType.CoconutJelly)),
                ("Topping_CheeseFoam", "Salted Cheese Foam", InventoryManager.Instance.GetToppingStock(ToppingType.CheeseFoam)),
                ("Topping_GoldenHoneyPearls", "Golden Honey Pearls", InventoryManager.Instance.GetToppingStock(ToppingType.GoldenHoneyPearls))
            };

            // Update Fallback Texts (Clean ASCII)
            if (milkStockText != null)
            {
                string text = "<b>MILKS</b>\n";
                foreach (var m in milks)
                {
                    text += $"• {m.name}  <color=#F1C40F>x {m.count:D2}</color>\n";
                }
                milkStockText.text = text.TrimEnd();
            }

            if (toppingStockText != null)
            {
                string text = "<b>TOPPINGS</b>\n";
                foreach (var t in toppings)
                {
                    text += $"• {t.name}  <color=#F1C40F>x {t.count:D2}</color>\n";
                }
                toppingStockText.text = text.TrimEnd();
            }

            // Populate Visual Container Cards
            if (inventoryCardsContainer != null)
            {
                PopulateUnifiedTwoColumnCards(inventoryCardsContainer, milks, toppings);
            }
            else
            {
                if (milkTableContainer != null)
                {
                    PopulateCardList(milkTableContainer, milks, "MILKS");
                }
                if (toppingTableContainer != null)
                {
                    PopulateCardList(toppingTableContainer, toppings, "TOPPINGS");
                }
            }

            if (marketNewsText != null)
            {
                if (MarketEventManager.Instance != null && MarketEventManager.Instance.ActiveEvent != null)
                {
                    var ev = MarketEventManager.Instance.ActiveEvent;
                    marketNewsText.text = $"<b>Market News:</b> <color=#FFAA00>{ev.title}</color> ({ev.daysRemaining}d left)\n<i>{ev.description}</i>";
                }
                else
                {
                    marketNewsText.text = "<b>Market News:</b> <i>Wholesale prices are stable today.</i>";
                }
            }
        }

        private void PopulateUnifiedTwoColumnCards(Transform container, (string key, string name, int count)[] milks, (string key, string name, int count)[] toppings)
        {
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Destroy(container.GetChild(i).gameObject);
            }

            RectTransform containerRt = container as RectTransform;
            float totalWidth = containerRt != null && containerRt.rect.width > 200 ? containerRt.rect.width : 780f;
            float colWidth = (totalWidth - 30f) * 0.5f;

            // Left Column: Milks
            GameObject milkColObj = new GameObject("MilksColumn", typeof(RectTransform));
            milkColObj.transform.SetParent(container, false);
            var milkColRt = milkColObj.GetComponent<RectTransform>();
            milkColRt.anchorMin = new Vector2(0, 0);
            milkColRt.anchorMax = new Vector2(0.49f, 1);
            milkColRt.offsetMin = Vector2.zero;
            milkColRt.offsetMax = Vector2.zero;
            PopulateCardList(milkColObj.transform, milks, "MILKS");

            // Right Column: Toppings
            GameObject toppingColObj = new GameObject("ToppingsColumn", typeof(RectTransform));
            toppingColObj.transform.SetParent(container, false);
            var toppingColRt = toppingColObj.GetComponent<RectTransform>();
            toppingColRt.anchorMin = new Vector2(0.51f, 0);
            toppingColRt.anchorMax = new Vector2(1, 1);
            toppingColRt.offsetMin = Vector2.zero;
            toppingColRt.offsetMax = Vector2.zero;
            PopulateCardList(toppingColObj.transform, toppings, "TOPPINGS");
        }

        private void PopulateCardList(Transform container, (string key, string name, int count)[] items, string sectionTitle)
        {
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Destroy(container.GetChild(i).gameObject);
            }

            float cardHeight = 44f;
            float spacingY = 6f;
            float headerHeight = 24f;

            // Section Header
            GameObject headerObj = new GameObject($"Header_{sectionTitle}", typeof(RectTransform), typeof(TextMeshProUGUI));
            headerObj.transform.SetParent(container, false);
            var headerRt = headerObj.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0, 1);
            headerRt.anchorMax = new Vector2(1, 1);
            headerRt.pivot = new Vector2(0.5f, 1);
            headerRt.sizeDelta = new Vector2(0, headerHeight);
            headerRt.anchoredPosition = new Vector2(0, 0);

            var headerTmp = headerObj.GetComponent<TextMeshProUGUI>();
            headerTmp.text = $"<b>{sectionTitle}</b>";
            headerTmp.fontSize = 14;
            headerTmp.alignment = TextAlignmentOptions.MidlineLeft;
            headerTmp.color = new Color(0.9f, 0.9f, 0.95f, 1f);

            // Item Cards
            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                Sprite icon = GetIngredientIcon(item.key);

                GameObject cardObj = new GameObject($"Card_{item.key}", typeof(RectTransform), typeof(Image));
                cardObj.transform.SetParent(container, false);
                var rt = cardObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(1, 1);
                rt.pivot = new Vector2(0.5f, 1);
                rt.sizeDelta = new Vector2(0, cardHeight);
                rt.anchoredPosition = new Vector2(0, -(headerHeight + 6f) - (i * (cardHeight + spacingY)));

                var cardImg = cardObj.GetComponent<Image>();
                cardImg.color = new Color(0.12f, 0.16f, 0.24f, 0.94f);

                // Left: Icon
                float leftOffset = 10f;
                if (icon != null)
                {
                    GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                    iconObj.transform.SetParent(cardObj.transform, false);
                    var iconRt = iconObj.GetComponent<RectTransform>();
                    iconRt.anchorMin = new Vector2(0, 0.5f);
                    iconRt.anchorMax = new Vector2(0, 0.5f);
                    iconRt.pivot = new Vector2(0, 0.5f);
                    iconRt.sizeDelta = new Vector2(30, 30);
                    iconRt.anchoredPosition = new Vector2(8, 0);

                    var img = iconObj.GetComponent<Image>();
                    img.sprite = icon;
                    img.preserveAspect = true;
                    leftOffset = 44f;
                }

                // Right: Count Badge Pill
                GameObject pillObj = new GameObject("CountPill", typeof(RectTransform), typeof(Image));
                pillObj.transform.SetParent(cardObj.transform, false);
                var pillRt = pillObj.GetComponent<RectTransform>();
                pillRt.anchorMin = new Vector2(1, 0.5f);
                pillRt.anchorMax = new Vector2(1, 0.5f);
                pillRt.pivot = new Vector2(1, 0.5f);
                pillRt.sizeDelta = new Vector2(58, 28);
                pillRt.anchoredPosition = new Vector2(-8, 0);

                var pillImg = pillObj.GetComponent<Image>();
                pillImg.color = new Color(0.18f, 0.24f, 0.36f, 0.90f);

                GameObject countTextObj = new GameObject("CountText", typeof(RectTransform), typeof(TextMeshProUGUI));
                countTextObj.transform.SetParent(pillObj.transform, false);
                var countTextRt = countTextObj.GetComponent<RectTransform>();
                countTextRt.anchorMin = Vector2.zero;
                countTextRt.anchorMax = Vector2.one;
                countTextRt.offsetMin = Vector2.zero;
                countTextRt.offsetMax = Vector2.zero;

                var countTmp = countTextObj.GetComponent<TextMeshProUGUI>();
                countTmp.text = $"<color=#F1C40F>x {item.count:D2}</color>";
                countTmp.fontSize = 13;
                countTmp.alignment = TextAlignmentOptions.Center;
                countTmp.enableWordWrapping = false;

                // Middle: Item Name
                GameObject nameObj = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
                nameObj.transform.SetParent(cardObj.transform, false);
                var nameRt = nameObj.GetComponent<RectTransform>();
                nameRt.anchorMin = new Vector2(0, 0);
                nameRt.anchorMax = new Vector2(1, 1);
                nameRt.offsetMin = new Vector2(leftOffset, 0);
                nameRt.offsetMax = new Vector2(-70, 0);

                var nameTmp = nameObj.GetComponent<TextMeshProUGUI>();
                nameTmp.text = $"<b>{item.name}</b>";
                nameTmp.fontSize = 13;
                nameTmp.alignment = TextAlignmentOptions.MidlineLeft;
                nameTmp.color = Color.white;
                nameTmp.enableWordWrapping = false;
            }
        }
    }

    public class CashRegisterAttentionHelper : MonoBehaviour
    {
        private Coroutine pulseRoutine;

        public void TriggerPulse(float duration = 2.5f)
        {
            if (!gameObject.activeInHierarchy) return;
            if (pulseRoutine != null) StopCoroutine(pulseRoutine);
            pulseRoutine = StartCoroutine(PulseRoutine(duration));
        }

        public void StopPulse()
        {
            if (pulseRoutine != null)
            {
                StopCoroutine(pulseRoutine);
                pulseRoutine = null;
            }
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        private System.Collections.IEnumerator PulseRoutine(float duration)
        {
            Transform tform = transform;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float wiggle = Mathf.Sin(elapsed * Mathf.PI * 6f) * 7f;
                float scale = 1f + Mathf.PingPong(elapsed * 2f, 0.25f);
                tform.localRotation = Quaternion.Euler(0, 0, wiggle);
                tform.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }

            tform.localRotation = Quaternion.identity;
            tform.localScale = Vector3.one;
            pulseRoutine = null;
        }
    }
}
