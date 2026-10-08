using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace GlassUI
{
    public class GlassSlider : MonoBehaviour, IBeginDragHandler, IDragHandler, IPointerDownHandler
    {
        public float min, max = 1f;
        public Action<float> onChanged;

        RectTransform _track;
        Image _fill;
        RectTransform _knob;
        float _value;
        Coroutine _anim;

        public float Value
        {
            get { return _value; }
            set { Set(value, true); }
        }

        public static GlassSlider Create(Transform parent, string name, float min, float max, float value,
            Action<float> onChanged, string suffix = "")
        {
            var row = GlassUI.Node(parent, "Slider: " + name);
            GlassUI.Fix(row, -1f, 34f);

            var label = GlassUI.Label(row, name, 15, TextAnchor.MiddleLeft, GlassTheme.InkSoft);
            GlassUI.Place(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(150f, 26f), Vector2.zero, new Vector2(0f, 0.5f));

            var valueLabel = GlassUI.Label(row, "", 15, TextAnchor.MiddleRight, GlassTheme.Blue, false, FontStyle.Bold);
            GlassUI.Place(valueLabel.rectTransform, new Vector2(1f, 0.5f), new Vector2(88f, 26f), Vector2.zero, new Vector2(1f, 0.5f));

            var track = GlassUI.Node(row, "Track");
            GlassUI.Stretch(track, 160f, 12f, 96f, 12f);

            var bg = GlassUI.Rect(track, "BG", new Color(1f, 1f, 1f, 0.40f), 99f);
            GlassUI.Stretch(bg.rectTransform);

            var fill = GlassUI.Rect(track, "Fill", GlassTheme.Blue, 99f);
            GlassUI.Stretch(fill.rectTransform);

            var knob = GlassUI.Rect(track, "Knob", Color.white, 99f);
            // 用锚点表示位置：拉伸矩形上改 anchoredPosition 会被拉伸/变形，锚点则天然跟着比例走
            knob.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            knob.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            knob.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            knob.rectTransform.sizeDelta = new Vector2(18f, 18f);
            knob.rectTransform.anchoredPosition = Vector2.zero;

            bg.raycastTarget = true;

            var slider = row.gameObject.AddComponent<GlassSlider>();
            slider._track = track;
            slider._fill = fill;
            slider._knob = knob.rectTransform;
            slider.min = min;
            slider.max = max;
            slider.onChanged = v =>
            {
                valueLabel.text = suffix == "" ? v.ToString("0.##") : $"{v:0.##}{suffix}";
                onChanged?.Invoke(v);
            };
            slider.Set(value, true);
            return slider;
        }

        public void OnPointerDown(PointerEventData eventData) => Apply(eventData);
        public void OnBeginDrag(PointerEventData eventData) => Apply(eventData);
        public void OnDrag(PointerEventData eventData) => Apply(eventData);

        void Apply(PointerEventData eventData)
        {
            if (_track == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_track, eventData.position, eventData.pressEventCamera, out var local))
                return;
            float t = Mathf.Clamp01((local.x - _track.rect.xMin) / Mathf.Max(1f, _track.rect.width));
            Set(Mathf.Lerp(min, max, t), false);
        }

        void Set(float value, bool instant)
        {
            _value = Mathf.Clamp(value, min, max);
            float t = max - min <= 0.0001f ? 0f : Mathf.InverseLerp(min, max, _value);
            Layout(t);
            onChanged?.Invoke(_value);
        }

        void Layout(float t)
        {
            if (_fill != null)
            {
                _fill.rectTransform.anchorMin = new Vector2(0f, 0f);
                _fill.rectTransform.anchorMax = new Vector2(t, 1f);
                _fill.rectTransform.offsetMin = Vector2.zero;
                _fill.rectTransform.offsetMax = Vector2.zero;
            }
            if (_knob != null)
            {
                _knob.anchorMin = new Vector2(t, 0.5f);
                _knob.anchorMax = new Vector2(t, 0.5f);
                _knob.anchoredPosition = Vector2.zero;
            }
        }
    }
    /// <summary>胶囊开关：圆钮滑动，带过冲。</summary>
}
