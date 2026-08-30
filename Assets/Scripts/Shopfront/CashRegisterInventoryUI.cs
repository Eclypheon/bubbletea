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

            if (dailySuppliesText != null)
            {
                int sugar = InventoryManager.Instance != null ? InventoryManager.Instance.GetStock("Sugar") : 0;
                int ice = InventoryManager.Instance != null ? InventoryManager.Instance.GetStock("Ice") : 0;
                dailySuppliesText.text = $"• <b>Cups & Straws:</b> <color=#2ECC71>Unlimited Supply</color>\n• <b>Sugar:</b> {sugar}% daily tank (Auto-restocked for $10/day)\n• <b>Ice:</b> {ice}% daily freezer (Auto-restocked for $10/day)";
            }

            if (teaStockText != null && InventoryManager.Instance != null)
            {
                teaStockText.text = $"• <b>Tea Bases:</b> <color=#2ECC71>Unlimited Supply</color>\n" +
                                    $"  <i>(Black, Green, Oolong, Thai, Taro, Wild Mountain)</i>";
            }

            if (milkStockText != null && InventoryManager.Instance != null)
            {
                milkStockText.text = $"• <b>Fresh Milk:</b> {InventoryManager.Instance.GetMilkStock(MilkType.FreshMilk)} servings\n" +
                                     $"• <b>Oat Milk:</b> {InventoryManager.Instance.GetMilkStock(MilkType.OatMilk)} servings\n" +
                                     $"• <b>Coconut Milk:</b> {InventoryManager.Instance.GetMilkStock(MilkType.CoconutMilk)} servings\n" +
                                     $"• <b>Condensed Milk:</b> {InventoryManager.Instance.GetMilkStock(MilkType.CondensedMilk)} servings";
            }

            if (toppingStockText != null && InventoryManager.Instance != null)
            {
                toppingStockText.text = $"• <b>Tapioca Pearls:</b> {InventoryManager.Instance.GetToppingStock(ToppingType.TapiocaPearls)} servings\n" +
                                        $"• <b>Popping Boba:</b> {InventoryManager.Instance.GetToppingStock(ToppingType.PoppingBoba)} servings\n" +
                                        $"• <b>Grass Jelly:</b> {InventoryManager.Instance.GetToppingStock(ToppingType.GrassJelly)} servings\n" +
                                        $"• <b>Egg Custard:</b> {InventoryManager.Instance.GetToppingStock(ToppingType.EggPudding)} servings\n" +
                                        $"• <b>Coconut Jelly:</b> {InventoryManager.Instance.GetToppingStock(ToppingType.CoconutJelly)} servings\n" +
                                        $"• <b>Cheese Foam:</b> {InventoryManager.Instance.GetToppingStock(ToppingType.CheeseFoam)} servings\n" +
                                        $"• <b>Golden Honey Pearls:</b> {InventoryManager.Instance.GetToppingStock(ToppingType.GoldenHoneyPearls)} servings";
            }

            if (marketNewsText != null)
            {
                if (MarketEventManager.Instance != null && MarketEventManager.Instance.ActiveEvent != null)
                {
                    marketNewsText.text = $"<b>Market News:</b> <color=#FFAA00>{MarketEventManager.Instance.ActiveEvent.title}</color>\n<i>{MarketEventManager.Instance.ActiveEvent.description}</i>";
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
