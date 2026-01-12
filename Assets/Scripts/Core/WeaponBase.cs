using System;
using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    [HideInInspector] public float currentSpread = 0f;

    // === 新增：全局静态事件，通知所有人“玩家开枪了” ===
    // 参数 Vector3 代表开枪的位置
    public static event Action<Vector3> OnGlobalPlayerFire;
    // 新增：定义阵营常量
    public const int TEAM_PLAYER = 0;
    public const int TEAM_ENEMY = 1;
    // 新增：当前武器的持有者阵营
    protected int ownerTeamID;
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


    // 新增：持有者标记
    protected bool isPlayerWeapon = true;

    public virtual void Initialize(bool isPlayer)
    {
        this.isPlayerWeapon = isPlayer;
        // 如果是玩家持有，阵营为0；NPC持有，阵营为1
        this.ownerTeamID = isPlayer ? TEAM_PLAYER : TEAM_ENEMY;
    }

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
        // 1. 执行具体的开火逻辑（生成子弹/射线）- 这个谁都要做
        FireLogic();

        // 2. 本地事件（播放枪口火光、音效等）- 这个谁都要做
        OnFire?.Invoke();

        // 3. === 关键修改：只有玩家持有才触发以下内容 ===
        if (isPlayerWeapon)
        {
            // 广播“玩家开火”事件（给 AI 听）
            // 如果 NPC 开火也广播这个，会导致 AI 听到自己开枪然后吓得逃跑
            OnGlobalPlayerFire?.Invoke(transform.position);

            // 应用后坐力 (如果你的后坐力代码写在这里)
            ApplyPlayerRecoil();
        }
    }
    // 可以在这里统一处理后坐力接口
    protected virtual void ApplyPlayerRecoil()
    {
        // 如果你用的是 NoodleArmController 的单例或者查找
        // 比如: NoodleArmController.Instance.ApplyExternalRecoil(...);
        // 或者 CameraShaker.Instance.Shake(...);
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

    // === 新增：通用的命中处理逻辑 (父类实现) ===
    // 子类 (SimplePistol/SMG) 只需要调用这个方法即可
    protected void ProcessHit(RaycastHit hit, Vector3 bulletDir, Transform firePoint)
    {
        // 1. 获取接口
        IDamageable damageable = hit.collider.GetComponent<IDamageable>();

        if (damageable != null)
        {
            // === 核心逻辑：敌我识别 ===
            // 如果持有者阵营 == 目标阵营，则不造成伤害 (防止友军误伤)
            if (damageable.GetTeamID() == this.ownerTeamID)
            {
                return;
            }

            // 2. 距离衰减计算 (需要 WeaponData，这里假设子类传参或者 WeaponBase 有引用)
            // 为了通用性，我们可以暂且忽略衰减，或者要求子类传入 WeaponData
            // 这里演示基础扣血：

            // *注意：为了更严谨的结构，你可以把 WeaponData 移到 WeaponBase 中，
            // 但为了不破坏你现有结构，我们假设这是一个基础伤害*
            float finalDamage = 10f; // 默认值，子类应该覆盖或传入

            // 调用接口扣血
            damageable.TakeDamage(finalDamage, hit.point, hit.normal);
        }

        // 2. === 新增：处理击中音效 ===
        if (AudioManager.Instance != null)
        {
            // 判断击中材质 (简单版：通过 Tag 或 Component)
            damageable = hit.collider.GetComponentInParent<IDamageable>();

            if (damageable != null)
            {
                // 打中活物 (NPC/Player) -> 播放肉体声音
                AudioManager.Instance.PlaySound3D(AudioManager.Instance.bulletImpactFlesh, hit.point, 0.8f);
            }
            else
            {
                // 打中死物 -> 简单判断是否是金属 (Tag) 或者默认石头/墙壁
                if (hit.collider.CompareTag("Metal"))
                {
                    AudioManager.Instance.PlaySound3D(AudioManager.Instance.bulletImpactMetal, hit.point, 0.6f);
                }
                else
                {
                    // 默认墙壁声音
                    AudioManager.Instance.PlaySound3D(AudioManager.Instance.bulletImpactStone, hit.point, 0.6f);
                }
            }
        }

        // 3. 物理推力
        if (hit.rigidbody != null)
        {
            hit.rigidbody.AddForceAtPosition(bulletDir * 5f, hit.point, ForceMode.Impulse);
        }
    }
}
