using UnityEngine;
using UnityEngine.UIElements;

public class SimplePistol : WeaponBase
{
    [Header("Configuration")]
    [SerializeField] private WeaponData weaponData; // 武器参数配置

    [Header("References")]
    [SerializeField] private Transform muzzlePoint;
    [SerializeField] private ParticleSystem muzzleFlash;

    private Camera mainCam;

    private void Start()
    {
        mainCam = Camera.main;

        // 注册监听 子弹速度+当前模式
        DebuggerManager.RegisterWatcher(DebuggerModuleType.WeaponSystem, "Bullet Speed", () => weaponData.bulletSpeed);
        DebuggerManager.RegisterWatcher(DebuggerModuleType.WeaponSystem, "Firing Mode", () => fireMode.ToString());
    }

    /// <summary>
    /// 开火逻辑
    /// </summary>
    protected override void FireLogic()
    {
        // 特效
        if (muzzleFlash != null) muzzleFlash.Play();

        // === 新增：播放开枪音效 ===
        // 从 AudioManager 获取手枪音效数组
        if (AudioManager.Instance != null)
        {
            // 这里的 pistolShoots 需要你在 AudioManager 里填好
            // 0.1f 的音调浮动让连射听起来不机械
            AudioManager.Instance.PlaySound3D(AudioManager.Instance.pistolShoots, muzzlePoint.position, 1.0f, 0.1f);
        }

        Vector3 rayOrigin;
        Vector3 rayDirection;

        // === 核心修改：区分玩家和 NPC 的发射逻辑 ===
        if (isPlayerWeapon)
        {
            // 玩家：从屏幕中心发射
            Ray centerRay = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            rayOrigin = centerRay.origin;
            rayDirection = centerRay.direction;
        }
        else
        {
            // NPC：从枪口物理方向发射 (因为 NPC 已经瞄准了)
            rayOrigin = muzzlePoint.position;
            rayDirection = muzzlePoint.forward;
        }
        // === 新增：画出调试射线 (并在场景中持续显示 2秒) ===
        // 红色代表射击轨迹
        Debug.DrawRay(rayOrigin, rayDirection * weaponData.maxRange, Color.red, 2.0f);
        // 应用散布
        Vector3 spreadDir = ApplySpread(rayDirection);

        if (Physics.Raycast(rayOrigin, spreadDir, out RaycastHit hit, weaponData.maxRange))
        {
            // === 新增：打中东西了，打印出来 ===
            Debug.Log($"NPC 打中了: {hit.collider.name}"); // 看看是不是打中了 Player
            HandleHit_WithDamage(hit, spreadDir); // 调用新的处理方法
        }

        Debug.Log($"<color=orange>{weaponName} 开火!</color>");
    }


    // === 修改后的 HandleHit ===
    private void HandleHit_WithDamage(RaycastHit hit, Vector3 bulletDir)
    {
        IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();

        if (damageable != null)
        {
            // === 1. 敌我识别 ===
            if (damageable.GetTeamID() == this.ownerTeamID) return; // 自己人，不打

            // === 2. 伤害计算 ===
            float distance = Vector3.Distance(muzzlePoint.position, hit.point);
            float distanceRatio = Mathf.Clamp01(distance / weaponData.maxRange);
            float damageMultiplier = weaponData.damageFalloff.Evaluate(distanceRatio);
            float finalDamage = weaponData.damage * damageMultiplier;

            if (AudioManager.Instance != null)
            {
                // 判断击中材质 (简单版：通过 Tag 或 Component)
                damageable = hit.collider.GetComponentInParent<IDamageable>();

                if (damageable != null)
                {
                    // 打中活物 (NPC/Player) -> 播放肉体声音
                    Debug.Log("播放肉体击中音效");
                    AudioManager.Instance.PlaySound3D(AudioManager.Instance.bulletImpactFlesh, hit.point, 0.8f);
                }

            }
            // === 3. 扣血 ===
            damageable.TakeDamage(finalDamage, hit.point, hit.normal);

            // 命中特效
            if (weaponData.hitVFXPrefab != null)
            {
                Instantiate(weaponData.hitVFXPrefab, hit.point, Quaternion.LookRotation(hit.normal));
            }
        }
        else
        {
            Debug.Log("播放金属击中音效");
            AudioManager.Instance.PlaySound3D(AudioManager.Instance.bulletImpactMetal, hit.point, 0.6f);


            // 打中墙壁等死物
            if (DecalManager.Instance != null)
                DecalManager.Instance.SpawnDecal(weaponData.decalPrefab, hit.point, hit.normal, weaponData.decalDuration);
        }

        if (hit.rigidbody != null)
        {
            hit.rigidbody.AddForceAtPosition(bulletDir * weaponData.impactForce, hit.point, ForceMode.Impulse);
        }
    }

    // 计算散布方向
    private Vector3 ApplySpread(Vector3 originalDir)
    {
        float x = Random.Range(-currentSpread, currentSpread);
        float y = Random.Range(-currentSpread, currentSpread);
        return Quaternion.Euler(x, y, 0) * originalDir;
    }
}
