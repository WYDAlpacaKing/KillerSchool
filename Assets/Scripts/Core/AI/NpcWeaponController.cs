using UnityEngine;

public class NpcWeaponController : MonoBehaviour
{
    [Header("配置")]
    public WeaponBase weaponPrefab; // 武器预制体 (NPC版)
    public Transform weaponHandAnchor; // 手部挂点

    [Header("显示设置")]
    [Tooltip("勾选：平时就拿着枪 (守卫/土匪)\n不勾选：平时藏着枪，打架才拿出来 (伪装者/目标)")]
    public bool showWeaponByDefault = true;

    [Header("射击参数")]
    public float burstRate = 1.0f;
    public float accuracyError = 0.5f;

    private WeaponBase currentWeapon;
    private float nextFireTime;

    void Start()
    {
        if (weaponPrefab != null && weaponHandAnchor != null)
        {
            SpawnWeapon();
        }
    }

    void SpawnWeapon()
    {
        // 实例化枪械
        currentWeapon = Instantiate(weaponPrefab, weaponHandAnchor);
        currentWeapon.transform.localPosition = Vector3.zero;
        currentWeapon.transform.localRotation = Quaternion.identity;

        // 初始化身份
        currentWeapon.Initialize(false);

        // === 核心修改：根据配置决定是否立刻显示 ===
        // 如果 showWeaponByDefault 是 false，这里就设为 false
        currentWeapon.gameObject.SetActive(showWeaponByDefault);
    }

    /// <summary>
    /// 控制武器显隐 (拔枪/收枪)
    /// </summary>
    public void SetWeaponVisible(bool visible)
    {
        if (currentWeapon != null)
        {
            currentWeapon.gameObject.SetActive(visible);
        }
    }

    public void TryShoot(Transform target)
    {
        if (currentWeapon == null) return;

        // 防御性编程：如果枪是藏起来的，先强制拔枪
        if (!currentWeapon.gameObject.activeSelf)
        {
            SetWeaponVisible(true);
        }

        if (Time.time >= nextFireTime)
        {
            currentWeapon.transform.LookAt(target.position + GetRandomOffset());
            currentWeapon.TriggerFire();
            nextFireTime = Time.time + currentWeapon.fireRate;
        }
        currentWeapon.transform.LookAt(target.position + Vector3.up * 1.5f);
    }

    Vector3 GetRandomOffset()
    {
        return Random.insideUnitSphere * accuracyError;
    }
}
