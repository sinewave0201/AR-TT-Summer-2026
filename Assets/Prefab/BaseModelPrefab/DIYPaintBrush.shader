Shader "Hidden/AR-TT/DIY Paint Brush"
{
    Properties
    {
        _MainTex ("Current Paint", 2D) = "black" {}
        _BrushColor ("Brush Color", Color) = (1, 1, 1, 1)
        _BrushStart ("Brush Start", Vector) = (0, 0, 0, 0)
        _BrushEnd ("Brush End", Vector) = (0, 0, 0, 0)
        _BrushRadius ("Brush Radius", Float) = 0.04
        _Erase ("Erase", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _BrushColor;
            float2 _BrushStart;
            float2 _BrushEnd;
            float _BrushRadius;
            float _Erase;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.uv = GetFullScreenTriangleTexCoord(input.vertexID);
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float4 currentPaint = SAMPLE_TEXTURE2D(
                    _MainTex,
                    sampler_MainTex,
                    input.uv);

                float2 segment = _BrushEnd - _BrushStart;
                float segmentLengthSquared = max(dot(segment, segment), 0.000001);
                float positionOnSegment = saturate(
                    dot(input.uv - _BrushStart, segment) /
                    segmentLengthSquared);
                float2 closestPoint =
                    _BrushStart + segment * positionOnSegment;
                float distanceToStroke = distance(input.uv, closestPoint);
                float coverage = 1.0 - smoothstep(
                    _BrushRadius * 0.75,
                    _BrushRadius,
                    distanceToStroke);

                if (_Erase > 0.5)
                {
                    currentPaint.a *= 1.0 - coverage;
                    return currentPaint;
                }

                float4 brushPaint = float4(_BrushColor.rgb, 1.0);
                return lerp(currentPaint, brushPaint, coverage);
            }
            ENDHLSL
        }
    }
}
