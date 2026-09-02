using System;
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
        [SerializeField] private Button endlessModeButton;

        private void Awake()
        {
            EnsureUIReferences();
        }

        private void Start()
        {
            EnsureUIReferences();

            if (modalRoot != null) modalRoot.SetActive(false);
            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(RestartGame);
                restartButton.onClick.AddListener(RestartGame);
            }
            if (endlessModeButton != null)
            {
                endlessModeButton.onClick.RemoveListener(ContinueInEndlessMode);
                endlessModeButton.onClick.AddListener(ContinueInEndlessMode);
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged -= HandleStateChanged;
                GameManager.Instance.OnStateChanged += HandleStateChanged;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged -= HandleStateChanged;
            }
        }

        private void EnsureUIReferences()
        {
            if (modalRoot == null) modalRoot = gameObject;

            if (restartButton == null)
            {
                var btns = GetComponentsInChildren<Button>(true);
                foreach (var b in btns)
                {
                    if (b.name.ToLower().Contains("restart") || b.name.ToLower().Contains("play") || b.name.ToLower().Contains("again"))
                    {
                        restartButton = b;
                        break;
                    }
                }
                if (restartButton == null && btns.Length > 0)
                {
                    restartButton = btns[0];
                }
            }

            if (endlessModeButton == null && restartButton != null)
            {
                // Look for existing button named Endless
                var btns = GetComponentsInChildren<Button>(true);
                foreach (var b in btns)
                {
                    if (b.name.ToLower().Contains("endless") || b.name.ToLower().Contains("continue"))
                    {
                        endlessModeButton = b;
                        break;
                    }
                }

                // If not found, dynamically clone from restartButton
                if (endlessModeButton == null && restartButton.transform.parent != null)
                {
                    GameObject endlessObj = Instantiate(restartButton.gameObject, restartButton.transform.parent);
                    endlessObj.name = "EndlessModeBtn";
                    endlessModeButton = endlessObj.GetComponent<Button>();
                    endlessModeButton.onClick.RemoveAllListeners();
                    endlessModeButton.onClick.AddListener(ContinueInEndlessMode);
                    var txt = endlessObj.GetComponentInChildren<TextMeshProUGUI>();
                    if (txt != null)
                    {
                        txt.text = "Endless Mode";
                    }
                }
            }

            if (titleText == null || messageText == null)
            {
                var tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var t in tmps)
                {
                    if (t.name.ToLower().Contains("title") && titleText == null) titleText = t;
                    else if ((t.name.ToLower().Contains("msg") || t.name.ToLower().Contains("message")) && messageText == null) messageText = t;
                }
            }
        }

        private void HandleStateChanged(GameState state)
        {
            EnsureUIReferences();

            if (state == GameState.GameOver)
            {
                if (modalRoot != null) modalRoot.SetActive(true);
                if (titleText != null) titleText.text = "<color=#FF4444>EVICTION NOTICE</color>";
                if (messageText != null) messageText.text = "You were unable to afford weekly rent. The landlord has locked the shutters and taken over the shop.";

                if (restartButton != null)
                {
                    restartButton.gameObject.SetActive(true);
                    var rt = restartButton.GetComponent<RectTransform>();
                    if (rt != null) rt.anchoredPosition = new Vector2(0f, -120f);
                    var txt = restartButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (txt != null) txt.text = "Try Again";
                }

                if (endlessModeButton != null)
                {
                    endlessModeButton.gameObject.SetActive(false);
                }
            }
            else if (state == GameState.GameWon)
            {
                if (modalRoot != null) modalRoot.SetActive(true);
                if (titleText != null) titleText.text = "<color=#FFD700>LOCATION BOUGHT OVER!</color>";
                if (messageText != null) messageText.text = "Congratulations! You earned enough through brewing and selling bubble tea to buy the deed to the building! The shop is forever yours!\n\n<size=80%><color=#A8E6CF>You can now continue in <b>Endless Mode</b> with weekly exponential rent escalation!</color></size>";

                if (restartButton != null)
                {
                    restartButton.gameObject.SetActive(true);
                    var rt = restartButton.GetComponent<RectTransform>();
                    if (rt != null) rt.anchoredPosition = new Vector2(-250f, -250f);
                    var txt = restartButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (txt != null) txt.text = "Play Again";
                }

                if (endlessModeButton != null)
                {
                    endlessModeButton.gameObject.SetActive(true);
                    var rt = endlessModeButton.GetComponent<RectTransform>();
                    if (rt != null) rt.anchoredPosition = new Vector2(200f, -250f);
                    var txt = endlessModeButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (txt != null) txt.text = "Endless Mode";
                }
            }
        }

        public void ContinueInEndlessMode()
        {
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.IsEndlessMode = true;
            }

            if (modalRoot != null)
            {
                modalRoot.SetActive(false);
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameState.NightPhase);
            }

            HUDController.Instance?.RefreshHUDDisplay();
            HUDController.Instance?.ShowNotification("Endless Mode Activated! Weekly rent will now escalate exponentially!", 5f);
        }

        public void RestartGame()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
