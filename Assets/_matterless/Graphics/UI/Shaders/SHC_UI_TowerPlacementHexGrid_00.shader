Shader "Unlit/Matterless/TowerPlacement"
{
    Properties
    {
        [NoScaleOffset] _MainTex ("Albedo (RGB)", 2D) = "white" {}
        [NoScaleOffset] _SummonNoise ("Summon Noise Texture", 2D) = "white" {}
        [HDR] _DefaultColor ("Default Color", Color) = (0, 0, 0, 0)
        [HDR] _CanceledColor ("Canceled Color", Color) = (0, 0, 0, 0)
        _SummonIsDespawnIsCancelled ("Summon IsDespawn IsCancelled", Vector) = (0, 0, 0)
}
    
    SubShader
    {
        
        Tags 
        { 
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"  
            "Queue" = "Geometry" 
        }
        
        Blend One OneMinusSrcAlpha
        Cull Off
        ZWrite On   
        ZTest LEqual
        
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"
            
            struct Attributes
            {
                float2 texcoord : TEXCOORD0;
                float2 frontTexcoord : TEXCOORD1;
                float2 sideTexcoord : TEXCOORD2;
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD1;
                half4 color : COLOR;
            };

            sampler _MainTex;
            sampler _SummonNoise;

            CBUFFER_START(UnityPerMaterial)
                float3 _SummonIsDespawnIsCancelled;
                float3 _DefaultColor;
                float3 _CanceledColor;
            CBUFFER_END

            float3 Unity_RotateAboutAxis_Radians_float(float3 In, float3 Axis, float Rotation)
            {
                float s = sin(Rotation);
                float c = cos(Rotation);
                float one_minus_c = 1.0 - c;

                Axis = normalize(Axis);
                float3x3 rot_mat = 
                {   one_minus_c * Axis.x * Axis.x + c, one_minus_c * Axis.x * Axis.y - Axis.z * s, one_minus_c * Axis.z * Axis.x + Axis.y * s,
                    one_minus_c * Axis.x * Axis.y + Axis.z * s, one_minus_c * Axis.y * Axis.y + c, one_minus_c * Axis.y * Axis.z - Axis.x * s,
                    one_minus_c * Axis.z * Axis.x - Axis.y * s, one_minus_c * Axis.y * Axis.z + Axis.x * s, one_minus_c * Axis.z * Axis.z + c
                };
                return mul(rot_mat,  In);
            }

            float2 GetPolarCoordinates(float2 UV, float2 Center, float RadialScale, float LengthScale)
            {
                float2 delta = UV - Center;
                float radius = length(delta) * 2 * RadialScale;
                float angle = atan2(delta.x, delta.y) * 1.0/6.28 * LengthScale;
                return float2(radius, angle);
            }

            #define EXTENTS float3(24.8471, 1, 22.9) * 0.5f
            #define PHASE0_HEIGHT 10.f
            #define SUMMON _SummonIsDespawnIsCancelled.x
            #define IS_DESPAWN _SummonIsDespawnIsCancelled.y
            #define IS_CANCELLED _SummonIsDespawnIsCancelled.z

            
            ///////////////////////////////////////////////////////////////////////////////
            //                  Vertex and Fragment functions                            //
            ///////////////////////////////////////////////////////////////////////////////
            Varyings vert (Attributes input)
            {
                Varyings output = (Varyings)0;
                
                float3 positionOS = input.positionOS.xyz;
                float3 bakedPivot = (float3(input.frontTexcoord.x, 0.5, input.frontTexcoord.y) * 2.0 - 1.0) * float3(-1, 1, -1) * EXTENTS;

                float noise = tex2Dlod(_SummonNoise, float4(input.frontTexcoord, 0, 0)).r;

                const float phase0 = saturate(SUMMON * 1.5f);
                const float phase1 = saturate(SUMMON * 3.0f - 2.f);
                const float invPhase0 = 1.f - phase0;

                // Move to pivot space
                float3 pivot = (positionOS - bakedPivot);

                // polar coordinates                
                float2 polar = GetPolarCoordinates(input.frontTexcoord, float2(.5f, .5f), 1, 1);
                float polarGradient = 1.f - polar.y - .49f;

                /////////////////////
                /// Summon Phase 0 //
                /////////////////////
                
                // Scale
                float3 phase0Pivot = pivot;
                phase0Pivot.xz *= 1.f + abs(frac(polarGradient) * 10.f) * (1.f - phase0);

                // Rotate
                phase0Pivot = Unity_RotateAboutAxis_Radians_float(phase0Pivot, float3(0.f, 1.f, 0.f), invPhase0 * 6.f);

                // Height
                phase0Pivot.y = (polarGradient * 10.f + 20.0f) * polar.x * invPhase0 + noise * invPhase0 * PHASE0_HEIGHT;

                pivot = lerp(phase0Pivot, pivot, IS_DESPAWN);
                
                //Color
                float phase0Color =  1 - frac(polar.y - .49f + phase0 ) * invPhase0;
                phase0Color = lerp(1, phase0Color, IS_DESPAWN);

                /////////////////////
                /// Summon Phase 1 //
                /////////////////////
                // Expanding Circle
                float circle = 0.5 + (1 - abs(smoothstep(phase1 * 2.5f - .2f, phase1 * 2.5f + .2f, polar.x * 2.0f) - .5f)); 
                
                // Scale/Height
                pivot.xz *=  circle;
                pivot.y += circle - 1;
                
                //Color
                float phase1Color = lerp(0,(1 - abs(smoothstep(phase1 + .1f, phase1 - .1f, polar.x - .2f))) * 50.f, phase1) * .1f;


                /////////////////////
                /// Despawn Phase  //
                /////////////////////
                
                // Scale/Hight
                float3 despawnPhasePivot = pivot;
                
                despawnPhasePivot.y += noise * 40.f * invPhase0;
                despawnPhasePivot.xz *= phase0;

                pivot = lerp(pivot, despawnPhasePivot, IS_DESPAWN);

                // Color
                float despawnColor = invPhase0 * 10 * IS_DESPAWN; 

                /////////////////
                ///  Output  ///
                ///////////////
                
                output.color.x = phase0Color * 1.2f + phase1Color + despawnColor;
                output.color.y = saturate(1 - polar.x * 0.8);
                output.color.zw = float2(phase0, phase1);
                
                // Move back to object space
                pivot += bakedPivot;
                
                // Phase0 culling
                pivot = (polar.y + phase0 * 1.2f - 0.1f > 0.5f) + IS_DESPAWN ?  pivot : pivot/0;
                output.positionCS = TransformObjectToHClip(pivot);

                output.positionOS = pivot;
                output.uv = input.texcoord;
                
                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                
                float2 DMask = tex2D(_MainTex, input.uv).rg;
                float mask = 1 - DMask.y;
                float inner = smoothstep(0.3, 1.0, DMask.x) * 0.2;
                float outline = 1 - smoothstep(0.32, 0.65, DMask.x) * mask;
                
                half4 col;
                col.rgb = lerp(_DefaultColor, _CanceledColor, IS_CANCELLED * (1 - input.color.w));
                col.xyz *= saturate(inner + outline) * input.color.x * input.color.y;
                col.a = input.color.x * input.color.z * 0.5f * input.color.y;
                
                return col;
            }
            ENDHLSL
        }
    }
}
