Shader "Hidden/FrostedGlass/Blur"
{
    // 分离式高斯模糊，17 抽真高斯核（权重用 exp(-x²/2σ²) 现算，不是近似盒状）。
    // 横竖各来一遍 = 等效 17×17 的卷积核；迭代多次 = 核更大。
    // σ 由 _BlurRadius 决定，所以"核有多大"就是一个参数。
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZTest Always Cull Off ZWrite Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GlobalSamplers.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        float2 _BlurDirection;              // (1,0) 横向 / (0,1) 纵向
        float  _BlurRadius;                 // σ（以目标像素为单位）：核有多宽
        float4 _FrostedSourceTexelSize;     // xy = 1/宽高，zw = 宽高

        half4 BlurFrag(Varyings input) : SV_Target
        {
            float2 uv = input.texcoord;
            float2 texel = _BlurDirection * _FrostedSourceTexelSize.xy;

            float sigma = max(_BlurRadius, 0.5);
            float twoSigmaSq = 2.0 * sigma * sigma;

            half3 sum = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb;
            float weightSum = 1.0;

            [unroll]
            for (int i = 1; i <= 8; i++)
            {
                float offset = i * sigma;                       // 采样点间距跟着 σ 走
                float w = exp(-(offset * offset) / twoSigmaSq); // 真高斯权重
                sum += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + texel * offset).rgb * w;
                sum += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - texel * offset).rgb * w;
                weightSum += 2.0 * w;
            }

            return half4(sum / weightSum, 1.0);
        }
        ENDHLSL

        Pass
        {
            Name "Frosted Glass Blur"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment BlurFrag
            ENDHLSL
        }
    }
    Fallback Off
}
