Shader "Unlit/CelColorShader"
{
    Properties
    {
        [Header(Main Texture Setting)]
        _MainTex ("Main Texture", 2D) = "white" {}
        _MainColor("Main Color", Color) = (1,1,1,1)
        _GlobalAlpha("Global Alpha", Range(0,1)) = 1
        [Space(3)]
        [Header(Outline Setting)]
        [Toggle(ENABLE_SELECT_OUTLINE)]_EnableSelectOutline ("Enable Select Outline", float) = 0
        [Enum(OFF,0,ON,1,] _ZWrite("ZWrite Mode", int) = 1
        _OutlineColor("Outline Color", Color) = (1,0.898,0,0.753)
        _OutlineTex("Outline Tex", 2D) = "black"{}
        _OutlineWidth ("Outline Width", Range(0, 2) ) = 0.5
        _CameraThreshold ("Camera Threshold", range(1,20)) = 10
        [Space(3)]
        [Header(Shadow Setting)]
        _RampTex("Shadow Ramp Tex", 2D) = "white"{}
        _ShadowStrength("Shadow Strength", range(0,1)) = 0.01
        _StencilID("StencilID", float) = 2
        _Plane("Plane", vector) = (0,1,0,0.2)
        [Space(3)]
        [Header(Data Channel)]
        _DataTex("Data Texture", 2D) = "white"{}
        [Space(3)]
        [Header(Face Shadow)]
        [Toggle(ENABLE_FACE_SHADOW_MAP)]_EnableFaceShadowMap ("Enable Face Shadow Map", float) = 0
        _FaceShadowMap ("Face Shadow Map", 2D) = "white" { }
        _ShadowColor("Shadow Color", Color) = (0,0,0,1)
        _FaceUOffset("Face U Offset", Range(-1,1)) = 0
        [Space(3)]
        [Header(Specular Setting)]
        [HDR]_SpecColor ("Specular Color", color) = (1, 1, 1, 1)
        _Shininess ("Shininess", range(0.1, 20.0)) = 10.0
        _SpecMulti ("Multiple Factor", range(0.1, 1.0)) = 1
        [Space(3)]
        [Header(RimLight Setting)]
        [HDR]_RimColor("Rim Color", Color) = (1,1,1,1)
        _RimSmooth("Rim Smooth", range(0.001, 1)) = 0.03
        _RimPow("Rim Pow", range(0.0, 10.0)) = 6.0
        _RimMin("Rim Min", Range(0, 1)) = 0.5
        [Space(3)]
        [Header(Bloom Setting)]
        _BloomFactor("Bloom Factor", range(0.0, 1.0)) = 1.0
        [Space(3)]
        [Header(Emission Setting)]
        //_EmissionFactor("Emission Factor", range(0, 20)) = 1.0
        [HDR]_EmissionColor("Emission Color", Color) = (0,0,0,1)

        [Header(Cut Setting)]
        [Space(3)]
        [Toggle(_MODELCUT_ON)]_ModelCut("Model Cut", int) = 0
        _ModelCutHeight("ModelCut Height", float) = 0

        [Header(Flash Setting)]
        [Space(3)]
        [Toggle(_FLASH_ON)]_Flash("Flash", int) = 0
        [Toggle(USE_EMISSION_MASK)]_UseEmissionMask("Use Emission Mask (DataTex.a)", int) = 0
        _FlashMaskTex("Flash Mask Tex",2D) = "white" {}
        _FlashShapeTex("Flash Shape Tex", 2D) = "white" { }
        _FlashMoveSpeed("Flash Move Speed", vector) = (1, 0, 0, 0)
        _FlashWidth("Flash Width", float) = 1
        [HDR]_FlashColor("Flash Color", Color) = (1,1,1,1)
    }

    CGINCLUDE
    #pragma multi_compile _ _MODELCUT_ON
    float  _ModelCutHeight;
    float4 _Plane;
    int CheckCut(float3 world)
    {
        //float d = abs(_Plane.w);

        //float offset = dot(world, (_Plane.w * _Plane.xyz) / d) - d;

        float3 normal = _Plane.xyz;
        if(dot(float3(0, 1, 0), _Plane.xyz) < 0)
        {
            normal = - normal;
        }

        float offset = dot(world, normal) - abs(_Plane.w);
        if(offset > _ModelCutHeight)
        {
            return 0;
        }

	    return 1;
    }
    ENDCG

    SubShader
    {
        Tags { "RenderType"="Opaque" }

        
        //Pass{
        //    ZWrite On
        //    ColorMask 0
        //    CGPROGRAM
        //        #pragma vertex vert
        //        #pragma fragment frag
        //        #include "UnityCG.cginc"
        //        struct a2v 
        //        {
        //            float4 vertex : POSITION;
        //        };

        //        struct v2f
        //        {
        //            float4 pos : SV_POSITION;
        //        };

        //        v2f vert (a2v v) {
        //            v2f o;
        //            UNITY_INITIALIZE_OUTPUT(v2f, o);
        //            float4 pos = UnityObjectToClipPos(v.vertex);
        //            o.pos = pos;
        //            return o;
        //        }

        //        half4 frag(v2f i) : SV_TARGET 
        //        {
        //            return fixed4(0,0,0,1);
        //        }
        //    ENDCG
        //}

        Pass
        {
            Cull Front
            ZWrite [_ZWrite]
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _OutlineWidth;
            float _CameraThreshold;
            float _EnableSelectOutline;
            float4 _OutlineColor;
            sampler2D _OutlineTex;
            float4 _MainTex_ST;
            float _GlobalAlpha;

            struct a2v 
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                float4 vertColor : COLOR;
                float4 tangent : TANGENT;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv:TEXCOORD0;
                float3 modelPos:TEXCOORD1;
                float3 normal:TEXCOORD2;
            };


            v2f vert (a2v v) 
            {
                v2f o;
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                float4 pos = UnityObjectToClipPos(v.vertex);
                float3 worldNormal = UnityObjectToWorldNormal(v.normal);
                float3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
                float3 worldBitangent = normalize(cross(worldNormal, worldTangent) * v.tangent.w);
                float3x3 tangentTransform = float3x3(worldTangent, worldBitangent, worldNormal);
                float3 fixNormal = normalize(mul(v.vertColor.xyz *2 - 1, tangentTransform));
                float3 localNormal = mul((float3x3)unity_WorldToObject, fixNormal);
                float3 viewNormal = mul((float3x3)UNITY_MATRIX_IT_MV, localNormal);
                //float3 viewNormal = mul((float3x3)UNITY_MATRIX_IT_MV, (v.vertColor.xyz *2 - 1));
                //float3 viewNormal = mul((float3x3)UNITY_MATRIX_IT_MV, v.tangent.xyz);
                float3 ndcNormal = normalize(TransformViewToProjection(viewNormal.xyz)) * pos.w;//将法线变换到NDC空间
                float4 nearUpperRight = mul(unity_CameraInvProjection, float4(1, 1, UNITY_NEAR_CLIP_VALUE, _ProjectionParams.y));//将近裁剪面右上角位置的顶点变换到观察空间
                float aspect = abs(nearUpperRight.y / nearUpperRight.x);//求得屏幕宽高比
                ndcNormal.x *= aspect;
                float distance = -UnityObjectToViewPos(v.vertex).z;
                if(_EnableSelectOutline >0){
                    pos.xy += 0.01 * (_OutlineWidth+0.6) * ndcNormal.xy * v.vertColor.w;
                }else{
                    pos.xy += 0.01 * _OutlineWidth * ndcNormal.xy * v.vertColor.w; //* min(1, _CameraThreshold / distance) ;
                }
                o.pos = pos;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.modelPos = mul(unity_ObjectToWorld, v.vertex);
                o.normal = worldNormal;
                return o;
            }

            fixed4 frag(v2f i) : SV_TARGET 
            {
#if defined(_MODELCUT_ON)
                if(CheckCut(i.modelPos))
                {
                    discard;
                }
#endif

                if(_EnableSelectOutline > 0){
                    return _OutlineColor;
                }else{
                    return fixed4(tex2D(_OutlineTex, i.uv).rgb, _GlobalAlpha);
                }
            }
            ENDCG
        }

        Pass
        {
            Tags
            {
                "LightMode" = "ForwardBase"
            }
            Cull Back
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            //unity 内置阴影参数
            #pragma multi_compile_fwdbase

            #pragma multi_compile _ _FLASH_ON

            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            struct a2v
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float3 tangent : TANGENT;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 posWS : TEXCOORD2;
                float4 pos : SV_POSITION;
                float2 uv2: TEXCOORD4;
                SHADOW_COORDS(3)
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainColor;
            sampler2D _RampTex;
            float4 _RampTex_ST;
            float _ShadowStrength;

            float _RimSmooth;
            float4 _RimColor;
            float _RimPow;
            float _RimMin;

            sampler2D _FaceShadowMap;
            float _EnableFaceShadowMap;
            float4 _ShadowColor;
            float _FaceUOffset;

            sampler2D _DataTex;

            int _UseEmissionMask;
            sampler2D _FlashShapeTex;
            sampler2D _FlashMaskTex;
            float4 _FlashMoveSpeed;
            float4 _FlashColor;
            float _FlashWidth;

            //float4 _SpecColor;
            float _Shininess;

            float _EmissionFactor;
            float4 _EmissionColor;

            float _GlobalAlpha;

            v2f vert (a2v v)
            {
                v2f o;
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.normalWS = UnityObjectToWorldNormal(v.normal);
                o.posWS = mul(unity_ObjectToWorld, v.vertex);
                o.uv.xy = TRANSFORM_TEX(v.uv, _MainTex);
                TRANSFER_SHADOW(o)
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
#if defined(_MODELCUT_ON)
                if(CheckCut(i.posWS))
                {
                    discard;
                }
#endif

                fixed4 finalColor;
                float3 lightDir = normalize(lerp(_WorldSpaceLightPos0.xyz, _WorldSpaceLightPos0.xyz - i.posWS.xyz,_WorldSpaceLightPos0.w));
                float3 cameraPos = _WorldSpaceCameraPos.xyz;
                //cameraPos.z = cameraPos.z - 100;
                float3 viewDir = normalize(cameraPos - i.posWS.xyz);
                float4 dataTex = tex2D(_DataTex, i.uv);
                //Half Lambert && Ramp
                //float shadow = SHADOW_ATTENUATION(i); //根据贴图与纹理坐标对纹理采样得到shadow值。
                //if(shadow > 0.5) shadow = 1;
                //else shadow = 0.1;
                fixed4 mainTex = tex2D(_MainTex, i.uv)*_MainColor;
                fixed4 baseColor = fixed4(mainTex.rgb, 1);
                float rampU = (0.5 + dot(lightDir, i.normalWS) * 0.5);
                //if(rampU > 0.5) rampU = shadow;
                float rampV = mainTex.a;
                fixed4 rampColor = tex2D(_RampTex, float2(rampU, rampV));
                baseColor = baseColor * rampColor * min((dataTex.g+_ShadowStrength), 1);
                //High light
                float3 halfDir = normalize(viewDir + lightDir);
                fixed spec = pow(saturate(dot(i.normalWS, halfDir)), _Shininess);
                spec = step(1.0f - dataTex.b, spec);
                fixed4 specularColor = spec * baseColor * dataTex.r * spec * _SpecColor;
                // Rim Light
                float lambertF = dot(lightDir, i.normalWS);
                float lambertD = max(0, -lambertF);
                lambertF = max(0, lambertF);
                float rim = (1 - saturate(dot(viewDir, i.normalWS)))*(i.normalWS.x < 0);
                float _EnableLambert = 0.5;
                float _EnableRim = 0.5;
                float _BloomFactor = 1;
                float rimDot = pow(rim, _RimPow);
                //rimDot = _EnableLambert * 0.3 * rimDot + (1 - _EnableLambert) * rimDot;
                float rimIntensity = smoothstep(0, 1, rimDot);
               // return fixed4(rimIntensity*0.8, rimIntensity*0.8, rimIntensity*0.8, 1);
                half4 rimColor = rimIntensity * _RimColor * baseColor;
                //return fixed4(rimIntensity, rimIntensity, rimIntensity, 1);
                //half4 rimColor = _EnableRim * pow(rimIntensity, 5) * _RimColor * baseColor;
                //rimColor.a = _EnableRim * rimIntensity * _BloomFactor;

                fixed4 emiColor = baseColor * dataTex.a * _EmissionColor;// * abs((frac(_Time.y * 0.5) - 0.5) * 2);
                if (_EnableFaceShadowMap){
                    float faceLightRamp = 1;
                    float3 faceLightMap =  tex2D(_FaceShadowMap, float2(i.uv.x, i.uv.y));
                    float3 _faceLightMap = tex2D(_FaceShadowMap, float2(1-i.uv.x + _FaceUOffset, i.uv.y));
                    float4 Front = mul(unity_ObjectToWorld, float4(0, 1, 0, 0));
                    float4 Up = mul(unity_ObjectToWorld, float4(-1, 0, 0, 0));
                    float3 Left = cross(Up,Front);
                    float3 Right = -Left;
		            float3 L = normalize(UnityWorldSpaceLightDir(i.posWS.xyz));
                    float FL = dot(normalize(Front.xz), normalize(L.xz));
                    float LL = dot(normalize(Left.xz), normalize(L.xz));
                    float RL = dot(normalize(Right.xz), normalize(L.xz));
                    float faceLight = faceLightMap.r;// + _FaceLightmpOffset ; //用来和 头发 身体的明暗过渡对齐
                    float angle = acos(FL);
                    float threshold = angle / 3.14;
                    if(LL < 0){
                        faceLightRamp = (faceLight > threshold);
                    }else{
                        faceLightRamp = (_faceLightMap.r > threshold);
                    }

                    if(faceLightRamp == 0){
                        rampU = 0.2;
                    }else{
                        rampU = 0.9;
                    }
                    rampColor = tex2D(_RampTex, float2(rampU, rampV));
                    return fixed4(mainTex.rgb * rampColor, _GlobalAlpha);
                    //if(faceLightRamp == 0)
                    //    return mainTex * _ShadowColor;
                    //return mainTex;
                }

#if defined(_FLASH_ON)
                float2 flash_uv = i.uv / _FlashWidth;
                flash_uv += _FlashMoveSpeed * _Time.y;
                flash_uv += _FlashMoveSpeed.zw;
                float flash_shape = tex2D(_FlashShapeTex, flash_uv).b;
                float flash_mask = dataTex.a;
                if (_UseEmissionMask == 0)
                {
                    flash_mask = tex2D(_FlashMaskTex, i.uv).a;
                }

                float4 flash_color = flash_shape * _FlashColor * flash_mask;
                if (flash_color.a > 0)
                {
                    baseColor += flash_color;
                }
#endif
                //flowingColor = dataTex.a * tex2D(_FlowingTex, i.uv2);
                return fixed4((baseColor + specularColor + emiColor + rimColor).rgb, _GlobalAlpha);
            }
            ENDCG
        }

                //LOD 100
       //阴影pass
        Pass
        {
            Tags
            {
                "LightMode" = "ForwardBase"
            }
            Stencil{
                Ref[_StencilID]
                Comp NotEqual
                Pass replace
            }

            ZWrite On
            Blend SrcAlpha OneMinusSrcAlpha
            offset -1, -1
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"
            struct a2v
            {
                float4 vertex : POSITION;
                half2 texcoord : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 modelPos:TEXCOORD0;
            };

            float _GlobalAlpha;
            v2f vert(a2v v)
            {
                v2f o;
                float4 worldpos = mul(unity_ObjectToWorld, v.vertex);
                float NP = dot(worldpos, _Plane.xyz);
                float NL = dot(_Plane.xyz, _WorldSpaceLightPos0.xyz);
                float3 delta = ((NP - _Plane.w) / NL) * _WorldSpaceLightPos0.xyz;
                float4 shadowPos = float4(worldpos.xyz - delta, worldpos.w);
                o.vertex = mul(unity_MatrixVP, shadowPos);
                o.modelPos = worldpos;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
#if defined(_MODELCUT_ON)
                if(CheckCut(i.modelPos))
                {
                    discard;
                }
#endif
                return fixed4(0,0,0,0.4*_GlobalAlpha);
            }
            ENDCG
        }
    }
    FallBack "Legacy Shaders/VertexLit"
}
