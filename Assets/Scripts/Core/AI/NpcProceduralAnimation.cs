using UnityEngine;
using UnityEngine.AI;

public class NpcProceduralAnimation : MonoBehaviour
{
    [Header("=== 核心部件引用 ===")]
    public Transform body;
    public Transform head;
    public LineRenderer legL, legR;
    public LineRenderer armL, armR;

    [Header("=== 身体参数调整 ===")]
    [Tooltip("身体离地面的高度")]
    public float bodyBaseHeight = 0.8f;

    [Tooltip("头部相对于身体的向上偏移")]
    public float headHeightOffset = 0.6f;

    [Tooltip("肩膀宽度")]
    public float shoulderWidth = 0.4f;

    [Tooltip("手臂长度")]
    public float armLength = 0.7f;

    [Tooltip("两腿之间的宽度")]
    public float hipWidth = 0.3f;

    [Tooltip("腿的最大长度")]
    public float legLength = 0.9f;

    [Header("=== 动画参数 ===")]
    public float walkSpeed = 10f;
    public float stepHeight = 0.3f;
    public float strideLength = 0.6f;
    public float bodyBobAmount = 0.05f;

    [Tooltip("地面图层 (必须设置!)")]
    public LayerMask groundLayer = 1; // Default

    [Header("=== 运行时数据 (自动) ===")]
    public Transform aimTarget;

    [Header("=== 外部控制参数 (由Brain控制) ===")]
    [Tooltip("抖动强度 (0=不抖, >0=瑟瑟发抖)")]
    public float currentShakeIntensity = 0f;

    [Header("=== 武器挂点 (新增) ===")]
    [Tooltip("在 Hierarchy 中创建一个空物体作为右手，拖到这里")]
    public Transform rightHandAnchor;

