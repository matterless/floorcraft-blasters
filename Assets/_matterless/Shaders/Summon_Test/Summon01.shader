// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Summon01"
{
	Properties
	{
		_Cutoff( "Mask Clip Value", Float ) = 0.5
		_Diffuse("Diffuse", 2D) = "white" {}
		_Summon_Amount("Summon_Amount", Range( -1 , 5)) = 0
		_4_Summon_contrast("4_Summon_contrast", Float) = 1
		_Noise_Tiling01("Noise_Tiling01", Vector) = (5,5,0,0)
		_Noise_Speed01("Noise_Speed01", Range( 1 , 2)) = 1
		_Noise_Tiling02("Noise_Tiling02", Vector) = (500,500,0,0)
		_Emission("Emission", Range( 1 , 10)) = 5
		_Diffuse_Brightness("Diffuse_Brightness", Range( 1 , 3)) = 1
		_Noise_Speed02("Noise_Speed02", Range( 1 , 5)) = 5
		_Diffuse_Saturation("Diffuse_Saturation", Range( 1 , 3)) = 1
		_Diffuse_Tint("Diffuse_Tint", Color) = (1,1,1,0)
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "TransparentCutout"  "Queue" = "AlphaTest+0" "IsEmissive" = "true"  }
		Cull Off
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#pragma target 3.0
		#pragma exclude_renderers xboxseries playstation switch nomrt 
		#pragma surface surf Standard keepalpha addshadow fullforwardshadows 
		struct Input
		{
			float2 uv_texcoord;
			float3 worldPos;
		};

		uniform float4 _Diffuse_Tint;
		uniform float _Diffuse_Brightness;
		uniform sampler2D _Diffuse;
		uniform float4 _Diffuse_ST;
		uniform float _Diffuse_Saturation;
		uniform float _Summon_Amount;
		uniform float2 _Noise_Tiling01;
		uniform float _Noise_Speed01;
		uniform float2 _Noise_Tiling02;
		uniform float _Noise_Speed02;
		uniform float _4_Summon_contrast;
		uniform float _Emission;
		uniform float _Cutoff = 0.5;


		float3 HSVToRGB( float3 c )
		{
			float4 K = float4( 1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0 );
			float3 p = abs( frac( c.xxx + K.xyz ) * 6.0 - K.www );
			return c.z * lerp( K.xxx, saturate( p - K.xxx ), c.y );
		}


		float3 RGBToHSV(float3 c)
		{
			float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
			float4 p = lerp( float4( c.bg, K.wz ), float4( c.gb, K.xy ), step( c.b, c.g ) );
			float4 q = lerp( float4( p.xyw, c.r ), float4( c.r, p.yzx ), step( p.x, c.r ) );
			float d = q.x - min( q.w, q.y );
			float e = 1.0e-10;
			return float3( abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
		}

		float3 mod2D289( float3 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }

		float2 mod2D289( float2 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }

		float3 permute( float3 x ) { return mod2D289( ( ( x * 34.0 ) + 1.0 ) * x ); }

		float snoise( float2 v )
		{
			const float4 C = float4( 0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439 );
			float2 i = floor( v + dot( v, C.yy ) );
			float2 x0 = v - i + dot( i, C.xx );
			float2 i1;
			i1 = ( x0.x > x0.y ) ? float2( 1.0, 0.0 ) : float2( 0.0, 1.0 );
			float4 x12 = x0.xyxy + C.xxzz;
			x12.xy -= i1;
			i = mod2D289( i );
			float3 p = permute( permute( i.y + float3( 0.0, i1.y, 1.0 ) ) + i.x + float3( 0.0, i1.x, 1.0 ) );
			float3 m = max( 0.5 - float3( dot( x0, x0 ), dot( x12.xy, x12.xy ), dot( x12.zw, x12.zw ) ), 0.0 );
			m = m * m;
			m = m * m;
			float3 x = 2.0 * frac( p * C.www ) - 1.0;
			float3 h = abs( x ) - 0.5;
			float3 ox = floor( x + 0.5 );
			float3 a0 = x - ox;
			m *= 1.79284291400159 - 0.85373472095314 * ( a0 * a0 + h * h );
			float3 g;
			g.x = a0.x * x0.x + h.x * x0.y;
			g.yz = a0.yz * x12.xz + h.yz * x12.yw;
			return 130.0 * dot( m, g );
		}


		struct Gradient
		{
			int type;
			int colorsLength;
			int alphasLength;
			float4 colors[8];
			float2 alphas[8];
		};


		Gradient NewGradient(int type, int colorsLength, int alphasLength, 
		float4 colors0, float4 colors1, float4 colors2, float4 colors3, float4 colors4, float4 colors5, float4 colors6, float4 colors7,
		float2 alphas0, float2 alphas1, float2 alphas2, float2 alphas3, float2 alphas4, float2 alphas5, float2 alphas6, float2 alphas7)
		{
			Gradient g;
			g.type = type;
			g.colorsLength = colorsLength;
			g.alphasLength = alphasLength;
			g.colors[ 0 ] = colors0;
			g.colors[ 1 ] = colors1;
			g.colors[ 2 ] = colors2;
			g.colors[ 3 ] = colors3;
			g.colors[ 4 ] = colors4;
			g.colors[ 5 ] = colors5;
			g.colors[ 6 ] = colors6;
			g.colors[ 7 ] = colors7;
			g.alphas[ 0 ] = alphas0;
			g.alphas[ 1 ] = alphas1;
			g.alphas[ 2 ] = alphas2;
			g.alphas[ 3 ] = alphas3;
			g.alphas[ 4 ] = alphas4;
			g.alphas[ 5 ] = alphas5;
			g.alphas[ 6 ] = alphas6;
			g.alphas[ 7 ] = alphas7;
			return g;
		}


		float4 SampleGradient( Gradient gradient, float time )
		{
			float3 color = gradient.colors[0].rgb;
			UNITY_UNROLL
			for (int c = 1; c < 8; c++)
			{
			float colorPos = saturate((time - gradient.colors[c-1].w) / ( 0.00001 + (gradient.colors[c].w - gradient.colors[c-1].w)) * step(c, (float)gradient.colorsLength-1));
			color = lerp(color, gradient.colors[c].rgb, lerp(colorPos, step(0.01, colorPos), gradient.type));
			}
			#ifndef UNITY_COLORSPACE_GAMMA
			color = half3(GammaToLinearSpaceExact(color.r), GammaToLinearSpaceExact(color.g), GammaToLinearSpaceExact(color.b));
			#endif
			float alpha = gradient.alphas[0].x;
			UNITY_UNROLL
			for (int a = 1; a < 8; a++)
			{
			float alphaPos = saturate((time - gradient.alphas[a-1].y) / ( 0.00001 + (gradient.alphas[a].y - gradient.alphas[a-1].y)) * step(a, (float)gradient.alphasLength-1));
			alpha = lerp(alpha, gradient.alphas[a].x, lerp(alphaPos, step(0.01, alphaPos), gradient.type));
			}
			return float4(color, alpha);
		}


		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float2 uv_Diffuse = i.uv_texcoord * _Diffuse_ST.xy + _Diffuse_ST.zw;
			float3 hsvTorgb211 = RGBToHSV( tex2D( _Diffuse, uv_Diffuse ).rgb );
			float3 hsvTorgb214 = HSVToRGB( float3(hsvTorgb211.x,( _Diffuse_Saturation * hsvTorgb211.y ),hsvTorgb211.z) );
			o.Albedo = ( _Diffuse_Tint * float4( ( _Diffuse_Brightness * hsvTorgb214 ) , 0.0 ) ).rgb;
			float mulTime153 = _Time.y * _Noise_Speed01;
			float2 panner151 = ( mulTime153 * float2( 0,-1 ) + float2( 0,0 ));
			float2 uv_TexCoord140 = i.uv_texcoord * _Noise_Tiling01 + panner151;
			float simplePerlin2D139 = snoise( uv_TexCoord140 );
			simplePerlin2D139 = simplePerlin2D139*0.5 + 0.5;
			float mulTime218 = _Time.y * _Noise_Speed02;
			float2 panner220 = ( mulTime218 * float2( 0,-1 ) + float2( 0,0 ));
			float2 uv_TexCoord221 = i.uv_texcoord * _Noise_Tiling02 + panner220;
			float simplePerlin2D222 = snoise( uv_TexCoord221 );
			simplePerlin2D222 = simplePerlin2D222*0.5 + 0.5;
			float3 ase_vertex3Pos = mul( unity_WorldToObject, float4( i.worldPos , 1 ) );
			float temp_output_73_0 = ( (-0.6 + (( 1.0 - _Summon_Amount ) - 0.0) * (0.6 - -0.6) / (1.0 - 0.0)) + ( ( simplePerlin2D139 * ( simplePerlin2D222 / 2.0 ) ) + ( ase_vertex3Pos.y / _4_Summon_contrast ) ) );
			float clampResult113 = clamp( (-4.0 + (temp_output_73_0 - 0.0) * (4.0 - -4.0) / (1.0 - 0.0)) , 0.0 , 1.0 );
			float temp_output_130_0 = ( 1.0 - clampResult113 );
			float4 temp_cast_3 = (temp_output_130_0).xxxx;
			Gradient gradient175 = NewGradient( 0, 4, 2, float4( 0, 0, 0, 0 ), float4( 1, 0, 0, 0.1441215 ), float4( 0.1150529, 1, 0, 0.5353017 ), float4( 0, 0.7968903, 1, 1 ), 0, 0, 0, 0, float2( 1, 0 ), float2( 1, 1 ), 0, 0, 0, 0, 0, 0 );
			float4 lerpResult206 = lerp( temp_cast_3 , SampleGradient( gradient175, i.uv_texcoord.y ) , temp_output_130_0);
			o.Emission = ( lerpResult206 * _Emission ).rgb;
			o.Alpha = 1;
			clip( temp_output_73_0 - _Cutoff );
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18935
313;73;1714;753;-468.1093;-296.3167;1;True;True
Node;AmplifyShaderEditor.RangedFloatNode;217;-3438.163,793.028;Inherit;False;Property;_Noise_Speed02;Noise_Speed02;9;0;Create;True;0;0;0;False;0;False;5;5;1;5;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;154;-3063.909,344.9862;Inherit;False;Property;_Noise_Speed01;Noise_Speed01;5;0;Create;True;0;0;0;False;0;False;1;0.5;1;2;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;218;-3148.161,790.028;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;153;-2773.908,341.9862;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;220;-2950.161,759.0278;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,-1;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;219;-2952.024,624.9277;Inherit;False;Property;_Noise_Tiling02;Noise_Tiling02;6;0;Create;True;0;0;0;False;0;False;500,500;500,500;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.TextureCoordinatesNode;221;-2736.09,672.0551;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.PannerNode;151;-2575.908,310.986;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,-1;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;150;-2577.771,176.8859;Inherit;False;Property;_Noise_Tiling01;Noise_Tiling01;4;0;Create;True;0;0;0;False;0;False;5,5;20,20;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.TextureCoordinatesNode;140;-2361.837,224.0135;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;225;-2225.12,768.8723;Inherit;False;Constant;_Float0;Float 0;12;0;Create;True;0;0;0;False;0;False;2;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;222;-2486.14,676.2597;Inherit;True;Simplex2D;True;False;2;0;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;224;-2040.741,662.4609;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;139;-2111.887,228.218;Inherit;True;Simplex2D;True;False;2;0;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;4;-1230.638,266.4218;Float;False;Property;_Summon_Amount;Summon_Amount;2;0;Create;True;0;0;0;False;0;False;0;0.19;-1;5;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;137;-1350.547,1058.185;Inherit;False;Property;_4_Summon_contrast;4_Summon_contrast;3;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.PosVertexDataNode;133;-1365.068,865.2787;Inherit;True;0;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleDivideOpNode;138;-1104.85,882.4697;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;226;-1120.632,537.793;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;71;-957.4672,267.2677;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;111;-790.0214,266.0522;Inherit;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;-0.6;False;4;FLOAT;0.6;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;158;-735.4394,657.1569;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;73;-374.8684,499.7705;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;78;-134.8806,-381.0206;Inherit;True;Property;_Diffuse;Diffuse;1;0;Create;True;0;0;0;False;0;False;-1;None;91b6d7d634ab1294e844f34c54f91a9f;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;1;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TFHCRemapNode;112;-75.33077,271.7869;Inherit;True;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;-4;False;4;FLOAT;4;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;213;166.3903,-91.48246;Inherit;False;Property;_Diffuse_Saturation;Diffuse_Saturation;10;0;Create;True;0;0;0;False;0;False;1;1.5;1;3;0;1;FLOAT;0
Node;AmplifyShaderEditor.RGBToHSVNode;211;198.012,-282.5984;Inherit;False;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.ClampOpNode;113;234.4404,186.6442;Inherit;True;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.GradientNode;175;616.3316,655.0132;Inherit;False;0;4;2;0,0,0,0;1,0,0,0.1441215;0.1150529,1,0,0.5353017;0,0.7968903,1,1;1,0;1,1;0;1;OBJECT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;178;606.8574,755.1637;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;212;526.126,-173.4485;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;130;482.8329,230.2112;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.HSVToRGBNode;214;770.9212,-290.189;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.GradientSampleNode;177;856.6867,671.0276;Inherit;True;2;0;OBJECT;;False;1;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;209;707.7952,-389.2068;Inherit;False;Property;_Diffuse_Brightness;Diffuse_Brightness;8;0;Create;True;0;0;0;False;0;False;1;1;1;3;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;215;1055.238,-495.33;Inherit;False;Property;_Diffuse_Tint;Diffuse_Tint;11;0;Create;True;0;0;0;False;0;False;1,1,1,0;1,1,1,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;208;1260.071,782.9722;Inherit;False;Property;_Emission;Emission;7;0;Create;True;0;0;0;False;0;False;5;5;1;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;206;1263.883,502.3256;Inherit;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;210;1052.038,-314.8262;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;216;1314.661,-330.0649;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;FLOAT3;0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;207;1575.071,620.9722;Inherit;True;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;2302.691,123.0738;Float;False;True;-1;2;ASEMaterialInspector;0;0;Standard;Summon01;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Off;0;False;-1;3;False;-1;False;0;False;-1;0;False;-1;False;0;Masked;0.5;True;True;0;False;TransparentCutout;;AlphaTest;All;14;d3d9;d3d11_9x;d3d11;glcore;gles;gles3;metal;vulkan;xbox360;xboxone;ps4;psp2;n3ds;wiiu;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;0;4;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;1;False;-1;1;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;True;Relative;0;;0;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;218;0;217;0
WireConnection;153;0;154;0
WireConnection;220;1;218;0
WireConnection;221;0;219;0
WireConnection;221;1;220;0
WireConnection;151;1;153;0
WireConnection;140;0;150;0
WireConnection;140;1;151;0
WireConnection;222;0;221;0
WireConnection;224;0;222;0
WireConnection;224;1;225;0
WireConnection;139;0;140;0
WireConnection;138;0;133;2
WireConnection;138;1;137;0
WireConnection;226;0;139;0
WireConnection;226;1;224;0
WireConnection;71;0;4;0
WireConnection;111;0;71;0
WireConnection;158;0;226;0
WireConnection;158;1;138;0
WireConnection;73;0;111;0
WireConnection;73;1;158;0
WireConnection;112;0;73;0
WireConnection;211;0;78;0
WireConnection;113;0;112;0
WireConnection;212;0;213;0
WireConnection;212;1;211;2
WireConnection;130;0;113;0
WireConnection;214;0;211;1
WireConnection;214;1;212;0
WireConnection;214;2;211;3
WireConnection;177;0;175;0
WireConnection;177;1;178;2
WireConnection;206;0;130;0
WireConnection;206;1;177;0
WireConnection;206;2;130;0
WireConnection;210;0;209;0
WireConnection;210;1;214;0
WireConnection;216;0;215;0
WireConnection;216;1;210;0
WireConnection;207;0;206;0
WireConnection;207;1;208;0
WireConnection;0;0;216;0
WireConnection;0;2;207;0
WireConnection;0;10;73;0
ASEEND*/
//CHKSM=B84EF32F326CF2C6CA3781EB640D395224D751BF