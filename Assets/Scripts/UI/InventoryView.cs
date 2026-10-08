using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace GlassUI
{
    public class InventoryView : MonoBehaviour
    {
        public Action onClose;

        readonly List<ItemDef> _all = new List<ItemDef>();
        readonly List<ItemDef> _visible = new List<ItemDef>();
        readonly List<InventorySlot> _slots = new List<InventorySlot>();
        readonly List<Image> _tabPills = new List<Image>();
        readonly List<Text> _tabLabels = new List<Text>();
        readonly List<ItemChip> _chips = new List<ItemChip>();

        RectTransform _grid;
        Text _detailName;
        Text _detailKind;
        Text _detailBody;
        Image _detailIcon;
        Text _detailIconText;
        Button _useButton;
        Text _useLabel;
        int _selected = -1;
        int _tab;

        public Action<int> onUseWeapon;
        public Action<float> onUseHeal;

        public static InventoryView Create(Transform root)
        {
            var panel = GlassUI.Panel(root, "背包", GlassTheme.RadiusPanel);
            GlassUI.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1080f, 660f), Vector2.zero);
            var view = panel.gameObject.AddComponent<InventoryView>();
            view.BuildUi(panel.transform);
            view.RefreshTab();
            return view;
        }

        void BuildUi(Transform panel)
        {
            var column = GlassUI.Col((RectTransform)panel, 14f, 26, TextAnchor.UpperLeft);

            // 标题行
            var header = GlassUI.Node(panel, "Header");
            GlassUI.Fix(header, -1f, 44f);
            var title = GlassUI.Label(header, "背包与装备", 28, TextAnchor.MiddleLeft, GlassTheme.Ink, false, FontStyle.Bold);
            GlassUI.Stretch(title.rectTransform, 0f, 0f, 200f, 0f);
            GlassUI.Halo(title, 1.2f);

            var capacity = GlassUI.Label(header, "负重 24 / 40", 15, TextAnchor.MiddleRight, GlassTheme.InkSoft);
            GlassUI.Stretch(capacity.rectTransform, 0f, 0f, 60f, 0f);

            var close = GlassUI.Pill(header, "关闭", () => onClose?.Invoke(), height: 36f, primary: false, fontSize: 15);
            GlassUI.Place(close.Rt(), new Vector2(1f, 0.5f), new Vector2(88f, 34f), Vector2.zero, new Vector2(1f, 0.5f));

            // 页签
            var tabs = GlassUI.Node(panel, "Tabs");
            GlassUI.Fix(tabs, -1f, 40f);
            var tabRow = GlassUI.Row(tabs, 10f);
            string[] kinds = { "全部", "武器", "消耗品", "材料" };
            for (int i = 0; i < kinds.Length; i++)
            {
                int index = i;
                var pill = GlassUI.Pill(tabRow.transform, kinds[i], () => { _tab = index; RefreshTab(); }, height: 34f, primary: index == 0, fontSize: 15);
                GlassUI.Fix(pill, 104f, 34f);
                _tabPills.Add(pill.targetGraphic as Image);
                _tabLabels.Add(pill.GetComponentInChildren<Text>());
            }

            // 主体：左网格 + 右详情
            var body = GlassUI.Node(panel, "Body");
            GlassUI.Flex(body, -1f, 1f);

            _grid = GlassUI.Node(body, "Grid");
            _grid.anchorMin = new Vector2(0f, 0f);
            _grid.anchorMax = new Vector2(0.62f, 1f);
            _grid.offsetMin = Vector2.zero;
            _grid.offsetMax = new Vector2(-12f, 0f);

            var detail = GlassUI.Panel(body, "Detail", GlassTheme.RadiusCard);
            detail.rectTransform.anchorMin = new Vector2(0.62f, 0f);
            detail.rectTransform.anchorMax = new Vector2(1f, 1f);
            detail.rectTransform.offsetMin = new Vector2(12f, 0f);
            detail.rectTransform.offsetMax = Vector2.zero;

            var detailColumn = GlassUI.Col(detail.rectTransform, 12f, 20, TextAnchor.UpperCenter);

            var iconHolder = GlassUI.Node(detail.transform, "Icon");
            GlassUI.Fix(iconHolder, -1f, 92f);
            _detailIcon = GlassUI.Rect(iconHolder, "Icon BG", GlassTheme.Blue, GlassTheme.RadiusCard);
            GlassUI.Place(_detailIcon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(92f, 92f), Vector2.zero);
            _detailIconText = GlassUI.Label(_detailIcon.transform, "—", 34, TextAnchor.MiddleCenter, GlassTheme.White, false, FontStyle.Bold);
            GlassUI.Stretch(_detailIconText.rectTransform);

            _detailName = GlassUI.Label(detail.transform, "未选择物品", 22, TextAnchor.MiddleCenter, GlassTheme.Ink, false, FontStyle.Bold);
            GlassUI.Fix(_detailName, -1f, 30f);

            _detailKind = GlassUI.Label(detail.transform, "点左边的格子看看", 14, TextAnchor.MiddleCenter, GlassTheme.InkSoft);
            GlassUI.Fix(_detailKind, -1f, 22f);

            _detailBody = GlassUI.Label(detail.transform, "", 15, TextAnchor.UpperLeft, GlassTheme.InkSoft, true);
            GlassUI.Flex(_detailBody, -1f, 1f, minHeight: 90f);

            var actions = GlassUI.Node(detail.transform, "Actions");
            GlassUI.Fix(actions, -1f, 46f);
            var actionRow = GlassUI.Row(actions, 10f, 0, TextAnchor.MiddleCenter);
            _useButton = GlassUI.Pill(actionRow.transform, "使用", () => UseSelected(), height: 44f, primary: true);
            GlassUI.Flex(_useButton, width: 1f);
            _useLabel = _useButton.GetComponentInChildren<Text>();

            var drop = GlassUI.Pill(actionRow.transform, "丢弃", () => DropSelected(), height: 44f, primary: false);
            GlassUI.Flex(drop, width: 1f);

            BuildItems();

            // 40 个格子（6 列）
            for (int i = 0; i < 36; i++)
            {
                var slot = InventorySlot.Create(_grid, i);
                _slots.Add(slot);
            }
            // 6 行 × (72+10) = 482，正好塞进面板内容区，不会顶出下沿
            GlassUI.Grid(_grid, 72f, 10f, 6);
        }

        void BuildItems()
        {
            _all.Clear();
            Add("AR-7 突击步枪", "武器", "史诗", "全自动，30 发弹匣，中距离最稳。", "枪", GlassTheme.Blue, 1, weaponIndex: 0);
            Add("SG-12 霰弹枪", "武器", "稀有", "一次打 8 颗弹丸，近距离伤害爆炸。", "散", GlassTheme.Warn, 1, weaponIndex: 1);
            Add("P-9 手枪", "武器", "精良", "半自动，精度高，换弹快。", "手", GlassTheme.Cyan, 1, weaponIndex: 2);
            Add("医疗包", "消耗品", "精良", "立即恢复 45 点生命。", "医", GlassTheme.Good, 4, heal: 45f);
            Add("能量饮料", "消耗品", "普通", "恢复 20 点生命，冷却 5 秒。", "饮", GlassTheme.Good, 7, heal: 20f);
            Add("穿甲弹", "消耗品", "稀有", "接下来 30 发无视护甲。", "弹", GlassTheme.Warn, 12);
            Add("战术手雷", "消耗品", "精良", "范围伤害，会打出硬直。", "雷", GlassTheme.Bad, 3);
            Add("合金废料", "材料", "普通", "改造武器的基础材料。", "料", GlassTheme.InkSoft, 48);
            Add("电路板", "材料", "精良", "拆解装备获得。", "电", GlassTheme.Cyan, 16);
            Add("稀有结晶", "材料", "史诗", "用于升级到传说品质。", "晶", GlassTheme.Violet, 2);
            Add("护甲片", "材料", "普通", "修补护甲用。", "甲", GlassTheme.Good, 21);
            Add("瞄准镜", "武器", "稀有", "装备后降低后坐。", "镜", GlassTheme.Blue, 1);
        }

        void Add(string name, string kind, string rarity, string desc, string badge, Color color, int count,
            float heal = 0f, int weaponIndex = -1)
        {
            _all.Add(new ItemDef
            {
                name = name, kind = kind, rarity = rarity, description = desc, badge = badge,
                color = color, count = count, heal = heal, weaponIndex = weaponIndex,
            });
        }

        void RefreshTab()
        {
            foreach (var chip in _chips) GlassUI.Kill(chip.gameObject);
            _chips.Clear();

            _visible.Clear();
            string filter = _tab == 0 ? null : _tab == 1 ? "武器" : _tab == 2 ? "消耗品" : "材料";
            foreach (var item in _all)
                if (filter == null || item.kind == filter) _visible.Add(item);

            for (int i = 0; i < _visible.Count && i < _slots.Count; i++)
            {
                var chip = ItemChip.Create(_slots[i].transform, _visible[i], this);
                _chips.Add(chip);
            }

            for (int i = _visible.Count; i < _slots.Count; i++) _slots[i].transform.SetAsLastSibling();

            // 选中态跟着 _tab 走
            for (int i = 0; i < _tabPills.Count; i++)
            {
                if (_tabPills[i] == null) continue;
                _tabPills[i].color = i == _tab ? GlassTheme.Blue : GlassTheme.White;
                if (i < _tabLabels.Count && _tabLabels[i] != null)
                    _tabLabels[i].color = i == _tab ? GlassTheme.White : GlassTheme.Blue;
            }

            if (_visible.Count > 0) Select(0);
        }

        public void Select(int index)
        {
            _selected = Mathf.Clamp(index, -1, _visible.Count - 1);
            if (_selected < 0 || _selected >= _visible.Count)
            {
                _detailName.text = "未选择物品";
                _detailKind.text = "";
                _detailBody.text = "";
                _detailIconText.text = "—";
                _detailIcon.color = GlassTheme.InkFaint;
                _useLabel.text = "使用";
                return;
            }

            var item = _visible[_selected];
            _detailName.text = item.name;
            _detailKind.text = $"{item.rarity} · {item.kind} · 数量 {item.count}";
            _detailBody.text = item.description +
                               "\n\n<color=#7C90AA>稀有度　</color>" + item.rarity +
                               "\n<color=#7C90AA>类型　　</color>" + item.kind +
                               "\n<color=#7C90AA>数量　　</color>" + item.count;
            _detailIcon.color = item.color;
            _detailIconText.text = item.badge;
            _useLabel.text = item.kind == "武器" ? "装备" : item.kind == "消耗品" ? "使用" : "查看";
        }

        void UseSelected()
        {
            if (_selected < 0 || _selected >= _visible.Count) return;
            var item = _visible[_selected];

            if (item.weaponIndex >= 0)
            {
                onUseWeapon?.Invoke(item.weaponIndex);
                return;
            }
            if (item.heal > 0f && item.count > 0)
            {
                onUseHeal?.Invoke(item.heal);
                item.count--;
                if (item.count <= 0) { _all.Remove(item); _visible.Remove(item); }
                RefreshTab();
            }
        }

        void DropSelected()
        {
            if (_selected < 0 || _selected >= _visible.Count) return;
            var item = _visible[_selected];
            if (item.count > 1) item.count--;
            else { _all.Remove(item); _visible.Remove(item); }
            RefreshTab();
        }

        public void Refresh()
        {
            RefreshTab();
        }
    }
    /// <summary>背包格子。</summary>
}
