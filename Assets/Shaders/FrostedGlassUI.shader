Shader "UI/FrostedGlass"
{
    // 毛玻璃面片：背景 = 这块控件身后那块屏幕区域被 URP 模糊后的画面，按屏幕 UV 1:1 裁切（不平铺不拉伸）。
    // 形状 / 圆角 / 边缘高光 / 描边全部来自贴图遮罩（GlassSpriteGenerator 生成）：
    //   A = 形状（含抗锯齿）, R = 从边缘往内的渐变（做玻璃厚度高光）, G = 描边带
    // 这样着色器只需要 POSITION / uv0 / 顶点色，不依赖任何额外顶点通道。
    Properties
    {
        _MainTex     ("形状遮罩 (A=形状 R=边缘渐变 G=描边带)", 2D) = "white" {}
        _Milkiness   ("奶白度（0 = 完全透出场景）", Range(0, 1)) = 0.14
        _VeilColor   ("奶白色", Color) = (1, 1, 1, 1)
        _RimColor    ("边缘高光", Color) = (1, 1, 1, 0.45)
        _RimBoost    ("边缘高光强度", Range(0, 3)) = 1
        _BlurStrength("模糊画面占比", Range(0, 1)) = 1
        _Opacity     ("整体不透明度", Range(0, 1)) = 1
        _Tint        ("玻璃染色（默认白 = 不上色）", Color) = (1, 1, 1, 1)
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
        // 固定 Always：本工程的 UI 用的是 Overlay 画布，本来就该压在所有 3D 之上。
        // 照抄 UI/Default 的 [unity_GUIZTestMode] 会在「用相机渲染到 RenderTexture 截图」时
        // 被近处地面挡住（LEqual + 画布平面距离），离屏验证会被误导。
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
            float  _Milkiness;
            float4 _VeilColor;
            float4 _RimColor;
            float  _RimBoost;
            float  _BlurStrength;
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

                // 屏幕 UV —— 每个像素取它正后方那点被模糊的画面
                float2 screenUv = i.screen.xy / i.screen.w;
                half3 scene = tex2D(_FrostedGlassTex, screenUv).rgb;

                half3 glass = lerp(scene, _VeilColor.rgb, _Milkiness);        // 奶白感，保证文字读得清
                glass += _RimColor.rgb * mask.r * _RimColor.a * _RimBoost;    // 边缘高光 = 有厚度的玻璃
                glass *= _Tint.rgb;
                glass = lerp(glass, scene, 1.0 - _BlurStrength);              // 0 = 完全不糊

                float stroke = saturate(mask.g * _StrokeStrength);
                glass = lerp(glass, _StrokeColor.rgb, stroke);

                float alpha = mask.a * i.color.a * _Opacity;
                alpha = lerp(alpha, alpha * _StrokeColor.a, stroke);

                return fixed4(glass * i.color.rgb, alpha);
            }
            ENDHLSL
        }
    }

    Fallback "UI/Default"
}
