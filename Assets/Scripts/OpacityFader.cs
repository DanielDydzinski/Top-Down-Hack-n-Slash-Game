using System.Collections.Generic;
using UnityEngine;

public class ThundercloudPrewarmFader : MonoBehaviour
{
    [Header("Shader Graph Properties")]
    [Tooltip("The reference name of your shader graph slider. Usually '_Opacity' for a property named Opacity.")]
    [SerializeField] private string shaderOpacityName = "_Opacity";
    [Range(0f, 1f)]
    [SerializeField] private float targetMaterialOpacity = 0.813f; // Defaulting to your screenshot value!

    [Header("Fade Settings")]
    [SerializeField] private float fadeInDuration = 1.5f;

    // Lists to cache all elements found underneath the parent
    private List<Material> shaderMaterials = new List<Material>();
    private List<ParticleSystem> particleSystems = new List<ParticleSystem>();

    private float elapsedTime = 0f;
    private bool isFadeComplete = false;

    void Start()
    {
        // 1. Gather all Renderers (Mesh Renderers, Sprite Renderers, etc.)
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer rend in renderers)
        {
            Material mat = rend.material; // Creates local instance so project files aren't edited permanently
            if (mat.HasProperty(shaderOpacityName))
            {
                mat.SetFloat(shaderOpacityName, 0f); // Initialize at zero
                shaderMaterials.Add(mat);
            }
        }

        // 2. Gather all Particle Systems (just in case 'Lightning' or 'Sparks' use them!)
        ParticleSystem[] particles = GetComponentsInChildren<ParticleSystem>();
        foreach (ParticleSystem ps in particles)
        {
            particleSystems.Add(ps);
            // Optional: You can force particle systems to fade color over life, 
            // but controlling their main particle emission multiplier works wonders.
        }
    }

    void Update()
    {
        if (isFadeComplete) return;

        elapsedTime += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsedTime / fadeInDuration);

        // --- Fade the Shader Graph Opacity Slider ---
        float currentShaderOpacity = Mathf.Lerp(0f, targetMaterialOpacity, progress);
        foreach (Material mat in shaderMaterials)
        {
            if (mat != null)
            {
                mat.SetFloat(shaderOpacityName, currentShaderOpacity);
            }
        }

        // --- Handshake Fade for Particles (Visual Sync) ---
        foreach (ParticleSystem ps in particleSystems)
        {
            if (ps != null)
            {
                var mainModule = ps.main;
                // Gradually scale alpha properties if your particles utilize vertex color tracking
                Color c = mainModule.startColor.color;
                c.a = progress;
                mainModule.startColor = c;
            }
        }

        if (progress >= 1f)
        {
            isFadeComplete = true;
        }
    }
}