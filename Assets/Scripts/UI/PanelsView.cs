using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace GlassUI
{
    // =====================================================================================
    //  通用控件：滑条 / 开关 / 分段选择。都长在毛玻璃上，都带非线性反馈。
    // =====================================================================================

    /// <summary>毛玻璃滑条：底槽 + 蓝色填充 + 圆钮，拖动改值。</summary>
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

            var bg = GlassUI.Rect(row, "BG", new Color(1f, 1f, 1f, 0.40f), 99f);
            GlassUI.Place(bg.rectTransform, new Vector2(1f, 0.5f), new Vector2(52f, 28f), Vector2.zero, new Vector2(1f, 0.5f));
            bg.raycastTarget = true;

            var knob = GlassUI.Rect(bg.transform, "Knob", Color.white, 99f);
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

            var holder = GlassUI.Rect(row, "Holder", new Color(1f, 1f, 1f, 0.32f), 99f);
            GlassUI.Place(holder.rectTransform, new Vector2(1f, 0.5f), new Vector2(260f, 30f), Vector2.zero, new Vector2(1f, 0.5f));

            var highlight = GlassUI.Rect(holder.transform, "Highlight", GlassTheme.Blue, 99f);
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

                var hit = GlassUI.Rect(item, "Hit", new Color(1f, 1f, 1f, 0f), 99f);
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
            var panel = GlassUI.Panel(root, "背包", GlassTheme.RadiusPanel, 0.09f);
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

            var detail = GlassUI.Panel(body, "Detail", GlassTheme.RadiusCard, 0.10f);
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
    public class InventorySlot : MonoBehaviour
    {
        public Image frame;
        public ItemChip occupant;

        public static InventorySlot Create(Transform parent, int index)
        {
            var frame = GlassUI.Rect(parent, $"Slot {index:00}", new Color(1f, 1f, 1f, 0.28f), GlassTheme.RadiusItem);
            GlassUI.Fix(frame, 72f, 72f);
            frame.raycastTarget = true;

            var slot = frame.gameObject.AddComponent<InventorySlot>();
            slot.frame = frame;
            return slot;
        }
    }

    /// <summary>物品图标 + 数量，可拖到别的格子。</summary>
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
            GlassUI.Stretch(holder, 5f, 5f, 5f, 5f);

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

    public class SettingsView : MonoBehaviour
    {
        public Action onClose;

        Camera _camera;
        Game.PlayerController _player;
        VolumeProfile _profile;
        Bloom _bloom;
        ColorAdjustments _color;
        Tonemapping _tone;
        Vignette _vignette;

        RectTransform _tabHolder;
        RectTransform _pageHolder;
        Image _tabHighlight;
        readonly List<RectTransform> _tabItems = new List<RectTransform>();
        readonly List<Text> _tabLabels = new List<Text>();
        Coroutine _tabAnim;
        Coroutine _tempAnim;
        int _tab;

        readonly Dictionary<string, float> _defaults = new Dictionary<string, float>();

        public static SettingsView Create(Transform root, Camera camera, Game.PlayerController player, VolumeProfile profile)
        {
            var panel = GlassUI.Panel(root, "设置", GlassTheme.RadiusPanel, 0.09f);
            GlassUI.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(980f, 620f), Vector2.zero);
            var view = panel.gameObject.AddComponent<SettingsView>();
            view._camera = camera;
            view._player = player;
            view._profile = profile;
            view.BuildUi(panel.transform);
            return view;
        }

        void BuildUi(Transform panel)
        {
            var column = GlassUI.Col((RectTransform)panel, 14f, 26, TextAnchor.UpperLeft);

            var header = GlassUI.Node(panel, "Header");
            GlassUI.Fix(header, -1f, 44f);
            var title = GlassUI.Label(header, "设置", 28, TextAnchor.MiddleLeft, GlassTheme.Ink, false, FontStyle.Bold);
            GlassUI.Stretch(title.rectTransform, 0f, 0f, 200f, 0f);
            GlassUI.Halo(title, 1.2f);

            var close = GlassUI.Pill(header, "返回", () => onClose?.Invoke(), height: 36f, primary: false, fontSize: 15);
            GlassUI.Place(close.Rt(), new Vector2(1f, 0.5f), new Vector2(88f, 34f), Vector2.zero, new Vector2(1f, 0.5f));

            var body = GlassUI.Node(panel, "Body");
            GlassUI.Flex(body, -1f, 1f);

            // 左侧页签
            var tabs = GlassUI.Panel(body, "Tabs", GlassTheme.RadiusCard, 0.10f);
            tabs.rectTransform.anchorMin = new Vector2(0f, 0f);
            tabs.rectTransform.anchorMax = new Vector2(0.28f, 1f);
            tabs.rectTransform.offsetMin = Vector2.zero;
            tabs.rectTransform.offsetMax = new Vector2(-12f, 0f);

            _tabHolder = GlassUI.Node(tabs.transform, "Tab Holder");
            GlassUI.Stretch(_tabHolder, 10f, 10f, 10f, 10f);
            // 注意：这里不能再挂布局组——页签是绝对定位的，布局组会把它们全压到顶部堆在一起

            _tabHighlight = GlassUI.Rect(_tabHolder, "Highlight", GlassTheme.Blue, GlassTheme.RadiusItem);
            GlassUI.Place(_tabHighlight.rectTransform, new Vector2(0f, 1f), new Vector2(200f, 46f), Vector2.zero, new Vector2(0f, 1f));

            string[] tabs2 = { "画面", "音频", "操作", "关于" };
            for (int i = 0; i < tabs2.Length; i++)
            {
                int index = i;
                var item = GlassUI.Node(_tabHolder, "Tab " + i);
                GlassUI.Place(item, new Vector2(0f, 1f), new Vector2(200f, 46f), new Vector2(0f, -i * 52f), new Vector2(0f, 1f));

                var hit = GlassUI.Rect(item, "Hit", new Color(1f, 1f, 1f, 0f), GlassTheme.RadiusItem);
                GlassUI.Stretch(hit.rectTransform);
                hit.raycastTarget = true;
                hit.gameObject.AddComponent<Button>().onClick.AddListener(() => SelectTab(index));

                var label = GlassUI.Label(item, tabs2[i], 16, TextAnchor.MiddleLeft, GlassTheme.InkSoft, false, FontStyle.Bold);
                GlassUI.Stretch(label.rectTransform, 18f, 0f, 8f, 0f);

                _tabItems.Add(item);
                _tabLabels.Add(label);
            }

            // 右侧页面
            var page = GlassUI.Panel(body, "Page", GlassTheme.RadiusCard, 0.10f);
            page.rectTransform.anchorMin = new Vector2(0.28f, 0f);
            page.rectTransform.anchorMax = new Vector2(1f, 1f);
            page.rectTransform.offsetMin = new Vector2(12f, 0f);
            page.rectTransform.offsetMax = Vector2.zero;

            _pageHolder = GlassUI.Node(page.transform, "Page Holder");
            GlassUI.Stretch(_pageHolder, 24f, 20f, 24f, 20f);

            SelectTab(0);
        }

        void SelectTab(int index)
        {
            _tab = index;

            for (int i = 0; i < _tabLabels.Count; i++)
                _tabLabels[i].color = i == _tab ? GlassTheme.White : GlassTheme.InkSoft;

            var target = _tabItems[_tab].anchoredPosition.y;
            if (!GlassAnim.Playing) _tabHighlight.rectTransform.anchoredPosition = new Vector2(0f, target);
            else
            {
                float from = _tabHighlight.rectTransform.anchoredPosition.y;
                GlassAnim.Start(this, ref _tabAnim, GlassAnim.Float(from, target, 0.24f,
                    v => _tabHighlight.rectTransform.anchoredPosition = new Vector2(0f, v), Ease.OutBack));
            }

            BuildPage();
        }

        void BuildPage()
        {
            GlassUI.Clear(_pageHolder);

            switch (_tab)
            {
                case 0: BuildVideoPage(_pageHolder); break;
                case 1: BuildAudioPage(_pageHolder); break;
                case 2: BuildControlPage(_pageHolder); break;
                default: BuildAboutPage(_pageHolder); break;
            }

            GlassUI.Refresh();
        }

        void BuildVideoPage(Transform parent)
        {
            var column = GlassUI.Col((RectTransform)parent, 12f, 0, TextAnchor.UpperLeft);

            var heading = GlassUI.Label(parent, "画面", 20, TextAnchor.MiddleLeft, GlassTheme.Ink, false, FontStyle.Bold);
            GlassUI.Fix(heading, -1f, 30f);

            GlassSlider.Create(parent, "视野 FOV", 60f, 110f, _camera != null ? _camera.fieldOfView : 75f, v =>
            {
                if (_camera != null) _camera.fieldOfView = v;
            }, "°");

            // 泛光强度：真的作用在 URP 的 Bloom 上
            Bloom bloom = null;
            if (_profile != null) _profile.TryGet(out bloom);
            _bloom = bloom;
            float bloomValue = bloom != null ? bloom.intensity.value : 0.85f;
            GlassSlider.Create(parent, "泛光强度", 0f, 2f, bloomValue, v =>
            {
                if (_bloom != null) _bloom.intensity.value = v;
            });

            ColorAdjustments color = null;
            if (_profile != null) _profile.TryGet(out color);
            _color = color;
            float exposure = color != null ? color.postExposure.value : 0.15f;
            GlassSlider.Create(parent, "亮度", -1f, 1f, exposure, v =>
            {
                if (_color != null) _color.postExposure.value = v;
            });

            // 画质：真的切 QualitySettings
            // 用固定短标签：QualitySettings.names 里像 "High Fidelity" 这种会在分段里撑爆
            GlassSegmented.Create(parent, "画质等级", new[] { "性能", "均衡", "高画质" },
                Mathf.Clamp(QualitySettings.GetQualityLevel(), 0, 2), v => QualitySettings.SetQualityLevel(v, true));

            GlassToggle.Create(parent, "垂直同步", QualitySettings.vSyncCount > 0, v => QualitySettings.vSyncCount = v ? 1 : 0);
        }

        void BuildAudioPage(Transform parent)
        {
            var heading = GlassUI.Label(parent, "音频", 20, TextAnchor.MiddleLeft, GlassTheme.Ink, false, FontStyle.Bold);
            GlassUI.Fix(heading, -1f, 30f);

            GlassSlider.Create(parent, "主音量", 0f, 1f, AudioListener.volume, v => AudioListener.volume = v);
            GlassSlider.Create(parent, "音效", 0f, 1f, 0.9f, v => { });
            GlassSlider.Create(parent, "音乐", 0f, 1f, 0.55f, v => { });
            GlassToggle.Create(parent, "静音", false, v => AudioListener.volume = v ? 0f : 1f);

            var note = GlassUI.Label(parent, "本 Demo 没有音频资源，这里只演示参数作用。", 14, TextAnchor.UpperLeft, GlassTheme.InkSoft, true);
            GlassUI.Fix(note, -1f, 40f);
        }

        void BuildControlPage(Transform parent)
        {
            var heading = GlassUI.Label(parent, "操作", 20, TextAnchor.MiddleLeft, GlassTheme.Ink, false, FontStyle.Bold);
            GlassUI.Fix(heading, -1f, 30f);

            GlassSlider.Create(parent, "鼠标灵敏度", 0.4f, 6f, _player != null ? _player.mouseSensitivity : 2.4f, v =>
            {
                if (_player != null) _player.mouseSensitivity = v;
            });

            GlassToggle.Create(parent, "锁定鼠标（推荐）", true, v => { });
            GlassToggle.Create(parent, "冲刺用 Shift", true, v => { });

            var keys = GlassUI.Label(parent,
                "移动 WASD　跳跃 空格　射击 鼠标左键　换弹 R\n切换武器 1 / 2 / 3 或滚轮　背包 Tab　设置 Esc",
                15, TextAnchor.UpperLeft, GlassTheme.InkSoft, true);
            GlassUI.Fix(keys, -1f, 60f);
        }

        void BuildAboutPage(Transform parent)
        {
            var heading = GlassUI.Label(parent, "关于", 20, TextAnchor.MiddleLeft, GlassTheme.Ink, false, FontStyle.Bold);
            GlassUI.Fix(heading, -1f, 30f);

            var body = GlassUI.Label(parent,
                "毛玻璃 UI 演示 · Unity 2022.3 + URP 14\n\n" +
                "背景不是贴图，是每帧把相机画面拷出来降采样再做分离高斯模糊，" +
                "UI 按屏幕 UV 采样那张模糊纹理——所以每块面板显示的都是它身后那块被糊掉的画面，" +
                "1:1 精确裁切，不平铺不拉伸。\n\n" +
                "圆角、描边、边缘高光来自程序化生成的九宫格遮罩，控件直接用 Image。",
                15, TextAnchor.UpperLeft, GlassTheme.InkSoft, true);
            GlassUI.Flex(body, -1f, 1f, minHeight: 200f);

            var credits = GlassUI.Label(parent, "配乐/模型：无（全部程序化生成，授权干净）", 13, TextAnchor.LowerLeft, GlassTheme.InkFaint);
            GlassUI.Fix(credits, -1f, 24f);
        }

        public void Open()
        {
            SelectTab(0);
        }
    }
}
