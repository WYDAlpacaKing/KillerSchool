using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    [HideInInspector] public float currentSpread = 0f;

    public enum FireMode
    {
        SemiAuto, // 半自动：点一下打一发
        FullAuto  // 全自动：按住一直打
    }

    [Header("Base Stats")]
    public string weaponName = "Weapon";
    public FireMode fireMode = FireMode.SemiAuto; 
    public float fireRate = 0.2f;
    public float recoilStrength = 1.0f;

    protected float lastFireTime;

    public System.Action OnFire;


    /// <summary>
    /// 处理输入的入口方法
    /// 由 NoodleArmController 调用 传入当前的输入状态
    /// </summary>
    public void HandleFiringInput(bool triggerDown, bool triggerHeld)
    {
        bool wantsToFire = false;

        switch (fireMode)
        {
            case FireMode.SemiAuto:
                wantsToFire = triggerDown;
                break;
            case FireMode.FullAuto:
                wantsToFire = triggerHeld;
                break;
        }

        if (wantsToFire)
        {
            TriggerFire();
        }
    }

    /// <summary>
    /// 尝试开火（包含冷却检测）
    /// </summary>
    public virtual void TriggerFire()
    {
        if (Time.time < lastFireTime + fireRate) return;

        lastFireTime = Time.time;
        FireLogic();
        OnFire?.Invoke();
    }

    protected abstract void FireLogic();

    public virtual void OnEquip()
    {
        gameObject.SetActive(true);
    }

    public virtual void OnUnequip()
    {
        gameObject.SetActive(false);
    }
}
