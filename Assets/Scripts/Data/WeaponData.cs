using UnityEngine;

[CreateAssetMenu(fileName = "NewWeaponConfig", menuName = "Killer/Weapon Config")]
public class WeaponData : ScriptableObject
{
    [Header("=== Base Stats ===")]
    public string bulletConfigName = "9mm Standard";
    public float damage = 10f;
    public float maxRange = 100f;
    public float fireRate = 0.2f;

    [Header("=== Damage Logic ===")]
    [Tooltip("伤害距离衰减曲线。\n" +
         "X轴: 距离 (0 = 枪口, 1 = 最大射程)\n" +
         "Y轴: 伤害倍率 (1 = 满伤, 0.5 = 半伤)")]
    public AnimationCurve damageFalloff = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.5f));

    [Header("=== Physics ===")]
    [Tooltip("击中刚体时的推力")]
    public float impactForce = 5f;
    [Tooltip("子弹速度 (如果是实体子弹)")]
    public float bulletSpeed = 100f;

    [Header("=== Visuals ===")]
    [Tooltip("击中墙壁的弹孔预制体")]
    public GameObject decalPrefab;

    [Tooltip("弹孔在墙上保留的时间 (秒)")]
    public float decalDuration = 10f; // 新增参数

    [Tooltip("击中敌人的血液/火花特效")]
    public GameObject hitVFXPrefab;

    [Tooltip("击中不同材质的音效 (可选预留)")]
    public AudioClip impactSound;
   

}
