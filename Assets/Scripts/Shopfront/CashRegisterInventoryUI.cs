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

        [Header("Display Text")]
        [SerializeField] private TextMeshProUGUI cashBalanceText;
        [SerializeField] private TextMeshProUGUI dailySuppliesText;
        [SerializeField] private TextMeshProUGUI teaStockText;
        [SerializeField] private TextMeshProUGUI milkStockText;
        [SerializeField] private TextMeshProUGUI toppingStockText;
        [SerializeField] private TextMeshProUGUI marketNewsText;

        [Header("Audio")]
        [SerializeField] private AudioClip registerChimeSound;

        private Coroutine pulseRoutine;

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

            if (milkStockText != null && InventoryManager.Instance != null)
            {
                int fresh = InventoryManager.Instance.GetMilkStock(MilkType.FreshMilk);
                int oat = InventoryManager.Instance.GetMilkStock(MilkType.OatMilk);
                int coconut = InventoryManager.Instance.GetMilkStock(MilkType.CoconutMilk);
                int condensed = InventoryManager.Instance.GetMilkStock(MilkType.CondensedMilk);

                milkStockText.text = $"<b>🥛 MILKS</b>\n" +
                                     $"• Fresh Whole Milk    <color=#F1C40F>x {fresh:D2}</color>\n" +
                                     $"• Barista Oat Milk    <color=#F1C40F>x {oat:D2}</color>\n" +
                                     $"• Organic Coconut Milk <color=#F1C40F>x {coconut:D2}</color>\n" +
                                     $"• Sweet Condensed Milk <color=#F1C40F>x {condensed:D2}</color>";
            }

            if (toppingStockText != null && InventoryManager.Instance != null)
            {
                int tapioca = InventoryManager.Instance.GetToppingStock(ToppingType.TapiocaPearls);
                int popping = InventoryManager.Instance.GetToppingStock(ToppingType.PoppingBoba);
                int grass = InventoryManager.Instance.GetToppingStock(ToppingType.GrassJelly);
                int egg = InventoryManager.Instance.GetToppingStock(ToppingType.EggPudding);
                int coconutJelly = InventoryManager.Instance.GetToppingStock(ToppingType.CoconutJelly);
                int cheese = InventoryManager.Instance.GetToppingStock(ToppingType.CheeseFoam);
                int golden = InventoryManager.Instance.GetToppingStock(ToppingType.GoldenHoneyPearls);

                toppingStockText.text = $"<b>🧋 TOPPINGS</b>\n" +
                                        $"• Raw Tapioca Pearls   <color=#F1C40F>x {tapioca:D2}</color>\n" +
                                        $"• Mango Popping Boba   <color=#F1C40F>x {popping:D2}</color>\n" +
                                        $"• Herbal Grass Jelly   <color=#F1C40F>x {grass:D2}</color>\n" +
                                        $"• Silky Egg Custard    <color=#F1C40F>x {egg:D2}</color>\n" +
                                        $"• Sweet Coconut Jelly  <color=#F1C40F>x {coconutJelly:D2}</color>\n" +
                                        $"• Salted Cheese Foam   <color=#F1C40F>x {cheese:D2}</color>\n" +
                                        $"• Golden Honey Pearls  <color=#F1C40F>x {golden:D2}</color>";
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
