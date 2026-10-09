Shader "Custom/TileEdgeGlow"
{
    Properties
    {
        _EdgeColor ("Edge Color", Color) = (1, 1, 1, 1)
        _EdgeIntensity ("Edge Intensity", Float) = 2
        _EdgeWidth ("Edge Width (UV)", Range(0.005, 0.5)) = 0.06
        _EdgeSoftness ("Edge Softness (UV)", Range(0.001, 0.5)) = 0.03
    }

    SubShader
    {
        // URP-specific: RenderPipeline tag
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "EdgeGlow"
            Tags { "LightMode"="UniversalForward" } // URP-specific

            Blend One One      // additivo: si somma al colore della tile sotto, effetto "luce"
            ZWrite Off
            ZTest LEqual
            Cull Off
            Offset -1, -1      // evita lo z-fighting con la tile

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl" // URP-specific

            // URP-specific: CBUFFER UnityPerMaterial = compatibile con SRP Batcher
            CBUFFER_START(UnityPerMaterial)
                half4 _EdgeColor;
                float _EdgeIntensity;
                float _EdgeWidth;
                float _EdgeSoftness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz); // URP-specific
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // distanza dal bordo più vicino: 0 sul bordo, 0.5 al centro
                float2 d2 = min(IN.uv, 1.0 - IN.uv);
                float d = min(d2.x, d2.y);

                float soft = max(_EdgeSoftness, 0.0001);
                float mask = 1.0 - smoothstep(_EdgeWidth - soft, _EdgeWidth, d);

                half3 col = _EdgeColor.rgb * _EdgeIntensity * mask;
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}