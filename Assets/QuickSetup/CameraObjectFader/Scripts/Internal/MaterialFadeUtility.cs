using UnityEngine;
using UnityEngine.Rendering;

namespace CameraObjectFader.Internal
{
    public static class MaterialFadeUtility
    {
        private static readonly int SurfaceProp = Shader.PropertyToID("_Surface");
        private static readonly int BlendProp = Shader.PropertyToID("_Blend");
        private static readonly int ZWriteProp = Shader.PropertyToID("_ZWrite");
        private static readonly int ModeProp = Shader.PropertyToID("_Mode");
        private static readonly int SrcBlendProp = Shader.PropertyToID("_SrcBlend");
        private static readonly int DstBlendProp = Shader.PropertyToID("_DstBlend");
        private static readonly int BaseColorProp = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorProp = Shader.PropertyToID("_Color");

        // Albedo
        private static readonly int BaseMapID = Shader.PropertyToID("_BaseMap");
        private static readonly int MainTexID = Shader.PropertyToID("_MainTex");
        // Normal
        private static readonly int BumpMapID = Shader.PropertyToID("_BumpMap");
        private static readonly int BumpScaleID = Shader.PropertyToID("_BumpScale");
        // Metallic workflow
        private static readonly int MetallicGlossMapID = Shader.PropertyToID("_MetallicGlossMap");
        private static readonly int MetallicID = Shader.PropertyToID("_Metallic");
        private static readonly int SmoothnessID = Shader.PropertyToID("_Smoothness");
        private static readonly int GlossMapScaleID = Shader.PropertyToID("_GlossMapScale");
        // Specular workflow
        private static readonly int SpecGlossMapID = Shader.PropertyToID("_SpecGlossMap");
        private static readonly int SpecColorID = Shader.PropertyToID("_SpecColor");
        // Emission
        private static readonly int EmissionMapID = Shader.PropertyToID("_EmissionMap");
        private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");
        // Occlusion
        private static readonly int OcclusionMapID = Shader.PropertyToID("_OcclusionMap");
        private static readonly int OcclusionStrID = Shader.PropertyToID("_OcclusionStrength");

        public enum SetupResult { Unknown, Standard, URP, HDRP }

