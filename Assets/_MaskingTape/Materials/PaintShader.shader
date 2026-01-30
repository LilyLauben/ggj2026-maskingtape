Shader "Custom/PaintShader"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "black" {}
        _Coordinates("Coordinate", Vector) = (0,0,0,0)
        _Color("Draw Color", Color ) = (1,0,0,1)
        _Strength("Strength", Range(0,1)) = 1
        _Size("Size", Vector) = (0,0,0,0)
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
                float2 _Size;
                half _Strength;
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
                float2 delta = (IN.uv * _TextureSize.xy) - _Coordinates.xy * _TextureSize.xy;
                
                //TODO: Rotation (if we get there)
                // float s = sin(-_Rotation);
                //float c = cos(-_Rotation);
                // delta = float2( c * delta.x - s * delta.y, s * delta.x + c * delta.y);
                
                // Draw rectangle
                float2 halfSize = _Size * 0.5;
                float2 d = abs(delta) - halfSize;
                half draw = step(max(d.x, d.y), 0.0);

                // Draw Circle
                // float distSq = dot(delta, delta);
                // float radiusSq = dot(_Size, _Size);
                // half draw = step(distSq, radiusSq);

                half4 drawColor = _Color * (draw * _Strength);
                return saturate(color + drawColor); 
            }
            ENDHLSL
        }
    }
}
