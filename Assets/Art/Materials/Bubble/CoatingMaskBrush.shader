Shader "Hidden/AR-TT/Coating Mask Brush"
{
    Properties
    {
        _MainTex("Mask", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }

        Pass
        {
            ZTest Always
            ZWrite Off
            Cull Off

            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment Frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float2 _BrushStart;
            float2 _BrushEnd;
            float _BrushRadius;

            fixed4 Frag(v2f_img input) : SV_Target
            {
                float existingMask = tex2D(_MainTex, input.uv).r;
                float2 segment = _BrushEnd - _BrushStart;
                float segmentLengthSquared = max(dot(segment, segment), 1e-8);
                float segmentPosition = saturate(
                    dot(input.uv - _BrushStart, segment) /
                    segmentLengthSquared
                );
                float2 closestPoint =
                    _BrushStart + segment * segmentPosition;
                float distanceToBrush = distance(input.uv, closestPoint);
                float brushMask = smoothstep(
                    _BrushRadius * 0.75,
                    _BrushRadius,
                    distanceToBrush
                );
                float result = min(existingMask, brushMask);
                return fixed4(result, result, result, 1);
            }
            ENDCG
        }
    }
}
