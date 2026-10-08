using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace GlassUI
{
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
            var panel = GlassUI.Panel(root, "设置", GlassTheme.RadiusPanel);
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
            var tabs = GlassUI.Panel(body, "Tabs", GlassTheme.RadiusCard);
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
            var page = GlassUI.Panel(body, "Page", GlassTheme.RadiusCard);
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
