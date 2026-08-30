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

        private Coroutine punchRoutine;

        private void Start()
        {
            if (bellButton != null)
            {
                bellButton.onClick.AddListener(RingBell);
            }
        }

        public void RingBell()
        {
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
