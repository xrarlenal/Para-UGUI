using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GlassUI
{
    public class HoverFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public float hoverScale = 1.04f;
        public float pressScale = 0.97f;
        public float duration = 0.16f;
        public float hoverLift = 1.5f;

        RectTransform _rt;
        Vector3 _baseScale = Vector3.one;
        Vector2 _basePos;
        Coroutine _anim;
        bool _hovering;

        void Awake()
        {
            _rt = transform as RectTransform;
            if (_rt != null)
            {
                _baseScale = _rt.localScale;
                _basePos = _rt.anchoredPosition;
            }
        }

        public void OnPointerEnter(PointerEventData eventData) { _hovering = true; Play(hoverScale, hoverLift); }
        public void OnPointerExit(PointerEventData eventData) { _hovering = false; Play(1f, 0f); }
        public void OnPointerDown(PointerEventData eventData) { Play(pressScale, 0f); }
        public void OnPointerUp(PointerEventData eventData) { Play(_hovering ? hoverScale : 1f, _hovering ? hoverLift : 0f); }

        void Play(float scale, float lift)
        {
            if (_rt == null) return;
            if (!GlassAnim.Playing)
            {
                _rt.localScale = _baseScale * scale;
                _rt.anchoredPosition = _basePos + new Vector2(0f, lift);
                return;
            }

            var fromScale = _rt.localScale;
            var toScale = _baseScale * scale;
            var fromPos = _rt.anchoredPosition;
            var toPos = _basePos + new Vector2(0f, lift);

            GlassAnim.Start(this, ref _anim, GlassAnim.Curve(duration, t =>
            {
                if (_rt == null) return;
                _rt.localScale = Vector3.LerpUnclamped(fromScale, toScale, t);
                _rt.anchoredPosition = Vector2.LerpUnclamped(fromPos, toPos, t);
            }, Ease.OutCubic));
        }
    }
}
