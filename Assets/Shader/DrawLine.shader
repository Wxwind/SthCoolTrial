Shader "Custom/DrawLine"
{
    Properties
    {
        _MainTex ("MainTex", 2D) = "white" { }
        _Coordinate ("Coordinate", Vector) = (0, 0, 0, 0)
        _Color ("DrawColor", Color) = (1, 0, 0, 0)
        _Size ("Size", Range(1, 500)) = 1
        _Strenth ("Strenth", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "LightMode" = "UniversalForward" "RenderType" = "Opaque" "Queue" = "Geometry" }
        LOD 100
        Pass
        {

            HLSLPROGRAM

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            #pragma vertex vert
            #pragma fragment frag


            Texture2D _MainTex;
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;
            float4 _Coordinate;
            float4 _Color;
            float _Size;
            float _Strenth;
            

            struct a2v
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };
            
            v2f vert(a2v v)//用在domain函数中处理新生成的点

            {
                v2f o;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                return o;
            }
            
            
            real4 frag(v2f o) : SV_Target
            {
                real4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, o.uv);
                //real4 col = real4(0.0,0.0,1.0,1.0);
                float draw = pow(saturate(1 - distance(o.uv, _Coordinate.xy)), 500 / _Size);//世界坐标映射回贴图uv
                real4 drawColor = _Color * (draw * _Strenth);
                //return saturate(drawColor + col);
                return saturate(drawColor+col);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
