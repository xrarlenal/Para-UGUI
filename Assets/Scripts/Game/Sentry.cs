using System.Collections.Generic;
using UnityEngine;

namespace GlassUI.Game
{
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
}
