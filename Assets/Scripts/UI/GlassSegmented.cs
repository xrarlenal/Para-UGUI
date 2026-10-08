using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace GlassUI
{
    public class GlassSegmented : MonoBehaviour
    {
        public Action<int> onChanged;
        public int Value { get; private set; }

        RectTransform _highlight;
        readonly List<RectTransform> _items = new List<RectTransform>();
        readonly List<Text> _labels = new List<Text>();
        Coroutine _anim;

        public static GlassSegmented Create(Transform parent, string name, string[] options, int value, Action<int> onChanged)
        {
            var row = GlassUI.Node(parent, "Segment: " + name);
            GlassUI.Fix(row, -1f, 34f);

            var label = GlassUI.Label(row, name, 15, TextAnchor.MiddleLeft, GlassTheme.InkSoft);
            GlassUI.Place(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(180f, 26f), Vector2.zero, new Vector2(0f, 0.5f));

            var holder = GlassUI.Rect(row, "Holder", new Color(0f, 0f, 0f, 0.58f), 6f, 1f, false);
            GlassUI.Place(holder.rectTransform, new Vector2(1f, 0.5f), new Vector2(260f, 30f), Vector2.zero, new Vector2(1f, 0.5f));

            var highlight = GlassUI.Rect(holder.transform, "Highlight", GlassTheme.Blue, 6f);
            highlight.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            highlight.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            highlight.rectTransform.pivot = new Vector2(0f, 0.5f);
            highlight.rectTransform.sizeDelta = new Vector2(254f / Mathf.Max(1, options.Length) - 6f, 24f);
            highlight.rectTransform.anchoredPosition = new Vector2(3f, 0f);

            var segmented = row.gameObject.AddComponent<GlassSegmented>();
            segmented._highlight = highlight.rectTransform;

            float width = 254f / options.Length;
            for (int i = 0; i < options.Length; i++)
            {
                int index = i;
                var item = GlassUI.Node(holder.transform, "Item " + i);
                GlassUI.Place(item, new Vector2(0f, 0.5f), new Vector2(width, 24f), new Vector2(3f + width * i, 0f), new Vector2(0f, 0.5f));

                var hit = GlassUI.Rect(item, "Hit", new Color(1f, 1f, 1f, 0f), 6f);
                GlassUI.Stretch(hit.rectTransform);
                hit.raycastTarget = true;
                hit.gameObject.AddComponent<Button>().onClick.AddListener(() => segmented.Select(index));

                var text = GlassUI.Label(item, options[i], 14, TextAnchor.MiddleCenter, GlassTheme.InkSoft, false, FontStyle.Bold);
                GlassUI.Stretch(text.rectTransform);

                segmented._items.Add(item);
                segmented._labels.Add(text);
            }

            segmented.onChanged = onChanged;
            segmented.Select(value, true);
            return segmented;
        }

        public void Select(int index, bool instant = false)
        {
            if (_items.Count == 0) return;
            Value = Mathf.Clamp(index, 0, _items.Count - 1);

            for (int i = 0; i < _labels.Count; i++)
                _labels[i].color = i == Value ? GlassTheme.White : GlassTheme.InkSoft;

            var target = _items[Value].anchoredPosition.x + 3f;
            if (instant || !GlassAnim.Playing)
            {
                _highlight.anchoredPosition = new Vector2(target, 0f);
                return;
            }

            float from = _highlight.anchoredPosition.x;
            GlassAnim.Start(this, ref _anim, GlassAnim.Float(from, target, 0.22f,
                v => _highlight.anchoredPosition = new Vector2(v, 0f), Ease.OutBack));
        }
    }
    // =====================================================================================
    //  背包：网格 + 拖放 + 详情 + 页签
    // =====================================================================================
}
