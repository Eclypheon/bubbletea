using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BubbleTeaShop
{
    public class CupStation : MonoBehaviour
    {
        public static CupStation Instance { get; private set; }

        [Header("Cup Visual Layers")]
        [SerializeField] private GameObject cupContainer;
        [SerializeField] private Image teaLiquidImage;
        [SerializeField] private Image milkLayerImage;
        [SerializeField] private RectTransform liquidLevelTransform;
        [SerializeField] private GameObject iceVisualParent;
        [SerializeField] private GameObject toppingsVisualParent;
        [SerializeField] private Image primaryToppingImage;
        [SerializeField] private Image secondaryToppingImage;
        [SerializeField] private GameObject sealedLidObject;
        [SerializeField] private GameObject strawObject;

        [Header("Optional Custom Topping Sprites")]
        [SerializeField] private Sprite tapiocaSprite;
        [SerializeField] private Sprite poppingBobaSprite;
        [SerializeField] private Sprite grassJellySprite;
        [SerializeField] private Sprite coconutJellySprite;
        [SerializeField] private Sprite eggPuddingSprite;
        [SerializeField] private Sprite goldenHoneyPearlsSprite;
        [SerializeField] private Sprite cheeseFoamSprite;

        public Sprite TapiocaSprite => tapiocaSprite;
        public Sprite PoppingBobaSprite => poppingBobaSprite;
        public Sprite GrassJellySprite => grassJellySprite;
        public Sprite CoconutJellySprite => coconutJellySprite;
        public Sprite EggPuddingSprite => eggPuddingSprite;
        public Sprite GoldenHoneyPearlsSprite => goldenHoneyPearlsSprite;
        public Sprite CheeseFoamSprite => cheeseFoamSprite;

        private const float BottomToppingStackedSpacing = 26f;
        private const float CheeseFoamYPos = 240f;
        private const float CheeseFoamMinY = 0.27f;
        private const float CheeseFoamMaxY = 1.0f;
        private const float CheeseFoamScaleX = 1.46f;

        [Header("Action Buttons")]
        [SerializeField] private Button newCupButton;
        [SerializeField] private Button trashCupButton;
        [SerializeField] private Button serveCupButton;
        [SerializeField] private TextMeshProUGUI cupStatusText;

        [Header("Audio (Optional)")]
        [SerializeField] private AudioClip trashSound;
        [SerializeField] private AudioClip serveSound;

        [Header("Runtime Cup")]
        [SerializeField] private BubbleTeaCup currentCup = new BubbleTeaCup();
        public BubbleTeaCup CurrentCup => currentCup;

        public event Action OnCupUpdated;

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
            if (newCupButton != null) newCupButton.onClick.AddListener(SpawnNewCup);
            if (trashCupButton != null) trashCupButton.onClick.AddListener(TrashCup);
            if (serveCupButton != null) serveCupButton.onClick.AddListener(ServeCup);

            SpawnNewCup();
        }

        public void SpawnNewCup()
        {
            currentCup.Reset();
            UpdateVisuals();
        }

        public void TrashCup()
        {
            if (trashSound != null)
            {
                AudioManager.Instance?.PlaySFX(trashSound);
            }

            currentCup.hasCup = false;
            currentCup.Reset();
            UpdateVisuals();
            SpawnNewCup();
        }

        public void AddTea(TeaBase tea)
        {
            if (!currentCup.hasCup) return;
            if (currentCup.isSealed) return;
            if (currentCup.tea != TeaBase.None) return; // Prevent overwriting existing tea

            currentCup.tea = tea;
            UpdateVisuals();
        }

        public void AddMilk(MilkType milk)
        {
            if (!currentCup.hasCup) return;
            if (currentCup.isSealed) return;
            if (currentCup.milk != MilkType.None) return; // Prevent overwriting existing milk

            currentCup.milk = milk;
            UpdateVisuals();
        }

        public void SetSugar(int percent)
        {
            if (!currentCup.hasCup) return;
            if (currentCup.isSealed) return;

            currentCup.sweetnessPercent = Mathf.Clamp(percent, 0, 100);
            UpdateVisuals();
        }

        public void SetIce(int percent)
        {
            if (!currentCup.hasCup) return;
            if (currentCup.isSealed) return;

            currentCup.icePercent = Mathf.Clamp(percent, 0, 100);
            UpdateVisuals();
        }

        public void AddTopping(ToppingType topping)
        {
            if (!currentCup.hasCup) return;
            if (currentCup.isSealed) return;

            if (!currentCup.toppings.Contains(topping))
            {
                currentCup.toppings.Add(topping);
            }
            UpdateVisuals();
        }

        public void SealCup()
        {
            if (!currentCup.hasCup) return;
            currentCup.isSealed = true;
            UpdateVisuals();
        }

        public void ServeCup()
        {
            if (!currentCup.hasCup) return;

            if (currentCup.tea == TeaBase.None && currentCup.toppings.Count == 0)
            {
                HUDController.Instance?.ShowNotification("Cannot serve an empty cup! Add tea and toppings first.");
                return;
            }

            if (!currentCup.isSealed)
            {
                HUDController.Instance?.ShowNotification("You must click 'Seal Lid' before serving!");
                CupSealer.Instance?.HighlightSealer();
                Debug.LogWarning("You must seal the drink before serving!");
                return;
            }

            if (CustomerManager.Instance != null && CustomerManager.Instance.CustomerController != null && CustomerManager.Instance.CustomerController.IsLandlordActive)
            {
                CustomerManager.Instance.CustomerController.ReceiveLandlordDrink(currentCup);
                OrderTicketUI.Instance?.HideTicket();
                if (serveSound != null) AudioManager.Instance?.PlaySFX(serveSound);
                currentCup.hasCup = false;
                UpdateVisuals();
                // Prepare next empty cup
                Invoke(nameof(SpawnNewCup), 0.5f);
            }
            else if (CustomerManager.Instance != null && CustomerManager.Instance.HasCustomerAtWindow)
            {
                CustomerManager.Instance.ServeCurrentCustomer(currentCup);
                OrderTicketUI.Instance?.HideTicket();
                if (serveSound != null) AudioManager.Instance?.PlaySFX(serveSound);
                currentCup.hasCup = false;
                UpdateVisuals();
                // Prepare next empty cup
                Invoke(nameof(SpawnNewCup), 0.5f);
            }
            else
            {
                HUDController.Instance?.ShowNotification("No customer waiting at the window!");
                Debug.Log("No customer currently waiting at the window to receive the drink!");
            }
        }

        public void UpdateVisuals()
        {
            if (cupContainer != null) cupContainer.SetActive(currentCup.hasCup);
            if (!currentCup.hasCup) return;

            // Tea color
            if (teaLiquidImage != null)
            {
                if (currentCup.tea == TeaBase.None)
                {
                    teaLiquidImage.gameObject.SetActive(false);
                }
                else
                {
                    teaLiquidImage.gameObject.SetActive(true);
                    teaLiquidImage.color = GetTeaColor(currentCup.tea);
                }
            }

            // Milk overlay
            if (milkLayerImage != null)
            {
                milkLayerImage.gameObject.SetActive(currentCup.milk != MilkType.None);
                if (currentCup.milk != MilkType.None)
                {
                    milkLayerImage.color = new Color(1f, 1f, 1f, 0.45f);
                }
            }

            // Ice visuals
            if (iceVisualParent != null)
            {
                iceVisualParent.SetActive(currentCup.icePercent > 0);
            }

            // Toppings visual (Supports multiple stacked & layered toppings + floating cheese foam)
            UpdateToppingsVisual();

            // Sealing & straw
            if (sealedLidObject != null) sealedLidObject.SetActive(currentCup.isSealed);
            if (strawObject != null) strawObject.SetActive(currentCup.isSealed);

            // Text summary
            if (cupStatusText != null)
            {
                string teaStr = currentCup.tea != TeaBase.None ? currentCup.tea.ToString() : "Empty";
                string milkStr = currentCup.milk != MilkType.None ? $" + {currentCup.milk}" : "";
                string topStr = currentCup.toppings.Count > 0 ? string.Join(", ", currentCup.toppings) : "No Toppings";
                string sealStr = currentCup.isSealed ? " [SEALED]" : " [OPEN]";
                cupStatusText.text = $"{teaStr}{milkStr} (Sugar: {currentCup.sweetnessPercent}% | Ice: {currentCup.icePercent}%)\nToppings: {topStr}{sealStr}";
            }

            OnCupUpdated?.Invoke();
        }

        private Sprite defaultBobaSprite;
        private Sprite defaultLiquidMaskSprite;

        private void EnsureFallbackSprites()
        {
#if UNITY_EDITOR
            if (defaultBobaSprite == null)
            {
                defaultBobaSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Topping_Boba.png");
            }
            if (defaultLiquidMaskSprite == null)
            {
                defaultLiquidMaskSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Cup_LiquidMask.png");
            }
#endif
            if (defaultBobaSprite == null || defaultLiquidMaskSprite == null)
            {
                var allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
                foreach (var s in allSprites)
                {
                    if (s == null) continue;
                    if (defaultBobaSprite == null && (s.name.Contains("Boba") || s.name.Contains("boba"))) defaultBobaSprite = s;
                    if (defaultLiquidMaskSprite == null && s.name.Contains("LiquidMask")) defaultLiquidMaskSprite = s;
                }
            }
        }

        private void UpdateToppingsVisual()
        {
            if (toppingsVisualParent == null) return;

            toppingsVisualParent.SetActive(currentCup.toppings.Count > 0);
            if (currentCup.toppings.Count == 0)
            {
                for (int i = 0; i < toppingsVisualParent.transform.childCount; i++)
                {
                    toppingsVisualParent.transform.GetChild(i).gameObject.SetActive(false);
                }
                var baseImg = toppingsVisualParent.GetComponent<Image>();
                if (baseImg != null) baseImg.enabled = false;
                return;
            }

            EnsureFallbackSprites();

            // Disable the base Image component on toppingsVisualParent if it has one so it doesn't conflict
            var rootImg = toppingsVisualParent.GetComponent<Image>();
            if (rootImg != null) rootImg.enabled = false;

            // Separate Cheese Foam (top foam layer) and bottom toppings (pearls/jellies)
            bool hasCheeseFoam = currentCup.toppings.Contains(ToppingType.CheeseFoam);
            List<ToppingType> bottomToppings = new List<ToppingType>();
            foreach (var top in currentCup.toppings)
            {
                if (top != ToppingType.CheeseFoam)
                {
                    bottomToppings.Add(top);
                }
            }

            // Hide all children first
            for (int i = 0; i < toppingsVisualParent.transform.childCount; i++)
            {
                toppingsVisualParent.transform.GetChild(i).gameObject.SetActive(false);
            }

            int layerIndex = 0;

            // 1. Render Bottom Toppings (Tapioca, Popping Boba, Grass Jelly, Coconut Jelly, Egg Pudding, Golden Honey Pearls)
            int bottomCount = bottomToppings.Count;
            for (int b = 0; b < bottomCount; b++)
            {
                ToppingType top = bottomToppings[b];
                GameObject layerObj = GetOrCreateToppingLayer(layerIndex, $"BottomTopping_{b}_{top}");
                layerObj.SetActive(true);

                RectTransform rt = layerObj.GetComponent<RectTransform>();
                Image img = layerObj.GetComponent<Image>();

                // Maintain full stretch with cup aspect ratio so toppings remain circular and unsquished
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                // Stack additional topping layers slightly higher in the cup
                float yOffset = b * BottomToppingStackedSpacing;
                rt.anchoredPosition = new Vector2(0f, yOffset);

                Sprite customSp = GetToppingSprite(top);
                if (customSp != null)
                {
                    img.sprite = customSp;
                    img.color = Color.white;
                }
                else
                {
                    img.sprite = defaultBobaSprite != null ? defaultBobaSprite : (primaryToppingImage != null ? primaryToppingImage.sprite : null);
                    img.color = GetToppingColor(top);
                }
                img.preserveAspect = false;
                img.raycastTarget = false;

                layerIndex++;
            }

            // 2. Render Cheese Foam on Top Rim (if present)
            if (hasCheeseFoam)
            {
                GameObject foamObj = GetOrCreateToppingLayer(layerIndex, "TopFoam_CheeseFoam");
                foamObj.SetActive(true);

                RectTransform rt = foamObj.GetComponent<RectTransform>();
                Image img = foamObj.GetComponent<Image>();

                // Foam layer sits at the top rim of the cup
                rt.anchorMin = new Vector2(0f, CheeseFoamMinY);
                rt.anchorMax = new Vector2(1f, CheeseFoamMaxY);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.anchoredPosition = new Vector2(0f, CheeseFoamYPos);
                rt.localScale = new Vector3(CheeseFoamScaleX, 1f, 1f);

                Sprite foamSp = cheeseFoamSprite != null ? cheeseFoamSprite : defaultLiquidMaskSprite;
                img.sprite = foamSp;
                img.color = GetToppingColor(ToppingType.CheeseFoam);
                img.preserveAspect = false;
                img.raycastTarget = false;

                layerIndex++;
            }
        }

        private GameObject GetOrCreateToppingLayer(int index, string layerName)
        {
            if (toppingsVisualParent == null) return null;

            if (index < toppingsVisualParent.transform.childCount)
            {
                var child = toppingsVisualParent.transform.GetChild(index).gameObject;
                child.name = layerName;
                return child;
            }

            GameObject newLayer = new GameObject(layerName, typeof(RectTransform), typeof(Image));
            newLayer.transform.SetParent(toppingsVisualParent.transform, false);
            return newLayer;
        }

        public static Color GetToppingColor(ToppingType topping)
        {
            return topping switch
            {
                ToppingType.TapiocaPearls => new Color(0.12f, 0.08f, 0.06f, 0.95f),    // Classic Dark Boba (Black/Dark Brown)
                ToppingType.PoppingBoba => new Color(1f, 0.35f, 0.45f, 0.95f),         // Bright Coral/Strawberry Red
                ToppingType.GrassJelly => new Color(0.1f, 0.2f, 0.12f, 0.95f),          // Herbal Glossy Dark Emerald Black
                ToppingType.CoconutJelly => new Color(0.92f, 0.95f, 0.98f, 0.85f),     // Frosty Translucent White
                ToppingType.EggPudding => new Color(1f, 0.82f, 0.3f, 0.95f),           // Golden Custard Yellow
                ToppingType.GoldenHoneyPearls => new Color(0.95f, 0.68f, 0.15f, 0.95f),// Honey Amber
                ToppingType.CheeseFoam => new Color(1f, 0.97f, 0.88f, 0.95f),          // Creamy Froth
                _ => new Color(0.15f, 0.12f, 0.1f, 0.95f)
            };
        }

        private Sprite GetToppingSprite(ToppingType topping)
        {
            return topping switch
            {
                ToppingType.TapiocaPearls => tapiocaSprite,
                ToppingType.PoppingBoba => poppingBobaSprite,
                ToppingType.GrassJelly => grassJellySprite,
                ToppingType.CoconutJelly => coconutJellySprite,
                ToppingType.EggPudding => eggPuddingSprite,
                ToppingType.GoldenHoneyPearls => goldenHoneyPearlsSprite,
                ToppingType.CheeseFoam => cheeseFoamSprite,
                _ => null
            };
        }

        private Color GetTeaColor(TeaBase tea)
        {
            return tea switch
            {
                TeaBase.BlackTea => new Color(0.6f, 0.25f, 0.12f, 0.9f),
                TeaBase.GreenTea => new Color(0.45f, 0.7f, 0.3f, 0.9f),
                TeaBase.OolongTea => new Color(0.75f, 0.45f, 0.18f, 0.9f),
                TeaBase.ThaiTea => new Color(0.95f, 0.45f, 0.1f, 0.95f),
                TeaBase.TaroTea => new Color(0.7f, 0.5f, 0.85f, 0.95f),
                _ => Color.white
            };
        }
    }
}
