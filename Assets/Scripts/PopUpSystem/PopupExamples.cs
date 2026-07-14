using UnityEngine;

namespace PopupSystem.Examples
{
    /// <summary>
    /// Drop this on a GameObject and wire up the definition assets to see
    /// each popup type in action. Delete before shipping.
    /// </summary>
    public class PopupExamples : MonoBehaviour
    {
        [SerializeField] private PopupDefinition quitConfirmation;
        [SerializeField] private PopupDefinition infoModal;
        [SerializeField] private PopupDefinition toast;

        // 1. Simple confirmation with callbacks
        public void ShowQuitDialog()
        {
            PopupManager.Instance.Show(quitConfirmation,
                onConfirm: () => Application.Quit(),
                onCancel: () => Debug.Log("Player stayed."));
        }

        // 2. Modal with per-call dynamic text via the fluent builder
        public void ShowLevelComplete(int score)
        {
            var request = new PopupRequest(infoModal)
                .WithTitle("Level Complete!")
                .WithMessage($"You scored {score} points.")
                .WithConfirm("Next Level", () => Debug.Log("Loading next..."));

            PopupManager.Instance.Show(request);
        }

        // 3. Fire-and-forget toast
        public void ShowSaveToast()
        {
            var request = new PopupRequest(toast).WithMessage("Game saved.");
            PopupManager.Instance.Show(request);
        }

        // 4. High-priority popup jumps the queue (e.g. connection lost)
        public void ShowUrgent()
        {
            var request = new PopupRequest(infoModal)
                .WithTitle("Connection Lost")
                .WithMessage("Reconnecting...")
                .WithPriority(100);

            PopupManager.Instance.Show(request);
        }

        // 5. Pause gameplay while any modal is open
        private void OnEnable()
        {
            if (PopupManager.Instance != null)
                PopupManager.Instance.ModalActiveChanged += HandleModalActive;
        }

        private void OnDisable()
        {
            if (PopupManager.Instance != null)
                PopupManager.Instance.ModalActiveChanged -= HandleModalActive;
        }

        private void HandleModalActive(bool active)
        {
            // Routed through TimeManager (not a raw Time.timeScale write) so this doesn't
            // clobber the timeScale snapshot the pause menu / slow-mo system relies on.
            if (active) TimeManager.SnapshotAndPause();
            else TimeManager.Unpause();
        }
    }
}
