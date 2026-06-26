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

        public enum SetupResult { Unknown, Standard, URP, HDRP }

        public static SetupResult SetupMaterialForFading(Material material)
        {
            if (material == null) return SetupResult.Unknown;

            string shaderName = material.shader.name;
            if (shaderName.Contains("Error") || shaderName.Contains("InternalError")) return SetupResult.Unknown;

            bool isHDRP = material.HasProperty("_SurfaceType");
            bool isURP = material.HasProperty(SurfaceProp);

            bool isStandardLit = shaderName.Contains("Lit") || shaderName.Contains("Standard") || shaderName.Contains("SimpleLit");

            if ((isHDRP || isURP) && isStandardLit) 
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
                
                if (material.HasProperty(ZWriteProp))
                    material.SetFloat(ZWriteProp, 0.0f); 

                material.renderQueue = (int)RenderQueue.Transparent;
                
                material.SetShaderPassEnabled("ShadowCaster", false); 
                
                return isHDRP ? SetupResult.HDRP : SetupResult.URP;
            }
            else if (material.HasProperty(ModeProp) && isStandardLit) 
            {
                material.SetFloat(ModeProp, 2.0f); // Fade
                
                material.SetInt(SrcBlendProp, (int)BlendMode.SrcAlpha);
                material.SetInt(DstBlendProp, (int)BlendMode.OneMinusSrcAlpha);
                if (material.HasProperty(ZWriteProp))
                    material.SetInt(ZWriteProp, 0);
                
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

        public static void SetAlpha(Material mat, float alpha)
        {
            if (mat == null) return;
            
            bool colorFound = false;

            if (mat.HasProperty(BaseColorProp))
            {
                Color c = mat.GetColor(BaseColorProp);
                c.a = alpha;
                mat.SetColor(BaseColorProp, c);
                colorFound = true;
            }
            
            if (!colorFound && mat.HasProperty(ColorProp))
            {
                Color c = mat.GetColor(ColorProp);
                c.a = alpha;
                mat.SetColor(ColorProp, c);
                colorFound = true;
            }

            if (!colorFound)
            {
                int mainColorId = Shader.PropertyToID("_MainColor");
                if (mat.HasProperty(mainColorId))
                {
                    Color c = mat.GetColor(mainColorId);
                    c.a = alpha;
                    mat.SetColor(mainColorId, c);
                    colorFound = true;
                }
            }

            if (!colorFound)
            {
                int tintColorId = Shader.PropertyToID("_TintColor"); 
                if (mat.HasProperty(tintColorId))
                {
                    Color c = mat.GetColor(tintColorId);
                    c.a = alpha;
                    mat.SetColor(tintColorId, c);
                    colorFound = true;
                }
            }
        }
    }
}
