using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GlassUI
{
    public static class GlassUI
    {
        public static Font Font
        {
            get
            {
                if (_font == null)
                {
                    _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    if (_font == null) Debug.LogWarning("[GlassUI] 取不到内置字体 LegacyRuntime.ttf");
                }
                return _font;
            }
        }

        static Font _font;

        // ---------------- 节点 ----------------

        public static RectTransform Node(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            rt.anchoredPosition3D = Vector3.zero;
            return rt;
        }

        public static void Clear(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i).gameObject;
                if (Application.isPlaying) UnityEngine.Object.Destroy(child);
                else UnityEngine.Object.DestroyImmediate(child);
            }
        }

        public static void Kill(UnityEngine.Object target)
        {
            if (target == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(target);
            else UnityEngine.Object.DestroyImmediate(target);
        }

        public static RectTransform Stretch(RectTransform rt, float left = 0f, float top = 0f, float right = 0f, float bottom = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        /// <summary>按锚点定位：锚点重合 + 固定尺寸。</summary>
        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 size, Vector2 pos, Vector2? pivot = null)
        {
            var p = pivot ?? new Vector2(0.5f, 0.5f);
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = p;
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            return rt;
        }

        public static RectTransform Top(RectTransform rt, float height, float left = 0f, float right = 0f, float top = 0f)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(left, -height - top);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        public static RectTransform Bottom(RectTransform rt, float height, float left = 0f, float right = 0f, float bottom = 0f)
        {
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, height + bottom);
            return rt;
        }

        // ---------------- 布局 ----------------

        public static VerticalLayoutGroup Col(RectTransform rt, float spacing = 10f, int pad = 0,
            TextAnchor align = TextAnchor.UpperLeft, bool controlWidth = true, bool controlHeight = true,
            bool expandWidth = true, bool expandHeight = false)
        {
            var g = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            g.spacing = spacing;
            g.padding = new RectOffset(pad, pad, pad, pad);
            g.childAlignment = align;
            g.childControlWidth = controlWidth;
            g.childControlHeight = controlHeight;
            g.childForceExpandWidth = expandWidth;
            g.childForceExpandHeight = expandHeight;
            return g;
        }

        public static HorizontalLayoutGroup Row(RectTransform rt, float spacing = 10f, int pad = 0,
            TextAnchor align = TextAnchor.MiddleLeft, bool controlWidth = true, bool controlHeight = true,
            bool expandWidth = false, bool expandHeight = true)
        {
            var g = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            g.spacing = spacing;
            g.padding = new RectOffset(pad, pad, pad, pad);
            g.childAlignment = align;
            g.childControlWidth = controlWidth;
            g.childControlHeight = controlHeight;
            g.childForceExpandWidth = expandWidth;
            g.childForceExpandHeight = expandHeight;
            return g;
        }

        public static GridLayoutGroup Grid(RectTransform rt, float cell, float spacing, int columns)
        {
            var g = rt.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = new Vector2(cell, cell);
            g.spacing = new Vector2(spacing, spacing);
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            g.constraintCount = Mathf.Max(1, columns);
            g.childAlignment = TextAnchor.UpperLeft;
            return g;
        }

        static LayoutElement Element(Component target)
        {
            var le = target.GetComponent<LayoutElement>();
            return le != null ? le : target.gameObject.AddComponent<LayoutElement>();
        }

        /// <summary>固定尺寸。对应轴的 flexible 必须显式写 0，否则内层布局组的弹性会漏上来。</summary>
        public static LayoutElement Fix(Component target, float width = -1f, float height = -1f)
        {
            var le = Element(target);
            if (width > 0f) { le.minWidth = width; le.preferredWidth = width; le.flexibleWidth = 0f; }
            if (height > 0f) { le.minHeight = height; le.preferredHeight = height; le.flexibleHeight = 0f; }
            return le;
        }

        public static LayoutElement Flex(Component target, float width = -1f, float height = -1f,
            float minWidth = -1f, float minHeight = -1f)
        {
            var le = Element(target);
            if (width >= 0f) le.flexibleWidth = width;
            if (height >= 0f) le.flexibleHeight = height;
            if (minWidth >= 0f) le.minWidth = minWidth;
            if (minHeight >= 0f) le.minHeight = minHeight;
            return le;
        }

        public static ContentSizeFitter Fit(RectTransform rt, ContentSizeFitter.FitMode horizontal, ContentSizeFitter.FitMode vertical)
        {
            var f = rt.gameObject.AddComponent<ContentSizeFitter>();
            f.horizontalFit = horizontal;
            f.verticalFit = vertical;
            return f;
        }

        public static CanvasGroup Group(RectTransform rt, float alpha = 1f, bool interactable = true, bool blockRaycasts = true)
        {
            var cg = rt.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = alpha;
            cg.interactable = interactable;
            cg.blocksRaycasts = blockRaycasts;
            return cg;
        }

        public static void Refresh()
        {
            Canvas.ForceUpdateCanvases();
        }

        public static void Rebuild(RectTransform rt)
        {
            if (rt != null) UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        }

        public static RectTransform Rt(this Component target)
        {
            return target == null ? null : target.transform as RectTransform;
        }

        // ---------------- 图元 ----------------

        /// <summary>
        /// 毛玻璃面板：背景 = 身后画面被 URP 做完高卷积核高斯模糊后的样子，按屏幕 UV 1:1 裁切。
        /// 纯模糊结果，不加任何白色/彩色叠加。
        /// 形状与圆角来自九宫格贴图（alpha），所以直接把 Image 用起来——布局、遮罩、Filled 都能照常使。
        /// </summary>
        public static Image Panel(Transform parent, string name = "Glass Panel", float radius = GlassTheme.RadiusPanel)
        {
            var rt = Node(parent, name);
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = GlassSprites.Get(GlassSprites.ForRadius(radius));
            image.type = Image.Type.Sliced;
            image.material = GlassMaterials.Frosted();
            image.color = Color.white;      // 白色 = 不给玻璃上色
            image.raycastTarget = true;
            return image;
        }

        /// <summary>实心圆角块（蓝白色条、胶囊、进度条、头像底）。</summary>
        public static Image Rect(Transform parent, string name, Color color, float radius = GlassTheme.RadiusPill)
        {
            var rt = Node(parent, name);
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = GlassSprites.Get(GlassSprites.ForRadius(radius));
            image.type = Image.Type.Sliced;
            image.material = GlassMaterials.Rounded();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>带描边的圆角块：输入框、选中态的边框。</summary>
        public static Image StrokedRect(Transform parent, string name, Color fill, float radius, Color stroke, float strokeStrength = 1f)
        {
            var image = Rect(parent, name, fill, radius);
            image.material = GlassMaterials.Stroked(stroke, strokeStrength);
            return image;
        }

        public static Text Label(Transform parent, string text, int size = 18, TextAnchor anchor = TextAnchor.MiddleLeft,
            Color? color = null, bool wrap = false, FontStyle style = FontStyle.Normal, string name = null)
        {
            var rt = Node(parent, name ?? ("Label: " + Shorten(text)));
            var label = rt.gameObject.AddComponent<Text>();
            label.font = Font;
            label.fontSize = size;
            label.text = text;
            label.alignment = anchor;
            label.color = color ?? GlassTheme.Ink;
            label.fontStyle = style;
            label.supportRichText = true;
            label.horizontalOverflow = wrap ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            return label;
        }

        /// <summary>给文字加一圈白色描边，压在玻璃上也能读清（不是给玻璃上色）。</summary>
        public static Outline Halo(Graphic graphic, float distance = 1.4f)
        {
            var outline = graphic.gameObject.AddComponent<Outline>();
            outline.effectColor = GlassTheme.Halo;
            outline.effectDistance = new Vector2(distance, -distance);
            outline.useGraphicAlpha = true;
            return outline;
        }

        /// <summary>胶囊按钮：圆角底 + 文字 + 悬停/按下的非线性反馈。</summary>
        public static Button Pill(Transform parent, string text, Action onClick, float height = 44f,
            float radius = GlassTheme.RadiusPill, bool primary = true, int fontSize = 17)
        {
            var bg = Rect(parent, "Pill: " + text, primary ? GlassTheme.Blue : GlassTheme.White, radius);
            bg.raycastTarget = true;
            Fix(bg, -1f, height);

            var button = bg.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.45f);
            colors.fadeDuration = 0.1f;
            button.colors = colors;

            var label = Label(bg.transform, text, fontSize, TextAnchor.MiddleCenter,
                primary ? GlassTheme.White : GlassTheme.Blue, false, FontStyle.Bold);
            Stretch(label.rectTransform, 18f, 0f, 18f, 0f);

            var fx = bg.gameObject.AddComponent<HoverFx>();
            fx.hoverScale = 1.035f;
            fx.pressScale = 0.97f;

            if (onClick != null) button.onClick.AddListener(() => onClick());
            return button;
        }

        static string Shorten(string s)
        {
            if (string.IsNullOrEmpty(s)) return "empty";
            return s.Length <= 12 ? s : s.Substring(0, 12) + "…";
        }
    }
    /// <summary>
    /// 悬停/按下的非线性反馈：缩放 + 轻微上浮。挂在任何带 RectTransform 的交互物件上都行。
    /// 编辑器里（离线截图）直接落终态，不跑协程。
    /// </summary>
}
