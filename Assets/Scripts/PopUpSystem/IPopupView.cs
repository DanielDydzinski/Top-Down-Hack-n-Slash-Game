using System;

namespace PopupSystem
{
    /// <summary>
    /// Contract every popup view implements. The manager only depends on
    /// this interface, so new popup styles (modal, toast, custom) can be
    /// dropped in without changing manager logic.
    /// </summary>
    public interface IPopupView
    {
        PopupType Type { get; }

        /// <summary>Show the popup. Invoke onClosed exactly once when done.</summary>
        void Display(PopupRequest request, Action onClosed);

        /// <summary>Force-close immediately (e.g. scene teardown).</summary>
        void ForceClose();
    }
}
