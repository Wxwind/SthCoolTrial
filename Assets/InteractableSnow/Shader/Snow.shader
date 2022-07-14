Shader "Custom/SnowColliable"
{
    Properties
    {
        _BaseColor ("BaseColor", Color) = (1, 1, 1, 1)
        _BaseTex ("BaseTex", 2D) = "white" { }
        _SnowTex ("SnowTex", 2D) = "white" { }
        _SnowColor ("SnowColor", Color) = (1, 1, 1, 1)
        _Displacement ("Displacement", Range(0, 30)) = 1
        _MaskTex ("MaskTex", 2D) = "black" { }
        _aaa ("TessellationFactor", Range(0.1, 100)) = 0.14
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "LightMode" = "UniversalForward" "RenderType" = "Opaque" "Queue" = "Geometry" }
        
        Pass
        {

            HLSLPROGRAM

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GeometricTools.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Tessellation.hlsl"

            #pragma vertex tessvert
            #pragma fragment frag
            #pragma hull hull
            #pragma domain domain
            
            #pragma target 5.0

            Texture2D _BaseTex;
            SAMPLER(sampler_BaseTex);
            Texture2D _SnowTex;
            SAMPLER(sampler_SnowTex);
            Texture2D _MaskTex;
            SAMPLER(sampler_MaskTex);
            float4 _BaseColor;
            float4 _SnowColor;
            float _Displacement;
            float _aaa;
            

            struct VertexInput
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                //float4 tangent : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct VertexOutput
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : TEXCOORD1;
            };

            //https://docs.microsoft.com/en-us/windows/win32/direct3d11/direct3d-11-advanced-stages-hull-shader-design
            struct TessVertex
            {
                float4 vertex : INTERNALTESSPOS;
                float3 normal : NORMAL;
                //float4 tangent : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct HS_OutputPatchConstant
            {
                float edge[3] : SV_TESSFACTOR;
                float inside : SV_INSIDETESSFACTOR;
                float3 vTangent[4] : TANGENT;
                float2 vUV[4] : TEXCOORD;
                float3 vTanUCorner[4] : TANUCORNER;
                float3 vTanVCorner[4] : TANVCORNER;
                float4 vCWts : TANWEIGHTS;
            };
            
            VertexOutput vert(VertexInput v)//用在domain函数中处理新生成的点
            {
                VertexOutput o;
                o.uv = v.uv;
                o.normal = normalize(mul(v.normal, (float3x3)unity_WorldToObject));
                float4 _MaskTex_var = SAMPLE_TEXTURE2D_LOD(_MaskTex, sampler_MaskTex, o.uv, 0);
                float4 _BaseTex_var = SAMPLE_TEXTURE2D_LOD(_BaseTex, sampler_BaseTex, o.uv, 0);
                v.vertex.xyz -= v.normal * (_BaseTex_var.r + _MaskTex_var.r - 0.7) * _Displacement;
                //v.vertex.xyz += v.normal * (_BaseTex_var.r - _MaskTex_var.r + 0.4) * _Displacement;

                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                return o;
            }

            TessVertex tessvert(VertexInput v)
            {
                TessVertex o;
                o.vertex = v.vertex;
                o.normal = v.normal;
                //o.tangent = v.tangent;
                o.uv = v.uv;
                return o;
            }

            real4 Tessellation(TessVertex v, TessVertex v1, TessVertex v2)
            {
                real minDset = 1.0;
                real maxDest = 25.0;
                real3 triVertexFactors = GetDistanceBasedTessFactor(v.vertex, v1.vertex, v2.vertex, _WorldSpaceCameraPos, minDset, maxDest) * _aaa;
                real4 a = CalcTriTessFactorsFromEdgeTessFactors(triVertexFactors);
                return a;
            }

            HS_OutputPatchConstant hullConst(InputPatch<TessVertex, 3> v)
            {
                HS_OutputPatchConstant o = (HS_OutputPatchConstant)0;
                real4 ts= Tessellation(v[0], v[1], v[2]);
                o.edge[0] = ts.x;
                o.edge[1] = ts.y;
                o.edge[2] = ts.z;
                o.inside = ts.w;
                return o;
            }

            [domain("tri")]//输入hull shader的图元为三角形
            [partitioning("fractional_odd")]//分割方式
            [outputtopology("triangle_cw")]//定义 tessellator 的输出基元类型,cw表示顺时针旋转，ccw表示逆时针
            [patchconstantfunc("hullConst")]//patch常量缓存函数，对于一个patch的每个顶点都会调用该函数一次，计算输出控制点
            [outputcontrolpoints(3)]//决定三个输出控制点
            TessVertex hull(InputPatch<TessVertex,3> v, uint id : SV_OUTPUTCONTROLPOINTID)
            {
                return v[id];
            }
            
            [domain("tri")]
            VertexOutput domain(HS_OutputPatchConstant tessFactors, const OutputPatch<TessVertex,3> vi, float3 bary : SV_DOMAINLOCATION)
            //bary:重心坐标

            {
                VertexInput v = (VertexInput) 0;
                //从重心空间转换到屏幕空间
                v.vertex = vi[0].vertex * bary.x + vi[1].vertex * bary.y + vi[2].vertex * bary.z;
                v.normal = vi[0].normal * bary.x + vi[1].normal * bary.y + vi[2].normal * bary.z;
                //v.tangent = vi[0].tangent * bary.x + vi[1].tangent * bary.y + vi[2].tangent * bary.z;
                v.uv = vi[0].uv * bary.x + vi[1].uv * bary.y + vi[2].uv * bary.z;
                //调用常规意义上的顶点着色器处理函数
                VertexOutput o = vert(v);
                return o;
            }

            float4 frag(VertexOutput o) : SV_Target
            {
                float4 _MaskTex_var = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex,o.uv);
                float4 _BaseTex_var = SAMPLE_TEXTURE2D(_BaseTex, sampler_BaseTex, o.uv) * _BaseColor;
                float4 _SnowColor_var = SAMPLE_TEXTURE2D(_SnowTex, sampler_SnowTex, o.uv) * _SnowColor;
                float4 c = lerp(_SnowColor_var, _BaseTex_var, _MaskTex_var.r);
                return float4(c.xyz, 1);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
