Shader "UI/FrostedGlass"
{
    // 毛玻璃面片 = 高卷积核高斯模糊后的画面，除此之外什么都不加。
    //   * 没有任何白色/彩色叠加（早期版本的"奶白度"和边缘高光都去掉了）
    //   * _Tint 是乘法（默认白 = 不变），需要压暗时用它，不是叠加
    //   * 形状/圆角来自九宫格遮罩的 alpha（GlassSpriteGenerator 生成）
    // 采样用屏幕 UV：每块面板显示的就是它身后那块被模糊的画面，1:1 裁切，不平铺不拉伸。
    Properties
    {
        _MainTex     ("形状遮罩 (A=形状 G=描边带)", 2D) = "white" {}
        _Opacity     ("整体不透明度", Range(0, 1)) = 1
        _Tint        ("乘法染色（默认白 = 不变）", Color) = (1, 1, 1, 1)
        _StrokeColor ("描边颜色", Color) = (1, 1, 1, 0)
        _StrokeStrength("描边强度", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        // 固定 Always：本工程 UI 用 Overlay 画布，本来就该压在所有 3D 之上；
        // 照抄 [unity_GUIZTestMode] 会在"相机渲染到 RT 截图"时被近处地面挡住，误导验证。
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

                // 屏幕 UV → 取正后方那点被高斯模糊的画面，原样输出
                float2 screenUv = i.screen.xy / i.screen.w;
                half3 glass = tex2D(_FrostedGlassTex, screenUv).rgb * _Tint.rgb;

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
