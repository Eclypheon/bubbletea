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
        private Coroutine attentionWiggleRoutine;

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

            // Start attention wiggle at the start of the day if shutters are closed
            if (!isOpen)
            {
                StartAttentionWiggle();
            }
        }

        public void StartAttentionWiggle()
        {
            if (attentionWiggleRoutine != null) StopCoroutine(attentionWiggleRoutine);
            attentionWiggleRoutine = StartCoroutine(AttentionWiggleLoop());
        }

        public void StopAttentionWiggle()
        {
            if (attentionWiggleRoutine != null)
            {
                StopCoroutine(attentionWiggleRoutine);
                attentionWiggleRoutine = null;
            }
            if (shutterToggleButton != null)
            {
                shutterToggleButton.transform.localRotation = Quaternion.identity;
                shutterToggleButton.transform.localScale = Vector3.one;
            }
        }

        private IEnumerator AttentionWiggleLoop()
        {
            if (shutterToggleButton == null) yield break;
            Transform tform = shutterToggleButton.transform;

            while (!isOpen)
            {
                float duration = 0.8f;
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / duration;
                    float scale = Mathf.Lerp(1f, 1.2f, Mathf.Sin(t * Mathf.PI));
                    tform.localRotation = Quaternion.identity;
                    tform.localScale = new Vector3(scale, scale, 1f);
                    yield return null;
                }

                tform.localRotation = Quaternion.identity;
                tform.localScale = Vector3.one;

                yield return new WaitForSeconds(1.2f);
            }
        }

        public void ToggleShutter()
        {
            StopAttentionWiggle();
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
                // 1. Block closing if Landlord or Mentor is currently waiting at the window
                if (CustomerManager.Instance != null && CustomerManager.Instance.CustomerController != null)
                {
                    if (CustomerManager.Instance.CustomerController.IsLandlordActive)
                    {
                        HUDController.Instance?.ShowNotification("You cannot close the shop while the Landlord is waiting for rent!");
                        return;
                    }
                    if (CustomerManager.Instance.CustomerController.IsMentorActive)
                    {
                        HUDController.Instance?.ShowNotification("Listen to your Mentor before closing the shop!");
                        return;
                    }
                }

                int currentDay = DayManager.Instance != null ? DayManager.Instance.CurrentDay : 1;

                // 2. Block closing on briefing days if the Mentor briefing hasn't arrived yet
                if (MentorController.Instance != null)
                {
                    if ((currentDay == 2 && !MentorController.Instance.HasCompletedDay2Briefing) ||
                        (currentDay == 5 && !MentorController.Instance.HasCompletedDay5Briefing) ||
                        (currentDay == 8 && !MentorController.Instance.HasCompletedDay8Briefing) ||
                        (currentDay == 11 && !MentorController.Instance.HasCompletedDay11Briefing) ||
                        (currentDay == 18 && !MentorController.Instance.HasCompletedDay18Briefing))
                    {
                        HUDController.Instance?.ShowNotification("Your Mentor is arriving to speak with you before closing!");
                        return;
                    }
                }

                // 3. Block closing if it is a rent day and the rent encounter hasn't settled yet
                if (currentDay % 7 == 0 && CustomerManager.Instance != null && !CustomerManager.Instance.RentEncounterTriggeredToday)
                {
                    HUDController.Instance?.ShowNotification("The Landlord is on his way! You must settle rent before closing.");
                    Debug.Log("Cannot close shutters: Rent collection pending!");
                    return;
                }

                // 3. Normal closing condition
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
                if (CustomerManager.Instance != null && CustomerManager.Instance.CustomerController != null)
                {
                    CustomerManager.Instance.CustomerController.DismissCustomer();
                }
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
