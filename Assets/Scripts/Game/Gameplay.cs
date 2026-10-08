using System.Collections.Generic;
using UnityEngine;

namespace GlassUI.Game
{
    /// <summary>全局游戏数据：血量、分数、当前武器。UI 全部从这里读，改数据就走统一的事件。</summary>
    public class GameState : MonoBehaviour
    {
        public static GameState Instance { get; private set; }

        public float maxHealth = 100f;
        public float health = 100f;
        public int score;
        public int kills;

        /// <summary>数值变化 → HUD 刷新。</summary>
        public System.Action onChanged;
        /// <summary>飘一条提示（右上角）。</summary>
        public System.Action<string> onToast;
        /// <summary>受击 → 屏幕边缘红闪。</summary>
        public System.Action<float> onDamaged;

        void Awake()
        {
            Instance = this;
        }

        public float HealthNormalized => Mathf.Clamp01(health / Mathf.Max(1f, maxHealth));

        public void Damage(float amount)
        {
            if (health <= 0f) return;
            health = Mathf.Max(0f, health - amount);
            onDamaged?.Invoke(amount);
            onChanged?.Invoke();

            if (health <= 0f) Respawn();
        }

        public void Heal(float amount)
        {
            health = Mathf.Min(maxHealth, health + amount);
            onChanged?.Invoke();
        }

        public void AddScore(int amount, bool kill = false)
        {
            score += amount;
            if (kill) kills++;
            onChanged?.Invoke();
        }

        void Respawn()
        {
            health = maxHealth;
            onToast?.Invoke("你被击倒了，已重新部署");
            onChanged?.Invoke();
        }
    }

    /// <summary>可击中的靶子：打中会翻倒、变色、加分，一秒后自己复位。</summary>
    public class ShootTarget : MonoBehaviour
    {
        public Color accent = new Color(0.28f, 0.55f, 1f);
        public int scoreValue = 10;

        Renderer _renderer;
        Material _material;
        Color _baseColor;
        Vector3 _baseEuler;
        Coroutine _anim;
        bool _down;

        void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _material = new Material(_renderer.sharedMaterial);
            _renderer.sharedMaterial = _material;
            _baseColor = _material.color;
            _baseEuler = transform.localEulerAngles;
        }

        public void Hit(Vector3 direction)
        {
            if (_down) return;
            _down = true;
            GameState.Instance?.AddScore(scoreValue, true);
            _material.color = accent;
            _material.EnableKeyword("_EMISSION");
            _material.SetColor("_EmissionColor", accent * 1.6f);

            GlassAnim.Start(this, ref _anim, KnockDown(direction));
        }

