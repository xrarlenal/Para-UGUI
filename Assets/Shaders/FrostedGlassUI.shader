Shader "UI/FrostedGlass"
{
    // 毛玻璃面片 = 从离屏模糊纹理上按屏幕坐标采样，仅此而已。
    //
    // 模糊实现照 Blur.cpp 的思路：把「没有 UI 的画面」拷进一张带 mipmap 的离屏纹理，
    // 生成整条 mip 链（逐层缩小 = 逐层更糊），UI 这边用 textureLod 读高层 LOD。
    // 这样采样范围可以做得非常大（LOD 7 ≈ 128 倍缩小），而且不会像固定 11×11 稀疏采样那样出现块状伪影。
    //
    //   _BlurLod   : mip LOD，就是"糊到什么程度"（0 = 原图，7 = 很糊）
    //   _Haze      : 参考里的 haze_amount，默认 0 = 不叠加任何颜色（要跟参考一致就设 0.14）
    //   _Tint      : 乘法（默认白 = 不变），要压暗用它
    Properties
    {
        _MainTex     ("形状遮罩 (A=形状 G=描边带)", 2D) = "white" {}
        _BlurLod     ("模糊强度 (mip LOD)", Range(0, 12)) = 7
        _Darken      ("黑色叠加", Range(0, 0.6)) = 0.1
        _Haze        ("haze 叠加", Range(0, 0.6)) = 0
        _HazeColor   ("haze 颜色", Color) = (0.95, 0.97, 1, 1)
        _Opacity     ("整体不透明度", Range(0, 1)) = 1
        _Tint        ("乘法染色（默认白 = 不变）", Color) = (1, 1, 1, 1)
        _StrokeColor ("描边颜色", Color) = (1, 1, 1, 0)
        _StrokeStrength("描边强度", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent"
            "PreviewType" = "Plane" "CanUseSpriteAtlas" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            Name "FrostedGlass"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float  _BlurLod;
            float  _Darken;
            float  _Haze;
            float4 _HazeColor;
            float  _Opacity;
            float4 _Tint;
            float4 _StrokeColor;
            float  _StrokeStrength;

            sampler2D _FrostedGlassTex;

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color  : COLOR;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos    : SV_POSITION;
                float4 color  : COLOR;
                float2 uv     : TEXCOORD0;
                float4 screen : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.screen = ComputeScreenPos(o.pos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 mask = tex2D(_MainTex, i.uv);

                // 屏幕坐标 → 直接读那条 mip 链的高层 LOD
                float2 screenUv = i.screen.xy / i.screen.w;
                half3 glass = tex2Dlod(_FrostedGlassTex, float4(screenUv, 0.0, _BlurLod)).rgb;

                glass *= (1.0 - _Darken);                     // 统一叠一层 10% 黑，让文字压得住
                glass = lerp(glass, _HazeColor.rgb, _Haze);   // 默认 0，不叠任何东西
                glass *= _Tint.rgb;

                float stroke = saturate(mask.g * _StrokeStrength);
                glass = lerp(glass, _StrokeColor.rgb, stroke);

                float alpha = mask.a * i.color.a * _Opacity;
                return fixed4(glass * i.color.rgb, alpha * lerp(1.0, _StrokeColor.a, stroke));
            }
            ENDHLSL
        }
    }

    Fallback "UI/Default"
}
