using System.Collections.Generic;
using UnityEngine;

namespace GlassUI
{
    /// <summary>
    /// 圆角贴图访问入口。贴图由菜单「Glass Demo / 0. 生成圆角贴图」程序化生成：
    /// alpha = 形状，RGB = 纯白。
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

        /// <summary>贴图里烘焙的圆角半径（从名字 roundNN 解析）。</summary>
        public static float BakedRadius(string spriteName)
        {
            if (string.IsNullOrEmpty(spriteName)) return 0f;
            if (spriteName.StartsWith("round") && float.TryParse(spriteName.Substring(5), out var value)) return value;
            return 0f;
        }

        /// <summary>按想要的圆角半径挑最接近的贴图（真实半径由 ppuMultiplier 换算，见 GlassUI.ApplyRadius）。</summary>
        public static string ForRadius(float radius)
        {
            if (radius <= 11f) return Round08;
            if (radius <= 18f) return Round14;
            if (radius <= 26f) return Round22;
            if (radius <= 37f) return Round30;
            return Round44;
        }
    }

    /// <summary>UI 材质：运行时创建、按参数分档缓存。</summary>
    public static class GlassMaterials
    {
        static Material _rounded;
        static readonly Dictionary<int, Material> FrostedCache = new Dictionary<int, Material>();

        /// <summary>实心圆角块：颜色走顶点色，全工程共用一份材质。</summary>
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

        /// <summary>毛玻璃：按 blurLod 分档共用。背景统一叠 20% 黑。</summary>
        public static Material Frosted(float blurLod = 7f)
        {
            int key = Mathf.RoundToInt(blurLod * 10f);
            if (FrostedCache.TryGetValue(key, out var cached) && cached != null) return cached;

            var shader = Shader.Find("UI/FrostedGlass");
            if (shader == null) return null;

            var material = new Material(shader)
            {
                name = $"FrostedGlass (lod {blurLod:0.0})",
                hideFlags = HideFlags.HideAndDontSave,
            };
            material.SetFloat("_BlurLod", blurLod);
            material.SetFloat("_Darken", 0.20f);
            FrostedCache[key] = material;
            return material;
        }

        /// <summary>圆角块（描边通道已取消，需要描边用单独的圆角框控件）。</summary>
        public static Material Stroked(Color strokeColor, float strength)
        {
            return Rounded();
        }
    }
}
