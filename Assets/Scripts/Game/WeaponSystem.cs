using System.Collections.Generic;
using UnityEngine;

namespace GlassUI.Game
{
    [ExecuteAlways]   // 编辑器里也建出枪模，离屏截图才看得到 viewmodel
    public class WeaponSystem : MonoBehaviour
    {
        public Camera viewCamera;
        public Transform muzzleAnchor;

        public readonly List<WeaponDef> weapons = new List<WeaponDef>();
        public int CurrentIndex { get; private set; }
        public WeaponDef Current => weapons.Count == 0 ? null : weapons[CurrentIndex];
        public int Ammo { get; private set; }
        public bool Reloading { get; private set; }

        public System.Action onChanged;

        readonly List<GameObject> _bodies = new List<GameObject>();
        Transform _holder;
        float _nextFireTime;
        Coroutine _switchAnim;
        Coroutine _reloadAnim;
        float _recoil;
        float _recoilVel;
        GameObject _flash;

        void Start()
        {
            if (viewCamera == null) viewCamera = GetComponentInChildren<Camera>();
            BuildDefaults();
            RebuildBodies();
            Equip(0, true);
        }

        void OnEnable()
        {
            if (!Application.isPlaying) Start();   // 编辑器里手动补一次
        }

        void BuildDefaults()
        {
            weapons.Clear();
            weapons.Add(new WeaponDef
            {
                name = "AR-7 突击步枪", kind = "突击步枪", damage = 16, fireRate = 0.105f,
                magSize = 30, reloadTime = 1.5f, spread = 0.75f, automatic = true,
                accent = new Color(0.28f, 0.55f, 1f),
                bodyLength = 0.44f, bodyHeight = 0.10f, barrelLength = 0.36f, stockScale = 1f,
            });
            weapons.Add(new WeaponDef
            {
                name = "SG-12 霰弹枪", kind = "霰弹枪", damage = 9, fireRate = 0.85f,
                magSize = 8, reloadTime = 2.2f, spread = 4.2f, pellets = 8, automatic = false,
                accent = new Color(0.95f, 0.62f, 0.25f),
                bodyLength = 0.50f, bodyHeight = 0.13f, barrelLength = 0.30f, stockScale = 1.35f,
            });
            weapons.Add(new WeaponDef
            {
                name = "P-9 手枪", kind = "手枪", damage = 24, fireRate = 0.26f,
                magSize = 12, reloadTime = 1.1f, spread = 0.5f, automatic = false,
                accent = new Color(0.55f, 0.80f, 1f),
                bodyLength = 0.26f, bodyHeight = 0.09f, barrelLength = 0.18f, stockScale = 0.5f,
            });
        }

        /// <summary>按参数拼一把枪（积木式 viewmodel，不需要美术资源）。</summary>
        void RebuildBodies()
        {
            if (_holder != null) GlassUI.Kill(_holder.gameObject);

            var holderGo = new GameObject("Weapon Holder");
            _holder = holderGo.transform;
            _holder.SetParent(viewCamera.transform, false);
            _holder.localPosition = new Vector3(0.24f, -0.20f, 0.34f);
            _holder.localRotation = Quaternion.Euler(3f, -4f, 0f);

            _bodies.Clear();
            foreach (var def in weapons) _bodies.Add(BuildBody(def));
        }

