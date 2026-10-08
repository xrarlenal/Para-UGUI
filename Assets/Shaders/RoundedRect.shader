Shader "UI/RoundedRect"
{
    // 实心圆角块（可选描边）。形状与描边带来自贴图遮罩：
    //   A = 形状（含抗锯齿）, G = 紧贴边缘的描边带
    // 不依赖额外顶点通道，圆角靠九宫格贴图缩放，边缘永远是干净的。
    Properties
    {
        _MainTex ("形状遮罩 (A=形状 G=描边带)", 2D) = "white" {}
        _StrokeColor ("描边颜色", Color) = (1, 1, 1, 0)
        _StrokeStrength ("描边强度", Range(0, 1)) = 0
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
            Name "RoundedRect"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _StrokeColor;
            float  _StrokeStrength;

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color  : COLOR;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos   : SV_POSITION;
                float4 color : COLOR;
                float2 uv    : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 mask = tex2D(_MainTex, i.uv);

                float stroke = saturate(mask.g * _StrokeStrength);
                half3 rgb = lerp(i.color.rgb, _StrokeColor.rgb, stroke);
                float alpha = mask.a * i.color.a * lerp(1.0, _StrokeColor.a, stroke);

                return fixed4(rgb, alpha);
            }
            ENDHLSL
        }
    }

    Fallback "UI/Default"
}
