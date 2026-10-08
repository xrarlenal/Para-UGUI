using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace GlassUI
{
    public class ItemDef
    {
        public string name;
        public string kind;          // 武器 / 消耗品 / 材料
        public string rarity;
        public string description;
        public string badge;         // 两字以内的图标占位
        public Color color;
        public int count = 1;
        public float heal;
        public int weaponIndex = -1;
    }
}
