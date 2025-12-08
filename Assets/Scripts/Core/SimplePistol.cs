using UnityEngine;

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

        // 计算散布
        Ray centerRay = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 spreadDir = ApplySpread(centerRay.direction);

        // 射线检测
        if (Physics.Raycast(centerRay.origin, spreadDir, out RaycastHit hit, weaponData.maxRange))
        {
            HandleHit(hit, spreadDir);
        }

        Debug.Log($"<color=orange>{weaponName} 开火!</color>");
    }

    
    private void HandleHit(RaycastHit hit, Vector3 bulletDir)
    {
        // 获取接口
        IDamageable damageable = hit.collider.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(weaponData.damage, hit.point, hit.normal);

            // 命中特效!!!!!!!!!!!
            if (weaponData.hitVFXPrefab != null)
            {
                Instantiate(weaponData.hitVFXPrefab, hit.point, Quaternion.LookRotation(hit.normal));
            }
        }
        else // 是非命中物体
        {
            DecalManager.Instance?.SpawnDecal(weaponData.decalPrefab, hit.point, hit.normal, weaponData.decalDuration);
        }

        
        if (hit.rigidbody != null) // 能推
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
