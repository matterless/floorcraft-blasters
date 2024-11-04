// Upgrade NOTE: replaced 'defined USE_NORMAL_MAP' with 'defined (USE_NORMAL_MAP)'

Shader "Unlit/Matcap/VehicleBody"
{
    Properties
    {
        _MainColor ("Color", Color) = (1,1,1,1)
        _DecalColor ("DecalColor", Color) = (1,1,1,1)
        _MainTex ("Albedo", 2D) = "white" {}
        [Toggle(USE_DECAL)] _UseDecal ("Use Decal", Float) = 0
        _DecalTex ("Decal", 2D) = "white" {}
        [Toggle] _AlbedoContribution ("Albedo Contribution", Integer) = 1
        
        //Matcap                
        
        [Toggle(USE_ARRAY)] _USE_ARRAY("Use Texture Array", Float) = 0
        [Toggle(USE_DIFFUSE_MATCAP)] _USE_DIFFUSE_MATCAP("Use Diffuse Matcap", Float) = 1
        [HDR][NoScaleOffset] _DiffuseMatcap ("Diffuse Matcap", 2D) = "white" {}
        [NoScaleOffset] _DiffuseMatcapArray ("Diffuse Matcap Array", 2DArray) = "white" {}
        _DiffuseMatcapIndex ("Matcap Index", Integer) = 0
        [Toggle(USE_SPECULAR_MATCAP)] _USE_SPECULAR_MATCAP("Use Specular Matcap", Float) = 1
        [NoScaleOffset]  _SpecularMatcap ("Specular Matcap", 2D) = "white" {}
        [NoScaleOffset] _SpecularMatcapArray ("Specular Matcap Array", 2DArray) = "white" {}
        _SpecularMatcapIndex ("Matcap Index", Integer) = 0
        
        
        //[NoScaleOffset] _ReflectionMatcap ("Reflection Matcap", 2D) = "white" {}
        _DiffuseMatcapStrength ("Diffuse Strength", Range(0, 2)) = 1.0
        _SpecularMatcapStrength ("Specular Strength", Range(0, 2)) = 1.0
        
        //Normal
        [Toggle(USE_NORMAL_MAP)] _USE_NORMAL_MAP ("Use normal map", Float) = 1 
        [Toggle(USE_DETAIL_NORMAL_MAP)] _USE_DETAIL_NORMAL_MAP ("Use detail normal map", Float) = 0
        
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _DetailNormalMap ("Detail Normal Map", 2D) = "bump" {}
        _DetailNormalMapArray ("Detail Normal Map Array", 2DArray) = "bump" {}
        _DetailNormalMapIndex ("Detail Normal Map Index", Integer) = 0
        _DetailNormalMapStrength ("Detail Normal Map Strength", Range(0, 1)) = 0.5
        [NoScaleOffset] _MaterialMask ("Material Mask", 2D) = "white" {}
        
        _NoiseTexture ("Noise Texture", 2D) = "white" {}
        _Summon ("Summon", Range(0, 1)) = 0.0
}
    SubShader
    {
        Tags { "RenderType" = "TransparentCutout"  "Queue" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        LOD 100
        
        

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog
            #pragma shader_feature USE_NORMAL_MAP
            #pragma shader_feature USE_DETAIL_NORMAL_MAP
            #pragma shader_feature USE_DIFFUSE_MATCAP
            #pragma shader_feature USE_SPECULAR_MATCAP
            #pragma shader_feature USE_ARRAY
            #pragma shader_feature USE_DECAL

            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                
                #if defined(USE_NORMAL_MAP) || defined(USE_DETAIL_NORMAL_MAP)
                    float4 tangent : TANGENT;
                #endif
                
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float2 noiseUV : TEXCOORD7;
                UNITY_FOG_COORDS(1)

                float3 objectPosition : TEXCOORD8;
                float4 vertex : SV_POSITION;
                float3 normal : TEXCOORD1;
                float3 view : TEXCOORD6;
                float3 worldObjectPosition: TEXCOORD9;
                
                #if defined(USE_NORMAL_MAP) || defined(USE_DETAIL_NORMAL_MAP)
                    float3 tspace0 : TEXCCOORD3;
                    float3 tspace1 : TEXCCOORD4;
                    float3 tspace2 : TEXCCOORD5;
                #endif

                #ifdef USE_DETAIL_NORMAL_MAP
                        float2 detailUV : TEXCOORD2;
                #endif
            };

            float4 _MainTex_ST;
            sampler2D _MainTex;

            #ifdef USE_DECAL
                float4 _MainColor;
                float4 _DecalColor;
                sampler2D _DecalTex;
            # endif

            #if defined (USE_DIFFUSE_MATCAP) || defined(USE_SPECULAR_MATCAP)
                float _SpecularMatcapStrength;
                float _DiffuseMatcapStrength;
                #if defined (USE_ARRAY)
                    UNITY_DECLARE_TEX2DARRAY(_DiffuseMatcapArray);
                    UNITY_DECLARE_TEX2DARRAY(_SpecularMatcapArray);
                    uint _DiffuseMatcapIndex;
                    uint _SpecularMatcapIndex;
                #else
                    sampler2D _DiffuseMatcap;
                    sampler2D _SpecularMatcap;
                    sampler2D _ReflectionMatcap;
                #endif
            #endif
            
            #ifdef USE_NORMAL_MAP
                sampler2D _NormalMap;
                
            #endif

            #define ARRAY_DETAIL_NORMAL_MAP
            
            #ifdef USE_DETAIL_NORMAL_MAP
                
                float _DetailNormalMapStrength;
                #ifdef ARRAY_DETAIL_NORMAL_MAP
                    UNITY_DECLARE_TEX2DARRAY(_DetailNormalMapArray);
                    uint _DetailNormalMapIndex;
                    float4 _DetailNormalMapArray_ST;
                #else
                    sampler2D _DetailNormalMap;
                    float4 _DetailNormalMap_ST;
                #endif   
            #endif
            
            
            float _AlbedoContribution;
            sampler2D _MaterialMask;
            sampler2D _NoiseTexture;
            float4 _NoiseTexture_ST;

            float _Summon;


            v2f vert (appdata v)
            {
                v2f o;
                o.noiseUV = TRANSFORM_TEX(v.uv, _NoiseTexture);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.objectPosition = v.vertex.xyz;
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.view = normalize(UnityObjectToViewPos(v.vertex.xyz));
                o.worldObjectPosition = mul(unity_ObjectToWorld, float4(0,0,0,1)).xyz;
                
                
                #if defined(USE_NORMAL_MAP) || defined(USE_DETAIL_NORMAL_MAP)
                    float3 wTangent = UnityObjectToWorldDir(v.tangent.xyz);
                    float tangentSign = v.tangent.w * unity_WorldTransformParams.w;
                    float3 wBitangent = cross(o.normal, wTangent) * tangentSign;
                    o.tspace0 = float3(wTangent.x, wBitangent.x, o.normal.x);
                    o.tspace1 = float3(wTangent.y, wBitangent.y, o.normal.y);
                    o.tspace2 = float3(wTangent.z, wBitangent.z, o.normal.z);
                #endif
                
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                #ifdef USE_DETAIL_NORMAL_MAP
                    #ifdef ARRAY_DETAIL_NORMAL_MAP
                        o.detailUV = TRANSFORM_TEX(v.uv, _DetailNormalMapArray);
                    #else
                        o.detailUV = TRANSFORM_TEX(v.uv, _DetailNormalMap);
                    #endif
                #endif
                
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            #define EPSILON 1.192092896e-07

            #ifdef USE_DETAIL_NORMAL_MAP
                float3 SurfgradFromPerturbedNormal (float3 baseNormal, float3 v)
                {
                    float k = dot(baseNormal, v);
                    return (k * baseNormal - v)/max(EPSILON, abs(k));
                }
            #endif

            inline float3 InverseTransform(float3 v, float3x3 m)
            {
                return normalize(float3(dot(v, m[0]), dot(v, m[1]), dot(v, m[2])));
            }
            
            //make three input  vectors orthogonal
            inline void Orthogonalize (inout float3 v1, inout float3 v2, inout float3 v3)
            {
                float3 a = cross((v1), v2);
                float3 b = cross((v1), v3);
                float3 c = cross(a, b);
                v1 = c;
                v2 = cross(a, c);
                v3 = cross(v2, c);
            }
            
            float2 GetMatcapUV(float3 normal, float3 position, float3 cameraPos)
            {
                float3 viewDir = normalize(position - cameraPos);
                float3 right = normalize(float3(-viewDir.z, 0, viewDir.x)); 
                float3 up = normalize(cross(viewDir, right));
                
                //Orthogonalize(right, up, viewDir);

                float3 transposed = InverseTransform(normal, float3x3(right, up, viewDir));
                return float2(-transposed.x, -transposed.y);;
            }

            //remap function using min max
            float Remap(float value, float minOld, float maxOld, float minNew, float maxNew)
            {
                return minNew + (value - minOld) * (maxNew - minNew) / (maxOld - minOld);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 baseNormal = normalize(i.normal);
                float3 worldNormal = baseNormal;

                #ifdef USE_DECAL
                    float decalMask = tex2D(_DecalTex, i.uv).a;
                #endif
                
                
                #ifdef USE_NORMAL_MAP
                    float3 tnormal = UnpackNormal(tex2D(_NormalMap, i.uv));
                    worldNormal.x = dot(i.tspace0, tnormal);
                    worldNormal.y = dot(i.tspace1, tnormal);
                    worldNormal.z = dot(i.tspace2, tnormal);
                    worldNormal = normalize(worldNormal);
                #endif

                #ifdef USE_DETAIL_NORMAL_MAP
                    #ifdef ARRAY_DETAIL_NORMAL_MAP
                        float3 tdetailNormal = (UNITY_SAMPLE_TEX2DARRAY(_DetailNormalMapArray, float4(i.detailUV, _DetailNormalMapIndex, 0))).ywz * 2 - 1;
                        tdetailNormal.z = sqrt(max(0, 1 - dot(tdetailNormal.xy, tdetailNormal.xy)));
                    #else
                        float3 tdetailNormal = UnpackNormal(tex2D (_DetailNormalMap, i.detailUV));
                    #endif
                
                    float3 perturebedDetailNormal;
                    perturebedDetailNormal.x = dot(i.tspace0, tdetailNormal);
                    perturebedDetailNormal.y = dot(i.tspace1, tdetailNormal);
                    perturebedDetailNormal.z = dot(i.tspace2, tdetailNormal);
                    float3 baseSurfgrad = SurfgradFromPerturbedNormal(baseNormal, worldNormal);
                    float materialMask = tex2D(_MaterialMask, i.uv).a;
                    float3 detailSurfgrad = SurfgradFromPerturbedNormal(baseNormal, perturebedDetailNormal) * _DetailNormalMapStrength * materialMask;
                    #ifdef USE_DECAL
                    detailSurfgrad *= 1 - decalMask;
                    #endif
                
                    detailSurfgrad += baseSurfgrad; 
                    
                    worldNormal = normalize(baseNormal - detailSurfgrad);
                #endif

                #define USE_ADVANCED_MATCAP_0
                
                #if defined(USE_DIFFUSE_MATCAP) || defined(USE_SPECULAR_MATCAP)
                    float3 viewSpaceNormal = normalize(mul((float3x3) UNITY_MATRIX_V, worldNormal).xyz);
                    #if defined(USE_ADVANCED_MATCAP_1)
                        float3 c = cross(normalize(i.view), viewSpaceNormal);
                        viewSpaceNormal = float3(-c.y, c.x, 0);
                    #endif
                    #if defined(USE_ADVANCED_MATCAP_2)
                        
                        viewSpaceNormal.xy = GetMatcapUV(worldNormal, i.worldObjectPosition, _WorldSpaceCameraPos);
                    #endif
                    float2 matCapUV = viewSpaceNormal.xy * 0.5 + 0.5;
                
                #endif
    
                //float2 clearCoatUV;
                //clearCoatUV.x = dot(normalize(UNITY_MATRIX_IT_MV[0].xyz), baseNormal);
				//clearCoatUV.y = dot(normalize(UNITY_MATRIX_IT_MV[1].xyz), baseNormal);
				//clearCoatUV.xy = clearCoatUV.xy * 0.5 + 0.5;

                #ifdef USE_DECAL
                    fixed4 col = lerp(_MainColor, _DecalColor, decalMask);
                #else
                    fixed4 col = lerp(float4(1, 1, 1, 1), tex2D(_MainTex, i.uv), _AlbedoContribution);
                #endif
                
                #if defined (USE_DIFFUSE_MATCAP)
                    #ifdef USE_ARRAY
                        float diffuseMatcap = UNITY_SAMPLE_TEX2DARRAY(_DiffuseMatcapArray, float4(matCapUV, _DiffuseMatcapIndex, 0)).a ;
                    #else
                        float diffuseMatcap = tex2D(_DiffuseMatcap, matCapUV).a;
                    #endif
                    col = lerp(col, col * diffuseMatcap, _DiffuseMatcapStrength);
                #endif
                
                #ifdef USE_SPECULAR_MATCAP
                    #ifdef USE_ARRAY
                        col = saturate(col + UNITY_SAMPLE_TEX2DARRAY(_SpecularMatcapArray, float4(matCapUV, _SpecularMatcapIndex, 0)).a * _SpecularMatcapStrength);
                    #else
                        col.xyz += tex2D(_SpecularMatcap, matCapUV).a * _SpecularMatcapStrength;
                        //col = saturate(col + tex2D(_ReflectionMatcap, clearCoatUV).a * 0.5);
                    #endif
                #endif
                
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                float noise =  tex2D(_NoiseTexture, i.noiseUV * 2.5 - float2(0, _Time.y * 0.4)).r * 0.5  * tex2D(_NoiseTexture, i.noiseUV * 0.6 - float2(0, _Time.y * 0.4)).g * 2.0 - 1.0;
                noise += 1.0 - i.objectPosition.y + _Summon * 6.0 - 2.0;
                col.xyz += saturate(1 - noise) * 5;
                col.a = saturate(noise);
                // col.rg = GetMatcapUV(baseNormal, float3(0, 0, 0), _WorldSpaceCameraPos).xy;
                // col.b = 0;
                return col;
            }
            ENDCG
        }
        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull[_Cull]

            HLSLPROGRAM
            #pragma exclude_renderers gles gles3 glcore
            #pragma target 4.5

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            // -------------------------------------
            // Universal Pipeline keywords

            // This is used during shadow map generation to differentiate between directional and punctual light shadows, as they use different formulas to apply Normal Bias
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
    }
}
