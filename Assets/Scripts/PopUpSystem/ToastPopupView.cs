using System;
using System.Collections;
using UnityEngine;
using TMPro;

namespace PopupSystem
{
    /// <summary>
    /// A non-blocking toast that fades in, waits, and fades out on its own.
    /// Designed to be pooled — Display is called on a reused instance and
    /// it fully resets each time.
    /// </summary>
    public class ToastPopupView : MonoBehaviour, IPopupView
    {
        [Header("References")]
        [SerializeField] private GameObject root;
        [SerializeField] private PopupAnimator animator;
        [SerializeField] private TMP_Text messageText;

        private Action onClosed;
        private Coroutine lifecycle;

        public PopupType Type => PopupType.Toast;

        private void Awake() => root.SetActive(false);

        public void Display(PopupRequest request, Action onClosed)
        {
            this.onClosed = onClosed;
            messageText.text = request.Message;
            root.SetActive(true);

            float hold = request.Definition != null
                ? request.Definition.autoDismissSeconds : 2.5f;

            if (lifecycle != null) StopCoroutine(lifecycle);
            lifecycle = StartCoroutine(Lifecycle(hold, request.OnConfirm));
        }

        private IEnumerator Lifecycle(float hold, Action onDismiss)
        {
            if (animator != null) animator.AnimateIn();
            yield return new WaitForSecondsRealtime(hold);

            bool done = false;
            if (animator != null) animator.AnimateOut(() => done = true);
            else done = true;
            while (!done) yield return null;

            root.SetActive(false);
            onDismiss?.Invoke();
            onClosed?.Invoke();
            lifecycle = null;
        }

        public void ForceClose()
        {
            if (lifecycle != null) StopCoroutine(lifecycle);
            root.SetActive(false);
            onClosed?.Invoke();
            lifecycle = null;
        }
    }
}
