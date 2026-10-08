using System.Collections.Generic;
using UnityEngine;

namespace GlassUI.Game
{
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
}
