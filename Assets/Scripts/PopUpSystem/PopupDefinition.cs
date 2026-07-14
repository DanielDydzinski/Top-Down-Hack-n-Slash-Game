using UnityEngine;

namespace PopupSystem
{
    /// <summary>
    /// Designer-authored popup configuration. Create assets via
    /// Create > UI > Popup Definition. Runtime data (callbacks,
    /// dynamic text) is layered on top via PopupRequest.
    /// </summary>
    [CreateAssetMenu(fileName = "Popup_", menuName = "UI/Popup Definition", order = 0)]
    public class PopupDefinition : ScriptableObject
    {
        [Header("Type")]
        public PopupType type = PopupType.Modal;

        [Header("Default Content")]
        public string title = "Title";
        [TextArea(2, 5)] public string message = "Message body.";

        [Header("Buttons")]
        public string confirmLabel = "OK";
        public string cancelLabel = "Cancel";
        public bool showCancel = false;

        [Header("Behaviour")]
        [Tooltip("Toasts auto-dismiss after this many seconds. Ignored for modals.")]
        public float autoDismissSeconds = 2.5f;
        [Tooltip("Higher priority popups jump the queue.")]
        public int priority = 0;
    }

    public enum PopupType
    {
        Modal,        // Blocks input, waits for user
        Confirmation, // Modal with confirm/cancel
        Toast         // Non-blocking, auto-dismisses
    }
}
