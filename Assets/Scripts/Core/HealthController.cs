using System;
using UnityEngine;

public class HealthController : MonoBehaviour, IDamageable
{
    [Header("Basic Stats")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("Team Settings")]
    [Tooltip("0 = 玩家阵营, 1 = 敌人阵营")]
    public int teamID = 1;

    // 事件：受伤、死亡
    public event Action OnDeath;
    public event Action<float> OnTakeDamage;

    private bool isDead = false;

    

    private void Start()
    {
        currentHealth = maxHealth;

        // === 新增：初始化时更新 UI (仅限玩家) ===
        if (teamID == 0 && HUDController.Instance != null)
        {
            HUDController.Instance.UpdateHealthUI(currentHealth);
        }
    }

    public int GetTeamID()
    {
        return teamID;
    }

    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (isDead) return;

        currentHealth -= amount;
        OnTakeDamage?.Invoke(amount);

        // 可以在这里生成飙血特效
        // Instantiate(bloodVFX, hitPoint, Quaternion.LookRotation(hitNormal));
        if (teamID == 0 && HUDController.Instance != null)
        {
            HUDController.Instance.UpdateHealthUI(currentHealth);
        }
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        OnDeath?.Invoke();
        Debug.Log($"{gameObject.name} 死亡！");

        if(teamID == 1)
        {
            // 敌人死亡逻辑
            // 比如播放死亡动画，掉落物品等
        }
        else if(teamID == 0)
        {
            Debug.Log("玩家死亡，触发游戏结束逻辑");
            AudioManager.Instance.PlaySound2D(AudioManager.Instance.playerDead, 1.0f);
            GameManager.Instance.PlayerDead();
        }

        // 销毁或禁用碰撞体，防止鞭尸
        var collider = GetComponent<Collider>();
        if (collider) collider.enabled = false;
    }
}
