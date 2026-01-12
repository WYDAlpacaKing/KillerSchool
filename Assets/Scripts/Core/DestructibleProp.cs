using System;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DestructibleProp : MonoBehaviour, IDamageable
{
    [Header("Prop Stats")]
    [SerializeField] private float maxHealth = 50f;
    [SerializeField] private bool isInvincible = false; // 是否无敌 只受力
    [SerializeField] private GameObject destroyVFX; // 破碎特效

    private float currentHealth;
    private Rigidbody rb;

    public Action OnDestroyed; // 物体被销毁时的回调

    [Header("Team Settings")]
    [Tooltip("通常设为 999 (中立) 或 2 (障碍物)，确保不等于 Player(0) 或 Enemy(1)")]
    [SerializeField] private int teamID = 999; // 默认中立

    private void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody>();
    }
    // === 修复点：实现 GetTeamID ===
    public int GetTeamID()
    {
        // 返回配置好的 teamID，而不是抛出异常
        return teamID;
    }

    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (isInvincible) return;

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die(OnDestroyed);
        }
    }

    private void Die(Action actionWhenDead = null)
    {
        if (destroyVFX != null)
        {
            Instantiate(destroyVFX, transform.position, transform.rotation);
        }

        actionWhenDead?.Invoke();

        // 简单销毁  后面可以用对象池
        Destroy(gameObject);
    }

}
