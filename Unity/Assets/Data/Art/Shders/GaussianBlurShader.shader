Shader "Ux_URP_PostProcessing/GaussianBlurShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    
    ////【HLSLINCLUDE】---【ENDHLSL】用于编写多个vert或者frag，方便管理
    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"                //函数库：主要用于各种的空间变换
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"                //函数库：获取深度图

    sampler2D _MainTex; float4 _MainTex_TexelSize;      // Main TexelSize;
    float _GaussianBlurRadius;      // 高斯模糊半径
    float _BlurDepth;               // 模糊景深
    float _FullBlurValue;           // 总体模糊程度
    
    struct appdata
    {
        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;
    };

    struct v2f
    {
        float2 uv : TEXCOORD0;
        float4 vertex : SV_POSITION;
    };

    //【vert】
    v2f GaussianBlur_vert (appdata v)
    {
        v2f o;
        o.vertex = TransformObjectToHClip(v.vertex);
        o.uv = v.uv;
        return o;
    }

    // 【frag：横-高斯模糊】
    float4 GaussianBlurHorizontal_frag (v2f i) : SV_Target      
    {
        //获取深度图
        float depth = SampleSceneDepth(i.uv);       // 获取深度图【越远值越小】
        float depthValue = Linear01Depth(depth, _ZBufferParams);        // 深度值越远越大
        //[权重计算]
        float blurWeight_01 = min(0.3,pow(depthValue, _BlurDepth) * _FullBlurValue);       // 两侧模糊权重1
        float blurWeight_02 = blurWeight_01 / 2;        // 两侧模糊权重2
        float midWeight = 1.0 - 2 * (blurWeight_01 + blurWeight_02);                                         // 中间权重
        //[计算col]
        float4 leftCol_01 = tex2D(_MainTex, i.uv + float2(-_MainTex_TexelSize.x, 0.0f) * _GaussianBlurRadius) * blurWeight_01;      // 左Col_01 近
        float4 leftCol_02 = tex2D(_MainTex, i.uv + float2(-_MainTex_TexelSize.x * 2, 0.0f) * _GaussianBlurRadius) * blurWeight_02;      // 左Col_02 远
        float4 midCol = tex2D(_MainTex, i.uv) * midWeight;      // 中间值
        float4 rightCol_01 = tex2D(_MainTex, i.uv + float2(_MainTex_TexelSize.x, 0.0f) * _GaussianBlurRadius) * blurWeight_01;      // 右Col_01 近
        float4 rightCol_02 = tex2D(_MainTex, i.uv + float2(_MainTex_TexelSize.x * 2, 0.0f) * _GaussianBlurRadius) * blurWeight_02;      // 右Col_01 远
        //颜色输出
        float4 col = leftCol_01 + leftCol_02+ midCol + rightCol_01 + rightCol_02;       

        return col;
    }
    // 【frag：纵-高斯模糊】
        float4 GaussianBlurVertical_frag (v2f i) : SV_Target      // 横-高斯模糊
    {
        //获取深度图
        float depth = SampleSceneDepth(i.uv);       // 获取深度图【越远值越小】
        float depthValue = Linear01Depth(depth, _ZBufferParams);        // 深度值越远越大
        //[权重计算]
        float blurWeight_01 = min(0.3,pow(depthValue, _BlurDepth) * _FullBlurValue);       // 两侧模糊权重1
        float blurWeight_02 = blurWeight_01 / 2;        // 两侧模糊权重2
        float midWeight = 1.0 - 2 * (blurWeight_01 + blurWeight_02);                                         // 中间权重
        //[计算col]
        float4 upCol_01 = tex2D(_MainTex, i.uv + float2(0.0f, _MainTex_TexelSize.y) * _GaussianBlurRadius) * blurWeight_01;      // 上Col_01 近
        float4 upCol_02 = tex2D(_MainTex, i.uv + float2(0.0f, _MainTex_TexelSize.y * 2) * _GaussianBlurRadius) * blurWeight_02;      // 上Col_02 远
        float4 midCol = tex2D(_MainTex, i.uv) * midWeight;      // 中间值
        float4 downCol_01 = tex2D(_MainTex, i.uv + float2(0.0f, -_MainTex_TexelSize.y) * _GaussianBlurRadius) * blurWeight_01;      // 下Col
        float4 downCol_02 = tex2D(_MainTex, i.uv + float2(0.0f, -_MainTex_TexelSize.y * 2) * _GaussianBlurRadius) * blurWeight_02;      // 下Col
        //颜色输出
        float4 col = upCol_01 + upCol_02 + midCol + downCol_01 + downCol_02;       

        return col;
    }
    ENDHLSL
    
    ////【SubShader】---用与编写多个Pass,方便调用
    SubShader
    {
        Tags {
            "RenderPipeline" = "UniversalRenderPipeline"   
            "RenderType"="Transparent"
            "Queue" = "Transparent"       
        }
        Cull Off        // 关闭绘制剔除-启用双面绘制        //SubShader的设置会应用所有Pass
        ZWrite Off      // 关闭深度写入
        ZTest Always    // 总是通过ZTest

        //【Pass0：横模糊】
        Pass
        {
            Name "GaussianBlurHorizontalPass"
            HLSLPROGRAM
            #pragma vertex GaussianBlur_vert
            #pragma fragment GaussianBlurHorizontal_frag        // 使用 横-高斯模糊frag
            ENDHLSL
        }
        
        //【Pass1：纵模糊】
        Pass
        {
            Name "GaussianBlurVerticalPass"
            HLSLPROGRAM
            #pragma vertex GaussianBlur_vert
            #pragma fragment GaussianBlurVertical_frag      // 使用 纵-高斯模糊frag
            ENDHLSL
        }
    }
}
