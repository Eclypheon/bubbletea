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

        public enum InventoryTab
        {
            Items,
            Upgrades
        }

        [Header("UI Panels")]
        [SerializeField] private GameObject inventoryModalPanel;
        [SerializeField] private Button cashRegisterButton;
        [SerializeField] private Button closeButton;

        [Header("Tab Navigation")]
        [Tooltip("Button that toggles between Items (Stock) and Upgrades tab. Wire this in Inspector!")]
        [SerializeField] private Button tabToggleButton;
        [SerializeField] private TextMeshProUGUI tabToggleText;
        [SerializeField] private GameObject itemsTabPanel;
        [SerializeField] private GameObject upgradesTabPanel;
        [SerializeField] private Transform upgradesScrollContainer;

        [Header("Testing & Cheats")]
        [Tooltip("When enabled, allows toggling the Upgrades tab even before Day 8 (useful for testing).")]
        [SerializeField] private bool bypassDayRequirementForTesting = false;

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

        private InventoryTab currentTab = InventoryTab.Items;
        private int lastToggleFrame = -1;

        public InventoryTab CurrentTab => currentTab;
        public Button TabToggleButton => tabToggleButton;
        public GameObject UpgradesTabPanel => upgradesTabPanel;
        public GameObject ItemsTabPanel => itemsTabPanel;

        public bool IsUpgradesUnlocked()
        {
            if (bypassDayRequirementForTesting) return true;
            if (GameManager.Instance != null && GameManager.Instance.IsBlitzMode) return false;
            int day = DayManager.Instance != null ? DayManager.Instance.CurrentDay : 1;
            return day >= 8;
        }

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

            EnsureTabsUI();

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

        public void EnsureTabsUI()
        {
            Transform modalRoot = inventoryModalPanel != null ? inventoryModalPanel.transform : transform;

            // 1. Auto-discover Tab Toggle Button if unassigned
            if (tabToggleButton == null)
            {
                tabToggleButton = FindButtonInChildren(modalRoot, "TabToggleButton", "UpgradesButton", "TabSwitchButton", "UpgradeTabBtn", "TabButton", "ToggleTabBtn", "UpgradesBtn", "ToggleBtn", "Upgrades", "Upgrade");
            }

            if (tabToggleButton != null && tabToggleText == null)
            {
                tabToggleText = tabToggleButton.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (tabToggleButton != null)
            {
                tabToggleButton.onClick.RemoveListener(ToggleTab);
                if (tabToggleButton.onClick.GetPersistentEventCount() == 0)
                {
                    tabToggleButton.onClick.AddListener(ToggleTab);
                }
            }

            // 2. Auto-discover itemsTabPanel
            if (itemsTabPanel == null)
            {
                Transform found = modalRoot.Find("ItemsTabPanel") ?? modalRoot.Find("ItemsPanel") ?? modalRoot.Find("StockPanel");
                if (found != null)
                {
                    itemsTabPanel = found.gameObject;
                }
                else if (inventoryCardsContainer != null)
                {
                    itemsTabPanel = inventoryCardsContainer.gameObject;
                }
            }

            // 3. Auto-discover or dynamically create upgradesTabPanel
            if (upgradesTabPanel == null)
            {
                Transform found = modalRoot.Find("UpgradesTabPanel") ?? modalRoot.Find("UpgradesPanel");
                if (found != null)
                {
                    upgradesTabPanel = found.gameObject;
                    upgradesScrollContainer = found.Find("ScrollView/Viewport/Content") ??
                                             found.Find("Scroll View/Viewport/Content") ??
                                             found.Find("Viewport/Content") ??
                                             found.Find("Content") ?? found;
                }
                else
                {
                    // Create dynamic upgrades tab panel matching inventoryCardsContainer's layout
                    upgradesTabPanel = new GameObject("UpgradesTabPanel", typeof(RectTransform));
                    Transform parent = itemsTabPanel != null ? itemsTabPanel.transform.parent : modalRoot;
                    upgradesTabPanel.transform.SetParent(parent, false);

                    var upgRt = upgradesTabPanel.GetComponent<RectTransform>();
                    if (inventoryCardsContainer != null && inventoryCardsContainer.TryGetComponent<RectTransform>(out var cardsRt))
                    {
                        upgRt.anchorMin = cardsRt.anchorMin;
                        upgRt.anchorMax = cardsRt.anchorMax;
                        upgRt.pivot = cardsRt.pivot;
                        upgRt.anchoredPosition = cardsRt.anchoredPosition;
                        upgRt.sizeDelta = cardsRt.sizeDelta;
                    }
                    else
                    {
                        upgRt.anchorMin = new Vector2(0.5f, 0.5f);
                        upgRt.anchorMax = new Vector2(0.5f, 0.5f);
                        upgRt.pivot = new Vector2(0.5f, 0.5f);
                        upgRt.anchoredPosition = new Vector2(0f, 115f);
                        upgRt.sizeDelta = new Vector2(1180f, 715f);
                    }

                    // Create Scroll View
                    GameObject scrollGo = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
                    scrollGo.transform.SetParent(upgradesTabPanel.transform, false);
                    var scrollRt = scrollGo.GetComponent<RectTransform>();
                    scrollRt.anchorMin = Vector2.zero;
                    scrollRt.anchorMax = Vector2.one;
                    scrollRt.offsetMin = Vector2.zero;
                    scrollRt.offsetMax = Vector2.zero;

                    var scrollImg = scrollGo.GetComponent<Image>();
                    scrollImg.color = new Color(0f, 0f, 0f, 0.01f);
                    scrollImg.raycastTarget = true;

                    var scrollRect = scrollGo.GetComponent<ScrollRect>();
                    scrollRect.horizontal = false;
                    scrollRect.vertical = true;
                    scrollRect.scrollSensitivity = 30f;

                    // Viewport with RectMask2D
                    GameObject viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
                    viewportGo.transform.SetParent(scrollGo.transform, false);
                    var viewRt = viewportGo.GetComponent<RectTransform>();
                    viewRt.anchorMin = Vector2.zero;
                    viewRt.anchorMax = Vector2.one;
                    viewRt.offsetMin = Vector2.zero;
                    viewRt.offsetMax = Vector2.zero;

                    // Content
                    GameObject contentGo = new GameObject("Content", typeof(RectTransform));
                    contentGo.transform.SetParent(viewportGo.transform, false);
                    var contentRt = contentGo.GetComponent<RectTransform>();
                    contentRt.anchorMin = new Vector2(0.5f, 1f);
                    contentRt.anchorMax = new Vector2(0.5f, 1f);
                    contentRt.pivot = new Vector2(0.5f, 1f);
                    contentRt.anchoredPosition = Vector2.zero;
                    contentRt.sizeDelta = new Vector2(upgRt.sizeDelta.x, 600f);

                    scrollRect.viewport = viewRt;
                    scrollRect.content = contentRt;

                    upgradesScrollContainer = contentGo.transform;
                }
            }
        }

        private Button FindButtonInChildren(Transform root, params string[] names)
        {
            if (root == null) return null;

            // Direct children matching names
            foreach (var n in names)
            {
                Transform found = root.Find(n);
                if (found != null && found.TryGetComponent<Button>(out var b)) return b;
            }

            // Recursive search by exact name
            var allButtons = root.GetComponentsInChildren<Button>(true);
            foreach (var b in allButtons)
            {
                foreach (var n in names)
                {
                    if (b.name.Equals(n, StringComparison.OrdinalIgnoreCase)) return b;
                }
            }

            // Fallback: search by partial name match
            foreach (var b in allButtons)
            {
                if (b == closeButton || b == cashRegisterButton) continue;
                string lname = b.name.ToLowerInvariant();
                if (lname.Contains("upgrade") || lname.Contains("tab") || lname.Contains("toggle"))
                {
                    return b;
                }
            }

            // Fallback: search by text component
            foreach (var b in allButtons)
            {
                if (b == closeButton || b == cashRegisterButton) continue;
                var tmp = b.GetComponentInChildren<TextMeshProUGUI>(true);
                if (tmp != null)
                {
                    string txt = tmp.text.ToLowerInvariant();
                    if (txt.Contains("upgrade") || txt.Contains("item") || txt.Contains("stock"))
                    {
                        return b;
                    }
                }
            }

            return null;
        }

        public void ToggleTab()
        {
            if (Time.frameCount == lastToggleFrame) return;
            lastToggleFrame = Time.frameCount;

            bool isUnlocked = IsUpgradesUnlocked();
            if (!isUnlocked)
            {
                HUDController.Instance?.ShowNotification("Shop Upgrades unlock on Day 8 (Week 2)!", 2.5f);
                return;
            }

            currentTab = (currentTab == InventoryTab.Items) ? InventoryTab.Upgrades : InventoryTab.Items;
            RefreshTabState();
        }

        public void SwitchToItemsTab() => SetTab(InventoryTab.Items);
        public void SwitchToUpgradesTab() => SetTab(InventoryTab.Upgrades);

        public void SetTab(InventoryTab tab)
        {
            if (tab == InventoryTab.Upgrades && !IsUpgradesUnlocked())
            {
                HUDController.Instance?.ShowNotification("Shop Upgrades unlock on Day 8 (Week 2)!", 2.5f);
                return;
            }

            currentTab = tab;
            RefreshTabState();
        }

        public void RefreshTabState()
        {
            EnsureTabsUI();

            bool isUnlocked = IsUpgradesUnlocked();

            if (!isUnlocked)
            {
                currentTab = InventoryTab.Items;
            }

            // Update Tab Toggle Button state and label (without overwriting custom button sprite color)
            if (tabToggleButton != null)
            {
                tabToggleButton.interactable = isUnlocked;

                if (tabToggleText != null)
                {
                    if (!isUnlocked)
                    {
                        tabToggleText.text = "Upgrades (Day 8)";
                    }
                    else
                    {
                        tabToggleText.text = (currentTab == InventoryTab.Items) ? "Upgrades" : "Items";
                    }
                }
            }

            if (currentTab == InventoryTab.Items)
            {
                if (itemsTabPanel != null) itemsTabPanel.SetActive(true);
                if (upgradesTabPanel != null) upgradesTabPanel.SetActive(false);

                UpdateInventoryDisplay();
            }
            else
            {
                if (itemsTabPanel != null) itemsTabPanel.SetActive(false);
                if (upgradesTabPanel != null)
                {
                    upgradesTabPanel.transform.SetAsLastSibling();
                    upgradesTabPanel.SetActive(true);
                }

                PopulateUpgradesDisplay();
            }
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

            currentTab = InventoryTab.Items;
            RefreshTabState();

            if (inventoryModalPanel != null)
            {
                // Bring inventory modal in front of shopfront & HUD top bar (so translucent background covers HUD stats)
                inventoryModalPanel.transform.SetAsLastSibling();
                inventoryModalPanel.SetActive(true);

                // Keep status hints on top of the modal if desired
                if (HUDController.Instance != null && HUDController.Instance.StatusHintText != null)
                {
                    HUDController.Instance.StatusHintText.transform.SetAsLastSibling();
                }
            }
        }

        public void CloseInventoryModal()
        {
            if (inventoryModalPanel != null)
            {
                inventoryModalPanel.SetActive(false);
            }
        }

        private string FormatStockCount(int count)
        {
            string colorHex = count == 0 ? "#FF4444" : (count <= 6 ? "#F1C40F" : "#2ECC71");
            return $"<color={colorHex}>x {count:D2}</color>";
        }

        public void UpdateInventoryDisplay()
        {
            if (EconomyManager.Instance != null && cashBalanceText != null)
            {
                cashBalanceText.text = $"Shop Balance: <color=#2ECC71>${EconomyManager.Instance.CurrentCash:F2}</color>";
            }

            if (InventoryManager.Instance == null) return;

            // Milks Data (Only show items that the player has owned at least 1 of before)
            var allMilks = new (string key, string name, int count)[]
            {
                ("Milk_FreshMilk", "Fresh Whole Milk", InventoryManager.Instance.GetMilkStock(MilkType.FreshMilk)),
                ("Milk_OatMilk", "Barista Oat Milk", InventoryManager.Instance.GetMilkStock(MilkType.OatMilk)),
                ("Milk_CoconutMilk", "Organic Coconut Milk", InventoryManager.Instance.GetMilkStock(MilkType.CoconutMilk)),
                ("Milk_CondensedMilk", "Sweet Condensed Milk", InventoryManager.Instance.GetMilkStock(MilkType.CondensedMilk))
            };
            var milksList = new List<(string key, string name, int count)>();
            foreach (var m in allMilks)
            {
                if (InventoryManager.Instance.HasEverHadStock(m.key))
                {
                    milksList.Add(m);
                }
            }
            var milks = milksList.ToArray();

            // Toppings Data (Only show items that the player has owned at least 1 of before)
            var allToppings = new (string key, string name, int count)[]
            {
                ("Topping_TapiocaPearls", "Raw Tapioca Pearls", InventoryManager.Instance.GetToppingStock(ToppingType.TapiocaPearls)),
                ("Topping_PoppingBoba", "Mango Popping Boba", InventoryManager.Instance.GetToppingStock(ToppingType.PoppingBoba)),
                ("Topping_GrassJelly", "Herbal Grass Jelly", InventoryManager.Instance.GetToppingStock(ToppingType.GrassJelly)),
                ("Topping_EggPudding", "Silky Egg Pudding", InventoryManager.Instance.GetToppingStock(ToppingType.EggPudding)),
                ("Topping_CoconutJelly", "Sweet Coconut Jelly", InventoryManager.Instance.GetToppingStock(ToppingType.CoconutJelly)),
                ("Topping_CheeseFoam", "Salted Cheese Foam", InventoryManager.Instance.GetToppingStock(ToppingType.CheeseFoam)),
                ("Topping_GoldenHoneyPearls", "Golden Honey Pearls", InventoryManager.Instance.GetToppingStock(ToppingType.GoldenHoneyPearls))
            };
            var toppingsList = new List<(string key, string name, int count)>();
            foreach (var t in allToppings)
            {
                if (InventoryManager.Instance.HasEverHadStock(t.key))
                {
                    toppingsList.Add(t);
                }
            }
            var toppings = toppingsList.ToArray();

            // Update Fallback Texts (Clean ASCII with Color Coding)
            if (milkStockText != null)
            {
                string text = "<b>MILKS</b>\n";
                foreach (var m in milks)
                {
                    text += $"- {m.name}  {FormatStockCount(m.count)}\n";
                }
                milkStockText.text = text.TrimEnd();
            }

            if (toppingStockText != null)
            {
                string text = "<b>TOPPINGS</b>\n";
                foreach (var t in toppings)
                {
                    text += $"- {t.name}  {FormatStockCount(t.count)}\n";
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
                if (MarketEventManager.Instance != null && MarketEventManager.Instance.ActiveEvent != null && !string.IsNullOrEmpty(MarketEventManager.Instance.ActiveEvent.title))
                {
                    var ev = MarketEventManager.Instance.ActiveEvent;
                    marketNewsText.text = $"<b>Market News:</b> <color=#FFAA00>{ev.title}</color> ({ev.daysRemaining}d left)\n<i>{ev.description}</i>";
                }
                else
                {
                    marketNewsText.text = "<b>Market News:</b> <i>There is no significant news affecting the markets.</i>";
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

            float cardHeight = 62f;
            float spacingY = 8f;
            float headerHeight = 32f;

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
            headerTmp.fontSize = 22;
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
                cardImg.color = new Color(0.12f, 0.16f, 0.24f, 0.95f);

                // Left: Icon
                float leftOffset = 14f;
                if (icon != null)
                {
                    GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                    iconObj.transform.SetParent(cardObj.transform, false);
                    var iconRt = iconObj.GetComponent<RectTransform>();
                    iconRt.anchorMin = new Vector2(0, 0.5f);
                    iconRt.anchorMax = new Vector2(0, 0.5f);
                    iconRt.pivot = new Vector2(0, 0.5f);
                    iconRt.sizeDelta = new Vector2(48, 48);
                    iconRt.anchoredPosition = new Vector2(10, 0);

                    var img = iconObj.GetComponent<Image>();
                    img.sprite = icon;
                    img.preserveAspect = true;
                    leftOffset = 66f;
                }

                // Right: Count Badge Pill
                GameObject pillObj = new GameObject("CountPill", typeof(RectTransform), typeof(Image));
                pillObj.transform.SetParent(cardObj.transform, false);
                var pillRt = pillObj.GetComponent<RectTransform>();
                pillRt.anchorMin = new Vector2(1, 0.5f);
                pillRt.anchorMax = new Vector2(1, 0.5f);
                pillRt.pivot = new Vector2(1, 0.5f);
                pillRt.sizeDelta = new Vector2(88, 40);
                pillRt.anchoredPosition = new Vector2(-12, 0);

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
                countTmp.text = FormatStockCount(item.count);
                countTmp.fontSize = 20;
                countTmp.alignment = TextAlignmentOptions.Center;
                countTmp.textWrappingMode = TextWrappingModes.NoWrap;

                // Middle: Item Name
                GameObject nameObj = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
                nameObj.transform.SetParent(cardObj.transform, false);
                var nameRt = nameObj.GetComponent<RectTransform>();
                nameRt.anchorMin = new Vector2(0, 0);
                nameRt.anchorMax = new Vector2(1, 1);
                nameRt.offsetMin = new Vector2(leftOffset, 0);
                nameRt.offsetMax = new Vector2(-105, 0);

                var nameTmp = nameObj.GetComponent<TextMeshProUGUI>();
                nameTmp.text = $"<b>{item.name}</b>";
                nameTmp.fontSize = 21;
                nameTmp.alignment = TextAlignmentOptions.MidlineLeft;
                nameTmp.color = Color.white;
                nameTmp.textWrappingMode = TextWrappingModes.NoWrap;
            }
        }

        public void PopulateUpgradesDisplay()
        {
            if (upgradesScrollContainer == null || UpgradeManager.Instance == null) return;

            var upgrades = UpgradeManager.Instance.Upgrades;
            int purchasedCount = 0;
            for (int i = 0; i < upgrades.Count; i++)
            {
                if (upgrades[i].isPurchased) purchasedCount++;
            }

            // Clear previous cards
            for (int i = upgradesScrollContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(upgradesScrollContainer.GetChild(i).gameObject);
            }

            int cols = 2;
            int totalRows = Mathf.CeilToInt((float)upgrades.Count / cols);

            RectTransform containerRt = upgradesScrollContainer as RectTransform;
            float totalWidth = containerRt != null && containerRt.rect.width > 200 ? containerRt.rect.width : 760f;
            float paddingX = 10f;
            float paddingY = 12f;
            float spacingX = 16f;
            float spacingY = 12f;

            float cardWidth = (totalWidth - (paddingX * 2) - spacingX) / cols;
            float cardHeight = 112f;

            float totalHeight = (paddingY * 2) + (totalRows * cardHeight) + ((totalRows - 1) * spacingY) + 40f;
            if (containerRt != null)
            {
                containerRt.sizeDelta = new Vector2(totalWidth, totalHeight);
                containerRt.anchoredPosition = Vector2.zero;
            }

            // Header summary
            GameObject headerGo = new GameObject("Header_Summary", typeof(RectTransform), typeof(TextMeshProUGUI));
            headerGo.transform.SetParent(upgradesScrollContainer, false);
            var headerRt = headerGo.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0.5f, 1f);
            headerRt.anchorMax = new Vector2(0.5f, 1f);
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.sizeDelta = new Vector2(totalWidth - 20f, 32f);
            headerRt.anchoredPosition = new Vector2(0f, -paddingY);

            var headerTmp = headerGo.GetComponent<TextMeshProUGUI>();
            headerTmp.text = $"<b>SHOP UPGRADES</b>  |  <color=#2ECC71>{purchasedCount}/{upgrades.Count} Active</color>";
            headerTmp.fontSize = 20;
            headerTmp.alignment = TextAlignmentOptions.MidlineLeft;
            headerTmp.color = Color.white;

            float startX = -totalWidth * 0.5f + paddingX + (cardWidth * 0.5f);
            float startY = -paddingY - 36f - (cardHeight * 0.5f);

            for (int i = 0; i < upgrades.Count; i++)
            {
                var u = upgrades[i];
                bool isOwned = u.isPurchased;

                int row = i / cols;
                int col = i % cols;
                Vector2 pos = new Vector2(startX + col * (cardWidth + spacingX), startY - row * (cardHeight + spacingY));

                GameObject cardObj = new GameObject($"Card_{u.type}", typeof(RectTransform), typeof(Image));
                cardObj.transform.SetParent(upgradesScrollContainer, false);
                var rt = cardObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(cardWidth, cardHeight);
                rt.anchoredPosition = pos;

                var img = cardObj.GetComponent<Image>();
                img.color = isOwned ? new Color(0.10f, 0.22f, 0.16f, 0.95f) : new Color(0.12f, 0.14f, 0.18f, 0.70f);

                // Right Badge Pill
                float badgeWidth = 90f;
                GameObject badgeObj = new GameObject("Badge", typeof(RectTransform), typeof(Image));
                badgeObj.transform.SetParent(cardObj.transform, false);
                var badgeRt = badgeObj.GetComponent<RectTransform>();
                badgeRt.anchorMin = new Vector2(1, 0.5f);
                badgeRt.anchorMax = new Vector2(1, 0.5f);
                badgeRt.pivot = new Vector2(1, 0.5f);
                badgeRt.sizeDelta = new Vector2(badgeWidth, 36f);
                badgeRt.anchoredPosition = new Vector2(-10, 0);

                var badgeImg = badgeObj.GetComponent<Image>();
                badgeImg.color = isOwned ? new Color(0.18f, 0.55f, 0.34f, 0.95f) : new Color(0.25f, 0.25f, 0.28f, 0.60f);

                GameObject badgeTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                badgeTextObj.transform.SetParent(badgeObj.transform, false);
                var badgeTextRt = badgeTextObj.GetComponent<RectTransform>();
                badgeTextRt.anchorMin = Vector2.zero;
                badgeTextRt.anchorMax = Vector2.one;
                badgeTextRt.offsetMin = Vector2.zero;
                badgeTextRt.offsetMax = Vector2.zero;

                var badgeTmp = badgeTextObj.GetComponent<TextMeshProUGUI>();
                badgeTmp.text = isOwned ? "<color=#FFFFFF><b>ACTIVE</b></color>" : "<color=#888888>LOCKED</color>";
                badgeTmp.fontSize = 15;
                badgeTmp.alignment = TextAlignmentOptions.Center;
                badgeTmp.textWrappingMode = TextWrappingModes.NoWrap;

                // Left Info Text
                GameObject infoTextObj = new GameObject("InfoText", typeof(RectTransform), typeof(TextMeshProUGUI));
                infoTextObj.transform.SetParent(cardObj.transform, false);
                var infoRt = infoTextObj.GetComponent<RectTransform>();
                infoRt.anchorMin = new Vector2(0, 0);
                infoRt.anchorMax = new Vector2(1, 1);
                infoRt.offsetMin = new Vector2(12f, 6f);
                infoRt.offsetMax = new Vector2(-(badgeWidth + 18f), -6f);

                var infoTmp = infoTextObj.GetComponent<TextMeshProUGUI>();
                infoTmp.fontSize = 15f;
                infoTmp.alignment = TextAlignmentOptions.MidlineLeft;
                infoTmp.textWrappingMode = TextWrappingModes.Normal;
                infoTmp.lineSpacing = -2f;

                string titleHeader = isOwned
                    ? $"<color=#2ECC71><b>{u.title}</b></color>"
                    : $"<color=#A0A0A0><b>{u.title}</b></color>";

                string desc = isOwned
                    ? $"<size=13><color=#BDC3C7>{u.description}</color></size>"
                    : $"<size=13><color=#777777>Available in Night Phase (${u.cost:F2})</color></size>";

                string effect = isOwned
                    ? $"<size=13><color=#FFAA00><b>Effect:</b> {u.effect}</color></size>"
                    : $"<size=13><color=#888888><b>Effect:</b> {u.effect}</color></size>";

                infoTmp.text = $"{titleHeader}\n{desc}\n{effect}";
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
