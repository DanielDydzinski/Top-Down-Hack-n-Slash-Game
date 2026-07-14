# Unity Popup System

An event-driven, decoupled popup system with three popup types, priority
queuing, ScriptableObject configuration, object pooling, and coroutine-based
animations (no external tween dependency).

## Files

All files live flat under `Assets/Scripts/PopUpSystem/`:

```
PopupManager.cs      # Entry point: queue, routing, pooling
PopupPool.cs          # Generic component pool (used for toasts)
PopupDefinition.cs    # ScriptableObject — designer-authored config
PopupRequest.cs       # Runtime request + fluent builder
IPopupView.cs         # Shared contract for all views
PopupAnimator.cs      # Fade + scale transitions (CanvasGroup)
ModalPopupView.cs     # Blocking modal / confirmation
ToastPopupView.cs     # Non-blocking, auto-dismissing, pooled
PopupExamples.cs      # Sample call sites (delete before shipping)
```

## Scene setup

1. **Canvas** — add a UI Canvas (Screen Space – Overlay is fine).
2. **Modal view** — build a panel with a title text, message text, confirm
   button, and cancel button (all TextMeshPro). Add a `CanvasGroup` and the
   `PopupAnimator` component to the panel root, then add `ModalPopupView` and
   wire up its serialized references. Set its `root` to the panel GameObject.
3. **Toast prefab** — build a smaller panel with a message text, a
   `CanvasGroup`, `PopupAnimator`, and `ToastPopupView`. Save it as a prefab.
4. **Manager** — add an empty GameObject, attach `PopupManager`, and assign
   the modal view, toast prefab, and a toast parent transform.

## Usage

```csharp
// From a ScriptableObject with callbacks
PopupManager.Instance.Show(quitConfirmation,
    onConfirm: () => Application.Quit());

// With dynamic content via the fluent builder
var req = new PopupRequest(infoModal)
    .WithTitle("Level Complete!")
    .WithMessage($"Score: {score}")
    .WithConfirm("Next", () => LoadNext());
PopupManager.Instance.Show(req);

// Toast — fire and forget
PopupManager.Instance.Show(new PopupRequest(toastDef).WithMessage("Saved."));
```

## Design decisions

- **Separation of concerns.** The manager owns queue/routing logic; views own
  presentation. Game code never references a view directly, so you can reskin
  or replace the UI without touching gameplay.
- **Interface-driven views.** `IPopupView` lets you add new popup styles
  without changing the manager.
- **Priority queue.** Modals are shown one at a time, ordered by priority, so
  an urgent "Connection Lost" can jump ahead of a queued "Level Complete".
- **Toasts run in parallel** with modals and are pooled, since they can appear
  rapidly.
- **ScriptableObject config** lets designers author popups without code.
- **No tween dependency** — `PopupAnimator` uses coroutines with unscaled time
  so popups animate even when `Time.timeScale == 0` (paused). If you already
  use DOTween/LeanTween, swap the interpolation and keep the method signatures.

## Dependency injection

The singleton is there for convenience. With Zenject/VContainer, remove the
`Instance`/`DontDestroyOnLoad` code, bind `PopupManager` as a single instance,
and inject it where needed.
```
