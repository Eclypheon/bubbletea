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
        [SerializeField] private GameObject sealedLidObject;
        [SerializeField] private GameObject strawObject;

        [Header("Action Buttons")]
        [SerializeField] private Button newCupButton;
        [SerializeField] private Button trashCupButton;
        [SerializeField] private Button serveCupButton;
        [SerializeField] private TextMeshProUGUI cupStatusText;

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
            currentCup.hasCup = false;
            currentCup.Reset();
            UpdateVisuals();
            SpawnNewCup();
        }

        public void AddTea(TeaBase tea)
        {
            if (!currentCup.hasCup) return;
            if (currentCup.isSealed) return;

            currentCup.tea = tea;
            UpdateVisuals();
        }

        public void AddMilk(MilkType milk)
        {
            if (!currentCup.hasCup) return;
            if (currentCup.isSealed) return;

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

            if (CustomerManager.Instance != null && CustomerManager.Instance.HasCustomerAtWindow)
            {
                CustomerManager.Instance.ServeCurrentCustomer(currentCup);
                currentCup.hasCup = false;
                UpdateVisuals();
                // Prepare next empty cup
                Invoke(nameof(SpawnNewCup), 0.5f);
            }
            else
            {
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

        private Color GetTeaColor(TeaBase tea)
        {
            return tea switch
            {
                TeaBase.BlackTea => new Color(0.6f, 0.25f, 0.12f, 0.9f),
                TeaBase.GreenTea => new Color(0.45f, 0.7f, 0.3f, 0.9f),
                TeaBase.OolongTea => new Color(0.75f, 0.45f, 0.18f, 0.9f),
                TeaBase.ThaiTea => new Color(0.95f, 0.45f, 0.1f, 0.95f),
                TeaBase.TaroTea => new Color(0.7f, 0.5f, 0.85f, 0.95f),
                TeaBase.MatchaTea => new Color(0.35f, 0.65f, 0.25f, 0.95f),
                TeaBase.WildMountainTea => new Color(0.85f, 0.65f, 0.25f, 0.95f),
                _ => Color.white
            };
        }
    }
}
