using UnityEngine;

public class SafeGroundRecorder : MonoBehaviour
{
    [Header("边缘检测设置")]
    [Tooltip("判定为地面的图层 (千万不要把水面的 Layer 放进来！)")]
    public LayerMask groundLayer;
    [Tooltip("脚底检测半径")]
    public float checkRadius = 0.4f;
    [Tooltip("向下检测的距离")]
    public float checkDistance = 0.2f;
    [Tooltip("最大可站立坡度")]
    public float maxSlopeAngle = 50f;

    [Header("=== 核心修复：防抖动设置 ===")]
    [Tooltip("必须在地面稳定站立多久，才记录为安全点？(推荐 0.2 - 0.5)")]
    public float safeTimeThreshold = 0.3f; // 0.3秒的缓冲期
    [Tooltip("如果踩到的物体 tag 是这个，绝对不记录 (比如 Water)")]
    public string unsafeTag = "Water";

    // 内部变量
    private Vector3 _lastSafePosition;
    private Quaternion _lastSafeRotation;
    private Rigidbody _rb;
    private CharacterController _cc;

    // 计时器
    private float _groundedTimer = 0f;

    void Start()
    {
        _lastSafePosition = transform.position;
        _lastSafeRotation = transform.rotation;
        _rb = GetComponent<Rigidbody>();
        _cc = GetComponent<CharacterController>();
    }

    void Update()
    {
        CheckAndRecordPosition();
    }

    void CheckAndRecordPosition()
    {
        RaycastHit hit;
        Vector3 origin = transform.position + Vector3.up * checkRadius;
        Vector3 direction = Vector3.down;

        bool isGroundedRaw = Physics.SphereCast(origin, checkRadius, direction, out hit, checkDistance, groundLayer);

        if (isGroundedRaw)
        {
            // 1. 坡度检查
            float angle = Vector3.Angle(Vector3.up, hit.normal);
            if (angle > maxSlopeAngle)
            {
                ResetSafeTimer(); // 坡度太陡，重置计时，视为不稳定
                return;
            }

            // 2. 标签检查 (防止记录水面)
            if (hit.collider.CompareTag(unsafeTag))
            {
                ResetSafeTimer(); // 是水面，重置计时
                return;
            }

            // === 核心修复逻辑 ===
            // 只有当持续检测到地面时，计时器才增加
            _groundedTimer += Time.deltaTime;

            // 只有稳定站立超过阈值，才更新存档点
            if (_groundedTimer > safeTimeThreshold)
            {
                _lastSafePosition = transform.position;
                _lastSafeRotation = transform.rotation;
                // 注意：这里不要重置计时器，保持它大于阈值，这样只要一直站着就能持续更新
            }
        }
        else
        {
            // 一旦悬空 (掉出悬崖的瞬间)，立即重置计时器
            // 这样刚才那一瞬间的"边缘位置"就不会被记录下来
            ResetSafeTimer();
        }
    }

    void ResetSafeTimer()
    {
        _groundedTimer = 0f;
    }

    public void Respawn()
    {
        if (_cc != null) _cc.enabled = false;

        // 停止物理
        if (_rb != null)
        {
            _rb.linearVelocity = Vector3.zero; // Unity 6 写法，旧版用 velocity
            _rb.angularVelocity = Vector3.zero;
            _rb.Sleep();
        }

        transform.position = _lastSafePosition;
        transform.rotation = _lastSafeRotation;

        // 防穿模微调
        transform.position += Vector3.up * 0.2f;

        if (_cc != null) _cc.enabled = true;

        // 复活后重置计时器，防止复活瞬间再次记录
        ResetSafeTimer();

        Debug.Log("已复活至最近的安全平台");
    }

    void OnDrawGizmos()
    {
        Gizmos.color = _groundedTimer > safeTimeThreshold ? Color.green : Color.red;
        Vector3 origin = transform.position + Vector3.up * checkRadius;
        Gizmos.DrawWireSphere(origin + Vector3.down * checkDistance, checkRadius);
    }
}
