Shader "Hidden/FrostedGlass/Blur"
{
    // 分离式高斯模糊：一次只糊一个方向，横向纵向各来一遍。
    // 用 Blitter.BlitCameraTexture(cmd, src, dst, material, pass) 驱动，
    // 所以顶点/全屏三角形走 core 的 Blit.hlsl（Vert / Varyings / _BlitTexture 都是它提供的）。
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZTest Always Cull Off ZWrite Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GlobalSamplers.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        float2 _BlurDirection;              // (1,0) 横向 / (0,1) 纵向
        float  _BlurRadius;                 // 以「本次目标像素」为单位
        float4 _FrostedSourceTexelSize;     // xy = 1/宽高（目标 RT），zw = 宽高

        half4 BlurFrag(Varyings input) : SV_Target
        {
            float2 step = _BlurDirection * _FrostedSourceTexelSize.xy * _BlurRadius;

            // 5 次采样 + 双线性插值 ≈ 9 抽高斯，权重和为 1
            half3 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord).rgb * 0.2270270270;
            col += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord + step * 1.3846153846).rgb * 0.3162162162;
            col += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord - step * 1.3846153846).rgb * 0.3162162162;
            col += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord + step * 3.2307692308).rgb * 0.0702702703;
            col += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord - step * 3.2307692308).rgb * 0.0702702703;

            return half4(col, 1.0);
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
