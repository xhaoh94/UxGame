Shader "Ux/UIBlurGlobal"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _BlurSize ("Blur Size", Range(0, 8)) = 2
        _BlurTint ("Blur Tint", Color) = (1,1,1,1)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "UI_BLUR_GLOBAL"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"

            sampler2D _UIBlurTex;
            float4 _UIBlurTex_TexelSize;
            fixed4 _Color;
            fixed4 _BlurTint;
            float _BlurSize;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.texcoord;
                float2 texel = _UIBlurTex_TexelSize.xy * _BlurSize;

                fixed4 col = tex2D(_UIBlurTex, uv) * 0.25;
                col += tex2D(_UIBlurTex, uv + float2(texel.x, 0.0)) * 0.125;
                col += tex2D(_UIBlurTex, uv - float2(texel.x, 0.0)) * 0.125;
                col += tex2D(_UIBlurTex, uv + float2(0.0, texel.y)) * 0.125;
                col += tex2D(_UIBlurTex, uv - float2(0.0, texel.y)) * 0.125;
                col += tex2D(_UIBlurTex, uv + texel) * 0.0625;
                col += tex2D(_UIBlurTex, uv - texel) * 0.0625;
                col += tex2D(_UIBlurTex, uv + float2(texel.x, -texel.y)) * 0.0625;
                col += tex2D(_UIBlurTex, uv + float2(-texel.x, texel.y)) * 0.0625;

                col *= _BlurTint;
                col.a = 1.0;
                return col * i.color;
            }
            ENDCG
        }
    }

    Fallback Off
}
