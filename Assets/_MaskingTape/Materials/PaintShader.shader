Shader "Custom/PaintShader"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "black" {}
        _Coordinates("Coordinate", Vector) = (0,0,0,0)
        _Color("Draw Color", Color ) = (1,0,0,1)
        _Strength("Strength", Range(0,1)) = 1
        _Size("Size", Range(1,500)) = 0
        _TextureSize("TextureSize", Vector) = (1024,1024, 0, 0)
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        Lighting Off
        ZWrite Off // Disable writing to the depth buffer for proper blending
        Cull Back // Culls back faces (optional, but standard for most objects)
        Blend SrcAlpha OneMinusSrcAlpha // Standard alpha blending formula

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _Coordinates, _Color;
                half _Size, _Strength;
                float2 _TextureSize;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                //half draw = pow(saturate(1.0h - distance(IN.uv, _Coordinates.xy)), 500.0h / _Size);
                float2 delta = (IN.uv * _TextureSize.xy) - _Coordinates.xy * _TextureSize.xy;
                float distSq = dot(delta, delta);
                float radiusSq = dot(_Size, _Size);
                half draw = step(distSq, radiusSq);
                half4 drawColor = _Color * (draw * _Strength);
                return saturate(color + drawColor); 
            }
            ENDHLSL
        }
    }
}