        System.Collections.IEnumerator KnockDown(Vector3 direction)
        {
            var from = transform.localEulerAngles;
            var axis = Vector3.Cross(Vector3.up, direction).normalized;
            float angle = 82f;
            yield return GlassAnim.Curve(0.22f, t => transform.localEulerAngles = Vector3.Lerp(from, from + axis * angle, t), Ease.OutBack);
            yield return new WaitForSeconds(0.7f);
            yield return GlassAnim.Curve(0.35f, t => transform.localEulerAngles = Vector3.Lerp(from + axis * angle, _baseEuler, t), Ease.OutCubic);

            _material.color = _baseColor;
            _material.DisableKeyword("_EMISSION");
            _down = false;
        }
    }

    /// <summary>哨戒炮：隔一段时间朝玩家开火，让 HUD 的血量/受击反馈有真实数据。</summary>
    public class Sentry : MonoBehaviour
    {
        public float interval = 2.6f;
        public float damage = 7f;
        public float range = 60f;

        Transform _player;
        LineRenderer _tracer;
        float _timer;

        void Start()
        {
            var player = GameObject.Find("Player");
            if (player != null) _player = player.transform;
            _timer = Random.Range(0.5f, interval);

            _tracer = gameObject.AddComponent<LineRenderer>();
            _tracer.positionCount = 2;
            _tracer.startWidth = 0.06f;
            _tracer.endWidth = 0.02f;
            _tracer.material = new Material(Shader.Find("Sprites/Default"));
            _tracer.startColor = new Color(1f, 0.35f, 0.4f);
            _tracer.endColor = new Color(1f, 0.6f, 0.6f, 0.1f);
            _tracer.enabled = false;
        }

        void Update()
        {
            if (_player == null || GameState.Instance == null) return;

            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = interval;

            var origin = transform.position;
            var toPlayer = _player.position + Vector3.up - origin;
            if (toPlayer.magnitude > range) return;

            // 视线被掩体挡住就不打——顺便让"躲掩体"这件事成立
            if (Physics.Raycast(origin, toPlayer.normalized, out var hit, toPlayer.magnitude))
                if (!hit.transform.IsChildOf(_player)) return;

            _tracer.SetPosition(0, origin);
            _tracer.SetPosition(1, _player.position + Vector3.up);
            _tracer.enabled = true;
            GameState.Instance.Damage(damage);
            Invoke(nameof(HideTracer), 0.07f);
        }

        void HideTracer()
        {
            if (_tracer != null) _tracer.enabled = false;
        }
    }

    /// <summary>一把武器的数据。</summary>
    [System.Serializable]
    public class WeaponDef
    {
        public string name = "武器";
        public string kind = "突击步枪";
        public int damage = 18;
        public float fireRate = 0.11f;
        public int magSize = 30;
        public float reloadTime = 1.4f;
        public float spread = 0.6f;
        public int pellets = 1;
        public bool automatic = true;
        public Color accent = new Color(0.28f, 0.55f, 1f);

        // 程序化枪械外观参数
        public float bodyLength = 0.42f;
        public float bodyHeight = 0.09f;
        public float barrelLength = 0.34f;
        public float stockScale = 1f;
    }

    /// <summary>
    /// 武器系统：三把程序化拼出来的枪、切换动画、开火、后坐、换弹、命中反馈。
    /// 第一人称视角下枪挂在相机下面。
    /// </summary>
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

            var metal = new Color(0.16f, 0.18f, 0.22f);
            var dark = new Color(0.10f, 0.11f, 0.13f);

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
            if (_holder == null) return;

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
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public Transform head;
        public float walkSpeed = 5.2f;
        public float sprintSpeed = 8.2f;
        public float jumpHeight = 1.15f;
        public float gravity = -18f;
        public float mouseSensitivity = 2.4f;
        public bool lookEnabled = true;
        public bool moveEnabled = true;

        CharacterController _controller;
        Vector3 _velocity;
        float _pitch;

        void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (head == null)
            {
                var cam = GetComponentInChildren<Camera>();
                if (cam != null) head = cam.transform;
            }
        }

        void Update()
        {
            if (lookEnabled && Cursor.lockState == CursorLockMode.Locked)
            {
                float mx = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
                float my = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

                transform.Rotate(Vector3.up * mx, Space.Self);
                _pitch = Mathf.Clamp(_pitch - my, -85f, 85f);
                if (head != null) head.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            }

            float x = 0f, z = 0f;
            if (moveEnabled)
            {
                x = Input.GetAxisRaw("Horizontal");
                z = Input.GetAxisRaw("Vertical");
            }

            var move = transform.right * x + transform.forward * z;
            if (move.sqrMagnitude > 1f) move.Normalize();

            float speed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed;
            if (_controller.isGrounded && _velocity.y < 0f) _velocity.y = -2f;

            if (moveEnabled && _controller.isGrounded && Input.GetKeyDown(KeyCode.Space))
                _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

            _velocity.y += gravity * Time.deltaTime;
            _controller.Move((move * speed + Vector3.up * _velocity.y) * Time.deltaTime);

            // 走路时轻微头部起伏，让第一人称不那么生硬
            if (head != null && _controller.isGrounded && move.sqrMagnitude > 0.01f)
            {
                float bob = Mathf.Sin(Time.time * (speed > walkSpeed + 0.1f ? 13f : 9f)) * 0.022f;
                var cam = head.GetComponent<Camera>();
                if (cam != null) cam.transform.localPosition = new Vector3(0f, bob, 0f);
            }
        }
    }
}
