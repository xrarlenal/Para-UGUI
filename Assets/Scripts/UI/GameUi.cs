using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace GlassUI
{
    /// <summary>
    /// UI 总入口：建画布、装 HUD 与两个面板、处理按键、开面板时进慢动作并解锁鼠标。
    /// 挂在场景里的 Canvas 上。
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Canvas))]
    public class GameUi : MonoBehaviour
    {
        public bool startLocked = true;

        Canvas _canvas;
        CanvasScaler _scaler;
        HudView _hud;
        InventoryView _inventory;
        SettingsView _settings;

        // 这几个必须序列化：GameUi.Setup() 是在编辑器里连续线时调的，
        // 私有字段不序列化的话，场景一重载就全变成 null（界面直接读不到游戏数据）。
        [SerializeField] Game.GameState _state;
        [SerializeField] Game.WeaponSystem _weapons;
        [SerializeField] Game.PlayerController _player;
        [SerializeField] Camera _camera;
        [SerializeField] VolumeProfile _profile;

        CanvasGroup _inventoryGroup;
        CanvasGroup _settingsGroup;
        CanvasGroup _introGroup;
        RectTransform _inventoryRect;
        RectTransform _settingsRect;
        RectTransform _introRect;

        bool _inventoryOpen;
        bool _settingsOpen;
        Coroutine _inventoryAnim;
        Coroutine _settingsAnim;
        Coroutine _introAnim;
        float _defaultTimeScale = 1f;

        public void Setup(Game.GameState state, Game.WeaponSystem weapons, Game.PlayerController player, Camera camera, VolumeProfile profile)
        {
            _state = state;
            _weapons = weapons;
            _player = player;
            _camera = camera;
            _profile = profile;
        }

        /// <summary>缺引用就在场景里找一遍：比依赖编辑器连线稳。</summary>
        void ResolveReferences()
        {
            if (_state == null) _state = Object.FindFirstObjectByType<Game.GameState>();
            if (_weapons == null) _weapons = Object.FindFirstObjectByType<Game.WeaponSystem>();
            if (_player == null) _player = Object.FindFirstObjectByType<Game.PlayerController>();
            if (_camera == null)
            {
                _camera = Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>();
            }
            if (_profile == null)
            {
                var volume = Object.FindFirstObjectByType<Volume>();
                if (volume != null) _profile = volume.sharedProfile;
            }
        }

        void Awake()
        {
            _canvas = GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            _scaler = GetComponent<CanvasScaler>();
            if (_scaler == null) _scaler = gameObject.AddComponent<CanvasScaler>();
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.referenceResolution = new Vector2(1920f, 1080f);
            _scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            _scaler.matchWidthOrHeight = 0.5f;

            if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();
            EnsureEventSystem();
        }

        void Start()
        {
            BuildUi();
            if (Application.isPlaying) SetCursorLocked(startLocked);
        }

        void OnEnable()
        {
            // 编辑器里 OnEnable/Start 不一定按运行时顺序来，这里兜一次：保证场景一开就有界面
            if (!Application.isPlaying) BuildUi();
        }

        static void EnsureEventSystem()
        {
            bool exists = Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(
                FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 0;
            if (!exists)
                new GameObject("EventSystem",
                    typeof(UnityEngine.EventSystems.EventSystem),
                    typeof(UnityEngine.EventSystems.StandaloneInputModule));
        }

        public void BuildUi()
        {
            ResolveReferences();
            GlassUI.Clear(transform);
            var root = (RectTransform)transform;

            // HUD（不含面板）
            var hudHolder = GlassUI.Node(root, "HUD");
            GlassUI.Stretch(hudHolder);
            _hud = hudHolder.gameObject.AddComponent<HudView>();
            _hud.Build(hudHolder, _state, _weapons);
            _hud.onWeaponChipClicked = index => _weapons?.Equip(index);

            // 背包
            _inventory = InventoryView.Create(root);
            _inventoryRect = (RectTransform)_inventory.transform;
            _inventoryGroup = GlassUI.Group(_inventoryRect, 0f, true, false);
            _inventory.onClose = () => ToggleInventory(false);
            _inventory.onUseWeapon = index => { _weapons?.Equip(index); _hud.Toast($"已装备 {_weapons?.Current?.name}"); };
            _inventory.onUseHeal = amount =>
            {
                _state?.Heal(amount);
                _hud.Toast($"使用医疗包，恢复 {amount:0} 点生命");
            };
            _inventoryRect.gameObject.SetActive(false);

            // 设置
            _settings = SettingsView.Create(root, _camera, _player, _profile);
            _settingsRect = (RectTransform)_settings.transform;
            _settingsGroup = GlassUI.Group(_settingsRect, 0f, true, false);
            _settings.onClose = () => ToggleSettings(false);
            _settingsRect.gameObject.SetActive(false);

            // 开场简报（给作品集一个入场的节奏）
            BuildIntro(root);

            GlassUI.Refresh();
        }

        void BuildIntro(RectTransform root)
        {
            var card = GlassUI.Panel(root, "Intro", GlassTheme.RadiusPanel, 0.10f);
            _introRect = card.rectTransform;
            GlassUI.Place(_introRect, new Vector2(0.5f, 0.5f), new Vector2(760f, 320f), Vector2.zero);
            _introGroup = GlassUI.Group(_introRect, 0f, true, true);
            var column = GlassUI.Col(_introRect, 12f, 36, TextAnchor.UpperLeft);

            var title = GlassUI.Label(card.transform, "训练场", 40, TextAnchor.MiddleLeft, GlassTheme.Ink, false, FontStyle.Bold);
            GlassUI.Fix(title, -1f, 52f);
            GlassUI.Halo(title, 1.6f);

            var accent = GlassUI.Rect(card.transform, "Accent", GlassTheme.Blue, 3f);
            GlassUI.Fix(accent, -1f, 6f);

            var body = GlassUI.Label(card.transform,
                "左侧掩体后可以躲哨戒炮的射线，打中靶子加分。\n" +
                "Tab 打开背包（里面有医疗包），Esc 打开设置，1/2/3 或滚轮换枪。",
                17, TextAnchor.UpperLeft, GlassTheme.InkSoft, true);
            GlassUI.Fix(body, -1f, 76f);

            var row = GlassUI.Node(card.transform, "Actions");
            GlassUI.Fix(row, -1f, 48f);
            var buttons = GlassUI.Row(row, 10f, 0, TextAnchor.MiddleLeft);
            var start = GlassUI.Pill(buttons.transform, "开始训练", () => CloseIntro(), height: 46f, primary: true);
            GlassUI.Fix(start, 150f, 46f);
            var introHint = GlassUI.Label(buttons.transform, "进游戏后鼠标会被锁定，按 Esc 可以解开", 14, TextAnchor.MiddleLeft, GlassTheme.InkFaint);
            GlassUI.Fix(introHint, 380f, 24f);
        }

        void CloseIntro()
        {
            if (GlassAnim.Playing) GlassAnim.Start(this, ref _introAnim, FadeGroup(_introGroup, _introRect, false, 0.28f, 40f));
            else
            {
                ApplyFinal(_introGroup, _introRect, false, 40f);
                _introRect.gameObject.SetActive(false);
            }
            ApplyPanelState();
        }

        // ---------------- 面板开关 ----------------

        void Update()
        {
            if (!Application.isPlaying) return;
            if (Input.GetKeyDown(KeyCode.Tab)) ToggleInventory(!_inventoryOpen);
            if (Input.GetKeyDown(KeyCode.Escape)) ToggleSettings(!_settingsOpen);

            if (!AnyPanelOpen())
            {
                if (_weapons != null)
                {
                    if (Input.GetKeyDown(KeyCode.Alpha1)) _weapons.Equip(0);
                    if (Input.GetKeyDown(KeyCode.Alpha2)) _weapons.Equip(1);
                    if (Input.GetKeyDown(KeyCode.Alpha3)) _weapons.Equip(2);
                    if (Input.GetKeyDown(KeyCode.R)) _weapons.Reload();

                    float scroll = Input.GetAxisRaw("Mouse ScrollWheel");
                    if (Mathf.Abs(scroll) > 0.01f) _weapons.SwitchBy(scroll > 0f ? 1 : -1);

                    if (Input.GetMouseButtonDown(0)) _weapons.Trigger(true);
                    if (Input.GetMouseButton(0)) _weapons.HoldFire();
                }
            }
        }

        public void OnFiredFeedback()
        {
            _hud?.OnFired(0.7f);
        }

        bool AnyPanelOpen() => _inventoryOpen || _settingsOpen || (_introGroup != null && _introGroup.alpha > 0.9f && _introRect.gameObject.activeSelf);

        void ToggleInventory(bool open)
        {
            _inventoryOpen = open;
            if (open && _settingsOpen) ToggleSettings(false);

            _inventoryRect.gameObject.SetActive(true);
            if (GlassAnim.Playing)
            {
                GlassAnim.Start(this, ref _inventoryAnim, FadeGroup(_inventoryGroup, _inventoryRect, open, 0.26f, 26f));
                if (!open) Invoke(nameof(HideInventoryLater), 0.3f);
            }
            else
            {
                // 编辑器里（离屏截图）直接落终态，免得协程不跑导致面板透明
                ApplyFinal(_inventoryGroup, _inventoryRect, open, 26f);
                if (!open) _inventoryRect.gameObject.SetActive(false);
            }
            ApplyPanelState();
        }

        void HideInventoryLater()
        {
            if (!_inventoryOpen) _inventoryRect.gameObject.SetActive(false);
        }

        void ToggleSettings(bool open)
        {
            _settingsOpen = open;
            if (open && _inventoryOpen) ToggleInventory(false);

            _settingsRect.gameObject.SetActive(true);
            if (GlassAnim.Playing)
            {
                GlassAnim.Start(this, ref _settingsAnim, FadeGroup(_settingsGroup, _settingsRect, open, 0.26f, 26f));
                if (!open) Invoke(nameof(HideSettingsLater), 0.3f);
            }
            else
            {
                ApplyFinal(_settingsGroup, _settingsRect, open, 26f);
                if (!open) _settingsRect.gameObject.SetActive(false);
            }
            ApplyPanelState();
        }

        static void ApplyFinal(CanvasGroup group, RectTransform rect, bool open, float offset)
        {
            if (group != null)
            {
                group.alpha = open ? 1f : 0f;
                group.interactable = open;
                group.blocksRaycasts = open;
            }
            if (rect != null)
            {
                rect.localScale = Vector3.one;
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, open ? 0f : offset);
            }
        }

        // ---- 给离屏截图用的公开入口 ----
        public void PrepareForCapture()
        {
            if (_introGroup != null)
            {
                _introGroup.alpha = 0f;
                _introRect.gameObject.SetActive(false);
            }
        }

        public void OpenInventoryForCapture()
        {
            _inventoryOpen = false;
            ToggleInventory(true);
        }

        public void OpenSettingsForCapture()
        {
            _settingsOpen = false;
            ToggleSettings(true);
        }

        public void ClosePanelsForCapture()
        {
            ToggleInventory(false);
            ToggleSettings(false);
        }

        void HideSettingsLater()
        {
            if (!_settingsOpen) _settingsRect.gameObject.SetActive(false);
        }

        /// <summary>开面板 = 慢动作 + 解锁鼠标 + 停止视角/移动。</summary>
        void ApplyPanelState()
        {
            bool open = AnyPanelOpen();
            Time.timeScale = open ? 0.25f : _defaultTimeScale;
            SetCursorLocked(!open);

            if (_player != null)
            {
                _player.lookEnabled = !open;
                _player.moveEnabled = !open;
            }
        }

        void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        static System.Collections.IEnumerator FadeGroup(CanvasGroup group, RectTransform rect, bool show, float duration, float offset)
        {
            if (group == null) yield break;

            group.interactable = show;
            group.blocksRaycasts = show;
            float from = group.alpha;
            float to = show ? 1f : 0f;
            var baseScale = show ? new Vector3(0.94f, 0.94f, 1f) : Vector3.one;
            var endScale = show ? Vector3.one : new Vector3(1.03f, 1.03f, 1f);
            float fromOffset = rect.anchoredPosition.y;
            float toOffset = show ? 0f : offset;

            yield return GlassAnim.Curve(duration, t =>
            {
                if (group == null) return;
                group.alpha = Mathf.Lerp(from, to, t);
                if (rect != null)
                {
                    rect.localScale = Vector3.LerpUnclamped(baseScale, endScale, t);
                    rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, Mathf.Lerp(fromOffset, toOffset, t));
                }
            }, show ? Ease.OutBack : Ease.OutCubic);

            if (rect != null) rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, toOffset);
        }
    }
}
