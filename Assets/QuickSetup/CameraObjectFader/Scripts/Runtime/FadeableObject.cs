using System.Collections.Generic;
using UnityEngine;
using CameraObjectFader.Internal;

namespace CameraObjectFader
{
    [AddComponentMenu("Camera Object Fader/Fadeable Object")]
    public class FadeableObject : MonoBehaviour
    {
        [Tooltip("If true, this component will also find and fade renderers in child objects.")]
        public bool fadeChildren = false;

        private class RendererData
        {
            public Renderer renderer;
            public Material[] originalSharedMaterials;
            public Material[] temporaryMaterials; 
            public bool isUsingTemps;
        }

        private List<RendererData> _renderers = new List<RendererData>();
        private bool _isInitialized = false;
        
        private float _currentAlpha = 1.0f;
        private float _targetAlpha = 1.0f;
        private float _fadeSpeed = 5.0f;
        
        private bool _shouldBeFaded = false;

        private void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (_isInitialized)
            {
                RestoreOriginalMaterials();
            }

            _renderers.Clear();
            
            Renderer[] rends;
            if (fadeChildren)
                rends = GetComponentsInChildren<Renderer>();
            else
                rends = GetComponents<Renderer>();

            foreach (var r in rends)
            {
                if (r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;
                if (r.GetComponentInParent<FaderIgnore>() != null) continue;

                var data = new RendererData
                {
                    renderer = r,
                    originalSharedMaterials = r.sharedMaterials,
                    temporaryMaterials = null, 
                    isUsingTemps = false
                };
                _renderers.Add(data);
            }

            _currentAlpha = 1.0f;
            _isInitialized = true;
        }

        public void SetFadeTarget(float alpha, float speed)
        {
            _targetAlpha = alpha;
            _fadeSpeed = speed;
            _shouldBeFaded = (_targetAlpha < 0.99f);
            
            enabled = true;
        }

        public bool ManualUpdate(float deltaTime)
        {
            if (!_isInitialized) return false;

            if (Mathf.Abs(_currentAlpha - _targetAlpha) > 0.001f)
            {
                _currentAlpha = Mathf.MoveTowards(_currentAlpha, _targetAlpha, _fadeSpeed * deltaTime);
                ApplyAlphaToMaterials();
            }
            else
            {
                _currentAlpha = _targetAlpha;
                
                if (_currentAlpha >= 0.999f && !_shouldBeFaded)
                {
                    RestoreOriginalMaterials();
                    return false;
                }
            }

            return true;
        }

        private void ApplyAlphaToMaterials()
        {
            if (_currentAlpha < 0.999f)
            {
                PrepareTemporaryMaterials();
            }

            int count = _renderers.Count;
            for (int i = 0; i < count; i++)
            {
                var data = _renderers[i];
                if (data.renderer == null) continue;

                if (_currentAlpha <= 0.01f)
                {
                    if (data.renderer.enabled) data.renderer.enabled = false;
                    continue;
                }
                else
                {
                    if (!data.renderer.enabled) data.renderer.enabled = true;
                }

                if (data.isUsingTemps && data.temporaryMaterials != null)
                {
                    int matCount = data.temporaryMaterials.Length;
                    for (int j = 0; j < matCount; j++)
                    {
                        MaterialFadeUtility.SetAlpha(data.temporaryMaterials[j], _currentAlpha);
                    }
                }
            }
        }

        private void PrepareTemporaryMaterials()
        {
            foreach (var data in _renderers)
            {
                if (data.renderer == null) continue;
                if (data.isUsingTemps) continue; 

                data.temporaryMaterials = data.renderer.materials; 
                
                for (int i = 0; i < data.temporaryMaterials.Length; i++)
                {
                    MaterialFadeUtility.SetupMaterialForFading(data.temporaryMaterials[i]);
                    MaterialFadeUtility.SetAlpha(data.temporaryMaterials[i], _currentAlpha);
                }

                data.isUsingTemps = true;
            }
        }

        private void RestoreOriginalMaterials()
        {
            foreach (var data in _renderers)
            {
                if (data.renderer == null) continue;
                if (!data.isUsingTemps) continue;

                data.renderer.sharedMaterials = data.originalSharedMaterials;
                
                if (!data.renderer.enabled) data.renderer.enabled = true;

                if (data.temporaryMaterials != null)
                {
                    foreach (var mat in data.temporaryMaterials)
                    {
                        if (mat != null) Destroy(mat);
                    }
                }

                data.temporaryMaterials = null;
                data.isUsingTemps = false;
            }
        }

        private void OnDisable()
        {
            RestoreOriginalMaterials();
        }

        private void OnDestroy()
        {
            RestoreOriginalMaterials();
        }

        public bool IsFullyOpaque => _currentAlpha >= 0.999f;
    }
}
