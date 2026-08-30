using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BubbleTeaShop
{
    public class DeskBell : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private Button bellButton;
        [SerializeField] private RectTransform bellTransform;
        [SerializeField] private AudioSource bellAudioSource;
        [SerializeField] private AudioClip bellSound;

        public static DeskBell Instance { get; private set; }

        private Coroutine punchRoutine;
        private Coroutine attentionWiggleRoutine;

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
            if (bellButton != null)
            {
                bellButton.onClick.AddListener(RingBell);
            }
            if (bellTransform == null)
            {
                bellTransform = GetComponent<RectTransform>();
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
            if (bellTransform != null)
            {
                bellTransform.localScale = Vector3.one;
                bellTransform.localRotation = Quaternion.identity;
            }
        }

        private IEnumerator AttentionWiggleLoop()
        {
            if (bellTransform == null) yield break;

            while (true)
            {
                // Enlarge & wiggle
                float duration = 0.8f;
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / duration;
                    float wiggle = Mathf.Sin(t * Mathf.PI * 6f) * 10f;
                    float scale = Mathf.Lerp(1f, 1.25f, Mathf.Sin(t * Mathf.PI));
                    bellTransform.localRotation = Quaternion.Euler(0, 0, wiggle);
                    bellTransform.localScale = new Vector3(scale, scale, 1f);
                    yield return null;
                }

                bellTransform.localRotation = Quaternion.identity;
                bellTransform.localScale = Vector3.one;

                // Pause before repeating
                yield return new WaitForSeconds(1.2f);
            }
        }

        public void RingBell()
        {
            StopAttentionWiggle();

            // Play animation punch
            if (punchRoutine != null) StopCoroutine(punchRoutine);
            punchRoutine = StartCoroutine(PunchBellAnimation());

            // Play sound
            if (bellSound != null)
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFX(bellSound);
                }
                else if (bellAudioSource != null)
                {
                    bellAudioSource.PlayOneShot(bellSound);
                }
            }

            // Call next customer if shutter is open or customer is waiting
            if (CustomerManager.Instance != null && CustomerManager.Instance.CustomerController != null && CustomerManager.Instance.CustomerController.IsLandlordActive)
            {
                HUDController.Instance?.ShowNotification("The Landlord is waiting! Settle your rent first.");
                return;
            }

            if (GameManager.Instance != null && (GameManager.Instance.CurrentState == GameState.ShopOpen || GameManager.Instance.CurrentState == GameState.CustomerWaiting))
            {
                CustomerManager.Instance?.TryCallNextCustomer();
            }
            else
            {
                Debug.Log("Open the shop shutters first before ringing the bell!");
            }
        }

        private IEnumerator PunchBellAnimation()
        {
            if (bellTransform == null) yield break;

            Vector3 origScale = Vector3.one;
            Vector3 squishedScale = new Vector3(1.15f, 0.85f, 1f);

            float elapsed = 0f;
            while (elapsed < 0.08f)
            {
                elapsed += Time.deltaTime;
                bellTransform.localScale = Vector3.Lerp(origScale, squishedScale, elapsed / 0.08f);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < 0.12f)
            {
                elapsed += Time.deltaTime;
                bellTransform.localScale = Vector3.Lerp(squishedScale, origScale, elapsed / 0.12f);
                yield return null;
            }

            bellTransform.localScale = origScale;
        }
    }
}
