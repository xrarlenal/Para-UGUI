using System.Collections.Generic;
using UnityEngine;

namespace GlassUI.Game
{
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
}
