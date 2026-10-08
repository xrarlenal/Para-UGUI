using System;
using System.Collections;
using UnityEngine;

namespace GlassUI
{
    /// <summary>
    /// 全 Demo 的配色与动效参数。主色就蓝白两套 + 少量语义色（成功/警告/危险）。
    /// 注意：这些颜色只上在文字、图标、描边、状态上——玻璃面本身的颜色来自场景，
    /// 不在这里叠色（UI/FrostedGlass 的 _Tint 默认白、_Milkiness 只给一点点奶白感）。
    /// </summary>
    public static class GlassTheme
    {
        // ---- 文字与前景（打在毛玻璃上，所以用深蓝墨色压住亮场景）----
        public static readonly Color Ink = Hex("#0A1526");
        public static readonly Color InkSoft = Hex("#38506E");
        public static readonly Color InkFaint = Hex("#7C90AA");
        public static readonly Color Paper = Hex("#F7FAFF");

        // ---- 主色：蓝 / 白 ----
        public static readonly Color Blue = Hex("#2E6BFF");
        public static readonly Color BlueBright = Hex("#5B90FF");
        public static readonly Color BlueDeep = Hex("#0E2A66");
        public static readonly Color Cyan = Hex("#5FD0FF");
        public static readonly Color Violet = Hex("#8B6BFF");
        public static readonly Color White = Color.white;
        public static readonly Color WhiteSoft = Hex("#DCE8FA");

        // ---- 状态色（克制使用）----
        public static readonly Color Good = Hex("#2FBF8F");
        public static readonly Color Warn = Hex("#F2A93B");
        public static readonly Color Bad = Hex("#F0566B");

        /// <summary>文字外侧的白晕，用来在亮场景上保住可读性（不是给玻璃叠色）。</summary>
        public static readonly Color Halo = new Color(1f, 1f, 1f, 0.85f);

        // 圆角规范（面板大圆角、按钮胶囊、小组件中圆角）
        public const float RadiusPanel = 28f;
        public const float RadiusCard = 22f;
        public const float RadiusItem = 14f;
        public const float RadiusPill = 12f;

        public static Color Hex(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;
        }

        public static Color Alpha(this Color c, float a)
        {
            c.a = a;
            return c;
        }
    }

    /// <summary>非线性缓动：所有进出场、悬浮、切换都用这里的曲线，不用线性。</summary>
    public static class Ease
    {
        public static float Linear(float t) => t;

        public static float OutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
        public static float OutQuint(float t) => 1f - Mathf.Pow(1f - t, 5f);
        public static float OutExpo(float t) => t >= 1f ? 1f : 1f - Mathf.Pow(2f, -10f * t);

        public static float InOutCubic(float t) =>
            t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) * 0.5f;

        public static float OutBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            float p = t - 1f;
            return 1f + c3 * p * p * p + c1 * p * p;
        }

        public static float OutElastic(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            const float c4 = 2f * Mathf.PI / 3f;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
        }

        /// <summary>先冲过去再回落，适合面板展开。</summary>
        public static float Overshoot(float t) => OutBack(t);
    }

    /// <summary>
    /// 极简补间：和 UGUI 无关，纯协程。播放模式下才跑，编辑器里直接落终态（方便离线截图）。
    /// </summary>
    public static class GlassAnim
    {
        public static bool Playing => Application.isPlaying;

        public static void Start(MonoBehaviour host, ref Coroutine handle, IEnumerator routine)
        {
            if (!Playing) return;
            if (handle != null) host.StopCoroutine(handle);
            handle = host.StartCoroutine(routine);
        }

        public static void Stop(MonoBehaviour host, ref Coroutine handle)
        {
            if (handle == null) return;
            if (Playing) host.StopCoroutine(handle);
            handle = null;
        }

        public static IEnumerator Float(float from, float to, float duration, Action<float> step, Func<float, float> ease = null)
        {
            return Curve(duration, t => step(Mathf.LerpUnclamped(from, to, t)), ease);
        }

        public static IEnumerator Vector2(Vector2 from, Vector2 to, float duration, Action<Vector2> step, Func<float, float> ease = null)
        {
            return Curve(duration, t => step(UnityEngine.Vector2.LerpUnclamped(from, to, t)), ease);
        }

        public static IEnumerator Curve(float duration, Action<float> step, Func<float, float> ease = null)
        {
            if (duration <= 0f)
            {
                step(1f);
                yield break;
            }

            step(0f);
            float time = 0f;
            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(time / duration);
                step(ease != null ? ease(t) : t);
                yield return null;
            }
            step(1f);
        }
    }
}
