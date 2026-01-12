using UnityEngine;

public class SMG_0 : WeaponBase
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

        // 计算散布
        Ray centerRay = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 spreadDir = ApplySpread(centerRay.direction);
        if (AudioManager.Instance != null)
        {
            // 这里的 pistolShoots 需要你在 AudioManager 里填好
            // 0.1f 的音调浮动让连射听起来不机械
            AudioManager.Instance.PlaySound3D(AudioManager.Instance.smgShoots, muzzlePoint.position, 1.0f, 0.1f);
        }
        // 射线检测
        if (Physics.Raycast(centerRay.origin, spreadDir, out RaycastHit hit, weaponData.maxRange))
        {
            HandleHit_WithDamage(hit, spreadDir); // 调用新的处理方法
        }

        Debug.Log($"<color=orange>{weaponName} 开火!</color>");
    }


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
