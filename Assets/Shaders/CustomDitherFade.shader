Shader "Shader Graphs/CustomDitherFade"
{
    Properties
    {
        _MainTex            ("Base Map",            2D)         = "white" {}
        _BaseColor          ("Base Color",          Color)      = (1,1,1,1)
        _Color              ("Color (alias)",       Color)      = (1,1,1,1)
        _BumpMap            ("Normal Map",          2D)         = "bump"  {}
        _BumpScale          ("Normal Scale",        Float)      = 1.0
        _MetallicGlossMap   ("Metallic Map",        2D)         = "white" {}
        _Metallic           ("Metallic",            Range(0,1)) = 0.0
        _Smoothness         ("Smoothness",          Range(0,1)) = 0.3
        _EmissionMap        ("Emission Map",        2D)         = "black" {}
        [HDR] _EmissionColor("Emission Color",      Color)      = (0,0,0,1)
        _OcclusionMap       ("Occlusion Map",       2D)         = "white" {}
        _OcclusionStrength  ("Occlusion Strength",  Range(0,1)) = 1.0
        _DitherScale        ("Dither Scale",        Range(0.5,4)) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        // Lightmap support
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GlobalIllumination.hlsl"

        TEXTURE2D(_MainTex);          SAMPLER(sampler_MainTex);
        TEXTURE2D(_BumpMap);          SAMPLER(sampler_BumpMap);
        TEXTURE2D(_MetallicGlossMap); SAMPLER(sampler_MetallicGlossMap);
        TEXTURE2D(_EmissionMap);      SAMPLER(sampler_EmissionMap);
        TEXTURE2D(_OcclusionMap);     SAMPLER(sampler_OcclusionMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _BaseColor;
            float4 _Color;
            float  _BumpScale;
            float  _Metallic;
            float  _Smoothness;
            float4 _EmissionColor;
            float  _OcclusionStrength;
            float  _DitherScale;
        CBUFFER_END

        // ── Circle / floor globals ────────────────────────────────────────
        float4 _DitherCircle;
        float4 _DitherCircle2;
        float  _DitherUsePlayerCircle;
        float  _DitherUseMouseCircle;
        float  _PlayerWorldY;
        float  _FloorFadeOffset;

        static const float Bayer4x4[16] =
        {
             0,  8,  2, 10,
            12,  4, 14,  6,
             3, 11,  1,  9,
            15,  7, 13,  5
        };

        float DitherValue(float2 sp)
        {
            uint2 p = uint2(sp) % 4;
            return Bayer4x4[p.y * 4 + p.x] / 16.0;
        }

        float CircleOutsideness(float2 sp, float4 c)
        {
            return smoothstep(c.z, c.z + c.w, length(sp - c.xy));
        }

        struct Attributes
        {
            float4 positionOS  : POSITION;
            float3 normalOS    : NORMAL;
            float4 tangentOS   : TANGENT;
            float2 uv          : TEXCOORD0;
            float2 lightmapUV  : TEXCOORD1; // ← baked lightmap UVs
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS   : SV_POSITION;
            float3 positionWS   : TEXCOORD0;
            float3 normalWS     : TEXCOORD1;
            float3 tangentWS    : TEXCOORD2;
            float3 bitangentWS  : TEXCOORD3;
            float2 uv           : TEXCOORD4;
            // Stores lightmap UV or SH depending on LIGHTMAP_ON keyword
            DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 5);
            UNITY_VERTEX_OUTPUT_STEREO
        };
        ENDHLSL

        // ── ForwardLit ────────────────────────────────────────────────────
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // Shadow receiving
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _SHADOWS_SOFT

            // Lightmap variants — LIGHTMAP_ON is set by Unity when the object
            // has a valid baked lightmap; without it SH probes are used instead.
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK

            #pragma multi_compile_instancing

            Varyings vert(Attributes IN)
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                Varyings OUT;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs vp = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   vn = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);
                OUT.positionCS  = vp.positionCS;
                OUT.positionWS  = vp.positionWS;
                OUT.normalWS    = vn.normalWS;
                OUT.tangentWS   = vn.tangentWS;
                OUT.bitangentWS = vn.bitangentWS;
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);

                // Write lightmap UVs (or SH coefficients for non-lightmapped objects)
                OUTPUT_LIGHTMAP_UV(IN.lightmapUV, unity_LightmapST, OUT.lightmapUV);
                OUTPUT_SH(OUT.normalWS, OUT.vertexSH);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                float2 sp = IN.positionCS.xy;

                // ── Circle / floor mask ───────────────────────────────────
                float belowFloor = step(IN.positionWS.y, _PlayerWorldY - _FloorFadeOffset);

                // Per-circle: disabled circle contributes 1.0 (neutral for min)
                float playerOut = lerp(1.0, CircleOutsideness(sp, _DitherCircle),  _DitherUsePlayerCircle);
                float mouseOut  = lerp(1.0, CircleOutsideness(sp, _DitherCircle2), _DitherUseMouseCircle);

                // Both disabled -> anyEnabled=0 -> outsideness=0 everywhere -> whole object fades
                // At least one enabled -> pixels outside all enabled circles stay opaque
                float anyEnabled  = max(_DitherUsePlayerCircle, _DitherUseMouseCircle);
                float outsideness = max(lerp(0.0, min(playerOut, mouseOut), anyEnabled), belowFloor);

                float alpha          = _BaseColor.a;
                float effectiveAlpha = lerp(alpha, 1.0, outsideness);
                clip(effectiveAlpha - DitherValue(sp * _DitherScale));

                // ── Normal ────────────────────────────────────────────────
                half3 normalTS = UnpackNormalScale(
                    SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, IN.uv), _BumpScale);
                float3x3 TBN   = float3x3(normalize(IN.tangentWS),
                                          normalize(IN.bitangentWS),
                                          normalize(IN.normalWS));
                half3 normalWS = normalize(mul(normalTS, TBN));

                // ── Metallic / Smoothness ─────────────────────────────────
                half4 metallicSample = SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, IN.uv);
                half  metallic       = metallicSample.r * _Metallic;
                half  smoothness     = metallicSample.a * _Smoothness;

                // ── Albedo ────────────────────────────────────────────────
                half3 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv).rgb * _BaseColor.rgb;

                // ── Occlusion ─────────────────────────────────────────────
                half occ = lerp(1.0,
                    SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, IN.uv).g,
                    _OcclusionStrength);

                // ── Global Illumination ───────────────────────────────────
                // SAMPLE_GI automatically branches:
                //   LIGHTMAP_ON  → samples the baked lightmap texture (UV1)
                //   otherwise    → samples SH light probes
                // This is what makes static lightmapped objects look correct.
                half3 bakedGI = SAMPLE_GI(IN.lightmapUV, IN.vertexSH, normalWS) * occ;

                // ── Direct lighting ───────────────────────────────────────
                // For Mixed lighting, GetMainLight returns the real-time portion;
                // baked portion is already inside bakedGI via shadowmask.
                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                Light  mainLight   = GetMainLight(shadowCoord);
                half   NdotL       = saturate(dot(normalWS, mainLight.direction));
                half3  radiance    = mainLight.color * mainLight.shadowAttenuation * NdotL;

                // ── PBR approximation ─────────────────────────────────────
                half3 diffuse  = albedo * (1.0h - metallic);
                half3 specCol  = lerp(half3(0.04,0.04,0.04), albedo, metallic);

                float3 viewDir = normalize(GetCameraPositionWS() - IN.positionWS);
                float3 halfVec = normalize(mainLight.direction + viewDir);
                half   spec    = pow(saturate(dot(normalWS, halfVec)),
                                     exp2(smoothness * 10.0 + 1.0)) * smoothness;

                half3 color = diffuse  * (radiance + bakedGI)
                            + specCol * spec * mainLight.color * mainLight.shadowAttenuation;

                // ── Emission ──────────────────────────────────────────────
                color += SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, IN.uv).rgb
                       * _EmissionColor.rgb;

                return half4(color, 1.0);
            }
            ENDHLSL
        }

        // ── ShadowCaster ──────────────────────────────────────────────────
        // NO extra includes here — Core.hlsl, Lighting.hlsl and Shadows.hlsl
        // are already compiled in via HLSLINCLUDE above. Re-including them
        // causes silent compile failure which makes Unity fall back to the
        // ForwardLit pass for shadows — that's what creates the dither circle
        // pattern in the shadow map.
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex shadowVert
            #pragma fragment shadowFrag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            // These are standalone uniforms set by URP's shadow rendering.
            // In Unity 6 URP they are NOT declared via Lighting.hlsl/Shadows.hlsl,
            // so they must be declared explicitly here.
            float3 _LightDirection;
            float3 _LightPosition;

            struct ShadowOut
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            ShadowOut shadowVert(Attributes IN)
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                ShadowOut OUT;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float3 posWS  = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normWS = TransformObjectToWorldNormal(IN.normalOS);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDir = normalize(posWS - _LightPosition);
                #else
                    float3 lightDir = _LightDirection;
                #endif

                OUT.positionCS = TransformWorldToHClip(ApplyShadowBias(posWS, normWS, lightDir));
                return OUT;
            }

            // Solid shadow — no dither clip here intentionally.
            // The object always casts a full shadow regardless of fade amount.
            half4 shadowFrag() : SV_Target { return 0; }
            ENDHLSL
        }

        // ── DepthOnly ─────────────────────────────────────────────────────
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex depthVert
            #pragma fragment depthFrag
            #pragma multi_compile_instancing

            // Uses Attributes from HLSLINCLUDE — no re-include of Core.hlsl needed.
            struct DepthOut
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            DepthOut depthVert(Attributes IN)
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                DepthOut OUT;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }
            half4 depthFrag() : SV_Target { return 0; }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
