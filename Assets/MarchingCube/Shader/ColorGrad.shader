Shader "Custom/ColorGrad"
{
    Properties
    {
        _MainTex ("MainTex", 2D) = "white" { }
        _GradColorTex ("GradColorTex", 2D) = "white" { }
        _Cubemap ("Cubemap ", CUBE) = "_Skybox" { }
        _Diffuse ("Diffuse", Color) = (1, 1, 1, 1)
        _Specular ("specularColor", Color) = (1, 1, 1, 1)
        _Gloss ("Gloss", Range(1, 255)) = 20
        _EnvScale ("EnvScale", Range(0, 1)) = 0.5
        _BoundsY ("BoundsY", float) = 1
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
            samplerCUBE _Cubemap;
            float4 _GradColorTex_ST;
            float4 _MainTex_ST;
            float4 _Diffuse;
            float4 _Specular;
            float _Gloss;
            float _EnvScale;
            float _BoundsY;
            

            struct a2v
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct v2f
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half3 positionWS : TEXCOORD2;
            };
            
            v2f vert(a2v v)//用在domain函数中处理新生成的点

            {
                v2f o;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS.xyz);
                return o;
            }
            
            
            real4 frag(v2f o) : SV_Target
            {
                real4 mainColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, o.uv);//uv总是0，因为地形mesh是动态生成的，没有设置uv

                //chunkList的底部到顶部分别映射至u 0~1
                real h = smoothstep(-_BoundsY / 2, _BoundsY / 2, o.positionWS.y);
                real4 gradColor = SAMPLE_TEXTURE2D(_GradColorTex, sampler_GradColorTex, float2(h, 0.5));
                
                Light light = GetMainLight();
                half3 L = normalize(light.direction.xyz);
                half3 Clight = light.color.rgb;
                half3 V = SafeNormalize(_WorldSpaceCameraPos.xyz - o.positionWS);
                half3 H = SafeNormalize(V + L);
                half3 N = o.normalWS;
                
                //漫反射
                half3 diffuse = Clight * mainColor * _Diffuse * saturate(dot(L, N));

                //高光反射
                half3 specular = Clight * mainColor * _Specular * pow(max(0, dot(H, N)), _Gloss);
              
                //环境光
                half3 reflWS = reflect(-V, o.normalWS.xyz);
                real3 envreflColor = texCUBE(_Cubemap, reflWS).rgb * _EnvScale;

                real3 blinnPhong = diffuse + max(0,specular) + envreflColor;
                real3 res = gradColor.xyz * blinnPhong;

                return real4(res,1);
                //return b;
            }
            ENDHLSL
        }
    }
}
