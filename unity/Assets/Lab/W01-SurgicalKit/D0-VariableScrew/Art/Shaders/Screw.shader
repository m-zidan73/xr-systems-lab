// Variable-length screw for URP.
//
// One mesh serves every length. The vertex stage stretches only the shaft, leaving the
// head rigid because it mates with the plate hole, and retiles the thread normal map so
// the pitch stays constant in world units. Length and tray colour arrive per instance
// through a MaterialPropertyBlock set by Screw.cs.
//
// Model convention: axis along local +Z, head at z = 0, vertex colour red = 1 on the shaft.

Shader "ScrewSystem/Screw"
{
    Properties
    {
        [MainColor] _BaseColor      ("Base Color", Color)          = (0.62, 0.64, 0.68, 1)
        // Kept well below full metal on purpose. A true metal shows almost no diffuse
        // and relies on reflecting an environment, so with no probe or skybox in view it
        // renders close to black. 0.30 keeps the anodised colour bright and readable.
        _Metallic                   ("Metallic", Range(0,1))       = 0.30
        _Smoothness                 ("Smoothness", Range(0,1))     = 0.62

        [Normal] _ThreadNormal      ("Thread Normal", 2D)          = "bump" {}
        _NormalStrength             ("Thread Depth", Range(0,3))   = 1

        // Driven per instance by Screw.cs from the catalog. Lengths in metres, not
        // millimetres. Thread repeats and starts come from the spec's pitch, so two
        // diameters sharing this material still show different thread spacing.
        [HideInInspector] _Stretch      ("Stretch", Float)      = 0
        [HideInInspector] _HeadEnd      ("Head End", Float)     = 0.0025
        [HideInInspector] _ShaftLength  ("Shaft Length", Float) = 0.0155
        [HideInInspector] _ThreadTiling ("Thread Repeats", Float) = 12
        [HideInInspector] _ThreadStarts ("Thread Starts", Float)  = 1
        // 1 leaves the shaft at the nominal diameter, which is what the painted thread
        // wants. The geometry-thread variant sets this to core over major.
        [HideInInspector] _CoreScale    ("Core Scale", Float)     = 1
        [HideInInspector] _Highlight    ("Highlight", Float)      = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType"       = "Opaque"
            "RenderPipeline"   = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "Queue"            = "Geometry"
        }
        LOD 300

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "ScrewCommon.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _ThreadNormal_ST;
            float  _Metallic;
            float  _Smoothness;
            float  _ThreadTiling;
            float  _ThreadStarts;
            float  _NormalStrength;
            float  _Stretch;
            float  _HeadEnd;
            float  _ShaftLength;
            float  _CoreScale;
            float  _Highlight;
        CBUFFER_END
        ENDHLSL

        // ---------------------------------------------------------------- forward
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   ScrewVertex
            #pragma fragment ScrewFragment
            #pragma target   3.0

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_ThreadNormal);   SAMPLER(sampler_ThreadNormal);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float4 tangentWS   : TEXCOORD2;
                float2 uv          : TEXCOORD3;
                float  shaftMask   : TEXCOORD4;
                float  fogFactor   : TEXCOORD5;
            };

            Varyings ScrewVertex(Attributes IN)
            {
                Varyings OUT = (Varyings)0;

                float mask  = IN.color.r;
                float3 posOS = ScrewStretchPosition(IN.positionOS.xyz, _HeadEnd, _ShaftLength, _Stretch, _CoreScale);

                VertexPositionInputs pos = GetVertexPositionInputs(posOS);
                VertexNormalInputs   nrm = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                OUT.positionCS = pos.positionCS;
                OUT.positionWS = pos.positionWS;
                OUT.normalWS   = nrm.normalWS;
                OUT.tangentWS  = float4(nrm.tangentWS, IN.tangentOS.w * GetOddNegativeScale());
                OUT.uv         = ScrewThreadUV(IN.uv, mask, _ShaftLength, _Stretch,
                                               _ThreadTiling, _ThreadStarts);
                OUT.shaftMask  = saturate(mask);
                OUT.fogFactor  = ComputeFogFactor(pos.positionCS.z);
                return OUT;
            }

            half4 ScrewFragment(Varyings IN) : SV_Target
            {
                // Thread detail only where the shaft is. The head and the tip stay smooth,
                // which is why the mask exists at all.
                half3 tangentNormal = UnpackNormalScale(
                    SAMPLE_TEXTURE2D(_ThreadNormal, sampler_ThreadNormal, IN.uv),
                    _NormalStrength * IN.shaftMask);
                tangentNormal = normalize(lerp(half3(0, 0, 1), tangentNormal, IN.shaftMask));

                // Interpolation denormalises these, so renormalise before building the
                // basis or the shading drifts across each triangle.
                float3 normalWS  = normalize(IN.normalWS);
                float3 tangentWS = normalize(IN.tangentWS.xyz);
                float  sgn       = IN.tangentWS.w;
                float3 bitangent = sgn * cross(normalWS, tangentWS);
                half3x3 tbn = half3x3(tangentWS, bitangent, normalWS);

                SurfaceData surface = (SurfaceData)0;
                surface.albedo      = _BaseColor.rgb;
                surface.metallic    = _Metallic;
                surface.smoothness  = _Smoothness;
                surface.occlusion   = 1.0;
                surface.alpha       = 1.0;
                surface.normalTS    = tangentNormal;

                InputData input = (InputData)0;
                input.positionWS           = IN.positionWS;
                input.normalWS             = normalize(mul(tangentNormal, tbn));
                input.viewDirectionWS      = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                input.shadowCoord          = TransformWorldToShadowCoord(IN.positionWS);
                input.fogCoord             = IN.fogFactor;
                input.bakedGI              = SampleSH(input.normalWS);
                input.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);

                half4 color = UniversalFragmentPBR(input, surface);
                color.rgb = lerp(color.rgb, color.rgb * 1.5 + 0.06, _Highlight);   // set by Grabbable on hover
                color.rgb = MixFog(color.rgb, IN.fogFactor);
                return color;
            }
            ENDHLSL
        }

        // ----------------------------------------------------------- shadow caster
        // Same displacement, or the shadow is the length of the base mesh.
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

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

                float3 posOS = ScrewStretchPosition(IN.positionOS.xyz, _HeadEnd, _ShaftLength, _Stretch, _CoreScale);
                float3 positionWS = TransformObjectToWorld(posOS);
                float3 normalWS   = TransformObjectToWorldNormal(IN.normalOS);

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
            Cull Back

            HLSLPROGRAM
            #pragma vertex   DepthVertex
            #pragma fragment DepthFragment
            #pragma target   3.0

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings   { float4 positionCS : SV_POSITION; };

            Varyings DepthVertex(Attributes IN)
            {
                Varyings OUT;
                float3 posOS = ScrewStretchPosition(IN.positionOS.xyz, _HeadEnd, _ShaftLength, _Stretch, _CoreScale);
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
            Cull Back

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
                float3 posOS = ScrewStretchPosition(IN.positionOS.xyz, _HeadEnd, _ShaftLength, _Stretch, _CoreScale);
                OUT.positionCS = TransformObjectToHClip(posOS);
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
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
