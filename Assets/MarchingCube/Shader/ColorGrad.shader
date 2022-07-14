Shader "Custom/ColorGrad"
{
    Properties
    {
        _MainTex ("MainTex", 2D) = "white" { }
        _GradColorTex ("GradColor", 2D) = "white" { }
        _Scale ("Size", Range(1, 500)) = 1
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
            Texture2D _GradColorTex;
            SAMPLER(sampler_GradColorTex);
            float4 _GradColorTex_ST;
            float4 _MainTex_ST;
            

            struct a2v
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 uv : TEXCOORD0;
            };
            
            v2f vert(a2v v)//用在domain函数中处理新生成的点

            {
                v2f o;
                o.uv.xy = TRANSFORM_TEX(v.uv, _MainTex);
                o.uv.zw = real2(v.vertex.y, 0.5);
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                return o;
            }
            
            
            real4 frag(v2f o) : SV_Target
            {
                real4 mainColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, o.uv.xy);
                real4 gradColor = SAMPLE_TEXTURE2D(_GradColorTex, sampler_GradColorTex, o.uv.zw);
                return saturate(gradColor * mainColor);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/Fallback"
}
