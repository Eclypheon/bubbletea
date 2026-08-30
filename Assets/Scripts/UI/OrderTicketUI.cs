using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BubbleTeaShop
{
    public class OrderTicketUI : MonoBehaviour
    {
        public static OrderTicketUI Instance { get; private set; }

        [SerializeField] private GameObject ticketRoot;
        [SerializeField] private TextMeshProUGUI customerNameText;
        [SerializeField] private TextMeshProUGUI recipeDetailText;

        private Image backgroundImage;
        private CanvasGroup canvasGroup;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            backgroundImage = GetComponent<Image>();
            canvasGroup = GetComponent<CanvasGroup>();
            if (ticketRoot == null) ticketRoot = gameObject;

            if (CustomerManager.Instance != null)
            {
                CustomerManager.Instance.OnCustomerArrived += ShowTicket;
            }
        }

        private void Start()
        {
            if (CustomerManager.Instance != null)
            {
                CustomerManager.Instance.OnCustomerArrived -= ShowTicket;
                CustomerManager.Instance.OnCustomerArrived += ShowTicket;
            }
            HideTicket();
        }

        public void ShowTicket(DrinkOrder order)
        {
            if (order == null) return;

            gameObject.SetActive(true);

            if (ticketRoot != null && ticketRoot != gameObject)
            {
                ticketRoot.SetActive(true);
            }
            else
            {
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 1f;
                    canvasGroup.interactable = true;
                    canvasGroup.blocksRaycasts = true;
                }
                if (backgroundImage != null) backgroundImage.enabled = true;
                for (int i = 0; i < transform.childCount; i++)
                {
                    transform.GetChild(i).gameObject.SetActive(true);
                }
            }

            if (customerNameText != null)
            {
                customerNameText.gameObject.SetActive(true);
                customerNameText.text = $"#{order.customerName}";
            }

            if (recipeDetailText != null)
            {
                recipeDetailText.gameObject.SetActive(true);
                recipeDetailText.text = order.GetFormattedSummary();
            }
        }

        public void HideTicket()
        {
            if (ticketRoot != null && ticketRoot != gameObject)
            {
                ticketRoot.SetActive(false);
            }
            else
            {
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 0f;
                    canvasGroup.interactable = false;
                    canvasGroup.blocksRaycasts = false;
                }
                if (backgroundImage != null) backgroundImage.enabled = false;
                for (int i = 0; i < transform.childCount; i++)
                {
                    transform.GetChild(i).gameObject.SetActive(false);
                }
            }
        }
    }
}