        public static SetupResult SetupMaterialForFading(Material material)
        {
            if (material == null) return SetupResult.Unknown;

            string shaderName = material.shader.name;
            if (shaderName.Contains("Error") || shaderName.Contains("InternalError"))
                return SetupResult.Unknown;

            if (!shaderName.Contains("CustomDitherFade"))
            {
                Shader ditherShader = Shader.Find("Shader Graphs/CustomDitherFade");
                if (ditherShader != null)
                {
                    MaterialSnapshot snap = SnapshotMaterial(material);
                    material.shader = ditherShader;
                    RestoreSnapshot(material, snap);
                    material.renderQueue = (int)RenderQueue.Geometry;
                    material.SetShaderPassEnabled("ShadowCaster", true);
                    return SetupResult.URP;
                }
            }
            else
            {
                material.renderQueue = (int)RenderQueue.Geometry;
                material.SetShaderPassEnabled("ShadowCaster", true);
                return SetupResult.URP;
            }

            // ── Fallback transparent path ─────────────────────────────────
            bool isHDRP = material.HasProperty("_SurfaceType");
            bool isURP = material.HasProperty(SurfaceProp);
            bool isStdLit = (shaderName.Contains("Lit") || shaderName.Contains("Standard")
                          || shaderName.Contains("SimpleLit"))
                          && !shaderName.Contains("Dither");

            if ((isHDRP || isURP) && isStdLit)
            {
                if (isHDRP) material.SetFloat("_SurfaceType", 1.0f);
                if (isURP) material.SetFloat(SurfaceProp, 1.0f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                if (material.HasProperty(BlendProp))
                {
                    material.SetFloat(BlendProp, 0.0f);
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                }
                if (isHDRP)
                {
                    if (material.HasProperty("_BlendMode")) material.SetInt("_BlendMode", 0);
                    material.EnableKeyword("_ENABLE_ALPHA_2_STEP");
                }
                material.SetInt(SrcBlendProp, (int)BlendMode.SrcAlpha);
                material.SetInt(DstBlendProp, (int)BlendMode.OneMinusSrcAlpha);
                if (material.HasProperty(ZWriteProp)) material.SetFloat(ZWriteProp, 0.0f);
                material.renderQueue = (int)RenderQueue.Transparent;
                material.SetShaderPassEnabled("ShadowCaster", false);
                return isHDRP ? SetupResult.HDRP : SetupResult.URP;
            }
            else if (material.HasProperty(ModeProp) && isStdLit)
            {
                material.SetFloat(ModeProp, 2.0f);
                material.SetInt(SrcBlendProp, (int)BlendMode.SrcAlpha);
                material.SetInt(DstBlendProp, (int)BlendMode.OneMinusSrcAlpha);
                if (material.HasProperty(ZWriteProp)) material.SetInt(ZWriteProp, 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = (int)RenderQueue.Transparent;
                return SetupResult.Standard;
            }

            if (material.renderQueue < (int)RenderQueue.Transparent)
                material.renderQueue = (int)RenderQueue.Transparent;

            return SetupResult.Unknown;
        }

        // ─────────────────────────────────────────────────────────────────

        private enum WorkflowMode { Metallic, Specular, Unknown }

        private struct MaterialSnapshot
        {
            // Albedo
            public Texture albedoTex;
            public Vector2 albedoTiling;
            public Vector2 albedoOffset;
            public Color baseColor;
            // Normal
            public Texture normalTex;
            public float normalScale;
            // Workflow
            public WorkflowMode workflow;
            // Metallic
            public Texture metallicTex;
            public float metallic;
            public float smoothness;
            // Specular — stored in metallic/smoothness slots so same dither shader props receive them
            public Texture specGlossTex;
            public Color specColor;
            // Emission
            public Texture emissionTex;
            public Color emissionColor;
            // Occlusion
            public Texture occlusionTex;
            public float occlusionStrength;
        }

        private static MaterialSnapshot SnapshotMaterial(Material m)
        {
            var s = new MaterialSnapshot();

            // ── Albedo ────────────────────────────────────────────────────
            // URP uses _BaseMap; legacy Standard uses _MainTex
            if (m.HasProperty(BaseMapID) && m.GetTexture(BaseMapID) != null)
            {
                s.albedoTex = m.GetTexture(BaseMapID);
                s.albedoTiling = m.GetTextureScale("_BaseMap");
                s.albedoOffset = m.GetTextureOffset("_BaseMap");
            }
            else if (m.HasProperty(MainTexID) && m.GetTexture(MainTexID) != null)
            {
                s.albedoTex = m.GetTexture(MainTexID);
                s.albedoTiling = m.GetTextureScale("_MainTex");
                s.albedoOffset = m.GetTextureOffset("_MainTex");
            }
            else
            {
                s.albedoTiling = Vector2.one;
                s.albedoOffset = Vector2.zero;
            }

            s.baseColor = m.HasProperty(BaseColorProp) ? m.GetColor(BaseColorProp)
                        : m.HasProperty(ColorProp) ? m.GetColor(ColorProp)
                        : Color.white;

            // ── Normal ────────────────────────────────────────────────────
            s.normalTex = m.HasProperty(BumpMapID) ? m.GetTexture(BumpMapID) : null;
            s.normalScale = m.HasProperty(BumpScaleID) ? m.GetFloat(BumpScaleID) : 1f;

            // ── Detect workflow ───────────────────────────────────────────
            // URP Lit Specular workflow stores a _WorkflowMode float (0=Specular,1=Metallic)
            if (m.HasProperty(Shader.PropertyToID("_WorkflowMode")))
            {
                int wf = (int)m.GetFloat(Shader.PropertyToID("_WorkflowMode"));
                s.workflow = (wf == 0) ? WorkflowMode.Specular : WorkflowMode.Metallic;
            }
            else if (m.HasProperty(SpecColorID))
            {
                s.workflow = WorkflowMode.Specular;
            }
            else
            {
                s.workflow = WorkflowMode.Metallic;
            }

            // ── Metallic or Specular ──────────────────────────────────────
            if (s.workflow == WorkflowMode.Specular)
            {
                s.specGlossTex = m.HasProperty(SpecGlossMapID) ? m.GetTexture(SpecGlossMapID) : null;
                s.specColor = m.HasProperty(SpecColorID) ? m.GetColor(SpecColorID) : Color.grey;
                // Smoothness may still exist as a separate slider
                s.smoothness = m.HasProperty(SmoothnessID) ? m.GetFloat(SmoothnessID)
                               : m.HasProperty(GlossMapScaleID) ? m.GetFloat(GlossMapScaleID) : 0.3f;
            }
            else
            {
                s.metallicTex = m.HasProperty(MetallicGlossMapID) ? m.GetTexture(MetallicGlossMapID) : null;
                s.metallic = m.HasProperty(MetallicID) ? m.GetFloat(MetallicID) : 0f;
                s.smoothness = m.HasProperty(SmoothnessID) ? m.GetFloat(SmoothnessID)
                              : m.HasProperty(GlossMapScaleID) ? m.GetFloat(GlossMapScaleID) : 0.3f;
            }

            // ── Emission ──────────────────────────────────────────────────
            s.emissionTex = m.HasProperty(EmissionMapID) ? m.GetTexture(EmissionMapID) : null;
            s.emissionColor = m.HasProperty(EmissionColorID) ? m.GetColor(EmissionColorID) : Color.black;

            // ── Occlusion ─────────────────────────────────────────────────
            s.occlusionTex = m.HasProperty(OcclusionMapID) ? m.GetTexture(OcclusionMapID) : null;
            s.occlusionStrength = m.HasProperty(OcclusionStrID) ? m.GetFloat(OcclusionStrID) : 1f;

            return s;
        }

        private static void RestoreSnapshot(Material m, MaterialSnapshot s)
        {
            // ── Albedo ────────────────────────────────────────────────────
            if (s.albedoTex != null && m.HasProperty(MainTexID))
            {
                m.SetTexture(MainTexID, s.albedoTex);
                m.SetTextureScale("_MainTex", s.albedoTiling);
                m.SetTextureOffset("_MainTex", s.albedoOffset);
            }
            if (m.HasProperty(BaseColorProp)) m.SetColor(BaseColorProp, s.baseColor);
            else if (m.HasProperty(ColorProp)) m.SetColor(ColorProp, s.baseColor);

            // ── Normal ────────────────────────────────────────────────────
            if (s.normalTex != null && m.HasProperty(BumpMapID))
                m.SetTexture(BumpMapID, s.normalTex);
            if (m.HasProperty(BumpScaleID)) m.SetFloat(BumpScaleID, s.normalScale);

            // ── Metallic / Specular → dither shader uses Metallic slots ──
            if (s.workflow == WorkflowMode.Specular)
            {
                // Use base colour as-is — tinting by specColor was squaring the values
                // (baseColor * baseColor) which made everything too dark.
                if (m.HasProperty(BaseColorProp)) m.SetColor(BaseColorProp, s.baseColor);
                // Use spec/gloss map as metallic map for some surface detail
                if (s.specGlossTex != null && m.HasProperty(MetallicGlossMapID))
                    m.SetTexture(MetallicGlossMapID, s.specGlossTex);
                if (m.HasProperty(MetallicID)) m.SetFloat(MetallicID, 0f);
                if (m.HasProperty(SmoothnessID)) m.SetFloat(SmoothnessID, s.smoothness);
            }
            else
            {
                if (s.metallicTex != null && m.HasProperty(MetallicGlossMapID))
                    m.SetTexture(MetallicGlossMapID, s.metallicTex);
                if (m.HasProperty(MetallicID)) m.SetFloat(MetallicID, s.metallic);
                if (m.HasProperty(SmoothnessID)) m.SetFloat(SmoothnessID, s.smoothness);
            }

            // ── Emission ──────────────────────────────────────────────────
            if (s.emissionTex != null && m.HasProperty(EmissionMapID))
                m.SetTexture(EmissionMapID, s.emissionTex);
            if (m.HasProperty(EmissionColorID))
            {
                m.SetColor(EmissionColorID, s.emissionColor);
                if (s.emissionColor != Color.black) m.EnableKeyword("_EMISSION");
            }

            // ── Occlusion ─────────────────────────────────────────────────
            if (s.occlusionTex != null && m.HasProperty(OcclusionMapID))
                m.SetTexture(OcclusionMapID, s.occlusionTex);
            if (m.HasProperty(OcclusionStrID)) m.SetFloat(OcclusionStrID, s.occlusionStrength);
        }

        // ── Alpha setter (unchanged) ──────────────────────────────────────
        public static void SetAlpha(Material mat, float alpha)
        {
            if (mat == null) return;
            if (mat.HasProperty(BaseColorProp)) { Color c = mat.GetColor(BaseColorProp); c.a = alpha; mat.SetColor(BaseColorProp, c); return; }
            if (mat.HasProperty(ColorProp)) { Color c = mat.GetColor(ColorProp); c.a = alpha; mat.SetColor(ColorProp, c); return; }
            int mainColorId = Shader.PropertyToID("_MainColor");
            if (mat.HasProperty(mainColorId)) { Color c = mat.GetColor(mainColorId); c.a = alpha; mat.SetColor(mainColorId, c); return; }
            int tintColorId = Shader.PropertyToID("_TintColor");
            if (mat.HasProperty(tintColorId)) { Color c = mat.GetColor(tintColorId); c.a = alpha; mat.SetColor(tintColorId, c); }
        }
    }
}