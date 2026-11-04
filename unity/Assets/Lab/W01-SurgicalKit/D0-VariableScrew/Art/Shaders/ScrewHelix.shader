// Real geometry thread for URP.
//
// A straight ribbon wrapped into a helix in the vertex stage. Unlike the normal-mapped
// version this changes the silhouette, which is what you notice when a screw is held
// close in a headset. It costs about 3,200 vertices per screw, fixed regardless of
// length, and it keeps the one-mesh-for-every-length property because the helix is
// re-evaluated rather than stretched.
//
// Length, pitch, radius and colour all arrive per instance from HelixScrew.cs.

Shader "ScrewSystem/ScrewHelix"
{
    Properties
    {
        [MainColor] _BaseColor  ("Base Color", Color)       = (0.62, 0.64, 0.68, 1)
        _Metallic               ("Metallic", Range(0,1))    = 0.30
        _Smoothness             ("Smoothness", Range(0,1))  = 0.62
        _Taper                  ("End Run Out", Range(0.005, 0.2)) = 0.03

        // Driven per instance. Metres.
        [HideInInspector] _ThreadStartZ ("Thread Start Z", Float) = 0.0025
        [HideInInspector] _ThreadRun    ("Thread Run", Float)     = 0.0155
        [HideInInspector] _Pitch        ("Pitch", Float)          = 0.00125
        // The crest lands on major, the root on core. Nominal diameter is the crest.
        [HideInInspector] _CoreRadius   ("Core Radius", Float)    = 0.00125
        [HideInInspector] _MajorRadius  ("Major Radius", Float)   = 0.00175
    }

    SubShader
    {
        Tags
        {
            "RenderType"            = "Opaque"
            "RenderPipeline"        = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "Queue"                 = "Geometry"
        }
        LOD 300

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "ScrewHelix.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float  _Metallic;
            float  _Smoothness;
            float  _Taper;
            float  _ThreadStartZ;
            float  _ThreadRun;
            float  _Pitch;
            float  _CoreRadius;
            float  _MajorRadius;
        CBUFFER_END
        ENDHLSL

        // ---------------------------------------------------------------- forward
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off                       // the ribbon is an open strip

            HLSLPROGRAM
            #pragma vertex   HelixVertex
            #pragma fragment HelixFragment
            #pragma target   3.0

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float  fogFactor  : TEXCOORD2;
            };

            Varyings HelixVertex(Attributes IN)
            {
                Varyings OUT = (Varyings)0;

                float3 posOS, nrmOS;
                ScrewHelixWrap(IN.positionOS.xyz, IN.normalOS,
                               _ThreadStartZ, _ThreadRun, _Pitch, _CoreRadius, _MajorRadius, _Taper,
                               posOS, nrmOS);

                VertexPositionInputs pos = GetVertexPositionInputs(posOS);

                OUT.positionCS = pos.positionCS;
                OUT.positionWS = pos.positionWS;
                OUT.normalWS   = TransformObjectToWorldNormal(nrmOS);
                OUT.fogFactor  = ComputeFogFactor(pos.positionCS.z);
                return OUT;
            }

            half4 HelixFragment(Varyings IN, half facing : VFACE) : SV_Target
            {
                // Cull Off means we see backfaces down in the thread roots. Flip the
                // normal for those or they light as though they faced away.
                float3 normalWS = normalize(IN.normalWS) * (facing >= 0 ? 1.0 : -1.0);

                SurfaceData surface = (SurfaceData)0;
                surface.albedo     = _BaseColor.rgb;
                surface.metallic   = _Metallic;
                surface.smoothness = _Smoothness;
                surface.occlusion  = 1.0;
                surface.alpha      = 1.0;
                surface.normalTS   = half3(0, 0, 1);

                InputData input = (InputData)0;
                input.positionWS              = IN.positionWS;
                input.normalWS                = normalWS;
                input.viewDirectionWS         = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                input.shadowCoord             = TransformWorldToShadowCoord(IN.positionWS);
                input.fogCoord                = IN.fogFactor;
                input.bakedGI                 = SampleSH(normalWS);
                input.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);

                half4 color = UniversalFragmentPBR(input, surface);
                color.rgb = MixFog(color.rgb, IN.fogFactor);
                return color;
            }
            ENDHLSL
        }

        // ----------------------------------------------------------- shadow caster
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Off

            HLSLPROGRAM
            #pragma vertex   ShadowVertex
            #pragma fragment ShadowFragment
            #pragma target   3.0
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };
            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings ShadowVertex(Attributes IN)
            {
                Varyings OUT;

                float3 posOS, nrmOS;
                ScrewHelixWrap(IN.positionOS.xyz, IN.normalOS,
                               _ThreadStartZ, _ThreadRun, _Pitch, _CoreRadius, _MajorRadius, _Taper,
                               posOS, nrmOS);

                float3 positionWS = TransformObjectToWorld(posOS);
                float3 normalWS   = TransformObjectToWorldNormal(nrmOS);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif

                float4 positionCS = TransformWorldToHClip(
                    ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                OUT.positionCS = positionCS;
                return OUT;
            }

            half4 ShadowFragment(Varyings IN) : SV_Target { return 0; }
            ENDHLSL
        }

        // -------------------------------------------------------------- depth only
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Off

            HLSLPROGRAM
            #pragma vertex   DepthVertex
            #pragma fragment DepthFragment
            #pragma target   3.0

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };
            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings DepthVertex(Attributes IN)
            {
                Varyings OUT;
                float3 posOS, nrmOS;
                ScrewHelixWrap(IN.positionOS.xyz, IN.normalOS,
                               _ThreadStartZ, _ThreadRun, _Pitch, _CoreRadius, _MajorRadius, _Taper,
                               posOS, nrmOS);
                OUT.positionCS = TransformObjectToHClip(posOS);
                return OUT;
            }

            half4 DepthFragment(Varyings IN) : SV_Target { return 0; }
            ENDHLSL
        }

        // --------------------------------------------------------- depth + normals
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            Cull Off

            HLSLPROGRAM
            #pragma vertex   DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            #pragma target   3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
            };

            Varyings DepthNormalsVertex(Attributes IN)
            {
                Varyings OUT;
                float3 posOS, nrmOS;
                ScrewHelixWrap(IN.positionOS.xyz, IN.normalOS,
                               _ThreadStartZ, _ThreadRun, _Pitch, _CoreRadius, _MajorRadius, _Taper,
                               posOS, nrmOS);
                OUT.positionCS = TransformObjectToHClip(posOS);
                OUT.normalWS   = TransformObjectToWorldNormal(nrmOS);
                return OUT;
            }

            half4 DepthNormalsFragment(Varyings IN) : SV_Target
            {
                return half4(normalize(IN.normalWS) * 0.5 + 0.5, 0.0);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
