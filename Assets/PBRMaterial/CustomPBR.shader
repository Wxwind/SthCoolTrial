//unity2019.4.28f1c1，不同版本可能源代码位置不同
Shader "Custom/CustomPBR"
{
    
    Properties
    {
        _BaseColor ("Basecolor", Color) = (1, 1, 1, 1)
        _Albedo ("Albedo", 2D) = "white" { }
        [NoScaleOffset][Normal]_Normal ("Normal", 2D) = "bump" { }
        //[NoScaleOffset]_AO ("AO", range(0, 1)) = 0
        [NoScaleOffset]_MaskMap ("Mask", 2D) = "white" { }
        //[NoScaleOffset]_Roughness ("Roughness", 2D) = "white"{}
        _Roughness ("perceptualRoughness", range(0, 1)) = 1 //被人体直观感知到的线性变化的粗糙度
        _Bumpscale ("Bumpscale", range(0, 1)) = 1
        _Metallic ("Metallic", range(0, 1)) = 1

    }
    SubShader
    {
        
        Pass
        {

            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Albedo_ST;
                float4 _Normal_ST;
                float _Bumpscale;
                float _Metallic;
                float4 _BaseColor;
                float _Roughness;
                //float _AO;
            CBUFFER_END
            sampler2D _Albedo;
            sampler2D _Normal;
            Texture2D _MaskMap;
            SAMPLER(sampler_MaskMap);
            
            //用于直接光计算

            //法线分布函数
            float DistributionGGX(float NdotH, float roughness)
            {
                float a = roughness * roughness;
                float a2 = a * a;//分子
                float denom = NdotH * NdotH * (a2 - 1) + 1;//分母
                denom = denom * denom * PI;
                return a2 / denom;
            }

            //菲涅尔方程
            float3 FresnelSchlick(float3 F0, float VdotH)
            {
                //return F0+(1-F0)*pow(1-VdotH,5);
                return F0 + (1 - F0) * exp2((-5.55473 * VdotH - 6.98316) * VdotH);//ue4 in 2013siggraph，unity进一步用vdoth代替vdot

            }

            float GeometrySchlickGGX(float NdotV, float roughness)
            {
                float k = (roughness + 1) * (roughness + 1) / 8;//h_direct
                float nom = NdotV;
                float denom = NdotV * (1.0 - k) + k;
                return nom / denom;
            }

            //阴影遮罩函数
            float GeometrySmith(float NdotV, float NdotL, float roughness)
            {
                float ggx1 = GeometrySchlickGGX(NdotV, roughness);
                float ggx2 = GeometrySchlickGGX(NdotL, roughness);
                return ggx1 * ggx2;
            }
            
            //用于间接光

            float3 FresnelSchlickRoughness(float NdotV, float3 F0, float roughness)
            {
                //return F0 + saturate(1 - roughness - F0) * pow(clamp(1.0 - NdotV, 0.0, 1.0), 5.0);
                return F0 + saturate(1 - roughness - F0) * exp2((-5.55473 * NdotV - 6.98316) * NdotV);//拟合
            }


            float3 IndirSpeCube(float3 normalWS, float3 viewWS, float roughness, float AO)//==GlossyEnvironmentReflection
            {
                float3 reflectDirWS = reflect(-viewWS, normalWS);
                roughness = roughness * (1.7 - 0.7 * roughness);//Unity内部不是线性 调整下拟合曲线求近似
                float MidLevel = roughness * 6;//把粗糙度remap到0-6 7个阶级 然后进行lod采样
                float4 speColor = SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, reflectDirWS, MidLevel);//根据不同的等级进行采样
                #if !defined(UNITY_USE_NATIVE_HDR)
                    return DecodeHDREnvironment(speColor, unity_SpecCube0_HDR) * AO;//用DecodeHDREnvironment将颜色从HDR编码下解码。可以看到采样出的rgbm是一个4通道的值，最后一个m存的是一个参数，解码时将前三个通道表示的颜色乘上xM^y，x和y都是由环境贴图定义的系数，存储在unity_SpecCube0_HDR这个结构中。
                #else
                    return speColor.xyz * AO;
                #endif
            }

            float3 MyGlossyEnvironmentReflection(half3 normalWS,float3 viewWS, half perceptualRoughness, half occlusion)//line 589 in Lighting.hlsl
            {
                float3 reflectVector = reflect(-viewWS, normalWS);
                return GlossyEnvironmentReflection(reflectVector, perceptualRoughness, occlusion);
            }

            half3 MyReflectivitySpecular(half3 specular)//line 270 in Lighting.hlsl
            {
                #if defined(SHADER_API_GLES)
                    return specular.r;

                #else
                    return max(max(specular.r, specular.g), specular.b);
                #endif
            }

            half3 MyEnvironmentBRDFSpecular(float roughness2, float smoothness, half3 F0, float NdotV)//line 371 in Lighting.glsl
            {
                half fresnelTerm = Pow4(1.0 - NdotV);
                float surfaceReduction = 1.0 / (roughness2 * roughness2 + 1.0);

                float reflectivity = MyReflectivitySpecular(F0);
                float grazingTerm = saturate(smoothness + reflectivity);

                return surfaceReduction * lerp(F0, grazingTerm, fresnelTerm);
            }
 

            struct a2v
            {
                float4 positionOS : POSITION;
                float4 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float4 uv : TEXCOORD0;
            };
            struct v2f
            {
                float4 positionCS : SV_POSITION;
                float4 uv : TEXCOORD0;
                float4 normalWS : NORMAL;
                float4 tangentWS : TANGENT;
                float4 biotangentWS : TEXCOORD1;
            };
            
            v2f vert(a2v v)
            {
                v2f o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.normalWS.xyz = normalize(TransformObjectToWorldNormal(v.normalOS.xyz));
                o.tangentWS.xyz = normalize(TransformObjectToWorldDir(v.tangentOS.xyz));
                o.biotangentWS.xyz = normalize(cross(o.normalWS.xyz, o.tangentWS.xyz) * v.tangentOS.w);
                o.normalWS.w = positionWS.x;
                o.tangentWS.w = positionWS.y;
                o.biotangentWS.w = positionWS.z;
                o.uv.xy = TRANSFORM_TEX(v.uv, _Albedo);
                o.uv.zw = v.uv;
                return o;
            }
            
            half4 frag(v2f i) : SV_Target
            {
                //提取mask贴图中的金属度，AO和粗糙度
                float4 Mask = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, i.uv.zw);
                float Metallic = _Metallic;//Mask.r
                float AO = Mask.g;
                float Roughness =_Roughness;//这里的Roughness等价于unity源码中的perceptualSmoothness
                //这里的Roughness2等价于unity源码中的roughness,Roughness平方主要是考虑到人眼对粗糙度的感知是非线性的
                float Roughness2 = Roughness*Roughness;
                float smoothness = 1-_Roughness;///Mask.a

                Light light = GetMainLight();
                half3 Clight = light.color;
                float3 L = normalize(light.direction);
                float3 positionWS = float3(i.normalWS.w, i.tangentWS.w, i.biotangentWS.w);
                float3 V = SafeNormalize(_WorldSpaceCameraPos.xyz - positionWS);
                float3 H = SafeNormalize(L + V);
                float3 Albedo = tex2D(_Albedo, i.uv.xy).xyz * _BaseColor.xyz;
                float3 F0 = lerp(float3(0.04, 0.04, 0.04), Albedo, Metallic);
                //计算法线（把法线从贴图的切线空间转换到世界空间下）
                float3x3 TtoW = {
                    i.tangentWS.xyz, i.biotangentWS.xyz, i.normalWS.xyz
                };
                
                TtoW = transpose(TtoW);
                half3 NormalTS = UnpackNormalScale(tex2D(_Normal, i.uv.zw), _Bumpscale);
                // half3 NormalTS = UnpackNormal(tex2D( _Normal, i.uv.zw));
                // NormalTS.xy*=_Bumpscale;
                NormalTS.z = sqrt(1 - saturate(dot(NormalTS.xy, NormalTS.xy)));
                float3 N = normalize(mul(TtoW, NormalTS));


                //预先计算必要的点积
                float NdotH = max(dot(N, H), 0.000001);
                float VdotH = max(dot(V, H), 0.000001);
                float NdotV = max(dot(N, V), 0.000001);
                float NdotL = max(dot(N, L), 0.000001);

                //直接光漫反射
                float3 diffuse = Albedo;
                float3 Direct_Diffuse = diffuse * Clight * NdotL;//除于Π和最后的球面积分Π正好消掉

                //直接光高光反射
                float D = DistributionGGX(NdotH, Roughness);
                float3 F = FresnelSchlick(F0, VdotH);
                float G = GeometrySmith(NdotV, NdotL, Roughness);
                float3 specular = 0.25 * D * F * G / (NdotV * NdotL);
                float3 Direct_Specular = specular * Clight * NdotL;

                //直接光
                float3 ks = F;
                float3 kd = (1 - ks) * (1 - Metallic);
                //高光反射不用乘ks，因为本身就已经带了菲涅尔项F;
                //也不需要乘上Π，因为只有L+V=H的方向光线才会进入眼睛，不像漫反射会接收到来自四面八方的光，所以不需要对球面积分
                float3 DirectColor = kd * Direct_Diffuse + Direct_Specular;

                //间接光漫反射
                float3 SHColor = SampleSH(N);
                float3 Indir_Diffuse = SHColor * Albedo;

                //间接光高光反射
                float3 reflectDir = reflect(-V, N);
                float3 IndirSpeCubeColor = IndirSpeCube(N, V, Roughness2, AO);
                float3 Indir_Specular = MyEnvironmentBRDFSpecular(Roughness2, smoothness,F0, NdotV) * IndirSpeCubeColor;

                //间接光
                float3 kS = FresnelSchlickRoughness(NdotV, F0, Roughness);
                float3 kD = (1.0 - kS) * (1 - Metallic);
                float3 IndirectColor = kD * Indir_Diffuse * AO + Indir_Specular;

                //合并直接光和间接光
                float3 output = DirectColor + IndirectColor;

                return float4(output, 1.0f);
            }
            ENDHLSL
        }
    }
}