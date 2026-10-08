using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GlassUI.Game;

namespace GlassUI
{
    public class HudView : MonoBehaviour
    {
        GameState _state;
        Game.WeaponSystem _weapons;

        RectTransform _crosshair;
        readonly List<RectTransform> _ticks = new List<RectTransform>();
        float _spread;          // 0 = 收拢，1 = 张开
        float _spreadVel;

        GlassBar _health;
        Text _healthText;
        Text _ammoText;
        Text _reserveText;
        Text _weaponName;
        Text _scoreText;
        Image _damageFlash;
        RectTransform _hitMarker;
        float _hitTimer;

        Coroutine _tempAnim;
        readonly List<GameObject> _toasts = new List<GameObject>();
        readonly List<RectTransform> _weaponChips = new List<RectTransform>();
        readonly List<Image> _weaponChipImages = new List<Image>();
        readonly List<Text> _weaponChipLabels = new List<Text>();

        public System.Action<int> onWeaponChipClicked;

        public void Build(Transform root, GameState state, Game.WeaponSystem weapons)
        {
            _state = state;
            _weapons = weapons;

            BuildDamageFlash(root);
            BuildCrosshair(root);
            BuildHealth(root);
            BuildAmmo(root);
            BuildWeaponBar(root);
            BuildScore(root);

            if (_state != null)
            {
                _state.onChanged += Refresh;
                _state.onDamaged += OnDamaged;
                _state.onToast += Toast;
            }
            if (_weapons != null) _weapons.onChanged += Refresh;

            Refresh();
            _hitMarker.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (_state != null)
            {
                _state.onChanged -= Refresh;
                _state.onDamaged -= OnDamaged;
                _state.onToast -= Toast;
            }
            if (_weapons != null) _weapons.onChanged -= Refresh;
        }

        // ---------------- 受击红闪 ----------------

        void BuildDamageFlash(Transform root)
        {
            _damageFlash = GlassUI.Node(root, "Damage Flash").gameObject.AddComponent<Image>();
            _damageFlash.sprite = GlassSprites.Get(GlassSprites.Round08);
            _damageFlash.type = Image.Type.Sliced;
            _damageFlash.color = new Color(0.95f, 0.22f, 0.28f, 0f);
            _damageFlash.raycastTarget = false;
            GlassUI.Stretch(_damageFlash.rectTransform, -40f, -40f, -40f, -40f);
        }

        void OnDamaged(float amount)
        {
            _damageFlash.color = new Color(0.95f, 0.22f, 0.28f, 0.34f);
            ShowHitMarker(new Color(1f, 0.45f, 0.45f));
        }

        // ---------------- 准星 ----------------

        void BuildCrosshair(Transform root)
        {
            _crosshair = GlassUI.Node(root, "Crosshair");
            GlassUI.Place(_crosshair, new Vector2(0.5f, 0.5f), new Vector2(64f, 64f), Vector2.zero);

            for (int i = 0; i < 4; i++)
            {
                var tick = GlassUI.Rect(_crosshair, $"Tick {i}", new Color(1f, 1f, 1f, 0.92f), 6f);
                GlassUI.Place(tick.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(2.6f, 11f), Vector2.zero);
                _ticks.Add(tick.rectTransform);
            }

            _hitMarker = GlassUI.Node(root, "Hit Marker");
            GlassUI.Place(_hitMarker, new Vector2(0.5f, 0.5f), new Vector2(42f, 42f), Vector2.zero);
            for (int i = 0; i < 4; i++)
            {
                var mark = GlassUI.Rect(_hitMarker, $"Mark {i}", Color.white, 6f);
                GlassUI.Place(mark.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(2.4f, 9f), Vector2.zero);
                var rt = mark.rectTransform;
                rt.localRotation = Quaternion.Euler(0f, 0f, 45f + i * 90f);
                rt.anchoredPosition = Quaternion.Euler(0f, 0f, 45f + i * 90f) * new Vector2(0f, 11f);
            }
        }

        void LayoutCrosshair()
        {
            float gap = Mathf.Lerp(4f, 15f, _spread);
            for (int i = 0; i < _ticks.Count; i++)
            {
                float angle = i * 90f;
                var dir = Quaternion.Euler(0f, 0f, angle) * Vector2.up;
                _ticks[i].anchoredPosition = dir * gap;
                _ticks[i].localRotation = Quaternion.Euler(0f, 0f, angle);
            }
        }

        public void OnFired(float recoil)
        {
            _spread = Mathf.Clamp01(_spread + recoil);
        }

        void ShowHitMarker(Color color)
        {
            foreach (var mark in _hitMarker.GetComponentsInChildren<Image>()) mark.color = color;
            _hitMarker.gameObject.SetActive(true);
            _hitTimer = 0.16f;
        }

        public void PunchHud()
        {
        }

        // ---------------- 血量 / 弹药 / 分数 ----------------

