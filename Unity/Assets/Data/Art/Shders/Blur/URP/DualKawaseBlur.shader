﻿Shader "Ux/URP_PostProcessing/DualKawaseBlur"
{
	Properties
    {
        _MainTex ("Texture", 2D) = "white" { }
    }
	
	HLSLINCLUDE
	
	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
	
	TEXTURE2D (_MainTex);
	SAMPLER(sampler_MainTex);
	float4 _MainTex_TexelSize;
	uniform half _Offset;

	struct appdata
    {
		float4 vertex: POSITION;
        float2 uv: TEXCOORD0;
    };
	
	struct v2f
	{
		float4 pos: SV_POSITION;
		float2 uv: TEXCOORD0;
	};
	
	struct v2f_DownSample
	{
		float4 vertex: SV_POSITION;
		float2 texcoord: TEXCOORD0;
		float2 uv: TEXCOORD1;
		float4 uv01: TEXCOORD2;
		float4 uv23: TEXCOORD3;
	};
	
	
	struct v2f_UpSample
	{
		float4 vertex: SV_POSITION;
		float2 texcoord: TEXCOORD0;
		float4 uv01: TEXCOORD1;
		float4 uv23: TEXCOORD2;
		float4 uv45: TEXCOORD3;
		float4 uv67: TEXCOORD4;
	};
	
	
	v2f_DownSample Vert_DownSample(v2f v)
	{
		v2f_DownSample o = (v2f_DownSample)0;
		o.vertex = TransformObjectToHClip(v.pos.xyz);
		float2 uv = v.uv;
		
		_MainTex_TexelSize *= 0.5;
		o.uv = uv;
		o.uv01.xy = uv - _MainTex_TexelSize * float2(1 + _Offset, 1 + _Offset);//top right
		o.uv01.zw = uv + _MainTex_TexelSize * float2(1 + _Offset, 1 + _Offset);//bottom left
		o.uv23.xy = uv - float2(_MainTex_TexelSize.x, -_MainTex_TexelSize.y) * float2(1 + _Offset, 1 + _Offset);//top left
		o.uv23.zw = uv + float2(_MainTex_TexelSize.x, -_MainTex_TexelSize.y) * float2(1 + _Offset, 1 + _Offset);//bottom right
		
		return o;
	}
	
	half4 Frag_DownSample(v2f_DownSample i): SV_Target
	{
		half4 sum = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * 4;
		sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv01.xy);
		sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv01.zw);
		sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv23.xy);
		sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv23.zw);
		
		return sum * 0.125;
	}
	
	
	v2f_UpSample Vert_UpSample(v2f v)
	{
		v2f_UpSample o;
		o.vertex = TransformObjectToHClip(v.pos.xyz);
		float2 uv = v.uv;
		
		_MainTex_TexelSize *= 0.5;
		_Offset = float2(1 + _Offset, 1 + _Offset);
		
		o.uv01.xy = uv + float2(-_MainTex_TexelSize.x * 2, 0) * _Offset;
		o.uv01.zw = uv + float2(-_MainTex_TexelSize.x, _MainTex_TexelSize.y) * _Offset;
		o.uv23.xy = uv + float2(0, _MainTex_TexelSize.y * 2) * _Offset;
		o.uv23.zw = uv + _MainTex_TexelSize * _Offset;
		o.uv45.xy = uv + float2(_MainTex_TexelSize.x * 2, 0) * _Offset;
		o.uv45.zw = uv + float2(_MainTex_TexelSize.x, -_MainTex_TexelSize.y) * _Offset;
		o.uv67.xy = uv + float2(0, -_MainTex_TexelSize.y * 2) * _Offset;
		o.uv67.zw = uv - _MainTex_TexelSize * _Offset;
		
		return o;
	}
	
	half4 Frag_UpSample(v2f_UpSample i): SV_Target
	{
		half4 sum = 0;
		sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv01.xy);
		sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv01.zw) * 2;
		sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv23.xy);
		sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv23.zw) * 2;
		sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv45.xy);
		sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv45.zw) * 2;
		sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv67.xy);
		sum += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv67.zw) * 2;
		
		return sum * 0.0833;
	}
	
	ENDHLSL
	
	SubShader
	{
		Cull Off ZWrite Off ZTest Always
		
		Pass
		{
			HLSLPROGRAM
			
			#pragma vertex Vert_DownSample
			#pragma fragment Frag_DownSample
			
			ENDHLSL
			
		}
		
		Pass
		{
			HLSLPROGRAM
			
			#pragma vertex Vert_UpSample
			#pragma fragment Frag_UpSample
			
			ENDHLSL
			
		}
	}
}


