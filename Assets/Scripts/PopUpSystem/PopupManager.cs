using System;
using System.Collections.Generic;
using UnityEngine;

namespace PopupSystem
{
    /// <summary>
    /// Central entry point for showing popups. Game code depends only on
    /// this class (or an injected IPopupService wrapper) — never on views.
    ///
    /// Modals/confirmations are queued and shown one at a time, ordered by
    /// priority. Toasts are non-blocking, pooled, and shown immediately in
    /// parallel with any active modal.
    ///
    /// If you use Zenject/VContainer, delete the singleton bits and bind
    /// this as a single instance instead.
    /// </summary>
    public class PopupManager : MonoBehaviour
    {
        public static PopupManager Instance { get; private set; }

        [Header("Modal / Confirmation")]
        [Tooltip("A single reusable modal view in the scene.")]
        [SerializeField] private ModalPopupView modalView;

        [Header("Toasts")]
        [Tooltip("Toast prefab to pool.")]
        [SerializeField] private ToastPopupView toastPrefab;
        [SerializeField] private Transform toastParent;
        [SerializeField] private int toastPrewarm = 3;

        [Header("Lifetime")]
        [SerializeField] private bool persistAcrossScenes = true;

        // Priority queue implemented as a sorted list of buckets. For a game
        // this scale a simple insertion-sorted list is more than fast enough.
        private readonly List<PopupRequest> modalQueue = new();
        private bool isShowingModal;

        private PopupPool<ToastPopupView> toastPool;

        /// <summary>Raised when a modal opens/closes. Useful for pausing gameplay.</summary>
        public event Action<bool> ModalActiveChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (persistAcrossScenes) DontDestroyOnLoad(gameObject);

            if (toastPrefab != null)
                toastPool = new PopupPool<ToastPopupView>(
                    toastPrefab, toastParent != null ? toastParent : transform, toastPrewarm);
        }

        // ---- Public API ----

        /// <summary>Show a popup from a request. Routes by definition type.</summary>
        public void Show(PopupRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            PopupType type = request.Definition != null
                ? request.Definition.type : PopupType.Modal;

            if (type == PopupType.Toast) ShowToast(request);
            else EnqueueModal(request);
        }

        /// <summary>Convenience: build a request from a definition and show it.</summary>
        public PopupRequest Show(PopupDefinition definition,
            Action onConfirm = null, Action onCancel = null)
        {
            var request = new PopupRequest(definition);
            if (onConfirm != null) request.WithConfirm(null, onConfirm);
            if (onCancel != null) request.WithCancel(null, onCancel);
            Show(request);
            return request;
        }

        // ---- Modal handling ----

        private void EnqueueModal(PopupRequest request)
        {
            // Insertion sort by priority (descending), stable for equal priority.
            int i = modalQueue.Count;
            while (i > 0 && modalQueue[i - 1].Priority < request.Priority) i--;
            modalQueue.Insert(i, request);

            if (!isShowingModal) ShowNextModal();
        }

        private void ShowNextModal()
        {
            if (modalQueue.Count == 0)
            {
                if (isShowingModal)
                {
                    isShowingModal = false;
                    ModalActiveChanged?.Invoke(false);
                }
                return;
            }

            if (!isShowingModal)
            {
                isShowingModal = true;
                ModalActiveChanged?.Invoke(true);
            }

            PopupRequest next = modalQueue[0];
            modalQueue.RemoveAt(0);
            modalView.Display(next, ShowNextModal);
        }

        // ---- Toast handling ----

        private void ShowToast(PopupRequest request)
        {
            if (toastPool == null)
            {
                Debug.LogWarning("[PopupManager] No toast prefab configured.");
                return;
            }

            ToastPopupView toast = toastPool.Get();
            toast.transform.SetAsLastSibling();
            toast.Display(request, () => toastPool.Return(toast));
        }

        /// <summary>Clears queued modals and closes the active one.</summary>
        public void CloseAll()
        {
            modalQueue.Clear();
            modalView.ForceClose();
        }
    }
}
