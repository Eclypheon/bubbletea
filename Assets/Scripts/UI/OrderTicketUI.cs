using TMPro;
using UnityEngine;

namespace BubbleTeaShop
{
    public class OrderTicketUI : MonoBehaviour
    {
        [SerializeField] private GameObject ticketRoot;
        [SerializeField] private TextMeshProUGUI customerNameText;
        [SerializeField] private TextMeshProUGUI recipeDetailText;

        private void Start()
        {
            if (CustomerManager.Instance != null)
            {
                CustomerManager.Instance.OnCustomerArrived += ShowTicket;
            }
            if (ticketRoot != null) ticketRoot.SetActive(false);
        }

        public void ShowTicket(DrinkOrder order)
        {
            if (ticketRoot != null) ticketRoot.SetActive(true);
            if (customerNameText != null) customerNameText.text = $"#{order.customerName}";
            if (recipeDetailText != null) recipeDetailText.text = order.GetFormattedSummary();
        }

        public void HideTicket()
        {
            if (ticketRoot != null) ticketRoot.SetActive(false);
        }
    }
}
