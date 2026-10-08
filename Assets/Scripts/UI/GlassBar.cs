using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GlassUI.Game;

namespace GlassUI
{
    public class GlassBar : MonoBehaviour
    {
        Image _fill;
        float _value = 1f;
        float _displayed = 1f;
        float _velocity;

        public float Value
        {
            get { return _value; }
            set { _value = Mathf.Clamp01(value); }
        }

        public static GlassBar Create(Transform parent, Vector2 size, Vector2 anchorPos, Vector2 margin)
        {
            var holder = GlassUI.Node(parent, "Glass Bar");
            GlassUI.Place(holder, new Vector2(0f, 0f), size, new Vector2(margin.x, margin.y), new Vector2(0f, 0f));

            var background = GlassUI.Rect(holder, "BG", new Color(1f, 1f, 1f, 0.38f), 99f);
            GlassUI.Stretch(background.rectTransform);

            var fill = GlassUI.Rect(holder, "Fill", GlassTheme.Blue, 99f);
            GlassUI.Stretch(fill.rectTransform);

            var bar = holder.gameObject.AddComponent<GlassBar>();
            bar._fill = fill;
            return bar;
        }

        void Update()
        {
            // 血条用平滑跟随：掉血时能看出"追条"，比硬切有质感
            _displayed = Mathf.SmoothDamp(_displayed, _value, ref _velocity, 0.18f, 10f, Time.unscaledDeltaTime);
            if (_fill == null) return;

            var rt = _fill.rectTransform;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(Mathf.Clamp01(_displayed), 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            _fill.color = _displayed < 0.3f ? GlassTheme.Bad : GlassTheme.Blue;
        }
    }
}
