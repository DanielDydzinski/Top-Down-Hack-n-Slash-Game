using System;

namespace PopupSystem
{
    /// <summary>
    /// A single runtime popup request. Built from a PopupDefinition but
    /// allows per-call overrides (dynamic text, callbacks). Use the fluent
    /// builder methods for readable call sites.
    /// </summary>
    public class PopupRequest
    {
        public PopupDefinition Definition { get; }

        public string Title { get; private set; }
        public string Message { get; private set; }
        public string ConfirmLabel { get; private set; }
        public string CancelLabel { get; private set; }
        public bool ShowCancel { get; private set; }
        public int Priority { get; private set; }

        public Action OnConfirm { get; private set; }
        public Action OnCancel { get; private set; }

        public PopupRequest(PopupDefinition definition)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Title = definition.title;
            Message = definition.message;
            ConfirmLabel = definition.confirmLabel;
            CancelLabel = definition.cancelLabel;
            ShowCancel = definition.showCancel;
            Priority = definition.priority;
        }

        public PopupRequest WithTitle(string title) { Title = title; return this; }
        public PopupRequest WithMessage(string message) { Message = message; return this; }
        public PopupRequest WithConfirm(string label, Action onConfirm = null)
        {
            if (label != null) ConfirmLabel = label;
            OnConfirm = onConfirm;
            return this;
        }
        public PopupRequest WithCancel(string label, Action onCancel = null)
        {
            if (label != null) CancelLabel = label;
            OnCancel = onCancel;
            ShowCancel = true;
            return this;
        }
        public PopupRequest WithPriority(int priority) { Priority = priority; return this; }
    }
}