    private NavMeshAgent agent;
    private float walkCycle = 0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        // 强制开启世界坐标，确保线条正确渲染
        if (legL) legL.useWorldSpace = true;
        if (legR) legR.useWorldSpace = true;
        if (armL) armL.useWorldSpace = true;
        if (armR) armR.useWorldSpace = true;
    }

    void Update()
    {
        if (agent == null) return;

        float currentSpeed = agent.velocity.magnitude;
        bool isMoving = currentSpeed > 0.05f;

        // 更新行走循环
        if (isMoving)
        {
            walkCycle += Time.deltaTime * walkSpeed * (currentSpeed / (agent.speed + 0.1f));
        }
        else
        {
            // 站立时平滑重置
            walkCycle = Mathf.Lerp(walkCycle, 0, Time.deltaTime * 10f);
        }

        AnimateBody(isMoving);
        AnimateLegs(isMoving);
        AnimateArms();
    }

    void AnimateBody(bool isMoving)
    {
        float frequency = isMoving ? 10f : 2f;
        float amplitude = isMoving ? bodyBobAmount : bodyBobAmount * 0.5f;
        float bob = Mathf.Sin(Time.time * frequency) * amplitude;

        // === 1. 计算身体抖动 ===
        // 使用 PerlinNoise 生成连续的随机抖动
        // 乘以一个系数 (比如 50f) 来控制抖动频率
        float shakeX = (Mathf.PerlinNoise(Time.time * 50f, 0f) - 0.5f) * currentShakeIntensity;
        float shakeZ = (Mathf.PerlinNoise(0f, Time.time * 50f) - 0.5f) * currentShakeIntensity;
        Vector3 bodyShakeOffset = new Vector3(shakeX, 0, shakeZ);

        // 应用身体位置 (基础高度 + 呼吸 + 抖动)
        body.localPosition = new Vector3(0, bodyBaseHeight + bob, 0) + bodyShakeOffset;

        // === 2. 计算头部位置与抖动 (修复点：加入头部抖动) ===
        Vector3 targetHeadPos = body.position + (transform.up * headHeightOffset);
        // 先平滑跟随
        head.position = Vector3.Lerp(head.position, targetHeadPos, Time.deltaTime * 20f);

        // 如果在发抖，额外给头部叠加一个高频抖动
        if (currentShakeIntensity > 0.001f)
        {
            // 使用不同的采样偏移 (+10f, +20f) 和稍高的频率 (* 60f)，让头和身体抖动不同步
            float headShakeX = (Mathf.PerlinNoise(Time.time * 60f + 10f, 0f) - 0.5f) * currentShakeIntensity * 0.8f;
            float headShakeZ = (Mathf.PerlinNoise(0f, Time.time * 60f + 20f) - 0.5f) * currentShakeIntensity * 0.8f;
            // 直接叠加到最终位置上
            head.position += new Vector3(headShakeX, 0, headShakeZ);
        }
    }

    void AnimateLegs(bool isMoving)
    {
        UpdateLeg(legL, 0, -1, isMoving);
        UpdateLeg(legR, Mathf.PI, 1, isMoving);
    }

    void UpdateLeg(LineRenderer lr, float cycleOffset, float sideSign, bool isMoving)
    {
        if (lr == null) return;

        // 起点：髋关节 (随身体抖动)
        Vector3 hipPos = body.position + (transform.right * sideSign * hipWidth * 0.5f) + (Vector3.down * 0.1f);
        Vector3 finalFootPos;

        if (isMoving)
        {
            // --- 移动状态 ---
            float sinVal = Mathf.Sin(walkCycle + cycleOffset);
            float cosVal = Mathf.Cos(walkCycle + cycleOffset);

            Vector3 moveDir = transform.forward;
            if (agent.velocity.sqrMagnitude > 0.01f) moveDir = agent.velocity.normalized;

            Vector3 stepOffset = moveDir * sinVal * strideLength;
            float lift = Mathf.Abs(cosVal) * stepHeight;

            // 射线检测地面
            Vector3 rayOrigin = hipPos + (Vector3.up * 0.5f) + stepOffset;
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, legLength * 2f, groundLayer))
            {
                float height = hit.point.y + lift;
                finalFootPos = hit.point;
                finalFootPos.y = height;
            }
            else
            {
                finalFootPos = hipPos + (Vector3.down * legLength) + stepOffset;
                finalFootPos.y = transform.position.y - legLength * 0.5f + lift;
            }
        }
        else
        {
            // --- 站立/蹲下状态 ---
            Vector3 standPos = hipPos + (Vector3.down * legLength);

            Vector3 rayOrigin = hipPos + (Vector3.up * 0.5f);

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, legLength * 2f, groundLayer))
            {
                standPos.y = hit.point.y;
                // 可选：锁定 X/Z 以防止脚滑动，注释掉则脚会轻微跟随身体晃动
                standPos.x = hit.point.x;
                standPos.z = hit.point.z;
            }
            finalFootPos = standPos;
        }

        lr.SetPosition(0, hipPos);
        lr.SetPosition(1, finalFootPos);
    }

    void AnimateArms()
    {
        bool isAiming = (aimTarget != null);
        UpdateArm(armL, -1, isAiming);
        UpdateArm(armR, 1, isAiming);
    }

    void UpdateArm(LineRenderer lr, float sideSign, bool aiming)
    {
        if (lr == null) return;

        Vector3 shoulderPos = body.position + (transform.right * sideSign * shoulderWidth * 0.5f) + (Vector3.up * 0.1f);
        Vector3 handPos;

        if (aiming)
        {
            // --- 1. 瞄准状态 ---
            Vector3 dirToTarget = (aimTarget.position - shoulderPos).normalized;
            // 瞄准时加一点点抖动
            Vector3 aimShake = Random.insideUnitSphere * currentShakeIntensity * 0.3f;
            handPos = shoulderPos + dirToTarget * armLength + aimShake;
        }
        else if (currentShakeIntensity > 0.01f)
        {
            // --- 2. 畏缩状态 ---
            Vector3 surrenderDir = (transform.up * 1.5f + transform.forward * 0.5f + transform.right * sideSign * 0.3f).normalized;
            Vector3 fearShake = Random.insideUnitSphere * currentShakeIntensity * 1.2f;
            handPos = shoulderPos + (surrenderDir * armLength * 0.8f) + fearShake;
        }
        else
        {
            // --- 3. 下垂状态 ---
            float swing = Mathf.Sin(walkCycle + (sideSign > 0 ? 0 : Mathf.PI)) * 0.2f;
            Vector3 idleShake = Random.insideUnitSphere * currentShakeIntensity;
            handPos = shoulderPos + (Vector3.down * armLength) + (transform.forward * swing) + idleShake;
        }

        // === 新增：同步右手挂点的位置和旋转 ===
        // 如果是右手 (sideSign > 0 为右，具体看你的 UpdateArm 调用传参)
        // 之前我们在 AnimateArms 里调用 UpdateArm(armR, 1, isAiming); 所以 1 是右手
        if (sideSign > 0 && rightHandAnchor != null)
        {
            rightHandAnchor.position = handPos;

            // 计算手部旋转：如果是瞄准，枪口朝向目标；否则自然下垂或随手臂方向
            if (aiming)
            {
                rightHandAnchor.LookAt(aimTarget.position);
            }
            else
            {
                // 让枪自然垂下，或者跟随手臂向量
                rightHandAnchor.rotation = Quaternion.LookRotation(handPos - shoulderPos);
            }
        }

        lr.SetPosition(0, shoulderPos);
        lr.SetPosition(1, handPos);
    }

    public void SetAimTarget(Transform target)
    {
        aimTarget = target;
    }
}