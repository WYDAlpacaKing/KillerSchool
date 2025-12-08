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

    private void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody>();
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
