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

        [Header("Action Buttons")]
        [Tooltip("Optional - New cups are automatically spawned, but this can be assigned if desired")]
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
            if (InventoryManager.Instance != null)
            {
                if (!InventoryManager.Instance.ConsumeStock("Cup", 1))
                {
                    Debug.LogWarning("Out of cups! Buy more at the night market!");
                    return;
                }
            }

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

            if (CustomerManager.Instance != null && CustomerManager.Instance.HasCustomerAtWindow)
            {
                CustomerManager.Instance.ServeCurrentCustomer(currentCup);
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

            // Toppings visual
            if (toppingsVisualParent != null)
            {
                toppingsVisualParent.SetActive(currentCup.toppings.Count > 0);
            }

            // Auto-locate primary topping image if not explicitly assigned
            if (primaryToppingImage == null && toppingsVisualParent != null)
            {
                primaryToppingImage = toppingsVisualParent.GetComponent<Image>();
                if (primaryToppingImage == null)
                {
                    primaryToppingImage = toppingsVisualParent.GetComponentInChildren<Image>();
                }
            }

            if (primaryToppingImage != null)
            {
                if (currentCup.toppings.Count > 0)
                {
                    primaryToppingImage.gameObject.SetActive(true);
                    ToppingType firstTop = currentCup.toppings[0];
                    Sprite customSp = GetToppingSprite(firstTop);
                    if (customSp != null)
                    {
                        primaryToppingImage.sprite = customSp;
                        primaryToppingImage.color = Color.white;
                    }
                    else
                    {
                        primaryToppingImage.color = GetToppingColor(firstTop);
                    }
                }
                else
                {
                    primaryToppingImage.gameObject.SetActive(false);
                }
            }

            if (secondaryToppingImage != null)
            {
                if (currentCup.toppings.Count > 1)
                {
                    secondaryToppingImage.gameObject.SetActive(true);
                    ToppingType secondTop = currentCup.toppings[1];
                    Sprite customSp = GetToppingSprite(secondTop);
                    if (customSp != null)
                    {
                        secondaryToppingImage.sprite = customSp;
                        secondaryToppingImage.color = Color.white;
                    }
                    else
                    {
                        secondaryToppingImage.color = GetToppingColor(secondTop);
                    }
                }
                else
                {
                    secondaryToppingImage.gameObject.SetActive(false);
                }
            }

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
                TeaBase.WildMountainTea => new Color(0.85f, 0.65f, 0.25f, 0.95f),
                _ => Color.white
            };
        }
    }
}