        void BuildHealth(Transform root)
        {
            var panel = GlassUI.Panel(root, "Health Panel", GlassTheme.RadiusCard);
            GlassUI.Place(panel.rectTransform, new Vector2(0f, 0f), new Vector2(340f, 96f), new Vector2(48f, 48f), new Vector2(0f, 0f));

            var label = GlassUI.Label(panel.transform, "生命", 14, TextAnchor.UpperLeft, GlassTheme.InkSoft);
            GlassUI.Place(label.rectTransform, new Vector2(0f, 1f), new Vector2(80f, 20f), new Vector2(22f, -16f), new Vector2(0f, 1f));

            _healthText = GlassUI.Label(panel.transform, "100", 30, TextAnchor.UpperRight, GlassTheme.Ink, false, FontStyle.Bold);
            GlassUI.Place(_healthText.rectTransform, new Vector2(1f, 1f), new Vector2(90f, 34f), new Vector2(-22f, -12f), new Vector2(1f, 1f));
            GlassUI.Halo(_healthText, 1.2f);

            _health = GlassBar.Create(panel.transform, new Vector2(296f, 14f), new Vector2(0f, 0f), new Vector2(22f, 20f));
        }

        void BuildAmmo(Transform root)
        {
            var panel = GlassUI.Panel(root, "Ammo Panel", GlassTheme.RadiusCard);
            GlassUI.Place(panel.rectTransform, new Vector2(1f, 0f), new Vector2(316f, 96f), new Vector2(-48f, 48f), new Vector2(1f, 0f));

            _weaponName = GlassUI.Label(panel.transform, "—", 15, TextAnchor.UpperRight, GlassTheme.InkSoft);
            GlassUI.Place(_weaponName.rectTransform, new Vector2(1f, 1f), new Vector2(260f, 20f), new Vector2(-22f, -16f), new Vector2(1f, 1f));

            _ammoText = GlassUI.Label(panel.transform, "30", 36, TextAnchor.LowerRight, GlassTheme.Ink, false, FontStyle.Bold);
            GlassUI.Place(_ammoText.rectTransform, new Vector2(1f, 0f), new Vector2(110f, 44f), new Vector2(-22f, 14f), new Vector2(1f, 0f));
            GlassUI.Halo(_ammoText, 1.2f);

            _reserveText = GlassUI.Label(panel.transform, "/ ∞", 18, TextAnchor.LowerRight, GlassTheme.Blue);
            GlassUI.Place(_reserveText.rectTransform, new Vector2(1f, 0f), new Vector2(80f, 28f), new Vector2(-140f, 22f), new Vector2(1f, 0f));
        }

        void BuildScore(Transform root)
        {
            var panel = GlassUI.Panel(root, "Score Panel", GlassTheme.RadiusCard);
            GlassUI.Place(panel.rectTransform, new Vector2(0f, 1f), new Vector2(260f, 56f), new Vector2(48f, -40f), new Vector2(0f, 1f));

            var label = GlassUI.Label(panel.transform, "训练得分", 14, TextAnchor.MiddleLeft, GlassTheme.InkSoft);
            GlassUI.Place(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(120f, 24f), new Vector2(20f, 0f), new Vector2(0f, 0.5f));

            _scoreText = GlassUI.Label(panel.transform, "0", 26, TextAnchor.MiddleRight, GlassTheme.Blue, false, FontStyle.Bold);
            GlassUI.Place(_scoreText.rectTransform, new Vector2(1f, 0.5f), new Vector2(120f, 30f), new Vector2(-20f, 0f), new Vector2(1f, 0.5f));
        }

        // ---------------- 武器条（切换武器 UI）----------------

        void BuildWeaponBar(Transform root)
        {
            var bar = GlassUI.Panel(root, "Weapon Bar", GlassTheme.RadiusCard);
            GlassUI.Place(bar.rectTransform, new Vector2(0.5f, 0f), new Vector2(560f, 74f), new Vector2(0f, 48f), new Vector2(0.5f, 0f));
            var row = GlassUI.Row(bar.rectTransform, 10f, 12, TextAnchor.MiddleCenter);
            row.childForceExpandWidth = true;

            for (int i = 0; i < 3; i++)
            {
                int index = i;
                var chip = GlassUI.Rect(bar.transform, $"Weapon Chip {i + 1}", new Color(1f, 1f, 1f, 0.5f), GlassTheme.RadiusItem, 0.9f, true);
                GlassUI.Flex(chip, width: 1f);
                GlassUI.Fix(chip, -1f, 50f);
                chip.raycastTarget = true;

                var button = chip.gameObject.AddComponent<Button>();
                button.targetGraphic = chip;
                button.onClick.AddListener(() => onWeaponChipClicked?.Invoke(index));
                chip.gameObject.AddComponent<HoverFx>();

                var number = GlassUI.Label(chip.transform, (i + 1).ToString(), 13, TextAnchor.UpperLeft, GlassTheme.InkFaint);
                GlassUI.Place(number.rectTransform, new Vector2(0f, 1f), new Vector2(24f, 18f), new Vector2(10f, -6f), new Vector2(0f, 1f));

                var label = GlassUI.Label(chip.transform, "—", 15, TextAnchor.MiddleCenter, GlassTheme.Ink, false, FontStyle.Bold);
                GlassUI.Stretch(label.rectTransform, 8f, 6f, 8f, 6f);

                _weaponChips.Add(chip.rectTransform);
                _weaponChipImages.Add(chip);
                _weaponChipLabels.Add(label);
            }
        }

