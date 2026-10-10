// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Custom/Water"
{
	Properties
	{
		[HideInInspector] _EmissionColor("Emission Color", Color) = (1,1,1,1)
		[HideInInspector] _AlphaCutoff("Alpha Cutoff ", Range(0, 1)) = 0.5
		[ASEBegin]_DistanceFade("Distance Fade", Float) = 1000
		_DistanceFadeOffset("Distance Fade Offset", Float) = 500
		[Space(10)][Header(Main Parameters)][Space(10)]_NormalMap("Normal", 2D) = "bump" {}
		_NormalPower("Normal Power", Range( 0 , 1)) = 1
		_RefractionPower("Refraction Power", Range( 0 , 5)) = 1
		_NormalScale("Normal Scale", Float) = 1
		_NormalSpeed("Normal Speed", Float) = 0.1
		_NormalDirection("Normal Direction", Vector) = (1,0,-1,0.2)
		[Space(10)][Header(Foam)][Space(10)]_FoamMask("Foam Mask", 2D) = "white" {}
		_FoamNormal("Foam Normal", 2D) = "bump" {}
		_FoamDistance("Foam Distance", Range( 0 , 100)) = 1
		_FoamPower("Foam Power", Range( 0 , 1)) = 1
		_FoamScale("Foam Scale", Float) = 1
		_FoamContrast("Foam Contrast", Float) = 0
		_FoamSpeed("Foam Speed", Float) = 0.1
		[Space(30)]_EdgesFade("Edges Fade", Float) = 0.1
		_DepthColor1("Shallow Color (RGBA)", Color) = (0,0.6810271,0.6886792,1)
		_DepthColor("Depth Color (RGBA)", Color) = (0,0.6810271,0.6886792,1)
		_Depth("Depth", Float) = 1
		[Space(30)]_CausticsColor("Caustics Color", Color) = (0.5404058,0.8679245,0.8414827,1)
		_CausticsScale("Caustics Scale", Float) = 0.5
		_CausticsSpeed("Caustics Speed", Float) = 2
		[Space(30)]_WavesHeight("Waves Height", Float) = 25
		_WavesSpeed("Waves Speed", Float) = 5
		_WavesScale("Waves Scale", Float) = 10
		[Space(10)][Header(Metallic Smoothness)][Space(10)]_MetallicPower("Metallic Power", Range( 0 , 1)) = 1
		[ASEEnd]_SmoothnessPower("Smoothness Power", Range( 0 , 1)) = 1

		//_TransmissionShadow( "Transmission Shadow", Range( 0, 1 ) ) = 0.5
		//_TransStrength( "Trans Strength", Range( 0, 50 ) ) = 1
		//_TransNormal( "Trans Normal Distortion", Range( 0, 1 ) ) = 0.5
		//_TransScattering( "Trans Scattering", Range( 1, 50 ) ) = 2
		//_TransDirect( "Trans Direct", Range( 0, 1 ) ) = 0.9
		//_TransAmbient( "Trans Ambient", Range( 0, 1 ) ) = 0.1
		//_TransShadow( "Trans Shadow", Range( 0, 1 ) ) = 0.5
		//_TessPhongStrength( "Tess Phong Strength", Range( 0, 1 ) ) = 0.5
		//_TessValue( "Tess Max Tessellation", Range( 1, 32 ) ) = 16
		//_TessMin( "Tess Min Distance", Float ) = 10
		//_TessMax( "Tess Max Distance", Float ) = 25
		//_TessEdgeLength ( "Tess Edge length", Range( 2, 50 ) ) = 16
		//_TessMaxDisp( "Tess Max Displacement", Float ) = 25
	}

	SubShader
	{
		LOD 0

		

		Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
		Cull Back
		AlphaToMask Off
		HLSLINCLUDE
		#pragma target 4.6

		#ifndef ASE_TESS_FUNCS
		#define ASE_TESS_FUNCS
		float4 FixedTess( float tessValue )
		{
			return tessValue;
		}
		
		float CalcDistanceTessFactor (float4 vertex, float minDist, float maxDist, float tess, float4x4 o2w, float3 cameraPos )
		{
			float3 wpos = mul(o2w,vertex).xyz;
			float dist = distance (wpos, cameraPos);
			float f = clamp(1.0 - (dist - minDist) / (maxDist - minDist), 0.01, 1.0) * tess;
			return f;
		}

		float4 CalcTriEdgeTessFactors (float3 triVertexFactors)
		{
			float4 tess = 0.0f;
			tess.x = 0.5 * (triVertexFactors.y + triVertexFactors.z);
			tess.y = 0.5 * (triVertexFactors.x + triVertexFactors.z);
			tess.z = 0.5 * (triVertexFactors.x + triVertexFactors.y);
			tess.w = (triVertexFactors.x + triVertexFactors.y + triVertexFactors.z) / 3.0f;
			return tess;
		}

		float CalcEdgeTessFactor (float3 wpos0, float3 wpos1, float edgeLen, float3 cameraPos, float4 scParams )
		{
			float dist = distance (0.5 * (wpos0+wpos1), cameraPos);
			float len = distance(wpos0, wpos1);
			float f = max(len * scParams.y / (edgeLen * dist), 1.0);
			return f;
		}

		float DistanceFromPlane (float3 pos, float4 plane)
		{
			float d = dot (float4(pos,1.0f), plane);
			return d;
		}

		bool WorldViewFrustumCull (float3 wpos0, float3 wpos1, float3 wpos2, float cullEps, float4 planes[6] )
		{
			float4 planeTest;
			planeTest.x = (( DistanceFromPlane(wpos0, planes[0]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos1, planes[0]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos2, planes[0]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.y = (( DistanceFromPlane(wpos0, planes[1]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos1, planes[1]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos2, planes[1]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.z = (( DistanceFromPlane(wpos0, planes[2]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos1, planes[2]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos2, planes[2]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.w = (( DistanceFromPlane(wpos0, planes[3]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos1, planes[3]) > -cullEps) ? 1.0f : 0.0f ) +
						  (( DistanceFromPlane(wpos2, planes[3]) > -cullEps) ? 1.0f : 0.0f );
			return !all (planeTest);
		}

		float4 DistanceBasedTess( float4 v0, float4 v1, float4 v2, float tess, float minDist, float maxDist, float4x4 o2w, float3 cameraPos )
		{
			float3 f;
			f.x = CalcDistanceTessFactor (v0,minDist,maxDist,tess,o2w,cameraPos);
			f.y = CalcDistanceTessFactor (v1,minDist,maxDist,tess,o2w,cameraPos);
			f.z = CalcDistanceTessFactor (v2,minDist,maxDist,tess,o2w,cameraPos);

			return CalcTriEdgeTessFactors (f);
		}

		float4 EdgeLengthBasedTess( float4 v0, float4 v1, float4 v2, float edgeLength, float4x4 o2w, float3 cameraPos, float4 scParams )
		{
			float3 pos0 = mul(o2w,v0).xyz;
			float3 pos1 = mul(o2w,v1).xyz;
			float3 pos2 = mul(o2w,v2).xyz;
			float4 tess = 0.0f;
			tess.x = CalcEdgeTessFactor (pos1, pos2, edgeLength, cameraPos, scParams);
			tess.y = CalcEdgeTessFactor (pos2, pos0, edgeLength, cameraPos, scParams);
			tess.z = CalcEdgeTessFactor (pos0, pos1, edgeLength, cameraPos, scParams);
			tess.w = (tess.x + tess.y + tess.z) / 3.0f;
			return tess;
		}
		#endif //ASE_TESS_FUNCS
		ENDHLSL

		
		Pass
		{
			
			Name "Forward"
			Tags { "LightMode"="UniversalForward" }
			
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite Off
			ZTest LEqual
			Offset 0,0
			ColorMask RGBA
			

			HLSLPROGRAM
			#define _NORMAL_DROPOFF_TS 1
			// #pragma multi_compile_instancing
			// #pragma multi_compile _ LOD_FADE_CROSSFADE
			#pragma multi_compile_fog
			#define ASE_FOG 1
			#define _NORMALMAP 1
			#define ASE_SRP_VERSION 70503
			#define REQUIRE_DEPTH_TEXTURE 1
			#define REQUIRE_OPAQUE_TEXTURE 1

			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x

			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
			// #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
			// #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
			// #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
			#pragma multi_compile _ _SHADOWS_SOFT
			// #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
			
			// #pragma multi_compile _ DIRLIGHTMAP_COMBINED
			// #pragma multi_compile _ LIGHTMAP_ON

			#pragma vertex vert
			#pragma fragment frag

			#define SHADERPASS_FORWARD

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			
			#if ASE_SRP_VERSION <= 70108
			#define REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR
			#endif

			#if defined(UNITY_INSTANCING_ENABLED) && defined(_TERRAIN_INSTANCED_PERPIXEL_NORMAL)
			    #define ENABLE_TERRAIN_PERPIXEL_NORMAL
			#endif

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#define ASE_NEEDS_VERT_NORMAL
			#define ASE_NEEDS_FRAG_WORLD_VIEW_DIR
			#define ASE_NEEDS_FRAG_SCREEN_POSITION
			#define ASE_NEEDS_FRAG_WORLD_POSITION
			#define ASE_NEEDS_VERT_POSITION


			struct VertexInput
			{
				float4 vertex : POSITION;
				float3 ase_normal : NORMAL;
				float4 ase_tangent : TANGENT;
				float4 texcoord1 : TEXCOORD1;
				float4 texcoord : TEXCOORD0;
				
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 clipPos : SV_POSITION;
				float4 lightmapUVOrVertexSH : TEXCOORD0;
				half4 fogFactorAndVertexLight : TEXCOORD1;
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
				float4 shadowCoord : TEXCOORD2;
				#endif
				float4 tSpace0 : TEXCOORD3;
				float4 tSpace1 : TEXCOORD4;
				float4 tSpace2 : TEXCOORD5;
				#if defined(ASE_NEEDS_FRAG_SCREEN_POSITION)
				float4 screenPos : TEXCOORD6;
				#endif
				float4 ase_texcoord7 : TEXCOORD7;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			float4 _CausticsColor;
			float4 _DepthColor;
			float4 _DepthColor1;
			float4 _NormalDirection;
			float _WavesSpeed;
			float _SmoothnessPower;
			float _MetallicPower;
			float _CausticsSpeed;
			float _CausticsScale;
			float _Depth;
			float _NormalPower;
			float _NormalScale;
			float _NormalSpeed;
			float _RefractionPower;
			float _FoamPower;
			float _FoamScale;
			float _FoamSpeed;
			float _FoamDistance;
			float _EdgesFade;
			float _FoamContrast;
			float _WavesHeight;
			float _WavesScale;
			float _DistanceFade;
			float _DistanceFadeOffset;
			#ifdef _TRANSMISSION_ASE
				float _TransmissionShadow;
			#endif
			#ifdef _TRANSLUCENCY_ASE
				float _TransStrength;
				float _TransNormal;
				float _TransScattering;
				float _TransDirect;
				float _TransAmbient;
				float _TransShadow;
			#endif
			CBUFFER_END
			uniform float4 _CameraDepthTexture_TexelSize;
			sampler2D _FoamMask;
			sampler2D _NormalMap;
			sampler2D _FoamNormal;


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
			
			float4 CalculateContrast( float contrastValue, float4 colorTarget )
			{
				float t = 0.5 * ( 1.0 - contrastValue );
				return mul( float4x4( contrastValue,0,0,t, 0,contrastValue,0,t, 0,0,contrastValue,t, 0,0,0,1 ), colorTarget );
			}
			inline float4 ASE_ComputeGrabScreenPos( float4 pos )
			{
				#if UNITY_UV_STARTS_AT_TOP
				float scale = -1.0;
				#else
				float scale = 1.0;
				#endif
				float4 o = pos;
				o.y = pos.w * 0.5f;
				o.y = ( pos.y - o.y ) * _ProjectionParams.x * scale + o.y;
				return o;
			}
			
					float2 voronoihash110( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi110( float2 v, float time, inout float2 id, inout float2 mr, float smoothness )
					{
						float2 n = floor( v );
						float2 f = frac( v );
						float F1 = 8.0;
						float F2 = 8.0; float2 mg = 0;
						for ( int j = -1; j <= 1; j++ )
						{
							for ( int i = -1; i <= 1; i++ )
						 	{
						 		float2 g = float2( i, j );
						 		float2 o = voronoihash110( n + g );
								o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
								float d = 0.5 * dot( r, r );
						 		if( d<F1 ) {
						 			F2 = F1;
						 			F1 = d; mg = g; mr = r; id = o;
						 		} else if( d<F2 ) {
						 			F2 = d;
						 		}
						 	}
						}
						return F1;
					}
			
					float2 voronoihash129( float2 p )
					{
						
						p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
						return frac( sin( p ) *43758.5453);
					}
			
					float voronoi129( float2 v, float time, inout float2 id, inout float2 mr, float smoothness )
					{
						float2 n = floor( v );
						float2 f = frac( v );
						float F1 = 8.0;
						float F2 = 8.0; float2 mg = 0;
						for ( int j = -1; j <= 1; j++ )
						{
							for ( int i = -1; i <= 1; i++ )
						 	{
						 		float2 g = float2( i, j );
						 		float2 o = voronoihash129( n + g );
								o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
								float d = 0.5 * dot( r, r );
						 		if( d<F1 ) {
						 			F2 = F1;
						 			F1 = d; mg = g; mr = r; id = o;
						 		} else if( d<F2 ) {
						 			F2 = d;
						 		}
						 	}
						}
						return F1;
					}
			

			VertexOutput VertexFunction( VertexInput v  )
			{
				VertexOutput o = (VertexOutput)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				float2 appendResult195 = (float2(_WavesSpeed , _WavesSpeed));
				float3 ase_worldPos = mul(GetObjectToWorldMatrix(), v.vertex).xyz;
				float2 appendResult194 = (float2(ase_worldPos.x , ase_worldPos.z));
				float2 panner196 = ( 1.0 * _Time.y * appendResult195 + appendResult194);
				float simplePerlin2D199 = snoise( panner196*( _WavesScale / 100.0 ) );
				simplePerlin2D199 = simplePerlin2D199*0.5 + 0.5;
				float3 worldToObjDir273 = mul( GetWorldToObjectMatrix(), float4( ( v.ase_normal * ( simplePerlin2D199 * _WavesHeight ) ), 0 ) ).xyz;
				float3 WavesHeight49 = worldToObjDir273;
				
				float3 objectToViewPos = TransformWorldToView(TransformObjectToWorld(v.vertex.xyz));
				float eyeDepth = -objectToViewPos.z;
				o.ase_texcoord7.x = eyeDepth;
				
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord7.yzw = 0;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = v.vertex.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif
				float3 vertexValue = WavesHeight49;
				#ifdef ASE_ABSOLUTE_VERTEX_POS
					v.vertex.xyz = vertexValue;
				#else
					v.vertex.xyz += vertexValue;
				#endif
				v.ase_normal = v.ase_normal;

				float3 positionWS = TransformObjectToWorld( v.vertex.xyz );
				float3 positionVS = TransformWorldToView( positionWS );
				float4 positionCS = TransformWorldToHClip( positionWS );

				VertexNormalInputs normalInput = GetVertexNormalInputs( v.ase_normal, v.ase_tangent );

				o.tSpace0 = float4( normalInput.normalWS, positionWS.x);
				o.tSpace1 = float4( normalInput.tangentWS, positionWS.y);
				o.tSpace2 = float4( normalInput.bitangentWS, positionWS.z);

				OUTPUT_LIGHTMAP_UV( v.texcoord1, unity_LightmapST, o.lightmapUVOrVertexSH.xy );
				OUTPUT_SH( normalInput.normalWS.xyz, o.lightmapUVOrVertexSH.xyz );

				#if defined(ENABLE_TERRAIN_PERPIXEL_NORMAL)
					o.lightmapUVOrVertexSH.zw = v.texcoord;
					o.lightmapUVOrVertexSH.xy = v.texcoord * unity_LightmapST.xy + unity_LightmapST.zw;
				#endif

				half3 vertexLight = VertexLighting( positionWS, normalInput.normalWS );
				#ifdef ASE_FOG
					half fogFactor = ComputeFogFactor( positionCS.z );
				#else
					half fogFactor = 0;
				#endif
				o.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
				
				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
				VertexPositionInputs vertexInput = (VertexPositionInputs)0;
				vertexInput.positionWS = positionWS;
				vertexInput.positionCS = positionCS;
				o.shadowCoord = GetShadowCoord( vertexInput );
				#endif
				
				o.clipPos = positionCS;
				#if defined(ASE_NEEDS_FRAG_SCREEN_POSITION)
				o.screenPos = ComputeScreenPos(positionCS);
				#endif
				return o;
			}
			
			VertexOutput vert ( VertexInput v )
			{
				return VertexFunction( v );
			}

			#if defined(ASE_EARLY_Z_DEPTH_OPTIMIZE)
				#define ASE_SV_DEPTH SV_DepthLessEqual  
			#else
				#define ASE_SV_DEPTH SV_Depth
			#endif

			half4 frag ( VertexOutput IN 
						#ifdef ASE_DEPTH_WRITE_ON
						,out float outputDepth : ASE_SV_DEPTH
						#endif
						 ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

				#ifdef LOD_FADE_CROSSFADE
					LODDitheringTransition( IN.clipPos.xyz, unity_LODFade.x );
				#endif

				#if defined(ENABLE_TERRAIN_PERPIXEL_NORMAL)
					float2 sampleCoords = (IN.lightmapUVOrVertexSH.zw / _TerrainHeightmapRecipSize.zw + 0.5f) * _TerrainHeightmapRecipSize.xy;
					float3 WorldNormal = TransformObjectToWorldNormal(normalize(SAMPLE_TEXTURE2D(_TerrainNormalmapTexture, sampler_TerrainNormalmapTexture, sampleCoords).rgb * 2 - 1));
					float3 WorldTangent = -cross(GetObjectToWorldMatrix()._13_23_33, WorldNormal);
					float3 WorldBiTangent = cross(WorldNormal, -WorldTangent);
				#else
					float3 WorldNormal = normalize( IN.tSpace0.xyz );
					float3 WorldTangent = IN.tSpace1.xyz;
					float3 WorldBiTangent = IN.tSpace2.xyz;
				#endif
				float3 WorldPosition = float3(IN.tSpace0.w,IN.tSpace1.w,IN.tSpace2.w);
				float3 WorldViewDirection = _WorldSpaceCameraPos.xyz  - WorldPosition;
				float4 ShadowCoords = float4( 0, 0, 0, 0 );
				#if defined(ASE_NEEDS_FRAG_SCREEN_POSITION)
				float4 ScreenPos = IN.screenPos;
				#endif

				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
					ShadowCoords = IN.shadowCoord;
				#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
					ShadowCoords = TransformWorldToShadowCoord( WorldPosition );
				#endif
	
				WorldViewDirection = SafeNormalize( WorldViewDirection );

				float temp_output_368_0 = abs( WorldViewDirection.y );
				float4 ase_screenPosNorm = ScreenPos / ScreenPos.w;
				ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
				float screenDepth241 = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH( ase_screenPosNorm.xy ),_ZBufferParams);
				float distanceDepth241 = abs( ( screenDepth241 - LinearEyeDepth( ase_screenPosNorm.z,_ZBufferParams ) ) / ( _EdgesFade ) );
				float temp_output_377_0 = ( temp_output_368_0 * distanceDepth241 );
				float screenDepth230 = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH( ase_screenPosNorm.xy ),_ZBufferParams);
				float distanceDepth230 = abs( ( screenDepth230 - LinearEyeDepth( ase_screenPosNorm.z,_ZBufferParams ) ) / ( ( _FoamDistance * 0.1 ) ) );
				float screenDepth4 = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH( ase_screenPosNorm.xy ),_ZBufferParams);
				float distanceDepth4 = abs( ( screenDepth4 - LinearEyeDepth( ase_screenPosNorm.z,_ZBufferParams ) ) / ( _FoamDistance ) );
				float temp_output_6_0 = ( 1.0 - ( temp_output_368_0 * distanceDepth4 ) );
				float temp_output_215_0 = ( _FoamSpeed / 100.0 );
				float2 temp_cast_0 = (temp_output_215_0).xx;
				float4 appendResult31 = (float4(WorldPosition.x , WorldPosition.z , 0.0 , 0.0));
				float4 WorldSpace32 = appendResult31;
				float4 temp_output_225_0 = ( WorldSpace32 * ( _FoamScale / 100.0 ) );
				float2 panner213 = ( 1.0 * _Time.y * temp_cast_0 + temp_output_225_0.xy);
				float2 temp_cast_2 = (temp_output_215_0).xx;
				float2 panner254 = ( 1.0 * _Time.y * temp_cast_2 + ( 1.0 - temp_output_225_0 ).xy);
				float4 temp_cast_4 = (( temp_output_377_0 * ( ( ( 1.0 - ( temp_output_368_0 * distanceDepth230 ) ) + ( temp_output_6_0 * pow( ( temp_output_6_0 * ( tex2D( _FoamMask, panner213 ).r * tex2D( _FoamMask, panner254 ).r ) ) , ( 1.0 - 0.5 ) ) ) ) * _FoamPower ) )).xxxx;
				float4 clampResult9 = clamp( CalculateContrast(_FoamContrast,temp_cast_4) , float4( 0,0,0,0 ) , float4( 1,1,1,0 ) );
				float4 Edges62 = clampResult9;
				float4 ase_grabScreenPos = ASE_ComputeGrabScreenPos( ScreenPos );
				float4 ase_grabScreenPosNorm = ase_grabScreenPos / ase_grabScreenPos.w;
				float2 appendResult337 = (float2(_NormalDirection.x , _NormalDirection.y));
				float temp_output_106_0 = ( _NormalSpeed / 100.0 );
				float4 temp_output_68_0 = ( ( WorldSpace32 / 100.0 ) * _NormalScale );
				float2 panner72 = ( 1.0 * _Time.y * ( appendResult337 * temp_output_106_0 ) + temp_output_68_0.xy);
				float3 unpack65 = UnpackNormalScale( tex2D( _NormalMap, panner72 ), _NormalPower );
				unpack65.z = lerp( 1, unpack65.z, saturate(_NormalPower) );
				float2 appendResult338 = (float2(_NormalDirection.z , _NormalDirection.w));
				float2 panner73 = ( 1.0 * _Time.y * ( appendResult338 * ( temp_output_106_0 * 2.0 ) ) + ( temp_output_68_0 * ( _NormalScale * 1.2 ) ).xy);
				float3 unpack66 = UnpackNormalScale( tex2D( _NormalMap, panner73 ), _NormalPower );
				unpack66.z = lerp( 1, unpack66.z, saturate(_NormalPower) );
				float3 lerpResult237 = lerp( BlendNormal( unpack65 , unpack66 ) , UnpackNormalScale( tex2D( _FoamNormal, panner213 ), 1.0f ) , Edges62.rgb);
				float3 Normals81 = lerpResult237;
				float4 fetchOpaqueVal20 = float4( SHADERGRAPH_SAMPLE_SCENE_COLOR( ( ase_grabScreenPosNorm + float4( ( ( _RefractionPower / 10.0 ) * Normals81 ) , 0.0 ) ).xy ), 1.0 );
				float4 clampResult21 = clamp( fetchOpaqueVal20 , float4( 0,0,0,0 ) , float4( 1,1,1,0 ) );
				float4 Refraction60 = clampResult21;
				float screenDepth95 = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH( ase_screenPosNorm.xy ),_ZBufferParams);
				float distanceDepth95 = abs( ( screenDepth95 - LinearEyeDepth( ase_screenPosNorm.z,_ZBufferParams ) ) / ( _Depth ) );
				float3 temp_cast_10 = (( 1.0 - saturate( ( distanceDepth95 * abs( WorldViewDirection.y ) ) ) )).xxx;
				float3 temp_cast_11 = (( 1.0 - saturate( ( distanceDepth95 * abs( WorldViewDirection.y ) ) ) )).xxx;
				float3 gammaToLinear330 = FastSRGBToLinear( temp_cast_11 );
				float Depth98 = gammaToLinear330.x;
				float4 lerpResult326 = lerp( _DepthColor1 , _DepthColor , ( 1.0 - Depth98 ));
				float mulTime27 = _TimeParameters.x * _CausticsSpeed;
				float time110 = ( mulTime27 * 1.0 );
				float2 coords110 = WorldSpace32.xy * ( _CausticsScale * 0.5 );
				float2 id110 = 0;
				float2 uv110 = 0;
				float voroi110 = voronoi110( coords110, time110, id110, uv110, 0 );
				float time129 = mulTime27;
				float2 coords129 = WorldSpace32.xy * _CausticsScale;
				float2 id129 = 0;
				float2 uv129 = 0;
				float voroi129 = voronoi129( coords129, time129, id129, uv129, 0 );
				float Caustics47 = saturate( ( voroi110 + voroi129 ) );
				float clampResult56 = clamp( Caustics47 , 0.0 , 1.0 );
				float4 lerpResult52 = lerp( lerpResult326 , _CausticsColor , clampResult56);
				float4 blendOpSrc173 = Refraction60;
				float4 blendOpDest173 = lerpResult52;
				float4 lerpBlendMode173 = lerp(blendOpDest173,(( blendOpDest173 > 0.5 ) ? ( 1.0 - 2.0 * ( 1.0 - blendOpDest173 ) * ( 1.0 - blendOpSrc173 ) ) : ( 2.0 * blendOpDest173 * blendOpSrc173 ) ),( 1.0 - _DepthColor.a ));
				float4 Albedo58 = ( saturate( lerpBlendMode173 ));
				float4 lerpResult100 = lerp( Albedo58 , Refraction60 , Depth98);
				float4 clampResult109 = clamp( ( Edges62 + lerpResult100 ) , float4( 0,0,0,0 ) , float4( 1,1,1,0 ) );
				
				float clampResult249 = clamp( temp_output_377_0 , 0.0 , 1.0 );
				float eyeDepth = IN.ase_texcoord7.x;
				float cameraDepthFade293 = (( eyeDepth -_ProjectionParams.y - _DistanceFadeOffset ) / _DistanceFade);
				float Opacity263 = ( clampResult249 * saturate( ( 1.0 - ( temp_output_368_0 * cameraDepthFade293 ) ) ) );
				
				float3 Albedo = clampResult109.rgb;
				float3 Normal = Normals81;
				float3 Emission = 0;
				float3 Specular = 0.5;
				float Metallic = _MetallicPower;
				float Smoothness = _SmoothnessPower;
				float Occlusion = 1;
				float Alpha = Opacity263;
				float AlphaClipThreshold = 0.5;
				float AlphaClipThresholdShadow = 0.5;
				float3 BakedGI = 0;
				float3 RefractionColor = 1;
				float RefractionIndex = 1;
				float3 Transmission = 1;
				float3 Translucency = 1;
				#ifdef ASE_DEPTH_WRITE_ON
				float DepthValue = 0;
				#endif

				#ifdef _ALPHATEST_ON
					clip(Alpha - AlphaClipThreshold);
				#endif

				InputData inputData;
				inputData.positionWS = WorldPosition;
				inputData.viewDirectionWS = WorldViewDirection;
				inputData.shadowCoord = ShadowCoords;

				#ifdef _NORMALMAP
					#if _NORMAL_DROPOFF_TS
					inputData.normalWS = TransformTangentToWorld(Normal, half3x3( WorldTangent, WorldBiTangent, WorldNormal ));
					#elif _NORMAL_DROPOFF_OS
					inputData.normalWS = TransformObjectToWorldNormal(Normal);
					#elif _NORMAL_DROPOFF_WS
					inputData.normalWS = Normal;
					#endif
					inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
				#else
					inputData.normalWS = WorldNormal;
				#endif

				#ifdef ASE_FOG
					inputData.fogCoord = IN.fogFactorAndVertexLight.x;
				#endif

				inputData.vertexLighting = IN.fogFactorAndVertexLight.yzw;
				#if defined(ENABLE_TERRAIN_PERPIXEL_NORMAL)
					float3 SH = SampleSH(inputData.normalWS.xyz);
				#else
					float3 SH = IN.lightmapUVOrVertexSH.xyz;
				#endif

				inputData.bakedGI = SAMPLE_GI( IN.lightmapUVOrVertexSH.xy, SH, inputData.normalWS );
				#ifdef _ASE_BAKEDGI
					inputData.bakedGI = BakedGI;
				#endif
				half4 color = UniversalFragmentPBR(
					inputData, 
					Albedo, 
					Metallic, 
					Specular, 
					Smoothness, 
					Occlusion, 
					Emission, 
					Alpha);

				#ifdef _TRANSMISSION_ASE
				{
					float shadow = _TransmissionShadow;

					Light mainLight = GetMainLight( inputData.shadowCoord );
					float3 mainAtten = mainLight.color * mainLight.distanceAttenuation;
					mainAtten = lerp( mainAtten, mainAtten * mainLight.shadowAttenuation, shadow );
					half3 mainTransmission = max(0 , -dot(inputData.normalWS, mainLight.direction)) * mainAtten * Transmission;
					color.rgb += Albedo * mainTransmission;

					#ifdef _ADDITIONAL_LIGHTS
						int transPixelLightCount = GetAdditionalLightsCount();
						for (int i = 0; i < transPixelLightCount; ++i)
						{
							Light light = GetAdditionalLight(i, inputData.positionWS);
							float3 atten = light.color * light.distanceAttenuation;
							atten = lerp( atten, atten * light.shadowAttenuation, shadow );

							half3 transmission = max(0 , -dot(inputData.normalWS, light.direction)) * atten * Transmission;
							color.rgb += Albedo * transmission;
						}
					#endif
				}
				#endif

				#ifdef _TRANSLUCENCY_ASE
				{
					float shadow = _TransShadow;
					float normal = _TransNormal;
					float scattering = _TransScattering;
					float direct = _TransDirect;
					float ambient = _TransAmbient;
					float strength = _TransStrength;

					Light mainLight = GetMainLight( inputData.shadowCoord );
					float3 mainAtten = mainLight.color * mainLight.distanceAttenuation;
					mainAtten = lerp( mainAtten, mainAtten * mainLight.shadowAttenuation, shadow );

					half3 mainLightDir = mainLight.direction + inputData.normalWS * normal;
					half mainVdotL = pow( saturate( dot( inputData.viewDirectionWS, -mainLightDir ) ), scattering );
					half3 mainTranslucency = mainAtten * ( mainVdotL * direct + inputData.bakedGI * ambient ) * Translucency;
					color.rgb += Albedo * mainTranslucency * strength;

					#ifdef _ADDITIONAL_LIGHTS
						int transPixelLightCount = GetAdditionalLightsCount();
						for (int i = 0; i < transPixelLightCount; ++i)
						{
							Light light = GetAdditionalLight(i, inputData.positionWS);
							float3 atten = light.color * light.distanceAttenuation;
							atten = lerp( atten, atten * light.shadowAttenuation, shadow );

							half3 lightDir = light.direction + inputData.normalWS * normal;
							half VdotL = pow( saturate( dot( inputData.viewDirectionWS, -lightDir ) ), scattering );
							half3 translucency = atten * ( VdotL * direct + inputData.bakedGI * ambient ) * Translucency;
							color.rgb += Albedo * translucency * strength;
						}
					#endif
				}
				#endif

				#ifdef _REFRACTION_ASE
					float4 projScreenPos = ScreenPos / ScreenPos.w;
					float3 refractionOffset = ( RefractionIndex - 1.0 ) * mul( UNITY_MATRIX_V, WorldNormal ).xyz * ( 1.0 - dot( WorldNormal, WorldViewDirection ) );
					projScreenPos.xy += refractionOffset.xy;
					float3 refraction = SHADERGRAPH_SAMPLE_SCENE_COLOR( projScreenPos ) * RefractionColor;
					color.rgb = lerp( refraction, color.rgb, color.a );
					color.a = 1;
				#endif

				#ifdef ASE_FINAL_COLOR_ALPHA_MULTIPLY
					color.rgb *= color.a;
				#endif

				#ifdef ASE_FOG
					#ifdef TERRAIN_SPLAT_ADDPASS
						color.rgb = MixFogColor(color.rgb, half3( 0, 0, 0 ), IN.fogFactorAndVertexLight.x );
					#else
						color.rgb = MixFog(color.rgb, IN.fogFactorAndVertexLight.x);
					#endif
				#endif
				
				#ifdef ASE_DEPTH_WRITE_ON
					outputDepth = DepthValue;
				#endif

				return color;
			}

			ENDHLSL
		}

		
		// Pass
		// {
			
		// 	Name "ShadowCaster"
		// 	Tags { "LightMode"="ShadowCaster" }

		// 	ZWrite On
		// 	ZTest LEqual
		// 	AlphaToMask Off

		// 	HLSLPROGRAM
		// 	#define _NORMAL_DROPOFF_TS 1
		// 	// #pragma multi_compile_instancing
		// 	// #pragma multi_compile _ LOD_FADE_CROSSFADE
		// 	#pragma multi_compile_fog
		// 	#define ASE_FOG 1
		// 	#define _NORMALMAP 1
		// 	#define ASE_SRP_VERSION 70503
		// 	#define REQUIRE_DEPTH_TEXTURE 1

		// 	#pragma prefer_hlslcc gles
		// 	#pragma exclude_renderers d3d11_9x

		// 	#pragma vertex vert
		// 	#pragma fragment frag

		// 	#define SHADERPASS_SHADOWCASTER

		// 	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		// 	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
		// 	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
		// 	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

		// 	#define ASE_NEEDS_VERT_NORMAL
		// 	#define ASE_NEEDS_FRAG_WORLD_POSITION
		// 	#define ASE_NEEDS_VERT_POSITION


		// 	struct VertexInput
		// 	{
		// 		float4 vertex : POSITION;
		// 		float3 ase_normal : NORMAL;
				
		// 		UNITY_VERTEX_INPUT_INSTANCE_ID
		// 	};

		// 	struct VertexOutput
		// 	{
		// 		float4 clipPos : SV_POSITION;
		// 		#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
		// 		float3 worldPos : TEXCOORD0;
		// 		#endif
		// 		#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
		// 		float4 shadowCoord : TEXCOORD1;
		// 		#endif
		// 		float4 ase_texcoord2 : TEXCOORD2;
		// 		float4 ase_texcoord3 : TEXCOORD3;
		// 		UNITY_VERTEX_INPUT_INSTANCE_ID
		// 		UNITY_VERTEX_OUTPUT_STEREO
		// 	};

		// 	CBUFFER_START(UnityPerMaterial)
		// 	float4 _CausticsColor;
		// 	float4 _DepthColor;
		// 	float4 _DepthColor1;
		// 	float4 _NormalDirection;
		// 	float _WavesSpeed;
		// 	float _SmoothnessPower;
		// 	float _MetallicPower;
		// 	float _CausticsSpeed;
		// 	float _CausticsScale;
		// 	float _Depth;
		// 	float _NormalPower;
		// 	float _NormalScale;
		// 	float _NormalSpeed;
		// 	float _RefractionPower;
		// 	float _FoamPower;
		// 	float _FoamScale;
		// 	float _FoamSpeed;
		// 	float _FoamDistance;
		// 	float _EdgesFade;
		// 	float _FoamContrast;
		// 	float _WavesHeight;
		// 	float _WavesScale;
		// 	float _DistanceFade;
		// 	float _DistanceFadeOffset;
		// 	#ifdef _TRANSMISSION_ASE
		// 		float _TransmissionShadow;
		// 	#endif
		// 	#ifdef _TRANSLUCENCY_ASE
		// 		float _TransStrength;
		// 		float _TransNormal;
		// 		float _TransScattering;
		// 		float _TransDirect;
		// 		float _TransAmbient;
		// 		float _TransShadow;
		// 	#endif
		// 	CBUFFER_END
		// 	uniform float4 _CameraDepthTexture_TexelSize;


		// 	float3 mod2D289( float3 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }
		// 	float2 mod2D289( float2 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }
		// 	float3 permute( float3 x ) { return mod2D289( ( ( x * 34.0 ) + 1.0 ) * x ); }
		// 	float snoise( float2 v )
		// 	{
		// 		const float4 C = float4( 0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439 );
		// 		float2 i = floor( v + dot( v, C.yy ) );
		// 		float2 x0 = v - i + dot( i, C.xx );
		// 		float2 i1;
		// 		i1 = ( x0.x > x0.y ) ? float2( 1.0, 0.0 ) : float2( 0.0, 1.0 );
		// 		float4 x12 = x0.xyxy + C.xxzz;
		// 		x12.xy -= i1;
		// 		i = mod2D289( i );
		// 		float3 p = permute( permute( i.y + float3( 0.0, i1.y, 1.0 ) ) + i.x + float3( 0.0, i1.x, 1.0 ) );
		// 		float3 m = max( 0.5 - float3( dot( x0, x0 ), dot( x12.xy, x12.xy ), dot( x12.zw, x12.zw ) ), 0.0 );
		// 		m = m * m;
		// 		m = m * m;
		// 		float3 x = 2.0 * frac( p * C.www ) - 1.0;
		// 		float3 h = abs( x ) - 0.5;
		// 		float3 ox = floor( x + 0.5 );
		// 		float3 a0 = x - ox;
		// 		m *= 1.79284291400159 - 0.85373472095314 * ( a0 * a0 + h * h );
		// 		float3 g;
		// 		g.x = a0.x * x0.x + h.x * x0.y;
		// 		g.yz = a0.yz * x12.xz + h.yz * x12.yw;
		// 		return 130.0 * dot( m, g );
		// 	}
			

		// 	float3 _LightDirection;

		// 	VertexOutput VertexFunction( VertexInput v )
		// 	{
		// 		VertexOutput o;
		// 		UNITY_SETUP_INSTANCE_ID(v);
		// 		UNITY_TRANSFER_INSTANCE_ID(v, o);
		// 		UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o );

		// 		float2 appendResult195 = (float2(_WavesSpeed , _WavesSpeed));
		// 		float3 ase_worldPos = mul(GetObjectToWorldMatrix(), v.vertex).xyz;
		// 		float2 appendResult194 = (float2(ase_worldPos.x , ase_worldPos.z));
		// 		float2 panner196 = ( 1.0 * _Time.y * appendResult195 + appendResult194);
		// 		float simplePerlin2D199 = snoise( panner196*( _WavesScale / 100.0 ) );
		// 		simplePerlin2D199 = simplePerlin2D199*0.5 + 0.5;
		// 		float3 worldToObjDir273 = mul( GetWorldToObjectMatrix(), float4( ( v.ase_normal * ( simplePerlin2D199 * _WavesHeight ) ), 0 ) ).xyz;
		// 		float3 WavesHeight49 = worldToObjDir273;
				
		// 		float4 ase_clipPos = TransformObjectToHClip((v.vertex).xyz);
		// 		float4 screenPos = ComputeScreenPos(ase_clipPos);
		// 		o.ase_texcoord2 = screenPos;
		// 		float3 objectToViewPos = TransformWorldToView(TransformObjectToWorld(v.vertex.xyz));
		// 		float eyeDepth = -objectToViewPos.z;
		// 		o.ase_texcoord3.x = eyeDepth;
				
				
		// 		//setting value to unused interpolator channels and avoid initialization warnings
		// 		o.ase_texcoord3.yzw = 0;
		// 		#ifdef ASE_ABSOLUTE_VERTEX_POS
		// 			float3 defaultVertexValue = v.vertex.xyz;
		// 		#else
		// 			float3 defaultVertexValue = float3(0, 0, 0);
		// 		#endif
		// 		float3 vertexValue = WavesHeight49;
		// 		#ifdef ASE_ABSOLUTE_VERTEX_POS
		// 			v.vertex.xyz = vertexValue;
		// 		#else
		// 			v.vertex.xyz += vertexValue;
		// 		#endif

		// 		v.ase_normal = v.ase_normal;

		// 		float3 positionWS = TransformObjectToWorld( v.vertex.xyz );
		// 		#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
		// 		o.worldPos = positionWS;
		// 		#endif
		// 		float3 normalWS = TransformObjectToWorldDir(v.ase_normal);

		// 		float4 clipPos = TransformWorldToHClip( ApplyShadowBias( positionWS, normalWS, _LightDirection ) );

		// 		#if UNITY_REVERSED_Z
		// 			clipPos.z = min(clipPos.z, clipPos.w * UNITY_NEAR_CLIP_VALUE);
		// 		#else
		// 			clipPos.z = max(clipPos.z, clipPos.w * UNITY_NEAR_CLIP_VALUE);
		// 		#endif
		// 		#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
		// 			VertexPositionInputs vertexInput = (VertexPositionInputs)0;
		// 			vertexInput.positionWS = positionWS;
		// 			vertexInput.positionCS = clipPos;
		// 			o.shadowCoord = GetShadowCoord( vertexInput );
		// 		#endif
		// 		o.clipPos = clipPos;
		// 		return o;
		// 	}

		// 	VertexOutput vert ( VertexInput v )
		// 	{
		// 		return VertexFunction( v );
		// 	}

		// 	#if defined(ASE_EARLY_Z_DEPTH_OPTIMIZE)
		// 		#define ASE_SV_DEPTH SV_DepthLessEqual  
		// 	#else
		// 		#define ASE_SV_DEPTH SV_Depth
		// 	#endif

		// 	half4 frag(	VertexOutput IN 
		// 				#ifdef ASE_DEPTH_WRITE_ON
		// 				,out float outputDepth : ASE_SV_DEPTH
		// 				#endif
		// 				 ) : SV_TARGET
		// 	{
		// 		UNITY_SETUP_INSTANCE_ID( IN );
		// 		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );
				
		// 		#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
		// 		float3 WorldPosition = IN.worldPos;
		// 		#endif
		// 		float4 ShadowCoords = float4( 0, 0, 0, 0 );

		// 		#if defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
		// 			#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
		// 				ShadowCoords = IN.shadowCoord;
		// 			#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
		// 				ShadowCoords = TransformWorldToShadowCoord( WorldPosition );
		// 			#endif
		// 		#endif

		// 		float3 ase_worldViewDir = ( _WorldSpaceCameraPos.xyz - WorldPosition );
		// 		ase_worldViewDir = normalize(ase_worldViewDir);
		// 		float temp_output_368_0 = abs( ase_worldViewDir.y );
		// 		float4 screenPos = IN.ase_texcoord2;
		// 		float4 ase_screenPosNorm = screenPos / screenPos.w;
		// 		ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
		// 		float screenDepth241 = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH( ase_screenPosNorm.xy ),_ZBufferParams);
		// 		float distanceDepth241 = abs( ( screenDepth241 - LinearEyeDepth( ase_screenPosNorm.z,_ZBufferParams ) ) / ( _EdgesFade ) );
		// 		float temp_output_377_0 = ( temp_output_368_0 * distanceDepth241 );
		// 		float clampResult249 = clamp( temp_output_377_0 , 0.0 , 1.0 );
		// 		float eyeDepth = IN.ase_texcoord3.x;
		// 		float cameraDepthFade293 = (( eyeDepth -_ProjectionParams.y - _DistanceFadeOffset ) / _DistanceFade);
		// 		float Opacity263 = ( clampResult249 * saturate( ( 1.0 - ( temp_output_368_0 * cameraDepthFade293 ) ) ) );
				
		// 		float Alpha = Opacity263;
		// 		float AlphaClipThreshold = 0.5;
		// 		float AlphaClipThresholdShadow = 0.5;
		// 		#ifdef ASE_DEPTH_WRITE_ON
		// 		float DepthValue = 0;
		// 		#endif

		// 		#ifdef _ALPHATEST_ON
		// 			#ifdef _ALPHATEST_SHADOW_ON
		// 				clip(Alpha - AlphaClipThresholdShadow);
		// 			#else
		// 				clip(Alpha - AlphaClipThreshold);
		// 			#endif
		// 		#endif

		// 		#ifdef LOD_FADE_CROSSFADE
		// 			LODDitheringTransition( IN.clipPos.xyz, unity_LODFade.x );
		// 		#endif
		// 		#ifdef ASE_DEPTH_WRITE_ON
		// 			outputDepth = DepthValue;
		// 		#endif
		// 		return 0;
		// 	}

		// 	ENDHLSL
		// }

		
		// Pass
		// {
			
		// 	Name "DepthOnly"
		// 	Tags { "LightMode"="DepthOnly" }

		// 	ZWrite On
		// 	ColorMask 0
		// 	AlphaToMask Off

		// 	HLSLPROGRAM
		// 	#define _NORMAL_DROPOFF_TS 1
		// 	// #pragma multi_compile_instancing
		// 	// #pragma multi_compile _ LOD_FADE_CROSSFADE
		// 	#pragma multi_compile_fog
		// 	#define ASE_FOG 1
		// 	#define _NORMALMAP 1
		// 	#define ASE_SRP_VERSION 70503
		// 	#define REQUIRE_DEPTH_TEXTURE 1

		// 	#pragma prefer_hlslcc gles
		// 	#pragma exclude_renderers d3d11_9x

		// 	#pragma vertex vert
		// 	#pragma fragment frag

		// 	#define SHADERPASS_DEPTHONLY

		// 	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		// 	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
		// 	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
		// 	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

		// 	#define ASE_NEEDS_VERT_NORMAL
		// 	#define ASE_NEEDS_FRAG_WORLD_POSITION
		// 	#define ASE_NEEDS_VERT_POSITION


		// 	struct VertexInput
		// 	{
		// 		float4 vertex : POSITION;
		// 		float3 ase_normal : NORMAL;
				
		// 		UNITY_VERTEX_INPUT_INSTANCE_ID
		// 	};

		// 	struct VertexOutput
		// 	{
		// 		float4 clipPos : SV_POSITION;
		// 		#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
		// 		float3 worldPos : TEXCOORD0;
		// 		#endif
		// 		#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
		// 		float4 shadowCoord : TEXCOORD1;
		// 		#endif
		// 		float4 ase_texcoord2 : TEXCOORD2;
		// 		float4 ase_texcoord3 : TEXCOORD3;
		// 		UNITY_VERTEX_INPUT_INSTANCE_ID
		// 		UNITY_VERTEX_OUTPUT_STEREO
		// 	};

		// 	CBUFFER_START(UnityPerMaterial)
		// 	float4 _CausticsColor;
		// 	float4 _DepthColor;
		// 	float4 _DepthColor1;
		// 	float4 _NormalDirection;
		// 	float _WavesSpeed;
		// 	float _SmoothnessPower;
		// 	float _MetallicPower;
		// 	float _CausticsSpeed;
		// 	float _CausticsScale;
		// 	float _Depth;
		// 	float _NormalPower;
		// 	float _NormalScale;
		// 	float _NormalSpeed;
		// 	float _RefractionPower;
		// 	float _FoamPower;
		// 	float _FoamScale;
		// 	float _FoamSpeed;
		// 	float _FoamDistance;
		// 	float _EdgesFade;
		// 	float _FoamContrast;
		// 	float _WavesHeight;
		// 	float _WavesScale;
		// 	float _DistanceFade;
		// 	float _DistanceFadeOffset;
		// 	#ifdef _TRANSMISSION_ASE
		// 		float _TransmissionShadow;
		// 	#endif
		// 	#ifdef _TRANSLUCENCY_ASE
		// 		float _TransStrength;
		// 		float _TransNormal;
		// 		float _TransScattering;
		// 		float _TransDirect;
		// 		float _TransAmbient;
		// 		float _TransShadow;
		// 	#endif
		// 	CBUFFER_END
		// 	uniform float4 _CameraDepthTexture_TexelSize;


		// 	float3 mod2D289( float3 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }
		// 	float2 mod2D289( float2 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }
		// 	float3 permute( float3 x ) { return mod2D289( ( ( x * 34.0 ) + 1.0 ) * x ); }
		// 	float snoise( float2 v )
		// 	{
		// 		const float4 C = float4( 0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439 );
		// 		float2 i = floor( v + dot( v, C.yy ) );
		// 		float2 x0 = v - i + dot( i, C.xx );
		// 		float2 i1;
		// 		i1 = ( x0.x > x0.y ) ? float2( 1.0, 0.0 ) : float2( 0.0, 1.0 );
		// 		float4 x12 = x0.xyxy + C.xxzz;
		// 		x12.xy -= i1;
		// 		i = mod2D289( i );
		// 		float3 p = permute( permute( i.y + float3( 0.0, i1.y, 1.0 ) ) + i.x + float3( 0.0, i1.x, 1.0 ) );
		// 		float3 m = max( 0.5 - float3( dot( x0, x0 ), dot( x12.xy, x12.xy ), dot( x12.zw, x12.zw ) ), 0.0 );
		// 		m = m * m;
		// 		m = m * m;
		// 		float3 x = 2.0 * frac( p * C.www ) - 1.0;
		// 		float3 h = abs( x ) - 0.5;
		// 		float3 ox = floor( x + 0.5 );
		// 		float3 a0 = x - ox;
		// 		m *= 1.79284291400159 - 0.85373472095314 * ( a0 * a0 + h * h );
		// 		float3 g;
		// 		g.x = a0.x * x0.x + h.x * x0.y;
		// 		g.yz = a0.yz * x12.xz + h.yz * x12.yw;
		// 		return 130.0 * dot( m, g );
		// 	}
			

		// 	VertexOutput VertexFunction( VertexInput v  )
		// 	{
		// 		VertexOutput o = (VertexOutput)0;
		// 		UNITY_SETUP_INSTANCE_ID(v);
		// 		UNITY_TRANSFER_INSTANCE_ID(v, o);
		// 		UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

		// 		float2 appendResult195 = (float2(_WavesSpeed , _WavesSpeed));
		// 		float3 ase_worldPos = mul(GetObjectToWorldMatrix(), v.vertex).xyz;
		// 		float2 appendResult194 = (float2(ase_worldPos.x , ase_worldPos.z));
		// 		float2 panner196 = ( 1.0 * _Time.y * appendResult195 + appendResult194);
		// 		float simplePerlin2D199 = snoise( panner196*( _WavesScale / 100.0 ) );
		// 		simplePerlin2D199 = simplePerlin2D199*0.5 + 0.5;
		// 		float3 worldToObjDir273 = mul( GetWorldToObjectMatrix(), float4( ( v.ase_normal * ( simplePerlin2D199 * _WavesHeight ) ), 0 ) ).xyz;
		// 		float3 WavesHeight49 = worldToObjDir273;
				
		// 		float4 ase_clipPos = TransformObjectToHClip((v.vertex).xyz);
		// 		float4 screenPos = ComputeScreenPos(ase_clipPos);
		// 		o.ase_texcoord2 = screenPos;
		// 		float3 objectToViewPos = TransformWorldToView(TransformObjectToWorld(v.vertex.xyz));
		// 		float eyeDepth = -objectToViewPos.z;
		// 		o.ase_texcoord3.x = eyeDepth;
				
				
		// 		//setting value to unused interpolator channels and avoid initialization warnings
		// 		o.ase_texcoord3.yzw = 0;
		// 		#ifdef ASE_ABSOLUTE_VERTEX_POS
		// 			float3 defaultVertexValue = v.vertex.xyz;
		// 		#else
		// 			float3 defaultVertexValue = float3(0, 0, 0);
		// 		#endif
		// 		float3 vertexValue = WavesHeight49;
		// 		#ifdef ASE_ABSOLUTE_VERTEX_POS
		// 			v.vertex.xyz = vertexValue;
		// 		#else
		// 			v.vertex.xyz += vertexValue;
		// 		#endif

		// 		v.ase_normal = v.ase_normal;
		// 		float3 positionWS = TransformObjectToWorld( v.vertex.xyz );
		// 		float4 positionCS = TransformWorldToHClip( positionWS );

		// 		#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
		// 		o.worldPos = positionWS;
		// 		#endif

		// 		#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
		// 			VertexPositionInputs vertexInput = (VertexPositionInputs)0;
		// 			vertexInput.positionWS = positionWS;
		// 			vertexInput.positionCS = positionCS;
		// 			o.shadowCoord = GetShadowCoord( vertexInput );
		// 		#endif
		// 		o.clipPos = positionCS;
		// 		return o;
		// 	}

		// 	VertexOutput vert ( VertexInput v )
		// 	{
		// 		return VertexFunction( v );
		// 	}

		// 	#if defined(ASE_EARLY_Z_DEPTH_OPTIMIZE)
		// 		#define ASE_SV_DEPTH SV_DepthLessEqual  
		// 	#else
		// 		#define ASE_SV_DEPTH SV_Depth
		// 	#endif
		// 	half4 frag(	VertexOutput IN 
		// 				#ifdef ASE_DEPTH_WRITE_ON
		// 				,out float outputDepth : ASE_SV_DEPTH
		// 				#endif
		// 				 ) : SV_TARGET
		// 	{
		// 		UNITY_SETUP_INSTANCE_ID(IN);
		// 		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

		// 		#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
		// 		float3 WorldPosition = IN.worldPos;
		// 		#endif
		// 		float4 ShadowCoords = float4( 0, 0, 0, 0 );

		// 		#if defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
		// 			#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
		// 				ShadowCoords = IN.shadowCoord;
		// 			#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
		// 				ShadowCoords = TransformWorldToShadowCoord( WorldPosition );
		// 			#endif
		// 		#endif

		// 		float3 ase_worldViewDir = ( _WorldSpaceCameraPos.xyz - WorldPosition );
		// 		ase_worldViewDir = normalize(ase_worldViewDir);
		// 		float temp_output_368_0 = abs( ase_worldViewDir.y );
		// 		float4 screenPos = IN.ase_texcoord2;
		// 		float4 ase_screenPosNorm = screenPos / screenPos.w;
		// 		ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
		// 		float screenDepth241 = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH( ase_screenPosNorm.xy ),_ZBufferParams);
		// 		float distanceDepth241 = abs( ( screenDepth241 - LinearEyeDepth( ase_screenPosNorm.z,_ZBufferParams ) ) / ( _EdgesFade ) );
		// 		float temp_output_377_0 = ( temp_output_368_0 * distanceDepth241 );
		// 		float clampResult249 = clamp( temp_output_377_0 , 0.0 , 1.0 );
		// 		float eyeDepth = IN.ase_texcoord3.x;
		// 		float cameraDepthFade293 = (( eyeDepth -_ProjectionParams.y - _DistanceFadeOffset ) / _DistanceFade);
		// 		float Opacity263 = ( clampResult249 * saturate( ( 1.0 - ( temp_output_368_0 * cameraDepthFade293 ) ) ) );
				
		// 		float Alpha = Opacity263;
		// 		float AlphaClipThreshold = 0.5;
		// 		#ifdef ASE_DEPTH_WRITE_ON
		// 		float DepthValue = 0;
		// 		#endif

		// 		#ifdef _ALPHATEST_ON
		// 			clip(Alpha - AlphaClipThreshold);
		// 		#endif

		// 		#ifdef LOD_FADE_CROSSFADE
		// 			LODDitheringTransition( IN.clipPos.xyz, unity_LODFade.x );
		// 		#endif
		// 		#ifdef ASE_DEPTH_WRITE_ON
		// 		outputDepth = DepthValue;
		// 		#endif
		// 		return 0;
		// 	}
		// 	ENDHLSL
		// }

		
		// Pass
		// {
			
		// 	Name "Meta"
		// 	Tags { "LightMode"="Meta" }

		// 	Cull Off

		// 	HLSLPROGRAM
		// 	#define _NORMAL_DROPOFF_TS 1
		// 	// #pragma multi_compile_instancing
		// 	// #pragma multi_compile _ LOD_FADE_CROSSFADE
		// 	#pragma multi_compile_fog
		// 	#define ASE_FOG 1
		// 	#define _NORMALMAP 1
		// 	#define ASE_SRP_VERSION 70503
		// 	#define REQUIRE_DEPTH_TEXTURE 1
		// 	#define REQUIRE_OPAQUE_TEXTURE 1

		// 	#pragma prefer_hlslcc gles
		// 	#pragma exclude_renderers d3d11_9x

		// 	#pragma vertex vert
		// 	#pragma fragment frag

		// 	#define SHADERPASS_META

		// 	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		// 	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"
		// 	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
		// 	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

		// 	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
		// 	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
		// 	#define ASE_NEEDS_VERT_NORMAL
		// 	#define ASE_NEEDS_FRAG_WORLD_POSITION
		// 	#define ASE_NEEDS_VERT_POSITION


		// 	#pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

		// 	struct VertexInput
		// 	{
		// 		float4 vertex : POSITION;
		// 		float3 ase_normal : NORMAL;
		// 		float4 texcoord1 : TEXCOORD1;
		// 		float4 texcoord2 : TEXCOORD2;
				
		// 		UNITY_VERTEX_INPUT_INSTANCE_ID
		// 	};

		// 	struct VertexOutput
		// 	{
		// 		float4 clipPos : SV_POSITION;
		// 		#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
		// 		float3 worldPos : TEXCOORD0;
		// 		#endif
		// 		#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
		// 		float4 shadowCoord : TEXCOORD1;
		// 		#endif
		// 		float4 ase_texcoord2 : TEXCOORD2;
		// 		float4 ase_texcoord3 : TEXCOORD3;
		// 		UNITY_VERTEX_INPUT_INSTANCE_ID
		// 		UNITY_VERTEX_OUTPUT_STEREO
		// 	};

		// 	CBUFFER_START(UnityPerMaterial)
		// 	float4 _CausticsColor;
		// 	float4 _DepthColor;
		// 	float4 _DepthColor1;
		// 	float4 _NormalDirection;
		// 	float _WavesSpeed;
		// 	float _SmoothnessPower;
		// 	float _MetallicPower;
		// 	float _CausticsSpeed;
		// 	float _CausticsScale;
		// 	float _Depth;
		// 	float _NormalPower;
		// 	float _NormalScale;
		// 	float _NormalSpeed;
		// 	float _RefractionPower;
		// 	float _FoamPower;
		// 	float _FoamScale;
		// 	float _FoamSpeed;
		// 	float _FoamDistance;
		// 	float _EdgesFade;
		// 	float _FoamContrast;
		// 	float _WavesHeight;
		// 	float _WavesScale;
		// 	float _DistanceFade;
		// 	float _DistanceFadeOffset;
		// 	#ifdef _TRANSMISSION_ASE
		// 		float _TransmissionShadow;
		// 	#endif
		// 	#ifdef _TRANSLUCENCY_ASE
		// 		float _TransStrength;
		// 		float _TransNormal;
		// 		float _TransScattering;
		// 		float _TransDirect;
		// 		float _TransAmbient;
		// 		float _TransShadow;
		// 	#endif
		// 	CBUFFER_END
		// 	uniform float4 _CameraDepthTexture_TexelSize;
		// 	sampler2D _FoamMask;
		// 	sampler2D _NormalMap;
		// 	sampler2D _FoamNormal;


		// 	float3 mod2D289( float3 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }
		// 	float2 mod2D289( float2 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }
		// 	float3 permute( float3 x ) { return mod2D289( ( ( x * 34.0 ) + 1.0 ) * x ); }
		// 	float snoise( float2 v )
		// 	{
		// 		const float4 C = float4( 0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439 );
		// 		float2 i = floor( v + dot( v, C.yy ) );
		// 		float2 x0 = v - i + dot( i, C.xx );
		// 		float2 i1;
		// 		i1 = ( x0.x > x0.y ) ? float2( 1.0, 0.0 ) : float2( 0.0, 1.0 );
		// 		float4 x12 = x0.xyxy + C.xxzz;
		// 		x12.xy -= i1;
		// 		i = mod2D289( i );
		// 		float3 p = permute( permute( i.y + float3( 0.0, i1.y, 1.0 ) ) + i.x + float3( 0.0, i1.x, 1.0 ) );
		// 		float3 m = max( 0.5 - float3( dot( x0, x0 ), dot( x12.xy, x12.xy ), dot( x12.zw, x12.zw ) ), 0.0 );
		// 		m = m * m;
		// 		m = m * m;
		// 		float3 x = 2.0 * frac( p * C.www ) - 1.0;
		// 		float3 h = abs( x ) - 0.5;
		// 		float3 ox = floor( x + 0.5 );
		// 		float3 a0 = x - ox;
		// 		m *= 1.79284291400159 - 0.85373472095314 * ( a0 * a0 + h * h );
		// 		float3 g;
		// 		g.x = a0.x * x0.x + h.x * x0.y;
		// 		g.yz = a0.yz * x12.xz + h.yz * x12.yw;
		// 		return 130.0 * dot( m, g );
		// 	}
			
		// 	float4 CalculateContrast( float contrastValue, float4 colorTarget )
		// 	{
		// 		float t = 0.5 * ( 1.0 - contrastValue );
		// 		return mul( float4x4( contrastValue,0,0,t, 0,contrastValue,0,t, 0,0,contrastValue,t, 0,0,0,1 ), colorTarget );
		// 	}
		// 	inline float4 ASE_ComputeGrabScreenPos( float4 pos )
		// 	{
		// 		#if UNITY_UV_STARTS_AT_TOP
		// 		float scale = -1.0;
		// 		#else
		// 		float scale = 1.0;
		// 		#endif
		// 		float4 o = pos;
		// 		o.y = pos.w * 0.5f;
		// 		o.y = ( pos.y - o.y ) * _ProjectionParams.x * scale + o.y;
		// 		return o;
		// 	}
			
		// 			float2 voronoihash110( float2 p )
		// 			{
						
		// 				p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
		// 				return frac( sin( p ) *43758.5453);
		// 			}
			
		// 			float voronoi110( float2 v, float time, inout float2 id, inout float2 mr, float smoothness )
		// 			{
		// 				float2 n = floor( v );
		// 				float2 f = frac( v );
		// 				float F1 = 8.0;
		// 				float F2 = 8.0; float2 mg = 0;
		// 				for ( int j = -1; j <= 1; j++ )
		// 				{
		// 					for ( int i = -1; i <= 1; i++ )
		// 				 	{
		// 				 		float2 g = float2( i, j );
		// 				 		float2 o = voronoihash110( n + g );
		// 						o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
		// 						float d = 0.5 * dot( r, r );
		// 				 		if( d<F1 ) {
		// 				 			F2 = F1;
		// 				 			F1 = d; mg = g; mr = r; id = o;
		// 				 		} else if( d<F2 ) {
		// 				 			F2 = d;
		// 				 		}
		// 				 	}
		// 				}
		// 				return F1;
		// 			}
			
		// 			float2 voronoihash129( float2 p )
		// 			{
						
		// 				p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
		// 				return frac( sin( p ) *43758.5453);
		// 			}
			
		// 			float voronoi129( float2 v, float time, inout float2 id, inout float2 mr, float smoothness )
		// 			{
		// 				float2 n = floor( v );
		// 				float2 f = frac( v );
		// 				float F1 = 8.0;
		// 				float F2 = 8.0; float2 mg = 0;
		// 				for ( int j = -1; j <= 1; j++ )
		// 				{
		// 					for ( int i = -1; i <= 1; i++ )
		// 				 	{
		// 				 		float2 g = float2( i, j );
		// 				 		float2 o = voronoihash129( n + g );
		// 						o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
		// 						float d = 0.5 * dot( r, r );
		// 				 		if( d<F1 ) {
		// 				 			F2 = F1;
		// 				 			F1 = d; mg = g; mr = r; id = o;
		// 				 		} else if( d<F2 ) {
		// 				 			F2 = d;
		// 				 		}
		// 				 	}
		// 				}
		// 				return F1;
		// 			}
			

		// 	VertexOutput VertexFunction( VertexInput v  )
		// 	{
		// 		VertexOutput o = (VertexOutput)0;
		// 		UNITY_SETUP_INSTANCE_ID(v);
		// 		UNITY_TRANSFER_INSTANCE_ID(v, o);
		// 		UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

		// 		float2 appendResult195 = (float2(_WavesSpeed , _WavesSpeed));
		// 		float3 ase_worldPos = mul(GetObjectToWorldMatrix(), v.vertex).xyz;
		// 		float2 appendResult194 = (float2(ase_worldPos.x , ase_worldPos.z));
		// 		float2 panner196 = ( 1.0 * _Time.y * appendResult195 + appendResult194);
		// 		float simplePerlin2D199 = snoise( panner196*( _WavesScale / 100.0 ) );
		// 		simplePerlin2D199 = simplePerlin2D199*0.5 + 0.5;
		// 		float3 worldToObjDir273 = mul( GetWorldToObjectMatrix(), float4( ( v.ase_normal * ( simplePerlin2D199 * _WavesHeight ) ), 0 ) ).xyz;
		// 		float3 WavesHeight49 = worldToObjDir273;
				
		// 		float4 ase_clipPos = TransformObjectToHClip((v.vertex).xyz);
		// 		float4 screenPos = ComputeScreenPos(ase_clipPos);
		// 		o.ase_texcoord2 = screenPos;
				
		// 		float3 objectToViewPos = TransformWorldToView(TransformObjectToWorld(v.vertex.xyz));
		// 		float eyeDepth = -objectToViewPos.z;
		// 		o.ase_texcoord3.x = eyeDepth;
				
				
		// 		//setting value to unused interpolator channels and avoid initialization warnings
		// 		o.ase_texcoord3.yzw = 0;
				
		// 		#ifdef ASE_ABSOLUTE_VERTEX_POS
		// 			float3 defaultVertexValue = v.vertex.xyz;
		// 		#else
		// 			float3 defaultVertexValue = float3(0, 0, 0);
		// 		#endif
		// 		float3 vertexValue = WavesHeight49;
		// 		#ifdef ASE_ABSOLUTE_VERTEX_POS
		// 			v.vertex.xyz = vertexValue;
		// 		#else
		// 			v.vertex.xyz += vertexValue;
		// 		#endif

		// 		v.ase_normal = v.ase_normal;

		// 		float3 positionWS = TransformObjectToWorld( v.vertex.xyz );
		// 		#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
		// 		o.worldPos = positionWS;
		// 		#endif

		// 		o.clipPos = MetaVertexPosition( v.vertex, v.texcoord1.xy, v.texcoord1.xy, unity_LightmapST, unity_DynamicLightmapST );
		// 		#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
		// 			VertexPositionInputs vertexInput = (VertexPositionInputs)0;
		// 			vertexInput.positionWS = positionWS;
		// 			vertexInput.positionCS = o.clipPos;
		// 			o.shadowCoord = GetShadowCoord( vertexInput );
		// 		#endif
		// 		return o;
		// 	}

		// 	VertexOutput vert ( VertexInput v )
		// 	{
		// 		return VertexFunction( v );
		// 	}

		// 	half4 frag(VertexOutput IN  ) : SV_TARGET
		// 	{
		// 		UNITY_SETUP_INSTANCE_ID(IN);
		// 		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

		// 		#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
		// 		float3 WorldPosition = IN.worldPos;
		// 		#endif
		// 		float4 ShadowCoords = float4( 0, 0, 0, 0 );

		// 		#if defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
		// 			#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
		// 				ShadowCoords = IN.shadowCoord;
		// 			#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
		// 				ShadowCoords = TransformWorldToShadowCoord( WorldPosition );
		// 			#endif
		// 		#endif

		// 		float3 ase_worldViewDir = ( _WorldSpaceCameraPos.xyz - WorldPosition );
		// 		ase_worldViewDir = normalize(ase_worldViewDir);
		// 		float temp_output_368_0 = abs( ase_worldViewDir.y );
		// 		float4 screenPos = IN.ase_texcoord2;
		// 		float4 ase_screenPosNorm = screenPos / screenPos.w;
		// 		ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
		// 		float screenDepth241 = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH( ase_screenPosNorm.xy ),_ZBufferParams);
		// 		float distanceDepth241 = abs( ( screenDepth241 - LinearEyeDepth( ase_screenPosNorm.z,_ZBufferParams ) ) / ( _EdgesFade ) );
		// 		float temp_output_377_0 = ( temp_output_368_0 * distanceDepth241 );
		// 		float screenDepth230 = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH( ase_screenPosNorm.xy ),_ZBufferParams);
		// 		float distanceDepth230 = abs( ( screenDepth230 - LinearEyeDepth( ase_screenPosNorm.z,_ZBufferParams ) ) / ( ( _FoamDistance * 0.1 ) ) );
		// 		float screenDepth4 = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH( ase_screenPosNorm.xy ),_ZBufferParams);
		// 		float distanceDepth4 = abs( ( screenDepth4 - LinearEyeDepth( ase_screenPosNorm.z,_ZBufferParams ) ) / ( _FoamDistance ) );
		// 		float temp_output_6_0 = ( 1.0 - ( temp_output_368_0 * distanceDepth4 ) );
		// 		float temp_output_215_0 = ( _FoamSpeed / 100.0 );
		// 		float2 temp_cast_0 = (temp_output_215_0).xx;
		// 		float4 appendResult31 = (float4(WorldPosition.x , WorldPosition.z , 0.0 , 0.0));
		// 		float4 WorldSpace32 = appendResult31;
		// 		float4 temp_output_225_0 = ( WorldSpace32 * ( _FoamScale / 100.0 ) );
		// 		float2 panner213 = ( 1.0 * _Time.y * temp_cast_0 + temp_output_225_0.xy);
		// 		float2 temp_cast_2 = (temp_output_215_0).xx;
		// 		float2 panner254 = ( 1.0 * _Time.y * temp_cast_2 + ( 1.0 - temp_output_225_0 ).xy);
		// 		float4 temp_cast_4 = (( temp_output_377_0 * ( ( ( 1.0 - ( temp_output_368_0 * distanceDepth230 ) ) + ( temp_output_6_0 * pow( ( temp_output_6_0 * ( tex2D( _FoamMask, panner213 ).r * tex2D( _FoamMask, panner254 ).r ) ) , ( 1.0 - 0.5 ) ) ) ) * _FoamPower ) )).xxxx;
		// 		float4 clampResult9 = clamp( CalculateContrast(_FoamContrast,temp_cast_4) , float4( 0,0,0,0 ) , float4( 1,1,1,0 ) );
		// 		float4 Edges62 = clampResult9;
		// 		float4 ase_grabScreenPos = ASE_ComputeGrabScreenPos( screenPos );
		// 		float4 ase_grabScreenPosNorm = ase_grabScreenPos / ase_grabScreenPos.w;
		// 		float2 appendResult337 = (float2(_NormalDirection.x , _NormalDirection.y));
		// 		float temp_output_106_0 = ( _NormalSpeed / 100.0 );
		// 		float4 temp_output_68_0 = ( ( WorldSpace32 / 100.0 ) * _NormalScale );
		// 		float2 panner72 = ( 1.0 * _Time.y * ( appendResult337 * temp_output_106_0 ) + temp_output_68_0.xy);
		// 		float3 unpack65 = UnpackNormalScale( tex2D( _NormalMap, panner72 ), _NormalPower );
		// 		unpack65.z = lerp( 1, unpack65.z, saturate(_NormalPower) );
		// 		float2 appendResult338 = (float2(_NormalDirection.z , _NormalDirection.w));
		// 		float2 panner73 = ( 1.0 * _Time.y * ( appendResult338 * ( temp_output_106_0 * 2.0 ) ) + ( temp_output_68_0 * ( _NormalScale * 1.2 ) ).xy);
		// 		float3 unpack66 = UnpackNormalScale( tex2D( _NormalMap, panner73 ), _NormalPower );
		// 		unpack66.z = lerp( 1, unpack66.z, saturate(_NormalPower) );
		// 		float3 lerpResult237 = lerp( BlendNormal( unpack65 , unpack66 ) , UnpackNormalScale( tex2D( _FoamNormal, panner213 ), 1.0f ) , Edges62.rgb);
		// 		float3 Normals81 = lerpResult237;
		// 		float4 fetchOpaqueVal20 = float4( SHADERGRAPH_SAMPLE_SCENE_COLOR( ( ase_grabScreenPosNorm + float4( ( ( _RefractionPower / 10.0 ) * Normals81 ) , 0.0 ) ).xy ), 1.0 );
		// 		float4 clampResult21 = clamp( fetchOpaqueVal20 , float4( 0,0,0,0 ) , float4( 1,1,1,0 ) );
		// 		float4 Refraction60 = clampResult21;
		// 		float screenDepth95 = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH( ase_screenPosNorm.xy ),_ZBufferParams);
		// 		float distanceDepth95 = abs( ( screenDepth95 - LinearEyeDepth( ase_screenPosNorm.z,_ZBufferParams ) ) / ( _Depth ) );
		// 		float3 temp_cast_10 = (( 1.0 - saturate( ( distanceDepth95 * abs( ase_worldViewDir.y ) ) ) )).xxx;
		// 		float3 temp_cast_11 = (( 1.0 - saturate( ( distanceDepth95 * abs( ase_worldViewDir.y ) ) ) )).xxx;
		// 		float3 gammaToLinear330 = FastSRGBToLinear( temp_cast_11 );
		// 		float Depth98 = gammaToLinear330.x;
		// 		float4 lerpResult326 = lerp( _DepthColor1 , _DepthColor , ( 1.0 - Depth98 ));
		// 		float mulTime27 = _TimeParameters.x * _CausticsSpeed;
		// 		float time110 = ( mulTime27 * 1.0 );
		// 		float2 coords110 = WorldSpace32.xy * ( _CausticsScale * 0.5 );
		// 		float2 id110 = 0;
		// 		float2 uv110 = 0;
		// 		float voroi110 = voronoi110( coords110, time110, id110, uv110, 0 );
		// 		float time129 = mulTime27;
		// 		float2 coords129 = WorldSpace32.xy * _CausticsScale;
		// 		float2 id129 = 0;
		// 		float2 uv129 = 0;
		// 		float voroi129 = voronoi129( coords129, time129, id129, uv129, 0 );
		// 		float Caustics47 = saturate( ( voroi110 + voroi129 ) );
		// 		float clampResult56 = clamp( Caustics47 , 0.0 , 1.0 );
		// 		float4 lerpResult52 = lerp( lerpResult326 , _CausticsColor , clampResult56);
		// 		float4 blendOpSrc173 = Refraction60;
		// 		float4 blendOpDest173 = lerpResult52;
		// 		float4 lerpBlendMode173 = lerp(blendOpDest173,(( blendOpDest173 > 0.5 ) ? ( 1.0 - 2.0 * ( 1.0 - blendOpDest173 ) * ( 1.0 - blendOpSrc173 ) ) : ( 2.0 * blendOpDest173 * blendOpSrc173 ) ),( 1.0 - _DepthColor.a ));
		// 		float4 Albedo58 = ( saturate( lerpBlendMode173 ));
		// 		float4 lerpResult100 = lerp( Albedo58 , Refraction60 , Depth98);
		// 		float4 clampResult109 = clamp( ( Edges62 + lerpResult100 ) , float4( 0,0,0,0 ) , float4( 1,1,1,0 ) );
				
		// 		float clampResult249 = clamp( temp_output_377_0 , 0.0 , 1.0 );
		// 		float eyeDepth = IN.ase_texcoord3.x;
		// 		float cameraDepthFade293 = (( eyeDepth -_ProjectionParams.y - _DistanceFadeOffset ) / _DistanceFade);
		// 		float Opacity263 = ( clampResult249 * saturate( ( 1.0 - ( temp_output_368_0 * cameraDepthFade293 ) ) ) );
				
				
		// 		float3 Albedo = clampResult109.rgb;
		// 		float3 Emission = 0;
		// 		float Alpha = Opacity263;
		// 		float AlphaClipThreshold = 0.5;

		// 		#ifdef _ALPHATEST_ON
		// 			clip(Alpha - AlphaClipThreshold);
		// 		#endif

		// 		MetaInput metaInput = (MetaInput)0;
		// 		metaInput.Albedo = Albedo;
		// 		metaInput.Emission = Emission;
				
		// 		return MetaFragment(metaInput);
		// 	}
		// 	ENDHLSL
		// }

		
		// Pass
		// {
			
		// 	Name "Universal2D"
		// 	Tags { "LightMode"="Universal2D" }

		// 	Blend One Zero, One OneMinusSrcAlpha
		// 	ZWrite Off
		// 	ZTest LEqual
		// 	Offset 0,0
		// 	ColorMask RGBA

		// 	HLSLPROGRAM
		// 	#define _NORMAL_DROPOFF_TS 1
		// 	// #pragma multi_compile_instancing
		// 	// #pragma multi_compile _ LOD_FADE_CROSSFADE
		// 	#pragma multi_compile_fog
		// 	#define ASE_FOG 1
		// 	#define _NORMALMAP 1
		// 	#define ASE_SRP_VERSION 70503
		// 	#define REQUIRE_DEPTH_TEXTURE 1
		// 	#define REQUIRE_OPAQUE_TEXTURE 1

		// 	#pragma prefer_hlslcc gles
		// 	#pragma exclude_renderers d3d11_9x

		// 	#pragma vertex vert
		// 	#pragma fragment frag

		// 	#define SHADERPASS_2D

		// 	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		// 	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
		// 	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
		// 	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
		// 	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			
		// 	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
		// 	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
		// 	#define ASE_NEEDS_VERT_NORMAL
		// 	#define ASE_NEEDS_FRAG_WORLD_POSITION
		// 	#define ASE_NEEDS_VERT_POSITION


		// 	#pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

		// 	struct VertexInput
		// 	{
		// 		float4 vertex : POSITION;
		// 		float3 ase_normal : NORMAL;
				
		// 		UNITY_VERTEX_INPUT_INSTANCE_ID
		// 	};

		// 	struct VertexOutput
		// 	{
		// 		float4 clipPos : SV_POSITION;
		// 		#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
		// 		float3 worldPos : TEXCOORD0;
		// 		#endif
		// 		#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
		// 		float4 shadowCoord : TEXCOORD1;
		// 		#endif
		// 		float4 ase_texcoord2 : TEXCOORD2;
		// 		float4 ase_texcoord3 : TEXCOORD3;
		// 		UNITY_VERTEX_INPUT_INSTANCE_ID
		// 		UNITY_VERTEX_OUTPUT_STEREO
		// 	};

		// 	CBUFFER_START(UnityPerMaterial)
		// 	float4 _CausticsColor;
		// 	float4 _DepthColor;
		// 	float4 _DepthColor1;
		// 	float4 _NormalDirection;
		// 	float _WavesSpeed;
		// 	float _SmoothnessPower;
		// 	float _MetallicPower;
		// 	float _CausticsSpeed;
		// 	float _CausticsScale;
		// 	float _Depth;
		// 	float _NormalPower;
		// 	float _NormalScale;
		// 	float _NormalSpeed;
		// 	float _RefractionPower;
		// 	float _FoamPower;
		// 	float _FoamScale;
		// 	float _FoamSpeed;
		// 	float _FoamDistance;
		// 	float _EdgesFade;
		// 	float _FoamContrast;
		// 	float _WavesHeight;
		// 	float _WavesScale;
		// 	float _DistanceFade;
		// 	float _DistanceFadeOffset;
		// 	#ifdef _TRANSMISSION_ASE
		// 		float _TransmissionShadow;
		// 	#endif
		// 	#ifdef _TRANSLUCENCY_ASE
		// 		float _TransStrength;
		// 		float _TransNormal;
		// 		float _TransScattering;
		// 		float _TransDirect;
		// 		float _TransAmbient;
		// 		float _TransShadow;
		// 	#endif
		// 	CBUFFER_END
		// 	uniform float4 _CameraDepthTexture_TexelSize;
		// 	sampler2D _FoamMask;
		// 	sampler2D _NormalMap;
		// 	sampler2D _FoamNormal;


		// 	float3 mod2D289( float3 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }
		// 	float2 mod2D289( float2 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }
		// 	float3 permute( float3 x ) { return mod2D289( ( ( x * 34.0 ) + 1.0 ) * x ); }
		// 	float snoise( float2 v )
		// 	{
		// 		const float4 C = float4( 0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439 );
		// 		float2 i = floor( v + dot( v, C.yy ) );
		// 		float2 x0 = v - i + dot( i, C.xx );
		// 		float2 i1;
		// 		i1 = ( x0.x > x0.y ) ? float2( 1.0, 0.0 ) : float2( 0.0, 1.0 );
		// 		float4 x12 = x0.xyxy + C.xxzz;
		// 		x12.xy -= i1;
		// 		i = mod2D289( i );
		// 		float3 p = permute( permute( i.y + float3( 0.0, i1.y, 1.0 ) ) + i.x + float3( 0.0, i1.x, 1.0 ) );
		// 		float3 m = max( 0.5 - float3( dot( x0, x0 ), dot( x12.xy, x12.xy ), dot( x12.zw, x12.zw ) ), 0.0 );
		// 		m = m * m;
		// 		m = m * m;
		// 		float3 x = 2.0 * frac( p * C.www ) - 1.0;
		// 		float3 h = abs( x ) - 0.5;
		// 		float3 ox = floor( x + 0.5 );
		// 		float3 a0 = x - ox;
		// 		m *= 1.79284291400159 - 0.85373472095314 * ( a0 * a0 + h * h );
		// 		float3 g;
		// 		g.x = a0.x * x0.x + h.x * x0.y;
		// 		g.yz = a0.yz * x12.xz + h.yz * x12.yw;
		// 		return 130.0 * dot( m, g );
		// 	}
			
		// 	float4 CalculateContrast( float contrastValue, float4 colorTarget )
		// 	{
		// 		float t = 0.5 * ( 1.0 - contrastValue );
		// 		return mul( float4x4( contrastValue,0,0,t, 0,contrastValue,0,t, 0,0,contrastValue,t, 0,0,0,1 ), colorTarget );
		// 	}
		// 	inline float4 ASE_ComputeGrabScreenPos( float4 pos )
		// 	{
		// 		#if UNITY_UV_STARTS_AT_TOP
		// 		float scale = -1.0;
		// 		#else
		// 		float scale = 1.0;
		// 		#endif
		// 		float4 o = pos;
		// 		o.y = pos.w * 0.5f;
		// 		o.y = ( pos.y - o.y ) * _ProjectionParams.x * scale + o.y;
		// 		return o;
		// 	}
			
		// 			float2 voronoihash110( float2 p )
		// 			{
						
		// 				p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
		// 				return frac( sin( p ) *43758.5453);
		// 			}
			
		// 			float voronoi110( float2 v, float time, inout float2 id, inout float2 mr, float smoothness )
		// 			{
		// 				float2 n = floor( v );
		// 				float2 f = frac( v );
		// 				float F1 = 8.0;
		// 				float F2 = 8.0; float2 mg = 0;
		// 				for ( int j = -1; j <= 1; j++ )
		// 				{
		// 					for ( int i = -1; i <= 1; i++ )
		// 				 	{
		// 				 		float2 g = float2( i, j );
		// 				 		float2 o = voronoihash110( n + g );
		// 						o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
		// 						float d = 0.5 * dot( r, r );
		// 				 		if( d<F1 ) {
		// 				 			F2 = F1;
		// 				 			F1 = d; mg = g; mr = r; id = o;
		// 				 		} else if( d<F2 ) {
		// 				 			F2 = d;
		// 				 		}
		// 				 	}
		// 				}
		// 				return F1;
		// 			}
			
		// 			float2 voronoihash129( float2 p )
		// 			{
						
		// 				p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
		// 				return frac( sin( p ) *43758.5453);
		// 			}
			
		// 			float voronoi129( float2 v, float time, inout float2 id, inout float2 mr, float smoothness )
		// 			{
		// 				float2 n = floor( v );
		// 				float2 f = frac( v );
		// 				float F1 = 8.0;
		// 				float F2 = 8.0; float2 mg = 0;
		// 				for ( int j = -1; j <= 1; j++ )
		// 				{
		// 					for ( int i = -1; i <= 1; i++ )
		// 				 	{
		// 				 		float2 g = float2( i, j );
		// 				 		float2 o = voronoihash129( n + g );
		// 						o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
		// 						float d = 0.5 * dot( r, r );
		// 				 		if( d<F1 ) {
		// 				 			F2 = F1;
		// 				 			F1 = d; mg = g; mr = r; id = o;
		// 				 		} else if( d<F2 ) {
		// 				 			F2 = d;
		// 				 		}
		// 				 	}
		// 				}
		// 				return F1;
		// 			}
			

		// 	VertexOutput VertexFunction( VertexInput v  )
		// 	{
		// 		VertexOutput o = (VertexOutput)0;
		// 		UNITY_SETUP_INSTANCE_ID( v );
		// 		UNITY_TRANSFER_INSTANCE_ID( v, o );
		// 		UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o );

		// 		float2 appendResult195 = (float2(_WavesSpeed , _WavesSpeed));
		// 		float3 ase_worldPos = mul(GetObjectToWorldMatrix(), v.vertex).xyz;
		// 		float2 appendResult194 = (float2(ase_worldPos.x , ase_worldPos.z));
		// 		float2 panner196 = ( 1.0 * _Time.y * appendResult195 + appendResult194);
		// 		float simplePerlin2D199 = snoise( panner196*( _WavesScale / 100.0 ) );
		// 		simplePerlin2D199 = simplePerlin2D199*0.5 + 0.5;
		// 		float3 worldToObjDir273 = mul( GetWorldToObjectMatrix(), float4( ( v.ase_normal * ( simplePerlin2D199 * _WavesHeight ) ), 0 ) ).xyz;
		// 		float3 WavesHeight49 = worldToObjDir273;
				
		// 		float4 ase_clipPos = TransformObjectToHClip((v.vertex).xyz);
		// 		float4 screenPos = ComputeScreenPos(ase_clipPos);
		// 		o.ase_texcoord2 = screenPos;
				
		// 		float3 objectToViewPos = TransformWorldToView(TransformObjectToWorld(v.vertex.xyz));
		// 		float eyeDepth = -objectToViewPos.z;
		// 		o.ase_texcoord3.x = eyeDepth;
				
				
		// 		//setting value to unused interpolator channels and avoid initialization warnings
		// 		o.ase_texcoord3.yzw = 0;
				
		// 		#ifdef ASE_ABSOLUTE_VERTEX_POS
		// 			float3 defaultVertexValue = v.vertex.xyz;
		// 		#else
		// 			float3 defaultVertexValue = float3(0, 0, 0);
		// 		#endif
		// 		float3 vertexValue = WavesHeight49;
		// 		#ifdef ASE_ABSOLUTE_VERTEX_POS
		// 			v.vertex.xyz = vertexValue;
		// 		#else
		// 			v.vertex.xyz += vertexValue;
		// 		#endif

		// 		v.ase_normal = v.ase_normal;

		// 		float3 positionWS = TransformObjectToWorld( v.vertex.xyz );
		// 		float4 positionCS = TransformWorldToHClip( positionWS );

		// 		#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
		// 		o.worldPos = positionWS;
		// 		#endif

		// 		#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
		// 			VertexPositionInputs vertexInput = (VertexPositionInputs)0;
		// 			vertexInput.positionWS = positionWS;
		// 			vertexInput.positionCS = positionCS;
		// 			o.shadowCoord = GetShadowCoord( vertexInput );
		// 		#endif

		// 		o.clipPos = positionCS;
		// 		return o;
		// 	}

		// 	VertexOutput vert ( VertexInput v )
		// 	{
		// 		return VertexFunction( v );
		// 	}

		// 	half4 frag(VertexOutput IN  ) : SV_TARGET
		// 	{
		// 		UNITY_SETUP_INSTANCE_ID( IN );
		// 		UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( IN );

		// 		#if defined(ASE_NEEDS_FRAG_WORLD_POSITION)
		// 		float3 WorldPosition = IN.worldPos;
		// 		#endif
		// 		float4 ShadowCoords = float4( 0, 0, 0, 0 );

		// 		#if defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
		// 			#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
		// 				ShadowCoords = IN.shadowCoord;
		// 			#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
		// 				ShadowCoords = TransformWorldToShadowCoord( WorldPosition );
		// 			#endif
		// 		#endif

		// 		float3 ase_worldViewDir = ( _WorldSpaceCameraPos.xyz - WorldPosition );
		// 		ase_worldViewDir = normalize(ase_worldViewDir);
		// 		float temp_output_368_0 = abs( ase_worldViewDir.y );
		// 		float4 screenPos = IN.ase_texcoord2;
		// 		float4 ase_screenPosNorm = screenPos / screenPos.w;
		// 		ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
		// 		float screenDepth241 = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH( ase_screenPosNorm.xy ),_ZBufferParams);
		// 		float distanceDepth241 = abs( ( screenDepth241 - LinearEyeDepth( ase_screenPosNorm.z,_ZBufferParams ) ) / ( _EdgesFade ) );
		// 		float temp_output_377_0 = ( temp_output_368_0 * distanceDepth241 );
		// 		float screenDepth230 = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH( ase_screenPosNorm.xy ),_ZBufferParams);
		// 		float distanceDepth230 = abs( ( screenDepth230 - LinearEyeDepth( ase_screenPosNorm.z,_ZBufferParams ) ) / ( ( _FoamDistance * 0.1 ) ) );
		// 		float screenDepth4 = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH( ase_screenPosNorm.xy ),_ZBufferParams);
		// 		float distanceDepth4 = abs( ( screenDepth4 - LinearEyeDepth( ase_screenPosNorm.z,_ZBufferParams ) ) / ( _FoamDistance ) );
		// 		float temp_output_6_0 = ( 1.0 - ( temp_output_368_0 * distanceDepth4 ) );
		// 		float temp_output_215_0 = ( _FoamSpeed / 100.0 );
		// 		float2 temp_cast_0 = (temp_output_215_0).xx;
		// 		float4 appendResult31 = (float4(WorldPosition.x , WorldPosition.z , 0.0 , 0.0));
		// 		float4 WorldSpace32 = appendResult31;
		// 		float4 temp_output_225_0 = ( WorldSpace32 * ( _FoamScale / 100.0 ) );
		// 		float2 panner213 = ( 1.0 * _Time.y * temp_cast_0 + temp_output_225_0.xy);
		// 		float2 temp_cast_2 = (temp_output_215_0).xx;
		// 		float2 panner254 = ( 1.0 * _Time.y * temp_cast_2 + ( 1.0 - temp_output_225_0 ).xy);
		// 		float4 temp_cast_4 = (( temp_output_377_0 * ( ( ( 1.0 - ( temp_output_368_0 * distanceDepth230 ) ) + ( temp_output_6_0 * pow( ( temp_output_6_0 * ( tex2D( _FoamMask, panner213 ).r * tex2D( _FoamMask, panner254 ).r ) ) , ( 1.0 - 0.5 ) ) ) ) * _FoamPower ) )).xxxx;
		// 		float4 clampResult9 = clamp( CalculateContrast(_FoamContrast,temp_cast_4) , float4( 0,0,0,0 ) , float4( 1,1,1,0 ) );
		// 		float4 Edges62 = clampResult9;
		// 		float4 ase_grabScreenPos = ASE_ComputeGrabScreenPos( screenPos );
		// 		float4 ase_grabScreenPosNorm = ase_grabScreenPos / ase_grabScreenPos.w;
		// 		float2 appendResult337 = (float2(_NormalDirection.x , _NormalDirection.y));
		// 		float temp_output_106_0 = ( _NormalSpeed / 100.0 );
		// 		float4 temp_output_68_0 = ( ( WorldSpace32 / 100.0 ) * _NormalScale );
		// 		float2 panner72 = ( 1.0 * _Time.y * ( appendResult337 * temp_output_106_0 ) + temp_output_68_0.xy);
		// 		float3 unpack65 = UnpackNormalScale( tex2D( _NormalMap, panner72 ), _NormalPower );
		// 		unpack65.z = lerp( 1, unpack65.z, saturate(_NormalPower) );
		// 		float2 appendResult338 = (float2(_NormalDirection.z , _NormalDirection.w));
		// 		float2 panner73 = ( 1.0 * _Time.y * ( appendResult338 * ( temp_output_106_0 * 2.0 ) ) + ( temp_output_68_0 * ( _NormalScale * 1.2 ) ).xy);
		// 		float3 unpack66 = UnpackNormalScale( tex2D( _NormalMap, panner73 ), _NormalPower );
		// 		unpack66.z = lerp( 1, unpack66.z, saturate(_NormalPower) );
		// 		float3 lerpResult237 = lerp( BlendNormal( unpack65 , unpack66 ) , UnpackNormalScale( tex2D( _FoamNormal, panner213 ), 1.0f ) , Edges62.rgb);
		// 		float3 Normals81 = lerpResult237;
		// 		float4 fetchOpaqueVal20 = float4( SHADERGRAPH_SAMPLE_SCENE_COLOR( ( ase_grabScreenPosNorm + float4( ( ( _RefractionPower / 10.0 ) * Normals81 ) , 0.0 ) ).xy ), 1.0 );
		// 		float4 clampResult21 = clamp( fetchOpaqueVal20 , float4( 0,0,0,0 ) , float4( 1,1,1,0 ) );
		// 		float4 Refraction60 = clampResult21;
		// 		float screenDepth95 = LinearEyeDepth(SHADERGRAPH_SAMPLE_SCENE_DEPTH( ase_screenPosNorm.xy ),_ZBufferParams);
		// 		float distanceDepth95 = abs( ( screenDepth95 - LinearEyeDepth( ase_screenPosNorm.z,_ZBufferParams ) ) / ( _Depth ) );
		// 		float3 temp_cast_10 = (( 1.0 - saturate( ( distanceDepth95 * abs( ase_worldViewDir.y ) ) ) )).xxx;
		// 		float3 temp_cast_11 = (( 1.0 - saturate( ( distanceDepth95 * abs( ase_worldViewDir.y ) ) ) )).xxx;
		// 		float3 gammaToLinear330 = FastSRGBToLinear( temp_cast_11 );
		// 		float Depth98 = gammaToLinear330.x;
		// 		float4 lerpResult326 = lerp( _DepthColor1 , _DepthColor , ( 1.0 - Depth98 ));
		// 		float mulTime27 = _TimeParameters.x * _CausticsSpeed;
		// 		float time110 = ( mulTime27 * 1.0 );
		// 		float2 coords110 = WorldSpace32.xy * ( _CausticsScale * 0.5 );
		// 		float2 id110 = 0;
		// 		float2 uv110 = 0;
		// 		float voroi110 = voronoi110( coords110, time110, id110, uv110, 0 );
		// 		float time129 = mulTime27;
		// 		float2 coords129 = WorldSpace32.xy * _CausticsScale;
		// 		float2 id129 = 0;
		// 		float2 uv129 = 0;
		// 		float voroi129 = voronoi129( coords129, time129, id129, uv129, 0 );
		// 		float Caustics47 = saturate( ( voroi110 + voroi129 ) );
		// 		float clampResult56 = clamp( Caustics47 , 0.0 , 1.0 );
		// 		float4 lerpResult52 = lerp( lerpResult326 , _CausticsColor , clampResult56);
		// 		float4 blendOpSrc173 = Refraction60;
		// 		float4 blendOpDest173 = lerpResult52;
		// 		float4 lerpBlendMode173 = lerp(blendOpDest173,(( blendOpDest173 > 0.5 ) ? ( 1.0 - 2.0 * ( 1.0 - blendOpDest173 ) * ( 1.0 - blendOpSrc173 ) ) : ( 2.0 * blendOpDest173 * blendOpSrc173 ) ),( 1.0 - _DepthColor.a ));
		// 		float4 Albedo58 = ( saturate( lerpBlendMode173 ));
		// 		float4 lerpResult100 = lerp( Albedo58 , Refraction60 , Depth98);
		// 		float4 clampResult109 = clamp( ( Edges62 + lerpResult100 ) , float4( 0,0,0,0 ) , float4( 1,1,1,0 ) );
				
		// 		float clampResult249 = clamp( temp_output_377_0 , 0.0 , 1.0 );
		// 		float eyeDepth = IN.ase_texcoord3.x;
		// 		float cameraDepthFade293 = (( eyeDepth -_ProjectionParams.y - _DistanceFadeOffset ) / _DistanceFade);
		// 		float Opacity263 = ( clampResult249 * saturate( ( 1.0 - ( temp_output_368_0 * cameraDepthFade293 ) ) ) );
				
				
		// 		float3 Albedo = clampResult109.rgb;
		// 		float Alpha = Opacity263;
		// 		float AlphaClipThreshold = 0.5;

		// 		half4 color = half4( Albedo, Alpha );

		// 		#ifdef _ALPHATEST_ON
		// 			clip(Alpha - AlphaClipThreshold);
		// 		#endif

		// 		return color;
		// 	}
		// 	ENDHLSL
		// }
		
	}
	/*ase_lod*/
	CustomEditor "UnityEditor.ShaderGraph.PBRMasterGUI"
	Fallback "Hidden/InternalErrorShader"
	
}
/*ASEBEGIN
Version=18900
849;73;671;639;-712.2676;3002.293;1.220532;False;False
Node;AmplifyShaderEditor.CommentaryNode;163;-2176,-6912;Inherit;False;614;229;;3;30;31;32;World Space UVs;1,1,1,1;0;0
Node;AmplifyShaderEditor.WorldPosInputsNode;30;-2128,-6864;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.CommentaryNode;184;-2176,-1536;Inherit;False;3710.818;1532.722;;48;263;284;249;297;290;378;293;295;296;62;9;274;275;242;234;377;229;241;8;7;250;231;219;376;217;230;227;232;233;6;259;84;375;258;254;4;213;83;255;215;185;225;183;224;226;223;362;368;Foam / Edge Fade;1,1,1,1;0;0
Node;AmplifyShaderEditor.DynamicAppendNode;31;-1936,-6864;Inherit;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;32;-1792,-6864;Inherit;False;WorldSpace;-1;True;1;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RangedFloatNode;223;-2144,-384;Inherit;False;Property;_FoamScale;Foam Scale;12;0;Create;True;0;0;0;False;0;False;1;3.04;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;224;-2048,-512;Inherit;False;32;WorldSpace;1;0;OBJECT;;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;226;-1984,-384;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;100;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;183;-1792,-320;Inherit;False;Property;_FoamSpeed;Foam Speed;14;0;Create;True;0;0;0;False;0;False;0.1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;225;-1792,-448;Inherit;False;2;2;0;FLOAT4;0,0,0,0;False;1;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.ViewDirInputsCoordNode;362;-1152,-1408;Inherit;False;World;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;185;-1664,-1216;Inherit;False;Property;_FoamDistance;Foam Distance;10;0;Create;True;0;0;0;False;0;False;1;14;0;100;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;215;-1408,-320;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;100;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;255;-1408,-192;Inherit;False;1;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.AbsOpNode;368;-976,-1360;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DepthFade;4;-1152,-1216;Inherit;False;True;False;True;2;1;FLOAT3;0,0,0;False;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;254;-1152,-320;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PannerNode;213;-1152,-448;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TexturePropertyNode;83;-1152,-704;Inherit;True;Property;_FoamMask;Foam Mask;8;0;Create;True;0;0;0;False;3;Space(10);Header(Foam);Space(10);False;None;df6a164e45088bb4eaee92b8d6ba7514;False;white;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.SamplerNode;84;-768,-576;Inherit;True;Property;_TextureSample2;Texture Sample 2;17;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;375;-800,-1216;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;258;-768,-352;Inherit;True;Property;_TextureSample3;Texture Sample 3;18;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.OneMinusNode;6;-640,-1216;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;232;-1360,-1088;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;259;-464,-480;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;233;-384,-384;Inherit;False;Constant;_Float0;Float 0;21;0;Create;True;0;0;0;False;0;False;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;227;-192,-384;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;217;-384,-656;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DepthFade;230;-1152,-1088;Inherit;False;True;False;True;2;1;FLOAT3;0,0,0;False;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;376;-816,-1088;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;219;0,-576;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;250;-1376,-944;Inherit;False;Property;_EdgesFade;Edges Fade;15;0;Create;True;0;0;0;False;1;Space(30);False;0.1;4.9;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;165;-2176,-2816;Inherit;False;2271.568;1055.857;;28;81;237;238;80;239;65;66;72;64;11;73;76;71;79;70;74;68;77;75;91;106;69;78;67;92;335;337;338;Normals;1,1,1,1;0;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;7;160,-704;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;231;-640,-1088;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;229;320,-1088;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DepthFade;241;-1152,-960;Inherit;False;True;False;True;2;1;FLOAT3;0,0,0;False;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;67;-2144,-2752;Inherit;False;32;WorldSpace;1;0;OBJECT;;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RangedFloatNode;92;-2112,-2656;Inherit;False;Constant;_Float1;Float 1;18;0;Create;True;0;0;0;False;0;False;100;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;8;256,-576;Inherit;False;Property;_FoamPower;Foam Power;11;0;Create;True;0;0;0;False;0;False;1;0.014;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;78;-2112,-2016;Inherit;False;Property;_NormalSpeed;Normal Speed;6;0;Create;True;0;0;0;False;0;False;0.1;3;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.Vector4Node;335;-1904,-2272;Inherit;False;Property;_NormalDirection;Normal Direction;7;0;Create;True;0;0;0;False;0;False;1,0,-1,0.2;1,0,-1,0.2;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;69;-2064,-2464;Inherit;False;Property;_NormalScale;Normal Scale;5;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;91;-1952,-2720;Inherit;False;2;0;FLOAT4;0,0,0,0;False;1;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;234;512,-1088;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;106;-1904,-2016;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;100;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;377;-832,-960;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;242;640,-960;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;68;-1840,-2528;Inherit;False;2;2;0;FLOAT4;0,0,0,0;False;1;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;70;-1840,-2400;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;1.2;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;77;-1472,-2016;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;337;-1632,-2272;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;338;-1632,-2176;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;275;608,-832;Inherit;False;Property;_FoamContrast;Foam Contrast;13;0;Create;True;0;0;0;False;0;False;0;1.63;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleContrastOpNode;274;800,-960;Inherit;False;2;1;COLOR;0,0,0,0;False;0;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.CommentaryNode;168;-2176,-5504;Inherit;False;1621.547;337.7678;;10;371;372;374;98;333;330;102;370;95;96;Depth;1,1,1,1;0;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;76;-1440,-2400;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;71;-1632,-2080;Inherit;False;2;2;0;FLOAT4;0,0,0,0;False;1;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;79;-1328,-1936;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ClampOpNode;9;1024,-960;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;1,1,1,0;False;1;COLOR;0
Node;AmplifyShaderEditor.TexturePropertyNode;64;-1200,-2320;Inherit;True;Property;_NormalMap;Normal;2;0;Create;False;0;0;0;False;3;Space(10);Header(Main Parameters);Space(10);False;None;9c5b42a27f5ef2347b3f3930c7fcd5a5;True;bump;Auto;Texture2D;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.RangedFloatNode;11;-1184,-1856;Inherit;False;Property;_NormalPower;Normal Power;3;0;Create;True;0;0;0;False;0;False;1;0.325;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;96;-2128,-5424;Inherit;False;Property;_Depth;Depth;18;0;Create;True;0;0;0;False;0;False;1;1.1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;72;-1168,-2528;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ViewDirInputsCoordNode;371;-2032,-5328;Inherit;False;World;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.PannerNode;73;-1168,-2064;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;62;1280,-960;Inherit;False;Edges;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;66;-896,-2192;Inherit;True;Property;_TextureSample1;Texture Sample 1;15;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;True;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;65;-896,-2416;Inherit;True;Property;_TextureSample0;Texture Sample 0;15;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;True;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.AbsOpNode;372;-1824,-5328;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DepthFade;95;-1936,-5440;Inherit;False;True;False;True;2;1;FLOAT3;0,0,0;False;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;235;-2176,-3584;Inherit;False;2208.369;570.6409;;20;47;132;131;110;129;159;355;28;158;27;354;360;353;162;359;356;358;350;348;384;Caustics;1,1,1,1;0;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;374;-1664,-5440;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;239;-864,-1984;Inherit;True;Property;_FoamNormal;Foam Normal;9;0;Create;True;0;0;0;False;0;False;-1;None;3cfd62d17e8d12640ab92f82a01da633;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;162;-1344,-3248;Inherit;False;Property;_CausticsSpeed;Caustics Speed;21;0;Create;True;0;0;0;False;0;False;2;1.5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;238;-528,-1872;Inherit;False;62;Edges;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.BlendNormalsNode;80;-544,-2288;Inherit;False;0;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleTimeNode;27;-1136,-3248;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;237;-320,-2016;Inherit;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;158;-1120,-3120;Inherit;False;Property;_CausticsScale;Caustics Scale;20;0;Create;True;0;0;0;False;0;False;0.5;0.54;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;169;-2176,-4992;Inherit;False;1791.921;511.6932;;9;17;105;228;18;16;19;20;21;60;Refraction;1,1,1,1;0;0
Node;AmplifyShaderEditor.SaturateNode;370;-1520,-5440;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;384;-889.9045,-3106.613;Inherit;False;32;WorldSpace;1;0;OBJECT;;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;28;-928,-3440;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;17;-2144,-4736;Inherit;False;Property;_RefractionPower;Refraction Power;4;0;Create;True;0;0;0;False;0;False;1;2;0;5;0;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;102;-1376,-5440;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;81;-128,-2016;Inherit;False;Normals;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;159;-928,-3344;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;105;-1792,-4608;Inherit;False;81;Normals;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.VoronoiNode;110;-688,-3520;Inherit;False;0;0;1;0;1;False;1;False;False;4;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;3;FLOAT;0;FLOAT2;1;FLOAT2;2
Node;AmplifyShaderEditor.VoronoiNode;129;-688,-3280;Inherit;False;0;0;1;0;1;False;1;False;False;4;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;3;FLOAT;0;FLOAT2;1;FLOAT2;2
Node;AmplifyShaderEditor.SimpleDivideOpNode;228;-1792,-4736;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;10;False;1;FLOAT;0
Node;AmplifyShaderEditor.GammaToLinearNode;330;-1216,-5440;Inherit;False;0;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode;131;-464,-3392;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GrabScreenPosition;16;-1792,-4928;Inherit;False;0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.BreakToComponentsNode;333;-1008,-5440;Inherit;False;FLOAT3;1;0;FLOAT3;0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;18;-1536,-4736;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode;19;-1408,-4864;Inherit;False;2;2;0;FLOAT4;0,0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.CommentaryNode;166;-2176,-6528;Inherit;False;1282.369;831.7024;;13;58;173;175;292;52;117;56;326;55;325;53;329;327;Color;1,1,1,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;98;-768,-5440;Inherit;False;Depth;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;132;-320,-3392;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;327;-2144,-6288;Inherit;False;98;Depth;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;47;-160,-3392;Inherit;False;Caustics;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;164;-2176,-4352;Inherit;False;2172.17;608.8064;;14;49;273;200;201;191;48;199;196;198;194;195;197;193;192;Waves;1,1,1,1;0;0
Node;AmplifyShaderEditor.ScreenColorNode;20;-1152,-4864;Inherit;False;Global;_GrabScreen0;Grab Screen 0;9;0;Create;True;0;0;0;False;0;False;Object;-1;False;False;False;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.OneMinusNode;329;-1984,-6288;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;193;-2112,-4096;Inherit;False;Property;_WavesSpeed;Waves Speed;24;0;Create;True;0;0;0;False;0;False;5;5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode;192;-2112,-4288;Inherit;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.GetLocalVarNode;55;-2048,-5824;Inherit;False;47;Caustics;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;21;-816,-4864;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;1,1,1,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;53;-2048,-6208;Inherit;False;Property;_DepthColor;Depth Color (RGBA);17;0;Create;False;0;0;0;False;0;False;0,0.6810271,0.6886792,1;0.05206467,0.3498847,0.735849,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;325;-2048,-6464;Inherit;False;Property;_DepthColor1;Shallow Color (RGBA);16;0;Create;False;0;0;0;False;0;False;0,0.6810271,0.6886792,1;0,1,0.8509803,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;194;-1920,-4256;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ClampOpNode;56;-1792,-5824;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;60;-608,-4864;Inherit;False;Refraction;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;326;-1776,-6336;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;117;-2048,-6016;Inherit;False;Property;_CausticsColor;Caustics Color;19;0;Create;True;0;0;0;False;1;Space(30);False;0.5404058,0.8679245,0.8414827,1;0.2912512,0.5575273,0.7264151,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;295;-1360,-816;Inherit;False;Property;_DistanceFade;Distance Fade;0;0;Create;True;0;0;0;False;0;False;1000;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;197;-1728,-4032;Inherit;False;Property;_WavesScale;Waves Scale;25;0;Create;True;0;0;0;False;0;False;10;4;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;296;-1392,-736;Inherit;False;Property;_DistanceFadeOffset;Distance Fade Offset;1;0;Create;True;0;0;0;False;0;False;500;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;195;-1888,-4112;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.CameraDepthFade;293;-1168,-848;Inherit;False;3;2;FLOAT3;0,0,0;False;0;FLOAT;1;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;175;-1536,-6176;Inherit;False;60;Refraction;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;198;-1472,-4032;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;100;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;52;-1536,-6080;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.PannerNode;196;-1728,-4192;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.OneMinusNode;292;-1536,-5952;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.BlendOpsNode;173;-1280,-6080;Inherit;False;Overlay;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;1;False;1;COLOR;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;199;-1312,-4192;Inherit;True;Simplex2D;True;False;2;0;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;48;-2112,-3968;Inherit;False;Property;_WavesHeight;Waves Height;23;0;Create;True;0;0;0;False;1;Space(30);False;25;0.5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;378;-864,-800;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;290;-704,-800;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;191;-1024,-4000;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;58;-1088,-6080;Inherit;False;Albedo;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.NormalVertexDataNode;200;-992,-4240;Inherit;True;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;101;512,-2752;Inherit;False;98;Depth;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;99;512,-2848;Inherit;False;60;Refraction;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.SaturateNode;297;-544,-800;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;201;-704,-4160;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;59;512,-2944;Inherit;False;58;Albedo;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.ClampOpNode;249;-640,-960;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;284;-336,-896;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TransformDirectionNode;273;-512,-4224;Inherit;False;World;Object;False;Fast;False;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.GetLocalVarNode;63;704,-3072;Inherit;False;62;Edges;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;100;704,-2880;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;108;896,-2944;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;263;-176,-880;Inherit;False;Opacity;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;49;-256,-4160;Inherit;False;WavesHeight;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SwizzleNode;360;-1376,-3536;Inherit;False;FLOAT2;0;2;2;3;1;0;FLOAT4;0,0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;285;896,-2416;Inherit;False;263;Opacity;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldSpaceLightDirHlpNode;358;-1776,-3456;Inherit;False;True;1;0;FLOAT;0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.TransformPositionNode;350;-1776,-3312;Inherit;False;World;Object;False;Fast;True;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.Vector2Node;74;-1616,-2416;Inherit;False;Constant;_Vector1;Vector 1;14;0;Create;True;0;0;0;False;0;False;1,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.ClampOpNode;109;1056,-2944;Inherit;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;1,1,1,0;False;1;COLOR;0
Node;AmplifyShaderEditor.DynamicAppendNode;353;-1536,-3456;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;176;512,-2496;Inherit;False;Property;_MetallicPower;Metallic Power;26;0;Create;True;0;0;0;False;3;Space(10);Header(Metallic Smoothness);Space(10);False;1;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;356;-1568,-3248;Inherit;False;Property;_CousticOffset;Coustic Offset;22;0;Create;True;0;0;0;False;0;False;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.NegateNode;359;-1536,-3344;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;75;-1632,-1920;Inherit;False;Constant;_Vector2;Vector 2;14;0;Create;True;0;0;0;False;0;False;-1,0.2;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RangedFloatNode;13;512,-2400;Inherit;False;Property;_SmoothnessPower;Smoothness Power;27;0;Create;True;0;0;0;False;0;False;1;0.99;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;348;-2128,-3536;Inherit;False;Reconstruct World Position From Depth;-1;;1;e7094bcbcc80eb140b2a3dbe6a861de8;0;0;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;354;-1360,-3424;Inherit;False;3;3;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;51;896,-2320;Inherit;False;49;WavesHeight;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;82;512,-2624;Inherit;False;81;Normals;1;0;OBJECT;;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode;355;-928,-3536;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;343;2263.622,-3305.396;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;2;New Amplify Shader;94348b07e5e8bab40bd6c8a1e3df54cd;True;Meta;0;4;Meta;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;True;0;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;LightMode=Meta;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;341;2263.622,-3305.396;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;2;New Amplify Shader;94348b07e5e8bab40bd6c8a1e3df54cd;True;ShadowCaster;0;2;ShadowCaster;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;True;0;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;False;True;1;False;-1;True;3;False;-1;False;True;1;LightMode=ShadowCaster;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;339;1184,-2672;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;2;New Amplify Shader;94348b07e5e8bab40bd6c8a1e3df54cd;True;ExtraPrePass;0;0;ExtraPrePass;5;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;True;0;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;False;True;1;1;False;-1;0;False;-1;0;1;False;-1;0;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;True;True;True;True;True;0;False;-1;False;False;False;False;False;False;False;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;False;True;1;False;-1;True;3;False;-1;True;True;0;False;-1;0;False;-1;True;0;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;342;2263.622,-3305.396;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;2;New Amplify Shader;94348b07e5e8bab40bd6c8a1e3df54cd;True;DepthOnly;0;3;DepthOnly;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;True;0;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;False;False;True;False;False;False;False;0;False;-1;False;False;False;False;False;False;False;False;False;True;1;False;-1;False;False;True;1;LightMode=DepthOnly;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;344;2263.622,-3305.396;Float;False;False;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;2;New Amplify Shader;94348b07e5e8bab40bd6c8a1e3df54cd;True;Universal2D;0;5;Universal2D;0;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;True;0;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;0;0;False;True;0;5;False;-1;10;False;-1;1;1;False;-1;10;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;True;True;True;True;0;False;-1;False;False;False;False;False;False;False;False;False;True;2;False;-1;True;0;False;-1;True;False;0;False;-1;0;False;-1;True;1;LightMode=Universal2D;False;0;Hidden/InternalErrorShader;0;0;Standard;0;False;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;340;1216,-2688;Float;False;True;-1;2;UnityEditor.ShaderGraph.PBRMasterGUI;0;2;Custom/Water;94348b07e5e8bab40bd6c8a1e3df54cd;True;Forward;0;1;Forward;18;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;-1;False;True;0;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;3;RenderPipeline=UniversalPipeline;RenderType=Transparent=RenderType;Queue=Transparent=Queue=0;True;6;0;False;True;0;5;False;-1;10;False;-1;1;1;False;-1;10;False;-1;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;True;True;True;True;0;False;-1;False;False;False;False;False;False;False;True;False;255;False;-1;255;False;-1;255;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;7;False;-1;1;False;-1;1;False;-1;1;False;-1;False;True;2;False;-1;True;0;False;-1;True;False;0;False;-1;0;False;-1;True;1;LightMode=UniversalForward;False;0;Hidden/InternalErrorShader;0;0;Standard;38;Workflow;1;Surface;1;  Refraction Model;0;  Blend;0;Two Sided;1;Fragment Normal Space,InvertActionOnDeselection;0;Transmission;0;  Transmission Shadow;0.5,False,-1;Translucency;0;  Translucency Strength;1,False,-1;  Normal Distortion;0.5,False,-1;  Scattering;2,False,-1;  Direct;0.9,False,-1;  Ambient;0.1,False,-1;  Shadow;0.5,False,-1;Cast Shadows;1;  Use Shadow Threshold;0;Receive Shadows;1;GPU Instancing;1;LOD CrossFade;1;Built-in Fog;1;_FinalColorxAlpha;0;Meta Pass;1;Override Baked GI;0;Extra Pre Pass;0;DOTS Instancing;0;Tessellation;0;  Phong;0;  Strength;0.5,False,-1;  Type;0;  Tess;16,True,46;  Min;10,True,280;  Max;25,True,279;  Edge Length;16,False,-1;  Max Displacement;25,False,-1;Write Depth;0;  Early Z;0;Vertex Position,InvertActionOnDeselection;1;0;6;False;True;True;True;True;True;False;;False;0
WireConnection;31;0;30;1
WireConnection;31;1;30;3
WireConnection;32;0;31;0
WireConnection;226;0;223;0
WireConnection;225;0;224;0
WireConnection;225;1;226;0
WireConnection;215;0;183;0
WireConnection;255;0;225;0
WireConnection;368;0;362;2
WireConnection;4;0;185;0
WireConnection;254;0;255;0
WireConnection;254;2;215;0
WireConnection;213;0;225;0
WireConnection;213;2;215;0
WireConnection;84;0;83;0
WireConnection;84;1;213;0
WireConnection;375;0;368;0
WireConnection;375;1;4;0
WireConnection;258;0;83;0
WireConnection;258;1;254;0
WireConnection;6;0;375;0
WireConnection;232;0;185;0
WireConnection;259;0;84;1
WireConnection;259;1;258;1
WireConnection;227;0;233;0
WireConnection;217;0;6;0
WireConnection;217;1;259;0
WireConnection;230;0;232;0
WireConnection;376;0;368;0
WireConnection;376;1;230;0
WireConnection;219;0;217;0
WireConnection;219;1;227;0
WireConnection;7;0;6;0
WireConnection;7;1;219;0
WireConnection;231;0;376;0
WireConnection;229;0;231;0
WireConnection;229;1;7;0
WireConnection;241;0;250;0
WireConnection;91;0;67;0
WireConnection;91;1;92;0
WireConnection;234;0;229;0
WireConnection;234;1;8;0
WireConnection;106;0;78;0
WireConnection;377;0;368;0
WireConnection;377;1;241;0
WireConnection;242;0;377;0
WireConnection;242;1;234;0
WireConnection;68;0;91;0
WireConnection;68;1;69;0
WireConnection;70;0;69;0
WireConnection;77;0;106;0
WireConnection;337;0;335;1
WireConnection;337;1;335;2
WireConnection;338;0;335;3
WireConnection;338;1;335;4
WireConnection;274;1;242;0
WireConnection;274;0;275;0
WireConnection;76;0;337;0
WireConnection;76;1;106;0
WireConnection;71;0;68;0
WireConnection;71;1;70;0
WireConnection;79;0;338;0
WireConnection;79;1;77;0
WireConnection;9;0;274;0
WireConnection;72;0;68;0
WireConnection;72;2;76;0
WireConnection;73;0;71;0
WireConnection;73;2;79;0
WireConnection;62;0;9;0
WireConnection;66;0;64;0
WireConnection;66;1;73;0
WireConnection;66;5;11;0
WireConnection;65;0;64;0
WireConnection;65;1;72;0
WireConnection;65;5;11;0
WireConnection;372;0;371;2
WireConnection;95;0;96;0
WireConnection;374;0;95;0
WireConnection;374;1;372;0
WireConnection;239;1;213;0
WireConnection;80;0;65;0
WireConnection;80;1;66;0
WireConnection;27;0;162;0
WireConnection;237;0;80;0
WireConnection;237;1;239;0
WireConnection;237;2;238;0
WireConnection;370;0;374;0
WireConnection;28;0;27;0
WireConnection;102;0;370;0
WireConnection;81;0;237;0
WireConnection;159;0;158;0
WireConnection;110;0;384;0
WireConnection;110;1;28;0
WireConnection;110;2;159;0
WireConnection;129;0;384;0
WireConnection;129;1;27;0
WireConnection;129;2;158;0
WireConnection;228;0;17;0
WireConnection;330;0;102;0
WireConnection;131;0;110;0
WireConnection;131;1;129;0
WireConnection;333;0;330;0
WireConnection;18;0;228;0
WireConnection;18;1;105;0
WireConnection;19;0;16;0
WireConnection;19;1;18;0
WireConnection;98;0;333;0
WireConnection;132;0;131;0
WireConnection;47;0;132;0
WireConnection;20;0;19;0
WireConnection;329;0;327;0
WireConnection;21;0;20;0
WireConnection;194;0;192;1
WireConnection;194;1;192;3
WireConnection;56;0;55;0
WireConnection;60;0;21;0
WireConnection;326;0;325;0
WireConnection;326;1;53;0
WireConnection;326;2;329;0
WireConnection;195;0;193;0
WireConnection;195;1;193;0
WireConnection;293;0;295;0
WireConnection;293;1;296;0
WireConnection;198;0;197;0
WireConnection;52;0;326;0
WireConnection;52;1;117;0
WireConnection;52;2;56;0
WireConnection;196;0;194;0
WireConnection;196;2;195;0
WireConnection;292;0;53;4
WireConnection;173;0;175;0
WireConnection;173;1;52;0
WireConnection;173;2;292;0
WireConnection;199;0;196;0
WireConnection;199;1;198;0
WireConnection;378;0;368;0
WireConnection;378;1;293;0
WireConnection;290;0;378;0
WireConnection;191;0;199;0
WireConnection;191;1;48;0
WireConnection;58;0;173;0
WireConnection;297;0;290;0
WireConnection;201;0;200;0
WireConnection;201;1;191;0
WireConnection;249;0;377;0
WireConnection;284;0;249;0
WireConnection;284;1;297;0
WireConnection;273;0;201;0
WireConnection;100;0;59;0
WireConnection;100;1;99;0
WireConnection;100;2;101;0
WireConnection;108;0;63;0
WireConnection;108;1;100;0
WireConnection;263;0;284;0
WireConnection;49;0;273;0
WireConnection;360;0;348;0
WireConnection;350;0;348;0
WireConnection;109;0;108;0
WireConnection;353;0;358;1
WireConnection;353;1;358;3
WireConnection;359;0;350;2
WireConnection;354;0;353;0
WireConnection;354;1;359;0
WireConnection;354;2;356;0
WireConnection;355;0;360;0
WireConnection;355;1;354;0
WireConnection;340;0;109;0
WireConnection;340;1;82;0
WireConnection;340;3;176;0
WireConnection;340;4;13;0
WireConnection;340;6;285;0
WireConnection;340;8;51;0
ASEEND*/
//CHKSM=3CECC4B96D42DDD4D39D1A7EBCF89E589CB277CB