        GameObject BuildBody(WeaponDef def)
        {
            var root = new GameObject("Body: " + def.kind);
            root.transform.SetParent(_holder, false);

            var metal = new Color(0.46f, 0.50f, 0.58f);   // 提亮：原来深灰压在深地板上看不见
            var dark = new Color(0.30f, 0.33f, 0.39f);

            Part(root.transform, "Receiver", new Vector3(0f, 0f, 0f),
                new Vector3(def.bodyHeight * 2.6f, def.bodyHeight, def.bodyLength), metal);
            Part(root.transform, "Barrel", new Vector3(0f, def.bodyHeight * 0.35f, def.bodyLength * 0.5f + def.barrelLength * 0.5f),
                new Vector3(def.bodyHeight * 0.8f, def.bodyHeight * 0.8f, def.barrelLength), dark);
            Part(root.transform, "Stock", new Vector3(0f, -def.bodyHeight * 0.35f, -def.bodyLength * 0.5f - def.bodyLength * 0.28f * def.stockScale),
                new Vector3(def.bodyHeight * 1.8f, def.bodyHeight * 1.5f, def.bodyLength * 0.6f * def.stockScale), dark);
            Part(root.transform, "Magazine", new Vector3(0f, -def.bodyHeight * 1.6f, def.bodyLength * 0.1f),
                new Vector3(def.bodyHeight * 1.1f, def.bodyHeight * 2.2f, def.bodyLength * 0.28f), metal);
            Part(root.transform, "Grip", new Vector3(0f, -def.bodyHeight * 1.5f, -def.bodyLength * 0.25f),
                new Vector3(def.bodyHeight * 1.1f, def.bodyHeight * 2.0f, def.bodyLength * 0.2f), dark);

            // 发光装饰条：和 UI 的蓝色呼应
            var strip = Part(root.transform, "Accent", new Vector3(def.bodyHeight * 1.35f, def.bodyHeight * 0.1f, 0f),
                new Vector3(def.bodyHeight * 0.16f, def.bodyHeight * 0.5f, def.bodyLength * 0.8f), def.accent, true);

            // 枪口焰
            var flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flash.name = "Muzzle Flash";
            flash.transform.SetParent(root.transform, false);
            flash.transform.localPosition = new Vector3(0f, def.bodyHeight * 0.35f, def.bodyLength * 0.5f + def.barrelLength + 0.02f);
            flash.transform.localScale = Vector3.one * 0.06f;
            GlassUI.Kill(flash.GetComponent<Collider>());
            var flashRenderer = flash.GetComponent<Renderer>();
            var flashMat = new Material(flashRenderer.sharedMaterial);
            flashMat.color = new Color(1f, 0.85f, 0.55f);
            flashMat.EnableKeyword("_EMISSION");
            flashMat.SetColor("_EmissionColor", new Color(1f, 0.8f, 0.5f) * 4f);
            flashRenderer.sharedMaterial = flashMat;
            flash.SetActive(false);
            _flash = flash;
            return root;
        }