        // ---------------- 飘字 ----------------

        public void Toast(string message)
        {
            var panel = GlassUI.Panel(transform, "Toast", GlassTheme.RadiusItem);
            GlassUI.Place(panel.rectTransform, new Vector2(1f, 1f), new Vector2(10f, 40f), new Vector2(-48f, -40f), new Vector2(1f, 1f));
            var row = GlassUI.Row(panel.rectTransform, 0f, 0, TextAnchor.MiddleCenter);
            row.padding = new RectOffset(16, 16, 8, 8);
            var label = GlassUI.Label(panel.transform, message, 15, TextAnchor.MiddleCenter, GlassTheme.Ink);
            GlassUI.Fit(panel.rectTransform, ContentSizeFitter.FitMode.PreferredSize, ContentSizeFitter.FitMode.PreferredSize);
            var group = GlassUI.Group(panel.rectTransform, 1f, false, false);

            _toasts.Insert(0, panel.gameObject);
            while (_toasts.Count > 4)
            {
                var last = _toasts[_toasts.Count - 1];
                _toasts.RemoveAt(_toasts.Count - 1);
                GlassUI.Kill(last);
            }

            LayoutToasts();
            GlassAnim.Start(this, ref _tempAnim, ToastLife(group, panel.rectTransform));
        }

        void LayoutToasts()
        {
            float y = -40f;
            foreach (var go in _toasts)
            {
                if (go == null) continue;
                var rt = (RectTransform)go.transform;
                GlassAnim.Start(this, ref _tempAnim, GlassAnim.Vector2(rt.anchoredPosition, new Vector2(-48f, y), 0.22f,
                    v => rt.anchoredPosition = v, Ease.OutCubic));
                y -= 52f;
            }
        }

        System.Collections.IEnumerator ToastLife(CanvasGroup group, RectTransform rt)
        {
            yield return GlassAnim.Curve(0.18f, t =>
            {
                if (group == null) return;
                group.alpha = t;
                rt.localScale = Vector3.one * Mathf.Lerp(0.9f, 1f, Ease.OutBack(t));
            }, Ease.Linear);
            yield return new WaitForSeconds(1.9f);
            yield return GlassAnim.Curve(0.3f, t => { if (group != null) group.alpha = 1f - t; }, Ease.Linear);
            if (rt != null)
            {
                _toasts.Remove(rt.gameObject);
                LayoutToasts();
                GlassUI.Kill(rt.gameObject);
            }
        }

        // ---------------- 刷新 ----------------

        void Refresh()
        {
            if (_state != null)
            {
                _health.Value = _state.HealthNormalized;
                if (_healthText != null) _healthText.text = Mathf.CeilToInt(_state.health).ToString();
                if (_scoreText != null) _scoreText.text = $"{_state.score:0000}";
            }

            if (_weapons != null && _weapons.Current != null && _ammoText != null)
            {
                var def = _weapons.Current;
                if (_weaponName != null) _weaponName.text = def.name;
                if (_ammoText != null) _ammoText.text = _weapons.Reloading ? "--" : _weapons.Ammo.ToString("00");
                if (_reserveText != null) _reserveText.text = _weapons.Reloading ? "换弹中" : "/ ∞";

                for (int i = 0; i < _weaponChipImages.Count; i++)
                {
                    // 编辑器里 BuildUi 可能被调两次，旧实例的图片已被销毁 —— 访问会抛 MissingReference
                    if (_weaponChipImages[i] == null || _weaponChipLabels[i] == null) continue;
                    bool active = i == _weapons.CurrentIndex;
                    _weaponChipImages[i].color = active ? GlassTheme.Blue : new Color(1f, 1f, 1f, 0.5f);
                    _weaponChipLabels[i].color = active ? GlassTheme.White : GlassTheme.Ink;
                    if (i < _weapons.weapons.Count) _weaponChipLabels[i].text = _weapons.weapons[i].kind;
                }
            }
        }

        void Update()
        {
            // 准星张开后自己收拢：非线性衰减，比线性舒服
            _spread = Mathf.SmoothDamp(_spread, 0f, ref _spreadVel, 0.16f, 20f, Time.unscaledDeltaTime);
            LayoutCrosshair();

            if (_hitTimer > 0f)
            {
                _hitTimer -= Time.unscaledDeltaTime;
                if (_hitTimer <= 0f) _hitMarker.gameObject.SetActive(false);
            }

            if (_damageFlash != null && _damageFlash.color.a > 0f)
            {
                var c = _damageFlash.color;
                c.a = Mathf.MoveTowards(c.a, 0f, Time.unscaledDeltaTime * 1.1f);
                _damageFlash.color = c;
            }
        }
    }
    /// <summary>毛玻璃上的进度条（血条 / 装弹条都用它）。</summary>
}
