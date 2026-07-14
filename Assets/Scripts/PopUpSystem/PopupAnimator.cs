using System;
using System.Collections;
using UnityEngine;

namespace PopupSystem
{
    /// <summary>
    /// Handles fade + scale in/out for a popup using a CanvasGroup.
    /// Pure coroutine implementation so there's no external tween
    /// dependency. Swap the interpolation for DOTween/LeanTween if you
    /// already use one — just keep the AnimateIn/AnimateOut signatures.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class PopupAnimator : MonoBehaviour
    {
        [SerializeField] private float duration = 0.2f;
        [SerializeField] private Vector3 fromScale = new(0.85f, 0.85f, 1f);
        [SerializeField] private AnimationCurve ease =
            AnimationCurve.EaseInOut(0, 0, 1, 1);

        private CanvasGroup canvasGroup;
        private RectTransform rect;
        private Coroutine running;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            rect = transform as RectTransform;
        }

        public void AnimateIn(Action onComplete = null)
        {
            Restart(Animate(0f, 1f, fromScale, Vector3.one, onComplete));
        }

        public void AnimateOut(Action onComplete = null)
        {
            Restart(Animate(1f, 0f, Vector3.one, fromScale, onComplete));
        }

        private void Restart(IEnumerator routine)
        {
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(routine);
        }

        private IEnumerator Animate(float fromA, float toA,
            Vector3 fromS, Vector3 toS, Action onComplete)
        {
            float t = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = ease.Evaluate(Mathf.Clamp01(t / duration));
                canvasGroup.alpha = Mathf.Lerp(fromA, toA, k);
                if (rect != null) rect.localScale = Vector3.Lerp(fromS, toS, k);
                yield return null;
            }

            canvasGroup.alpha = toA;
            if (rect != null) rect.localScale = toS;

            bool visible = toA > 0.5f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;

            running = null;
            onComplete?.Invoke();
        }
    }
}
