// A see-through, unlit copy of a part that shows where it should go. Brighter at the silhouette, so the
// outline still reads when the real part overlaps it.
Shader "Lab/Ghost"
{
    Properties
    {
        _Color ("Colour", Color) = (0.4, 0.8, 1, 0.3)
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Ghost"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            // The lab's rule for custom URP shaders. Unlit doesn't need lights, but keeping every forward
            // pass the same avoids the silent dark-render bug coming back when someone adds lighting.
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewWS : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = position.positionCS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewWS = GetWorldSpaceViewDir(position.positionWS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float facing = abs(dot(normalize(input.normalWS), normalize(input.viewWS)));
                half alpha = _Color.a * (0.6h + 0.4h * (1.0h - (half)facing));
                return half4(_Color.rgb, alpha);
            }
            ENDHLSL
        }
    }
}
