Shader "AR-TT/Coating Clean Mask"
{
    Properties
    {
        _CoatingColor("Coating Color", Color) = (0.48, 0.48, 0.48, 1)
        _CleanMask("Clean Mask", 2D) = "white" {}
        _SurfaceOffset("Surface Offset", Float) = 0.002
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "Coating"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off
            ZWrite On
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half3 normalWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _CoatingColor;
                float4 _CleanMask_ST;
                float _SurfaceOffset;
            CBUFFER_END

            TEXTURE2D(_CleanMask);
            SAMPLER(sampler_CleanMask);

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 offsetPositionOS =
                    input.positionOS.xyz + input.normalOS * _SurfaceOffset;

                output.positionCS =
                    TransformObjectToHClip(offsetPositionOS);
                output.normalWS =
                    TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _CleanMask);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // Hide downward-facing surfaces, retaining sides and tops.
                clip(normalize(input.normalWS).y + 0.5h);

                half mask = SAMPLE_TEXTURE2D(
                    _CleanMask,
                    sampler_CleanMask,
                    input.uv
                ).r;

                clip(mask - 0.01h);
                clip(_CoatingColor.a - 0.001h);
                return half4(_CoatingColor.rgb, _CoatingColor.a * mask);
            }
            ENDHLSL
        }
    }
}
