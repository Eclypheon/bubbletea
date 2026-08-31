using TMPro;
using UnityEngine;

namespace BubbleTeaShop
{
    public class HUDController : MonoBehaviour
    {
        public static HUDController Instance { get; private set; }

        [Header("Top Bar Elements")]
        [SerializeField] private TextMeshProUGUI dayText;
        [SerializeField] private TextMeshProUGUI cashText;
        [SerializeField] private TextMeshProUGUI rentTimerText;
        [SerializeField] private TextMeshProUGUI customerCountText;
        [SerializeField] private TextMeshProUGUI statusHintText;

        private Coroutine notificationRoutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            BringToFront();
        }

        private void Start()
        {
            BringToFront();
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnCashChanged += UpdateCashDisplay;
                UpdateCashDisplay(EconomyManager.Instance.CurrentCash);
            }

            if (DayManager.Instance != null)
            {
                DayManager.Instance.OnDayStarted += UpdateDayDisplay;
                DayManager.Instance.OnCustomerProgressUpdated += UpdateCustomerCountDisplay;
                UpdateDayDisplay(DayManager.Instance.CurrentDay);
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged += UpdateStateHint;
                UpdateStateHint(GameManager.Instance.CurrentState);
            }
            else
            {
                UpdateStateHint(GameState.MorningPrep);
            }
        }

        private void UpdateCashDisplay(float cash)
        {
            if (cashText != null) cashText.text = $"${cash:F2}";
        }

        private void UpdateDayDisplay(int day)
        {
            if (dayText != null) dayText.text = $"Day {day}";
            
            if (rentTimerText != null && EconomyManager.Instance != null)
            {
                int daysLeft = EconomyManager.Instance.GetDaysUntilRent(day);
                float rentAmount = EconomyManager.Instance.GetTotalRentDue(day);
                rentTimerText.text = daysLeft == 0 ? $"Rent Due: TONIGHT (${rentAmount:F0})" : $"Rent in: {daysLeft}d (${rentAmount:F0})";
            }
        }

        private void UpdateCustomerCountDisplay(int current, int total)
        {
            if (customerCountText != null)
            {
                customerCountText.text = $"Customer: {current}/{total}";
            }
        }

        public void SetStatusHint(string text)
        {
            if (statusHintText == null) return;
            if (notificationRoutine != null)
            {
                StopCoroutine(notificationRoutine);
                notificationRoutine = null;
            }
            statusHintText.text = text;
        }

        public void BringToFront()
        {
            if (transform.parent != null)
            {
                transform.SetAsLastSibling();
            }
        }

        public void ShowNotification(string message, float duration = 2.5f)
        {
            BringToFront();
            if (statusHintText == null) return;
            if (notificationRoutine != null) StopCoroutine(notificationRoutine);
            notificationRoutine = StartCoroutine(NotificationRoutine(message, duration));
        }

        private System.Collections.IEnumerator NotificationRoutine(string message, float duration)
        {
            if (message.Contains("<color"))
            {
                statusHintText.text = $"<b>{message}</b>";
            }
            else
            {
                statusHintText.text = $"<color=#FFAA00><b>{message}</b></color>";
            }

            yield return new WaitForSeconds(duration);
            if (CustomerManager.Instance != null && CustomerManager.Instance.CustomerController != null && CustomerManager.Instance.CustomerController.IsLandlordActive)
            {
                statusHintText.text = "The Landlord has arrived to collect weekly rent!";
            }
            else if (GameManager.Instance != null)
            {
                UpdateStateHint(GameManager.Instance.CurrentState);
            }
        }

        public void SetStorefrontHUDVisible(bool visible)
        {
            if (dayText != null) dayText.gameObject.SetActive(visible);
            if (cashText != null) cashText.gameObject.SetActive(visible);
            if (rentTimerText != null) rentTimerText.gameObject.SetActive(visible);
            if (customerCountText != null) customerCountText.gameObject.SetActive(visible);
        }

        public void UpdateStateHint(GameState state)
        {
            bool isStorefront = (state == GameState.MorningPrep || 
                                 state == GameState.ShopOpen || 
                                 state == GameState.CustomerWaiting || 
                                 state == GameState.ShopClosing);
            SetStorefrontHUDVisible(isStorefront);

            if (statusHintText == null) return;

            if (CustomerManager.Instance != null && CustomerManager.Instance.CustomerController != null && CustomerManager.Instance.CustomerController.IsLandlordActive)
            {
                statusHintText.text = "The Landlord has arrived to collect weekly rent!";
                return;
            }

            statusHintText.text = state switch
            {
                GameState.MorningPrep => "Open the shutters to begin the day!",
                GameState.ShopOpen => "Ring the bell to call the next customer.",
                GameState.CustomerWaiting => "Prepare the customer's order!",
                GameState.ShopClosing => "All customers served! Pull down the shutter to close.",
                GameState.NightPhase => "Night Phase: Buy stock, forage, and upgrade.",
                GameState.GameOver => "Game Over! You lost the shop.",
                GameState.GameWon => "Victory! You own the shop permanently!",
                _ => ""
            };
        }
    }
}
