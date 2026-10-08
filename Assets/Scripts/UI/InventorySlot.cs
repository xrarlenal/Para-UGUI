using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace GlassUI
{
    public class InventorySlot : MonoBehaviour
    {
        public Image frame;
        public ItemChip occupant;

        public static InventorySlot Create(Transform parent, int index)
        {
            var frame = GlassUI.Rect(parent, $"Slot {index:00}", new Color(1f, 1f, 1f, 0.28f), GlassTheme.RadiusItem, 0.9f, true);
            GlassUI.Fix(frame, 72f, 72f);
            frame.raycastTarget = true;

            var slot = frame.gameObject.AddComponent<InventorySlot>();
            slot.frame = frame;
            return slot;
        }
    }
    /// <summary>物品图标 + 数量，可拖到别的格子。</summary>
}
