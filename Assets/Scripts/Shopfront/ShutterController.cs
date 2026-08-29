using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BubbleTeaShop
{
    public class ShutterController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform shutterRect;
        [SerializeField] private Button shutterToggleButton;
        [SerializeField] private TMPro.TextMeshProUGUI shutterButtonText;

        [Header("Positions")]
        [SerializeField] private float closedPosY = 0f;
        [SerializeField] private float openPosY = 600f;
        [SerializeField] private float moveDuration = 0.8f;

        private bool isOpen = false;
        private bool isMoving = false;

        public bool IsOpen => isOpen;
        public bool IsMoving => isMoving;

        public event Action OnShutterOpened;
        public event Action OnShutterClosed;

        private void Start()
        {
            if (shutterToggleButton != null)
            {
                shutterToggleButton.onClick.AddListener(ToggleShutter);
            }
            SetShutterPosition(closedPosY);
            UpdateUI();
        }

        public void ToggleShutter()
        {
            if (isMoving) return;

            // Only allow closing if day is ready to close or before opening
            if (isOpen)
            {
                if (DayManager.Instance.IsDayFinished || GameManager.Instance.CurrentState == GameState.ShopClosing)
                {
                    StartCoroutine(MoveShutterRoutine(openPosY, closedPosY, false));
                }
                else
                {
                    Debug.Log("Cannot close shutters while customers are still waiting today!");
                }
            }
            else
            {
                if (GameManager.Instance.CurrentState == GameState.MorningPrep)
                {
                    StartCoroutine(MoveShutterRoutine(closedPosY, openPosY, true));
                }
            }
        }

        private IEnumerator MoveShutterRoutine(float startY, float targetY, bool opening)
        {
            isMoving = true;
            float elapsed = 0f;

            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / moveDuration);
                SetShutterPosition(Mathf.Lerp(startY, targetY, t));
                yield return null;
            }

            SetShutterPosition(targetY);
            isOpen = opening;
            isMoving = false;
            UpdateUI();

            if (isOpen)
            {
                OnShutterOpened?.Invoke();
                GameManager.Instance?.OnShutterOpened();
            }
            else
            {
                OnShutterClosed?.Invoke();
                GameManager.Instance?.OnShutterClosed();
            }
        }

        private void SetShutterPosition(float y)
        {
            if (shutterRect != null)
            {
                Vector2 pos = shutterRect.anchoredPosition;
                pos.y = y;
                shutterRect.anchoredPosition = pos;
            }
        }

        private void UpdateUI()
        {
            if (shutterButtonText != null)
            {
                shutterButtonText.text = isOpen ? "Close Shutter" : "Open Shutter";
            }
        }
    }
}
