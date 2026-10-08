using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace GlassUI
{
    public class GlassToggle : MonoBehaviour
    {
        public Action<bool> onChanged;

        Image _background;
        RectTransform _knob;
        bool _isOn;
        Coroutine _anim;

        public bool IsOn
        {
            get { return _isOn; }
            set
            {
                if (_isOn == value) return;
                _isOn = value;
                GlassAnim.Start(this, ref _anim, Animate());
                onChanged?.Invoke(_isOn);
            }
        }

        public static GlassToggle Create(Transform parent, string name, bool value, Action<bool> onChanged)
        {
            var row = GlassUI.Node(parent, "Toggle: " + name);
            GlassUI.Fix(row, -1f, 34f);

            var label = GlassUI.Label(row, name, 15, TextAnchor.MiddleLeft, GlassTheme.InkSoft);
            GlassUI.Place(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(320f, 26f), Vector2.zero, new Vector2(0f, 0.5f));

            var bg = GlassUI.Rect(row, "BG", new Color(0f, 0f, 0f, 0.58f), 6f, 1f, false);   // 背景框降 10% 不透明 + 一点黑阴影
            GlassUI.Place(bg.rectTransform, new Vector2(1f, 0.5f), new Vector2(52f, 28f), Vector2.zero, new Vector2(1f, 0.5f));
            bg.raycastTarget = true;

            var knob = GlassUI.Rect(bg.transform, "Knob", Color.white, 6f);
            GlassUI.Place(knob.rectTransform, new Vector2(0f, 0.5f), new Vector2(22f, 22f), new Vector2(3f, 0f), new Vector2(0f, 0.5f));

            var toggle = row.gameObject.AddComponent<GlassToggle>();
            toggle._background = bg;
            toggle._knob = knob.rectTransform;
            toggle._isOn = !value;      // 先设成反值，再赋值触发动画
            toggle.onChanged = onChanged;
            toggle.IsOn = value;

            bg.gameObject.AddComponent<Button>().onClick.AddListener(() => toggle.IsOn = !toggle.IsOn);
            return toggle;
        }

        System.Collections.IEnumerator Animate()
        {
            float from = _knob.anchoredPosition.x;
            float to = _isOn ? 27f : 3f;
            var fromColor = _background.color;
            var toColor = _isOn ? GlassTheme.Blue : new Color(1f, 1f, 1f, 0.40f);

            yield return GlassAnim.Curve(0.2f, t =>
            {
                if (_knob != null) _knob.anchoredPosition = new Vector2(Mathf.Lerp(from, to, t), 0f);
                if (_background != null) _background.color = Color.Lerp(fromColor, toColor, t);
            }, Ease.OutBack);
        }
    }
    /// <summary>分段选择（代替下拉框）：一排选项，蓝色高亮块滑过去。</summary>
}
