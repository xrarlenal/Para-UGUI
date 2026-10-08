using UnityEngine;
using UnityEngine.UI;

namespace GlassUI
{
    /// <summary>
    /// 占位演示界面：用来确认「毛玻璃 + 蓝白 + 圆角」这一套视觉方向，
    /// 同时把背包 / 设置 / 战斗 HUD / 切换武器四块 UI 的观感先摆出来。
    ///
    /// 界面在运行时（或编辑器 execute-always）搭建：毛玻璃材质是运行时创建的，
    /// 存进场景没有意义，所以不烘焙层级。
    /// </summary>
    [ExecuteAlways]
    public class GlassDemoUi : MonoBehaviour
    {
        [Header("玻璃参数")]
        [Range(0f, 1f)] public float milkiness = 0.07f;
        [Range(0f, 3f)] public float rimBoost = 1f;
        [Range(0f, 1f)] public float blurStrength = 1f;

        void OnEnable()
        {
            Build();
        }

        void OnValidate()
        {
            if (!isActiveAndEnabled) return;
            Build();
        }

        public void Build()
        {
            GlassUI.Clear(transform);

            var root = (RectTransform)transform;

            // ---------- 左侧主面板（设置 / 背包的观感）----------
            var panel = GlassUI.Panel(root, "Main Panel", GlassTheme.RadiusPanel, milkiness, rimBoost, blurStrength);
            GlassUI.Place(panel.rectTransform, new Vector2(0f, 0.5f), new Vector2(470f, 566f), new Vector2(56f, 0f), new Vector2(0f, 0.5f));
            GlassUI.Col(panel.rectTransform, 14f, 28, TextAnchor.UpperLeft);

            var title = GlassUI.Label(panel.transform, "毛玻璃 UI", 40, TextAnchor.MiddleLeft, GlassTheme.Ink, false, FontStyle.Bold);
            GlassUI.Fix(title, -1f, 50f);
            GlassUI.Halo(title, 1.6f);

            var subtitle = GlassUI.Label(panel.transform,
                "背景是 URP 拷贝出来再模糊的那张全屏纹理，按屏幕 UV 精确裁切", 16, TextAnchor.UpperLeft, GlassTheme.InkSoft, true);
            GlassUI.Fix(subtitle, -1f, 46f);

            var accent = GlassUI.Rect(panel.transform, "Accent", GlassTheme.Blue, 3f);
            GlassUI.Fix(accent, -1f, 6f);

            AddInfoRow(panel.transform, "分辨率", "1920 × 1080");
            AddInfoRow(panel.transform, "模糊", "降采样 1/8 · 分离高斯 ×2");
            AddInfoRow(panel.transform, "圆角", "九宫格贴图，任意尺寸");

            var buttons = GlassUI.Node(panel.transform, "Buttons");
            GlassUI.Fix(buttons, -1f, 52f);
            GlassUI.Row(buttons, 12f);
            var primary = GlassUI.Pill(buttons, "进入战斗", () => Debug.Log("[GlassUI] 进入战斗"));
            GlassUI.Flex(primary, width: 1f);
            var secondary = GlassUI.Pill(buttons, "设置", () => Debug.Log("[GlassUI] 设置"), primary: false);
            GlassUI.Flex(secondary, width: 1f);

            // ---------- 右上：切换武器 ----------
            var weaponBar = GlassUI.Panel(root, "Weapon Bar", GlassTheme.RadiusCard, milkiness, rimBoost, blurStrength);
            GlassUI.Place(weaponBar.rectTransform, new Vector2(1f, 1f), new Vector2(430f, 78f), new Vector2(-56f, -48f), new Vector2(1f, 1f));
            var weaponRow = GlassUI.Row(weaponBar.rectTransform, 10f, 12, TextAnchor.MiddleCenter);
            weaponRow.childForceExpandWidth = true;

            AddWeaponChip(weaponBar.transform, "突击步枪", true);
            AddWeaponChip(weaponBar.transform, "霰弹枪", false);
            AddWeaponChip(weaponBar.transform, "手枪", false);

            // ---------- 底部：战斗 HUD ----------
            var hud = GlassUI.Panel(root, "Combat HUD", GlassTheme.RadiusCard, milkiness, rimBoost, blurStrength);
            GlassUI.Place(hud.rectTransform, new Vector2(0.5f, 0f), new Vector2(880f, 104f), new Vector2(0f, 48f), new Vector2(0.5f, 0f));
            GlassUI.Row(hud.rectTransform, 22f, 20, TextAnchor.MiddleCenter);

            var healthLabel = GlassUI.Label(hud.transform, "生命 82", 18, TextAnchor.MiddleLeft, GlassTheme.Ink, false, FontStyle.Bold);
            GlassUI.Fix(healthLabel, 84f, 30f);

            var barBg = GlassUI.Rect(hud.transform, "Health Bar BG", new Color(1f, 1f, 1f, 0.34f), GlassTheme.RadiusPill);
            GlassUI.Fix(barBg, 420f, 16f);

            var barFill = GlassUI.Rect(barBg.transform, "Health Bar Fill", GlassTheme.Blue, GlassTheme.RadiusPill);
            GlassUI.Place(barFill.rectTransform, new Vector2(0f, 0.5f), new Vector2(337f, 16f), new Vector2(0f, 0f), new Vector2(0f, 0.5f));

            var ammo = GlassUI.Label(hud.transform, "24 / 96", 26, TextAnchor.MiddleRight, GlassTheme.Ink, false, FontStyle.Bold);
            GlassUI.Fix(ammo, 150f, 30f);

            // ---------- 右下：提示卡 ----------
            var tip = GlassUI.Panel(root, "Tip Card", GlassTheme.RadiusCard, milkiness, rimBoost, blurStrength);
            GlassUI.Place(tip.rectTransform, new Vector2(1f, 0f), new Vector2(360f, 92f), new Vector2(-56f, 48f), new Vector2(1f, 0f));
            var tipLabel = GlassUI.Label(tip.transform, "鼠标移到按钮上试试非线性反馈", 15, TextAnchor.MiddleCenter,
                GlassTheme.InkSoft, true);
            GlassUI.Stretch(tipLabel.rectTransform, 18f, 12f, 18f, 12f);

            GlassUI.Refresh();
        }

