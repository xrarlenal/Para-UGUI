using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace GlassUI
{
    public class ItemChip : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler,
        IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public ItemDef item;
        InventoryView _view;
        InventorySlot _slot;
        CanvasGroup _group;
        RectTransform _rect;
        Transform _home;
        Coroutine _chipAnim;

        public static ItemChip Create(Transform parent, ItemDef item, InventoryView view)
        {
            var holder = GlassUI.Node(parent, "Item: " + item.name);
            GlassUI.Stretch(holder, 1f, 1f, 1f, 1f);   // 填满格子，别留出漏光的缝

            var bg = GlassUI.Rect(holder, "BG", item.color, GlassTheme.RadiusItem);
            GlassUI.Stretch(bg.rectTransform);

            var badge = GlassUI.Label(holder, item.badge, 26, TextAnchor.MiddleCenter, GlassTheme.White, false, FontStyle.Bold);
            GlassUI.Stretch(badge.rectTransform);

            if (item.count > 1)
            {
                var count = GlassUI.Label(holder, item.count.ToString(), 13, TextAnchor.LowerRight, GlassTheme.White, false, FontStyle.Bold);
                GlassUI.Stretch(count.rectTransform, 4f, 4f, 8f, 4f);
            }

            var chip = holder.gameObject.AddComponent<ItemChip>();
            chip.item = item;
            chip._view = view;
            chip._rect = holder;
            chip._group = holder.gameObject.AddComponent<CanvasGroup>();
            chip._slot = holder.GetComponentInParent<InventorySlot>();
            if (chip._slot != null) chip._slot.occupant = chip;
            return chip;
        }

        void Start()
        {
            if (GlassAnim.Playing)
                GlassAnim.Start(this, ref _chipAnim, GlassAnim.Curve(0.24f, t =>
                {
                    transform.localScale = Vector3.one * Mathf.Lerp(0.7f, 1f, Ease.OutBack(t));
                }, Ease.Linear));
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            var slot = GetComponentInParent<InventorySlot>();
            if (slot != null) _view.Select(slot.transform.GetSiblingIndex());
        }

        public void OnPointerEnter(PointerEventData eventData) { }
        public void OnPointerExit(PointerEventData eventData) { }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _home = transform.parent;
            _slot = GetComponentInParent<InventorySlot>();
            if (_slot != null) _slot.occupant = null;

            var canvas = GetComponentInParent<Canvas>();
            var root = canvas != null ? canvas.rootCanvas.transform : null;
            if (root != null) transform.SetParent(root, true);
            _group.blocksRaycasts = false;
            transform.localScale = Vector3.one * 1.08f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            transform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            var hit = eventData.pointerCurrentRaycast.gameObject;
            var target = hit != null ? hit.GetComponentInParent<InventorySlot>() : null;

            if (target != null && target.occupant == null)
            {
                transform.SetParent(target.transform, false);
                _rect.anchoredPosition = Vector2.zero;
                _slot = target;
                target.occupant = this;
            }
            else if (_home != null)
            {
                transform.SetParent(_home, false);
                _rect.anchoredPosition = Vector2.zero;
                _slot = GetComponentInParent<InventorySlot>();
                if (_slot != null) _slot.occupant = this;
            }

            transform.localScale = Vector3.one;
            _group.blocksRaycasts = true;

            var parentSlot = GetComponentInParent<InventorySlot>();
            if (parentSlot != null) _view.Select(parentSlot.transform.GetSiblingIndex());
        }
    }
    // =====================================================================================
    //  设置：页签 + 真正生效的参数
    // =====================================================================================
}
