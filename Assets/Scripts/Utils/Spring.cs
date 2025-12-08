using UnityEngine;


/// <summary>
/// 通用二阶弹簧系统
/// 模拟 F = -kx - dv (胡克定律 + 阻尼)
/// </summary>
[System.Serializable]
public class Spring
{
    // --- 参数 (对应你的文档) ---
    [Tooltip("刚度：决定回弹速度。值越大越弹。")]
    public float stiffness = 150f;
    [Tooltip("阻尼：决定停下来的速度。值越小越晃。")]
    public float damping = 10f;
    [Tooltip("模拟质量：越重越难推动。")]
    public float mass = 1f;

    // --- 内部状态 ---
    private Vector3 currentPos;
    private Vector3 currentVelocity;
    private Vector3 targetPos;

    // 初始化
    public void Init()
    {
        currentPos = Vector3.zero;
        currentVelocity = Vector3.zero;
        targetPos = Vector3.zero;
    }

    // 每一帧更新 (通常在 LateUpdate 调用)
    public Vector3 Update(float deltaTime)
    {
        // 力的累加
        Vector3 force = -stiffness * (currentPos - targetPos) - damping * currentVelocity;

        // F = ma => a = F/m
        Vector3 acceleration = force / mass;

        // 积分更新速度和位置
        currentVelocity += acceleration * deltaTime;
        currentPos += currentVelocity * deltaTime;

        return currentPos;
    }

    // 施加冲量 (例如：开火时的后坐力)
    public void AddForce(Vector3 force)
    {
        currentVelocity += force / mass;
    }

    // 设定目标点 (例如：Sway 导致的滞后位置)
    public void SetTarget(Vector3 target)
    {
        targetPos = target;
    }
}
