using TMPro;
using UnityEngine;

namespace BubbleTeaShop
{
    public class HUDController : MonoBehaviour
    {
        [Header("Top Bar Elements")]
        [SerializeField] private TextMeshProUGUI dayText;
        [SerializeField] private TextMeshProUGUI cashText;
        [SerializeField] private TextMeshProUGUI rentTimerText;
        [SerializeField] private TextMeshProUGUI customerCountText;
        [SerializeField] private TextMeshProUGUI statusHintText;

        private void Start()
        {
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnCashChanged += UpdateCashDisplay;
                UpdateCashDisplay(EconomyManager.Instance.CurrentCash);
            }

            if (DayManager.Instance != null)
            {
                DayManager.Instance.OnDayStarted += UpdateDayDisplay;
                DayManager.Instance.OnCustomerProgressUpdated += UpdateCustomerCountDisplay;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged += UpdateStateHint;
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
                rentTimerText.text = daysLeft == 0 ? "Rent Due: TONIGHT" : $"Rent in: {daysLeft}d";
            }
        }

        private void UpdateCustomerCountDisplay(int served, int total)
        {
            if (customerCountText != null)
            {
                customerCountText.text = $"Customers: {served}/{total}";
            }
        }

        private void UpdateStateHint(GameState state)
        {
            if (statusHintText == null) return;

            statusHintText.text = state switch
            {
                GameState.MorningPrep => "Open the shutters to begin the day!",
                GameState.ShopOpen => "Ring the bell 🔔 to call the next customer.",
                GameState.CustomerWaiting => "Customer waiting! Prepare their requested drink.",
                GameState.ShopClosing => "All customers served! Pull down the shutter to close.",
                GameState.NightPhase => "Night Phase: Buy stock, forage, and upgrade.",
                GameState.GameOver => "Game Over! You lost the shop.",
                GameState.GameWon => "Victory! You own the shop permanently!",
                _ => ""
            };
        }
    }
}
