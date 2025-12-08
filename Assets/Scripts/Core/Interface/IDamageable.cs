using UnityEngine;

/// <summary>
/// 万能伤害接口。
/// 只要物体挂载了实现此接口的脚本就可以互动受到伤害
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// 受到伤害时调用
    /// </summary>
    /// <param name="damage">伤害数值</param>
    /// <param name="hitPoint">击中点 (用于生成特效)</param>
    /// <param name="hitNormal">击中法线 (用于确定击退方向)</param>
    void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitNormal);
}
