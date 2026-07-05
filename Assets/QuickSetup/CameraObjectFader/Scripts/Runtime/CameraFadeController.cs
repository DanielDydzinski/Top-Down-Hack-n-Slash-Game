using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

namespace CameraObjectFader
{
    [AddComponentMenu("Camera Object Fader/Camera Fade Controller")]
    [HelpURL("https://quick-setup-website.pages.dev/documentation/camera-object-fader/getting-started/")]
    public class CameraFadeController : MonoBehaviour
    {
        public static CameraFadeController Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            _cam = GetComponent<Camera>();
        }

        public UnityEvent onObstructionStart;
        public UnityEvent onObstructionEnd;

        [Header("Targeting")]
        [Tooltip("The objects the camera should track. Any object blocking ANY target will fade.")]
        public List<Transform> targets = new List<Transform>();
        public Vector3 targetOffset = Vector3.up * 1.5f;

        [Header("Fade Settings")]
        public LayerMask fadeLayer = 1;
        [Range(0f, 1f)] public float fadeAlpha = 0.2f;
        public float fadeSpeed = 5f;
        public bool fadeChildrenRenderers = true;

        [Header("Detection Settings")]
        public bool useSphereCast = false;
        public float castRadius = 0.3f;
        public float minDistance = 0.1f;
        public float maxFadeDistance = 20.0f;

        [Header("Distance Scaling")]
        public bool useDistanceScaling = true;
        [Range(0f, 1f)] public float nearAlpha = 0.1f;
        [Range(0f, 1f)] public float farAlpha = 0.5f;

        [Header("Target Fading")]
        public bool fadeTargetWhenClose = false;
        public float targetFadeStartDistance = 2.0f;
        public float targetFadeEndDistance = 1.0f;

        [Header("Manual Exclusions")]
        public List<string> ignoreTags = new List<string> { "IgnoreFading" };
        public List<GameObject> ignoreObjects = new List<GameObject>();

        // ── Mouse Circle Mask ─────────────────────────────────────────────
        [Header("Mouse Circle Mask")]
        [Tooltip("The transform that PlayerToMouse moves to the mouse world position.\n" +
                 "Leave empty to disable the mouse circle entirely.")]
        public Transform mouseWorldTransform;

        [Tooltip("The mouse circle only appears when the mouse is hovering over an object\n" +
                 "that is ALREADY fading the player's view this frame.\n" +
                 "If the player isn't behind the wall, the mouse circle won't show either.")]
        public float mouseCircleRadius = 180f;
        public float mouseCircleFeather = 60f;

        [Header("Debug")]
        public bool showGizmos = true;

        // ── Shader global IDs ─────────────────────────────────────────────
        private static readonly int IDCircle2 = Shader.PropertyToID("_DitherCircle2");
        private static readonly Vector4 OffScreen = new Vector4(-99999f, -99999f, 1f, 1f);

        // --- PUBLIC API ---

        public void SetTarget(Transform newTarget)
        {
            targets.Clear();
            if (newTarget != null) targets.Add(newTarget);
        }

        public void AddTarget(Transform newTarget)
        {
            if (newTarget != null && !targets.Contains(newTarget))
                targets.Add(newTarget);
        }

        public void RemoveTarget(Transform targetToRemove)
        {
            if (targets.Contains(targetToRemove))
                targets.Remove(targetToRemove);
        }

        public void ClearTargets()
        {
            targets.Clear();
        }

        // --- INTERNAL STATE ---
        private Camera _cam;
        private bool _isCurrentlyObstructionActive = false;
        private List<FadeableObject> _activeFaders = new List<FadeableObject>();
        private RaycastHit[] _hitsBuffer = new RaycastHit[20];
        private Dictionary<int, FadeableObject> _knownFaders = new Dictionary<int, FadeableObject>();
        private HashSet<FadeableObject> _currentFrameFaders = new HashSet<FadeableObject>();
        private Dictionary<FadeableObject, float> _frameAlphaOverrides = new Dictionary<FadeableObject, float>();

        private void LateUpdate()
        {
            if (targets == null || targets.Count == 0) return;

            ProcessObstructions();
            UpdateFaders();

            // Mouse circle runs AFTER ProcessObstructions so _currentFrameFaders is
            // already populated with this frame's player-blocking objects.
            UpdateMouseCircle();
        }

        // ── Mouse circle ──────────────────────────────────────────────────

