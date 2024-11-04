Shader "Unlit/SHC_RF_Outline"
{
    Properties
    {
        _Thickness ("Thickness", Range(0, 30)) = 1.0
        _Color ("Color", Color) = (1,1,1,1)
        _DepthOffset ("Depth Offset", Range(0, 1)) = 0.0
        
    }
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalRenderPipeline"
        }


        Pass
        {
            Name "Outlines"
            Cull Front
            
            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float3 smoothNormalOS : TEXCOORD3;
            };

            struct Varyings 
            {
                float4 positionCS : SV_POSITION;
                
            };

            float _Thickness;
            float _DepthOffset;
            half4 _Color;

            Varyings vert (Attributes input)
            {
                Varyings output = (Varyings)0;
               
                float4 positionCS = TransformObjectToHClip(input.positionOS.xyz);
                float3 positionWS = mul(UNITY_MATRIX_M, input.positionOS).xyz;
                float3 normalCS = TransformWorldToHClipDir(TransformObjectToWorldNormal(input.smoothNormalOS ));
                //Distance From Camera
                float distance = length(_WorldSpaceCameraPos - mul(unity_ObjectToWorld, float4(0,0,0,1)).xyz);
                //modify the thickness based on distance
                float thickness = lerp(_Thickness, 2, saturate(distance - 0.3)); //At 0.3m, thickness is 2
                float2 offset = normalize(normalCS.xy) / _ScreenParams.xy * thickness * positionCS.w * 2.0;

                positionCS.xy += offset;
                positionCS.z -= _DepthOffset;

                output.positionCS = positionCS;
                
                return output;
            }
            
            half4 frag (Varyings input) : SV_Target
            {
                return  _Color;
            }
                
            ENDHLSL
        }
    }
}