        static GameObject Part(Transform parent, string name, Vector3 localPos, Vector3 scale, Color color, bool emissive = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            GlassUI.Kill(go.GetComponent<Collider>());

            var renderer = go.GetComponent<Renderer>();
            var mat = new Material(renderer.sharedMaterial) { color = color };
            if (emissive)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * 2.4f);
            }
            renderer.sharedMaterial = mat;
            return go;
        }

        /// <summary>强制把枪模建出来（编辑器/截图用）。</summary>
        public void EnsureBuilt()
        {
            if (viewCamera == null) viewCamera = GetComponentInChildren<Camera>();
            if (weapons.Count == 0) BuildDefaults();
            if (_holder == null) { RebuildBodies(); Equip(0, true); }

            Debug.Log($"[DIAG] 武器：holder={(_holder != null ? _holder.name : "null")} " +
                      $"children={(_holder != null ? _holder.childCount : 0)} " +
                      $"world={(_holder != null ? _holder.position.ToString("F2") : "-")} " +
                      $"cam={viewCamera?.name} 激活={gameObject.activeInHierarchy}");

            if (_holder != null)
            {
                var first = _holder.childCount > 0 ? _holder.GetChild(0) : null;
                if (first != null)
                {
                    var r = first.GetComponentInChildren<Renderer>();
                    var w = first.Find("Receiver");
                    Debug.Log($"[DIAG]   body0={first.name} active={first.gameObject.activeInHierarchy} " +
                              $"renderer={(r != null ? r.name : "null")} receiverWorld={(w != null ? w.position.ToString("F2") : "-")}");
                }
            }
        }

        public void Equip(int index, bool instant = false)
        {
            if (weapons.Count == 0) return;
            index = ((index % weapons.Count) + weapons.Count) % weapons.Count;

            if (!instant && index == CurrentIndex) return;

            CurrentIndex = index;
            var def = weapons[index];
            Ammo = def.magSize;
            Reloading = false;

            for (int i = 0; i < _bodies.Count; i++) _bodies[i].SetActive(i == index);

            GlassAnim.Stop(this, ref _reloadAnim);
            if (!instant) GlassAnim.Start(this, ref _switchAnim, SwitchAnim(def));
            else _holder.localRotation = Quaternion.Euler(3f, -4f, 0f);

            onChanged?.Invoke();
        }

        System.Collections.IEnumerator SwitchAnim(WeaponDef def)
        {
            // 先往下压枪，再抬起来 —— 非线性，收尾有点过冲
            var baseRot = Quaternion.Euler(3f, -4f, 0f);
            var low = baseRot * Quaternion.Euler(38f, 12f, 0f);
            yield return GlassAnim.Curve(0.12f, t => _holder.localRotation = Quaternion.Slerp(baseRot, low, t), Ease.OutCubic);
            yield return GlassAnim.Curve(0.22f, t => _holder.localRotation = Quaternion.Slerp(low, baseRot, t), Ease.OutBack);
        }

        public void SwitchBy(int delta) => Equip(CurrentIndex + delta);

        public void Trigger(bool pressed)
        {
            var def = Current;
            if (def == null || Reloading) return;
            if (Time.unscaledTime < _nextFireTime) return;
            if (!pressed) return;

            if (Ammo <= 0)
            {
                Reload();
                return;
            }

            if (!def.automatic) _nextFireTime = Time.unscaledTime + def.fireRate;
            Fire(def);
        }

        public void HoldFire()
        {
            var def = Current;
            if (def == null || !def.automatic || Reloading) return;
            if (Time.unscaledTime < _nextFireTime) return;
            if (Ammo <= 0) { Reload(); return; }
            Fire(def);
        }

        void Fire(WeaponDef def)
        {
            _nextFireTime = Time.unscaledTime + def.fireRate;
            Ammo--;
            _recoilVel += def.pellets > 1 ? 9f : 5.5f;

            if (_flash != null)
            {
                _flash.SetActive(true);
                _flash.transform.localScale = Vector3.one * (def.pellets > 1 ? 0.14f : 0.09f);
                CancelInvoke(nameof(HideFlash));
                Invoke(nameof(HideFlash), 0.045f);
            }

            var origin = viewCamera.transform.position;
            for (int i = 0; i < Mathf.Max(1, def.pellets); i++)
            {
                var forward = viewCamera.transform.forward;
                forward += viewCamera.transform.right * Random.Range(-def.spread, def.spread) * 0.01f;
                forward += viewCamera.transform.up * Random.Range(-def.spread, def.spread) * 0.01f;

                if (!Physics.Raycast(origin, forward.normalized, out var hit, 250f)) continue;

                var target = hit.collider.GetComponentInParent<ShootTarget>();
                if (target != null) target.Hit(forward.normalized);
                else Impact(hit.point, hit.normal);
            }

            onChanged?.Invoke();
        }

        void Impact(Vector3 point, Vector3 normal)
        {
            var spark = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            spark.name = "Impact";
            spark.transform.position = point + normal * 0.02f;
            spark.transform.localScale = Vector3.one * 0.07f;
            GlassUI.Kill(spark.GetComponent<Collider>());
            var renderer = spark.GetComponent<Renderer>();
            var mat = new Material(renderer.sharedMaterial);
            mat.color = new Color(1f, 0.9f, 0.7f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(1f, 0.85f, 0.6f) * 3f);
            renderer.sharedMaterial = mat;
            Destroy(spark, 0.6f);
        }

        void HideFlash()
        {
            if (_flash != null) _flash.SetActive(false);
        }

        public void Reload()
        {
            var def = Current;
            if (def == null || Reloading || Ammo == def.magSize) return;
            GlassAnim.Start(this, ref _reloadAnim, ReloadAnim(def));
        }

        System.Collections.IEnumerator ReloadAnim(WeaponDef def)
        {
            Reloading = true;
            onChanged?.Invoke();

            var baseRot = Quaternion.Euler(3f, -4f, 0f);
            var dip = baseRot * Quaternion.Euler(26f, -18f, 8f);
            yield return GlassAnim.Curve(0.22f, t => _holder.localRotation = Quaternion.Slerp(baseRot, dip, t), Ease.OutCubic);
            yield return new WaitForSeconds(def.reloadTime * 0.55f);
            yield return GlassAnim.Curve(0.26f, t => _holder.localRotation = Quaternion.Slerp(dip, baseRot, t), Ease.OutBack);

            Ammo = def.magSize;
            Reloading = false;
            onChanged?.Invoke();
        }

        void Update()
        {
            if (!Application.isPlaying || _holder == null) return;

            // 后坐：弹簧回正，比线性自然
            _recoil = Mathf.SmoothDamp(_recoil, 0f, ref _recoilVel, 0.09f, 40f, Time.unscaledDeltaTime);
            var baseRot = Quaternion.Euler(3f, -4f, 0f);
            var basePos = new Vector3(0.24f, -0.20f, 0.34f);
            _holder.localPosition = basePos + new Vector3(0f, _recoil * 0.006f, -_recoil * 0.02f);
            if (Reloading || (_switchAnim != null && !GlassAnim.Playing)) { }
            else _holder.localRotation = baseRot * Quaternion.Euler(-_recoil * 2.4f, 0f, _recoil * 1.2f);
        }
    }
    /// <summary>第一人称控制器：WASD + 鼠标视角 + 跳跃 + 冲刺；开 UI 面板时锁住视角和移动。</summary>
}
