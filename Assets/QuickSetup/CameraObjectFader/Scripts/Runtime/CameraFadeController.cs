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

        [Header("Debug")]
        public bool showGizmos = true;

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
        }

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

                // Perform Physics Check for this target
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
            
            // 1. Check if the object is the specific target we are casting toward
            if (go == currentTarget.gameObject)
            {
                // Only exclude the target itself if we are NOT supposed to fade it when close
                if (!fadeTargetWhenClose) return true;
            }
            else
            {
                // 2. Check if the object is ONE OF THE OTHER targets
                // We typically don't want Target A to cause Target B to fade just by standing in front of it.
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

            // 1. Draw Lines to Targets
            Gizmos.color = Color.yellow;
            foreach (var t in targets)
            {
                if (t == null) continue;
                Vector3 targetPos = t.position + targetOffset;
                Gizmos.DrawLine(camPos, targetPos);
                
                // Draw SphereCast radius along the line if enabled
                if (useSphereCast)
                {
                    Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.2f); // Transparent Yellow
                    // Visualization is approximate: drawing a few spheres along the path
                    Vector3 dir = (targetPos - camPos);
                    float dist = dir.magnitude;
                    if (dist > minDistance)
                    {
                        // Draw a cylinder-like representation or just start/end spheres
                        Gizmos.DrawWireSphere(camPos + dir.normalized * minDistance, castRadius);
                        Gizmos.DrawWireSphere(camPos + dir.normalized * Mathf.Min(dist, maxFadeDistance), castRadius);
                    }
                }
            }

            // 2. Draw Max Fade Distance
            Gizmos.color = new Color(1, 0, 0, 0.2f); // Red
            Gizmos.DrawWireSphere(camPos, maxFadeDistance);

            // 3. Draw Min Distance
            Gizmos.color = new Color(0, 1, 0, 0.3f); // Green
            Gizmos.DrawWireSphere(camPos, minDistance);

            // 4. Draw Target Fade Ranges
            if (fadeTargetWhenClose)
            {
                // Draw triggers around Camera (original view)
                Gizmos.color = new Color(0, 1, 1, 0.15f); // Faint Cyan
                Gizmos.DrawWireSphere(camPos, targetFadeStartDistance);

                Gizmos.color = new Color(0, 0, 1, 0.15f); // Faint Blue
                Gizmos.DrawWireSphere(camPos, targetFadeEndDistance);

                // Draw triggers around Targets to visualize Offset and Proximity (Symmetric View)
                foreach (var t in targets)
                {
                    if (t == null) continue;
                    Vector3 tPos = t.position + targetOffset;

                    // Show Offset Point - Crucial for understanding what point is being measured
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawWireSphere(tPos, 0.1f);
                    Gizmos.DrawLine(t.position, tPos);

                    // Fade Zones relative to Target
                    // If Camera enters these spheres (green/blue), fading happens
                    Gizmos.color = new Color(0, 1, 1, 0.5f); // Cyan - Start Fade Limit
                    Gizmos.DrawWireSphere(tPos, targetFadeStartDistance);

                    Gizmos.color = new Color(0, 0, 1, 0.5f); // Blue - Full Fade Limit
                    Gizmos.DrawWireSphere(tPos, targetFadeEndDistance);
                }
            }
        }
    }
}