        void AddInfoRow(Transform parent, string key, string value)
        {
            var row = GlassUI.Node(parent, "Row: " + key);
            GlassUI.Fix(row, -1f, 34f);

            var keyLabel = GlassUI.Label(row, key, 17, TextAnchor.MiddleLeft, GlassTheme.InkSoft);
            GlassUI.Place(keyLabel.rectTransform, new Vector2(0f, 0.5f), new Vector2(120f, 30f), Vector2.zero, new Vector2(0f, 0.5f));

            var valueLabel = GlassUI.Label(row, value, 17, TextAnchor.MiddleRight, GlassTheme.Blue, false, FontStyle.Bold);
            GlassUI.Stretch(valueLabel.rectTransform, 120f, 0f, 0f, 0f);
        }

        void AddWeaponChip(Transform parent, string name, bool active)
        {
            var chip = GlassUI.Rect(parent, "Chip: " + name, active ? GlassTheme.Blue : new Color(1f, 1f, 1f, 0.52f),
                GlassTheme.RadiusItem);
            GlassUI.Flex(chip, width: 1f);
            GlassUI.Fix(chip, -1f, 48f);
            chip.raycastTarget = true;
            chip.gameObject.AddComponent<HoverFx>();

            var label = GlassUI.Label(chip.transform, name, 16, TextAnchor.MiddleCenter,
                active ? GlassTheme.White : GlassTheme.Ink, false, FontStyle.Bold);
            GlassUI.Stretch(label.rectTransform, 8f, 0f, 8f, 0f);
        }
    }
}