        private void UpdateMouseCircle()
        {
            // No transform assigned → disable the mouse circle shader slot
            if (mouseWorldTransform == null)
            {
                Shader.SetGlobalVector(IDCircle2, OffScreen);
                return;
            }

            Vector3 camPos = transform.position;
            Vector3 toMouse = mouseWorldTransform.position - camPos;
            float dist = toMouse.magnitude;

            bool mouseIsOverSameObject = false;

            if (dist > 0.01f)
            {
                // Raycast from camera toward the mouse world position
                int hitCount = Physics.RaycastNonAlloc(
                    camPos, toMouse.normalized, _hitsBuffer, dist, fadeLayer);

                for (int i = 0; i < hitCount; i++)
                {
                    FadeableObject fader = _hitsBuffer[i].collider
                        .GetComponentInParent<FadeableObject>();

                    // Only activate if this object is ALSO blocking the player right now
                    if (fader != null && _currentFrameFaders.Contains(fader))
                    {
                        mouseIsOverSameObject = true;
                        break;
                    }
                }
            }

            if (mouseIsOverSameObject && _cam != null)
            {
                Vector3 sp = _cam.WorldToScreenPoint(mouseWorldTransform.position);
                if (sp.z > 0f)
                {
                    Shader.SetGlobalVector(IDCircle2,
                        new Vector4(sp.x, sp.y, mouseCircleRadius, mouseCircleFeather));
                    return;
                }
            }

            // Not over a shared fading object — hide the mouse circle
            Shader.SetGlobalVector(IDCircle2, OffScreen);
        }

        private void OnDisable()
        {
            Shader.SetGlobalVector(IDCircle2, OffScreen);
        }

        // ── Everything below is unchanged from the original asset ─────────

        private void ProcessObstructions()
        {
            _currentFrameFaders.Clear();
            _frameAlphaOverrides.Clear();

            Vector3 camPos = transform.position;

            foreach (var target in targets)
            {
                if (target == null) continue;

                Vector3 targetFullPos = target.position + targetOffset;
                Vector3 dir = targetFullPos - camPos;
                float totalDist = dir.magnitude;

                if (totalDist < minDistance) continue;

                int hitCount = useSphereCast
                    ? Physics.SphereCastNonAlloc(camPos, castRadius, dir.normalized, _hitsBuffer, totalDist, fadeLayer)
                    : Physics.RaycastNonAlloc(camPos, dir.normalized, _hitsBuffer, totalDist, fadeLayer);

                for (int i = 0; i < hitCount; i++)
                {
                    var hit = _hitsBuffer[i];
                    if (hit.distance > maxFadeDistance) continue;

                    GameObject go = hit.collider.gameObject;
                    if (ShouldExclude(go, target)) continue;

                    RegisterFaderForFrame(go, hit.distance, totalDist, target);
                }

                if (fadeTargetWhenClose && totalDist < targetFadeStartDistance)
                {
                    RegisterFaderForFrame(target.gameObject, totalDist, totalDist, target);
                }
            }
        }

