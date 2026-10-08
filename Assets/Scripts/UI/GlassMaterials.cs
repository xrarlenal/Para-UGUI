using System.Collections.Generic;
using UnityEngine;

namespace GlassUI
{
    /// <summary>
    /// 圆角贴图的访问入口。贴图由菜单「Glass Demo / 0. 生成圆角贴图」程序化生成。
    /// 一张贴图三个通道：A = 形状，R = 边缘渐变（玻璃厚度高光），G = 描边带。
    /// </summary>
    public static class GlassSprites
    {
        public const string Round08 = "GlassSprites/round08";
        public const string Round14 = "GlassSprites/round14";
        public const string Round22 = "GlassSprites/round22";
        public const string Round30 = "GlassSprites/round30";
        public const string Round44 = "GlassSprites/round44";

        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite Get(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath)) return null;
            if (Cache.TryGetValue(resourcePath, out var cached)) return cached;

            var sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite == null)
                Debug.LogWarning($"[GlassUI] 缺少圆角贴图 {resourcePath}，请先执行菜单「Glass Demo / 0. 生成圆角贴图」");

            Cache[resourcePath] = sprite;
            return sprite;
        }

        /// <summary>按想要的圆角半径挑最接近的贴图（九宫格圆角是固定像素的，半径要跟贴图对上）。</summary>
        public static string ForRadius(float radius)
        {
            if (radius <= 11f) return Round08;
            if (radius <= 18f) return Round14;
            if (radius <= 26f) return Round22;
            if (radius <= 37f) return Round30;
            return Round44;
        }
    }

    /// <summary>
    /// UI 材质。这些材质在运行时创建（编辑器里建出来的材质存不进场景），所以同一份配置只建一次并缓存。
    /// </summary>
    public static class GlassMaterials
    {
        static Material _rounded;
        static readonly Dictionary<int, Material> FrostedCache = new Dictionary<int, Material>();
        static readonly Dictionary<int, Material> StrokedCache = new Dictionary<int, Material>();

        /// <summary>实心圆角块：颜色走顶点色，所以全工程共用一份材质。</summary>
        public static Material Rounded()
        {
            if (_rounded == null)
            {
                var shader = Shader.Find("UI/RoundedRect");
                if (shader == null) return null;
                _rounded = new Material(shader) { name = "RoundedRect (shared)", hideFlags = HideFlags.HideAndDontSave };
            }
            return _rounded;
        }

        /// <summary>毛玻璃：奶白度不同就各要一份材质（同一档位共用）。</summary>
        public static Material Frosted(float milkiness, float rimBoost = 1f, float blurStrength = 1f)
        {
            int key = Mathf.RoundToInt(milkiness * 100f) * 10000 + Mathf.RoundToInt(rimBoost * 100f) * 100 + Mathf.RoundToInt(blurStrength * 100f);
            if (FrostedCache.TryGetValue(key, out var cached) && cached != null) return cached;

            var shader = Shader.Find("UI/FrostedGlass");
            if (shader == null) return null;

            var material = new Material(shader)
            {
                name = $"FrostedGlass (milk {milkiness:0.00})",
                hideFlags = HideFlags.HideAndDontSave,
            };
            material.SetFloat("_Milkiness", milkiness);
            material.SetFloat("_RimBoost", rimBoost);
            material.SetFloat("_BlurStrength", blurStrength);
            FrostedCache[key] = material;
            return material;
        }

        /// <summary>带描边的实心圆角块（输入框、选中态描边）。</summary>
        public static Material Stroked(Color strokeColor, float strength)
        {
            int key = ColorKey(strokeColor) * 100 + Mathf.RoundToInt(strength * 100f);
            if (StrokedCache.TryGetValue(key, out var cached) && cached != null) return cached;

            var shader = Shader.Find("UI/RoundedRect");
            if (shader == null) return null;

            var material = new Material(shader)
            {
                name = "RoundedRect (stroked)",
                hideFlags = HideFlags.HideAndDontSave,
            };
            material.SetColor("_StrokeColor", strokeColor);
            material.SetFloat("_StrokeStrength", Mathf.Clamp01(strength));
            StrokedCache[key] = material;
            return material;
        }

        static int ColorKey(Color c)
        {
            return (Mathf.RoundToInt(c.r * 255f) << 24) | (Mathf.RoundToInt(c.g * 255f) << 16) |
                   (Mathf.RoundToInt(c.b * 255f) << 8) | Mathf.RoundToInt(c.a * 255f);
        }
    }
}
