// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Matterless/Unlit/Laser"{
  //show values to edit in inspector
  Properties{
    [HDR] _Color ("Tint", Color) = (0, 0, 0, 1)
    [HDR] _TrailColor ("Trail Color", Color) = (0, 0, 0, 1)
    _NoiseTexture ("Noise Texture", 2D) = "white" {}
    _DissolveTexture ("Dissolve Texture", 2D) = "white" {}
    _Float ("Position", Range(0, 1)) = 0.5
  }

  SubShader{
    Tags 
        { 
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent" 
        }
    
    Blend SrcAlpha OneMinusSrcAlpha
    //Cull Off

    Pass{
      CGPROGRAM

      #include "UnityCG.cginc"
      #pragma vertex vert
      #pragma fragment frag
      // tex2Dlod in the vertex shader
      #pragma target 3.0


      fixed4 _Color;
      float _Float;

      // LaserBeamView draws one mesh holding a copy of the segment mesh per segment.
      // Vertex shaders here never read compute buffers: Mali GPUs on OpenGL ES do not support that.
      struct appdata
      {
          float4 vertex : POSITION;
          float3 normal : NORMAL;
          float2 uv : TEXCOORD0;
          // x: index of the segment copy this vertex belongs to
          float2 segment : TEXCOORD1;
      };

      struct v2f
      {
          float4 vertex   : SV_POSITION;
          float2 uv : TEXCOORD0;
          float3 objectPosition : TEXCOORD1;
          float3 normal  : TEXCOORD2;
          UNITY_VERTEX_OUTPUT_STEREO
      };

      sampler2D _NoiseTexture;
      //float4 _NoiseTexture_TexelSize;
      sampler2D _DissolveTexture;
      float _Position;
      float4x4 _LocalToWorld;
      float _Offset;
      float _Decay;
      float _TextureOffset;
      float _Distance;
      float4 _TrailColor;
      float _SegmentCount;

      //the vertex shader function
      v2f vert(appdata v)
      {
        v2f o;
        o.normal = v.normal;
        o.uv = v.uv;

        float3 position = v.vertex.xyz;
        float segment = v.segment.x;

        //Initial position with offsets
        position.z *= _Offset;
        position += float3(0, 0, segment * _Offset);


        // Shrink
        position.xy -= o.normal.xy * 0.5f * _Decay;
        // Taper
        position.xy *= saturate(position.z * 1 );

        //Noise displacement
        float3 noise = tex2Dlod(_NoiseTexture, float4(float2(position.z * 0.2f, _TextureOffset), 0, 0)).xyx * 2 - 1;
        position.xy += sin(position.z * 3 + 16 * _TextureOffset * 3.14) * 1.5 * _Decay;
        position.xy += noise.xy * _Decay * 4;// * (_Distance - position.z > 0.5);
        
        o.objectPosition = position;
        
        //To World Space
        position = mul(_LocalToWorld, float4(position, 1)).xyz;
        //To Clip Space
        o.vertex = UnityObjectToClipPos(float4(position, 1));

        // Only the first _SegmentCount segments are drawn: collapse the rest outside the view
        if (segment >= _SegmentCount)
            o.vertex = float4(2, 2, 2, 1);

        // //convert the vertex position from world space to clip space
        // return mul(UNITY_MATRIX_VP, float4(position, 1));
        return o;
      }

      //the fragment shader function
      fixed4 frag(v2f IN) : SV_TARGET
      {
        fixed4 col = _TrailColor;
        fixed noise = tex2D(_NoiseTexture, float2(IN.objectPosition.z - _Time.y * 0.01, _TextureOffset + _Time.y * 0.05)).x;
        float n = tex2D(_DissolveTexture, float2(IN.objectPosition.z * 0.1 - _Time.y * 2, IN.uv.x - _Time.y * 10.0 )).rgb;
        col = lerp(_Color * step(n, lerp(_Float, 1, _Decay)), col, saturate(_Decay * (_Distance - IN.objectPosition.z) * 2.0)) ;
        col.a = lerp(1.0f,saturate(noise), saturate((_Decay * _Distance - IN.objectPosition.z) * 2.0));

        if( col.a < 0.01)
            discard;
    
        //col.rgb = saturate((_Decay * _Distance - IN.objectPosition.z) * 2.0);
        
        return col;
      }

      ENDCG
    }
  }
  Fallback "VertexLit"
}