using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BubbleTeaShop
{
    public class EndGameModal : MonoBehaviour
    {
        [SerializeField] private GameObject modalRoot;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Button restartButton;

        private void Start()
        {
            if (modalRoot != null) modalRoot.SetActive(false);
            if (restartButton != null)
            {
                restartButton.onClick.AddListener(RestartGame);
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged += HandleStateChanged;
            }
        }

        private void HandleStateChanged(GameState state)
        {
            if (state == GameState.GameOver)
            {
                modalRoot.SetActive(true);
                titleText.text = "<color=#FF4444>EVICTION NOTICE</color>";
                messageText.text = "You were unable to afford weekly rent. The landlord has locked the shutters and taken over the shop.";
            }
            else if (state == GameState.GameWon)
            {
                modalRoot.SetActive(true);
                titleText.text = "<color=#FFD700>LOCATION BOUGHT OVER!</color>";
                messageText.text = "Congratulations! You earned enough through brewing and selling bubble tea to buy the deed to the building! The shop is forever yours!";
            }
        }

        public void RestartGame()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
