using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PopupSystem
{
    /// <summary>
    /// A blocking modal / confirmation popup. Handles both single-button
    /// (Modal) and two-button (Confirmation) layouts based on the request.
    /// Presentation only — no game logic lives here.
    /// </summary>
    public class ModalPopupView : MonoBehaviour, IPopupView
    {
        [Header("Type")]
        [SerializeField] private PopupType type = PopupType.Modal;

        [Header("References")]
        [SerializeField] private GameObject root;
        [SerializeField] private PopupAnimator animator;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private TMP_Text confirmLabel;
        [SerializeField] private TMP_Text cancelLabel;

        private Action onClosed;
        private Action pendingCallback;

        public PopupType Type => type;

        private void Awake() => root.SetActive(false);

        public void Display(PopupRequest request, Action onClosed)
        {
            this.onClosed = onClosed;

            titleText.text = request.Title;
            messageText.text = request.Message;
            confirmLabel.text = request.ConfirmLabel;

            cancelButton.gameObject.SetActive(request.ShowCancel);
            if (request.ShowCancel && cancelLabel != null)
                cancelLabel.text = request.CancelLabel;

            confirmButton.onClick.RemoveAllListeners();
            cancelButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(() => BeginClose(request.OnConfirm));
            cancelButton.onClick.AddListener(() => BeginClose(request.OnCancel));

            root.SetActive(true);
            if (animator != null) animator.AnimateIn();
        }

        private void BeginClose(Action callback)
        {
            pendingCallback = callback;
            if (animator != null) animator.AnimateOut(FinishClose);
            else FinishClose();
        }

        private void FinishClose()
        {
            root.SetActive(false);
            var cb = pendingCallback;
            pendingCallback = null;
            cb?.Invoke();
            onClosed?.Invoke();
        }

        public void ForceClose()
        {
            root.SetActive(false);
            pendingCallback = null;
            onClosed?.Invoke();
        }
    }
}
