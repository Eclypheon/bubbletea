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

        [Header("Audio & Feedback (Optional)")]
        [SerializeField] private AudioClip leverSound;
        [SerializeField] private AudioClip shutterMoveSound;

        private bool isOpen = false;
        private bool isMoving = false;
        private Coroutine leverFlipRoutine;

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

            // Animate lever flip
            AnimateLeverFlip();

            // Play lever click sound
            if (leverSound != null)
            {
                AudioManager.Instance?.PlaySFX(leverSound);
            }

            // Only allow closing if day is ready to close or before opening
            if (isOpen)
            {
                if (DayManager.Instance.IsDayFinished || GameManager.Instance.CurrentState == GameState.ShopClosing)
                {
                    StartCoroutine(MoveShutterRoutine(openPosY, closedPosY, false));
                }
                else
                {
                    HUDController.Instance?.ShowNotification("Customers are still waiting today!");
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

        private void AnimateLeverFlip()
        {
            if (shutterToggleButton == null) return;
            if (leverFlipRoutine != null) StopCoroutine(leverFlipRoutine);
            leverFlipRoutine = StartCoroutine(LeverFlipRoutine());
        }

        private IEnumerator LeverFlipRoutine()
        {
            Transform leverTransform = shutterToggleButton.transform;
            Vector3 originalScale = new Vector3(leverTransform.localScale.x, 1f, 1f);
            Vector3 flippedScale = new Vector3(leverTransform.localScale.x, -1f, 1f);

            // Flip down
            float elapsed = 0f;
            while (elapsed < 0.08f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / 0.08f;
                leverTransform.localScale = Vector3.Lerp(originalScale, flippedScale, t);
                yield return null;
            }
            leverTransform.localScale = flippedScale;

            yield return new WaitForSeconds(0.18f);

            // Flip back up
            elapsed = 0f;
            while (elapsed < 0.08f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / 0.08f;
                leverTransform.localScale = Vector3.Lerp(flippedScale, originalScale, t);
                yield return null;
            }
            leverTransform.localScale = originalScale;
        }

        private IEnumerator MoveShutterRoutine(float startY, float targetY, bool opening)
        {
            isMoving = true;
            float elapsed = 0f;

            if (shutterMoveSound != null)
            {
                AudioManager.Instance?.PlaySFX(shutterMoveSound, 0.8f);
            }

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