        private bool ShouldExclude(GameObject go, Transform currentTarget)
        {
            if (go == gameObject) return true;

            if (go == currentTarget.gameObject)
            {
                if (!fadeTargetWhenClose) return true;
            }
            else
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    if (targets[i] != null && targets[i].gameObject == go) return true;
                }
            }

            if (go.GetComponent<FaderIgnore>() != null) return true;
            if (ignoreObjects != null && ignoreObjects.Contains(go)) return true;

            if (ignoreTags != null && ignoreTags.Count > 0)
            {
                string tagStr = go.tag;
                for (int i = 0; i < ignoreTags.Count; i++)
                    if (tagStr == ignoreTags[i]) return true;
            }
            return false;
        }

        private void RegisterFaderForFrame(GameObject go, float hitDist, float totalDist, Transform currentTarget)
        {
            FadeableObject fader = GetOrAddFader(go);
            if (fader == null) return;

            _currentFrameFaders.Add(fader);

            float finalAlpha = fadeAlpha;
            if (go == currentTarget.gameObject && fadeTargetWhenClose)
            {
                float t = Mathf.InverseLerp(targetFadeEndDistance, targetFadeStartDistance, totalDist);
                finalAlpha = Mathf.Lerp(fadeAlpha, 1f, t);
            }
            else if (useDistanceScaling)
            {
                float tDist = Mathf.Clamp01(hitDist / totalDist);
                finalAlpha = Mathf.Lerp(nearAlpha, farAlpha, tDist);
            }

            if (_frameAlphaOverrides.ContainsKey(fader))
                _frameAlphaOverrides[fader] = Mathf.Min(_frameAlphaOverrides[fader], finalAlpha);
            else
                _frameAlphaOverrides[fader] = finalAlpha;
        }

        private FadeableObject GetOrAddFader(GameObject go)
        {
            FadeableObject fader = go.GetComponentInParent<FadeableObject>();

            if (fader != null)
            {
                int id = fader.gameObject.GetInstanceID();
                if (!_knownFaders.ContainsKey(id)) _knownFaders[id] = fader;
                if (!fader.enabled) fader.enabled = true;
                return fader;
            }

            fader = go.AddComponent<FadeableObject>();
            fader.fadeChildren = fadeChildrenRenderers;
            fader.Initialize();

            _knownFaders[go.GetInstanceID()] = fader;
            return fader;
        }

        private void UpdateFaders()
        {
            float deltaTime = Time.deltaTime;

            foreach (var fader in _currentFrameFaders)
            {
                float alpha = _frameAlphaOverrides.ContainsKey(fader) ? _frameAlphaOverrides[fader] : fadeAlpha;
                fader.SetFadeTarget(alpha, fadeSpeed);
                if (!_activeFaders.Contains(fader)) _activeFaders.Add(fader);
            }

            for (int i = _activeFaders.Count - 1; i >= 0; i--)
            {
                var fader = _activeFaders[i];
                if (fader == null) { _activeFaders.RemoveAt(i); continue; }

                if (!_currentFrameFaders.Contains(fader))
                    fader.SetFadeTarget(1.0f, fadeSpeed);

                if (!fader.ManualUpdate(deltaTime))
                    _activeFaders.RemoveAt(i);
            }

            HandleEvents();
            if (Time.frameCount % 300 == 0) CleanKnownFadersCache();
        }

        private void HandleEvents()
        {
            bool hasObstruction = _currentFrameFaders.Count > 0;
            if (hasObstruction && !_isCurrentlyObstructionActive)
            {
                _isCurrentlyObstructionActive = true;
                onObstructionStart?.Invoke();
            }
            else if (!hasObstruction && _isCurrentlyObstructionActive && _activeFaders.Count == 0)
            {
                _isCurrentlyObstructionActive = false;
                onObstructionEnd?.Invoke();
            }
        }

        private void CleanKnownFadersCache()
        {
            List<int> toRemove = new List<int>();
            foreach (var kvp in _knownFaders) if (kvp.Value == null) toRemove.Add(kvp.Key);
            foreach (var key in toRemove) _knownFaders.Remove(key);
        }

        private void OnDrawGizmosSelected()
        {
            if (!showGizmos) return;
            if (targets == null) return;

            Vector3 camPos = transform.position;

            Gizmos.color = Color.yellow;
            foreach (var t in targets)
            {
                if (t == null) continue;
                Vector3 targetPos = t.position + targetOffset;
                Gizmos.DrawLine(camPos, targetPos);

                if (useSphereCast)
                {
                    Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.2f);
                    Vector3 dir = (targetPos - camPos);
                    float dist = dir.magnitude;
                    if (dist > minDistance)
                    {
                        Gizmos.DrawWireSphere(camPos + dir.normalized * minDistance, castRadius);
                        Gizmos.DrawWireSphere(camPos + dir.normalized * Mathf.Min(dist, maxFadeDistance), castRadius);
                    }
                }
            }

            Gizmos.color = new Color(1, 0, 0, 0.2f);
            Gizmos.DrawWireSphere(camPos, maxFadeDistance);

            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawWireSphere(camPos, minDistance);

            if (fadeTargetWhenClose)
            {
                Gizmos.color = new Color(0, 1, 1, 0.15f);
                Gizmos.DrawWireSphere(camPos, targetFadeStartDistance);

                Gizmos.color = new Color(0, 0, 1, 0.15f);
                Gizmos.DrawWireSphere(camPos, targetFadeEndDistance);

                foreach (var t in targets)
                {
                    if (t == null) continue;
                    Vector3 tPos = t.position + targetOffset;

                    Gizmos.color = Color.magenta;
                    Gizmos.DrawWireSphere(tPos, 0.1f);
                    Gizmos.DrawLine(t.position, tPos);

                    Gizmos.color = new Color(0, 1, 1, 0.5f);
                    Gizmos.DrawWireSphere(tPos, targetFadeStartDistance);

                    Gizmos.color = new Color(0, 0, 1, 0.5f);
                    Gizmos.DrawWireSphere(tPos, targetFadeEndDistance);
                }
            }

            // Draw mouse circle preview in scene view
            if (mouseWorldTransform != null)
            {
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
                Gizmos.DrawWireSphere(mouseWorldTransform.position, 0.3f);
                Gizmos.DrawLine(camPos, mouseWorldTransform.position);
            }
        }
    }
}