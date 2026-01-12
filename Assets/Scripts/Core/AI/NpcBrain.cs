using UnityEngine;
using UnityEngine.AI;
using System.Collections;

// 强制要求物体必须有 NavMeshAgent 和 NpcProceduralAnimation 组件
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(NpcProceduralAnimation))]
[RequireComponent(typeof(HealthController))] // 新增依赖
public class NpcBrain : MonoBehaviour
{
    // === 1. 身份定义 ===
    public enum NpcType { Civilian, Guard, Bandit, Target }
    public enum AIState { Idle, Alert, Combat, Flee, Cower, Dead }

    [Header("=== 身份档案 ===")]
    public NpcType npcType = NpcType.Civilian;
    [Tooltip("是否胆小 (平民专用：True=逃跑，False=抱头蹲防)")]
    public bool isCoward = true;
    public float shakeIntensityWhenCower = 0.1f; // 警戒时的抖动强度
    private NpcWeaponController weaponController; // 新增引用
    [Header("=== 感知参数 ===")]
    private HealthController health; // 新增引用
    public float sightRange = 15f;    // 视野距离
    public float attackRange = 8f;    // 攻击/逃跑判定距离
    public float hearingRange = 20f;  // === 新增：听觉范围 (守卫/平民用) ===
    public float safeDistance = 20f;  // 逃跑安全距离
    //public LayerMask whatIsPlayer;    // 玩家图层

    [Header("=== 移动参数 ===")]
    [Tooltip("勾选后，NPC 在闲逛状态下会站在原地不动")]
    public bool isStationary = false; // === 新增 ===
    public float walkSpeed = 3.5f;
    public float runSpeed = 7.0f;
    public float wanderRadius = 10f;  // 闲逛半径

    [Header("=== 状态监视 (只读) ===")]
    [SerializeField] private AIState currentState = AIState.Idle;
    [SerializeField] private Transform playerTarget;

    [Header("Drop Settings")]
    public GameObject questItemPrefab; // 只有 Target 需要赋值这个

    // 组件引用
    private NavMeshAgent agent;
    private NpcProceduralAnimation anim;
    private float stateTimer = 0f;    // 通用计时器
    private float defaultBodyHeight;  // 记录初始身体高度

    // 模拟玩家状态 (实际开发中应从 Player 脚本获取)
    private bool playerIsHoldingGun = true; // 假设玩家总是拿着枪

    IEnumerator Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<NpcProceduralAnimation>();
        weaponController = GetComponent<NpcWeaponController>();
        defaultBodyHeight = anim.bodyBaseHeight;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj) playerTarget = playerObj.transform;

        health = GetComponent<HealthController>();
        if (health != null)
        {
            health.OnDeath += HandleDeath;
            health.OnTakeDamage += HandleTakeDamage;
            health.teamID = 1;
        }

        // === 关键修改：等待一帧 ===
        // 让 NavMeshAgent 有时间吸附到地面网格上
        yield return null;

        // 双重保险：只有当代理真的在网格上时，才进入状态
        if (agent.isOnNavMesh)
        {
            EnterState(AIState.Idle);
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} 未能吸附到 NavMesh，请检查出生点位置或 NavMesh 烘焙！");
            // 尝试强制吸附到最近的点（可选）
            /* NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 5.0f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
                agent.Warp(hit.position);
                EnterState(AIState.Idle);
            }
            */
        }
    }

    // === 新增：订阅/注销 枪声事件 ===
    void OnEnable()
    {
        WeaponBase.OnGlobalPlayerFire += ReactToGunshot;
    }

    void OnDisable()
    {
        WeaponBase.OnGlobalPlayerFire -= ReactToGunshot;
    }

    // === 新增：死亡处理 ===
    void HandleDeath()
    {
        Debug.Log("NPC 死亡！");
        EnterState(AIState.Dead);

        // 禁用 AI 和 动画脚本
        agent.enabled = false;
        if (anim != null) anim.enabled = false;

        // 开启布娃娃系统 (如果有) 或 播放死亡动画
        // 简单的物理倒地效果：
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.AddForce(Random.insideUnitSphere * 5f, ForceMode.Impulse);
        }

        // 如果手里有枪，把枪丢掉 (可选)
        if (weaponController != null)
        {
            // 这里可以写丢枪逻辑
            weaponController.SetWeaponVisible(false);
        }
        if (npcType == NpcType.Target && questItemPrefab != null)
        {
            // 检查是否是当前任务的目标 (防止提前去别的平台把怪杀了)
            // 简单处理：只要是 Target 就掉，能不能捡看 GameManager 状态
            // 更好处理：PlatformController 激活时才算数
            Instantiate(questItemPrefab, transform.position + Vector3.up, Quaternion.identity);
        }
        Destroy(this.gameObject); 
    }

    // === 新增：挨打反击逻辑 ===
    void HandleTakeDamage(float amount)
    {
        if (currentState == AIState.Dead) return;

        // 如果在发呆，挨打了立刻进入战斗或逃跑
        if (currentState == AIState.Idle)
        {
            if (isCoward) EnterState(AIState.Flee);
            else EnterState(AIState.Combat);
        }
    }
    // === 新增：枪声反应逻辑 ===
    void ReactToGunshot(Vector3 shotPosition)
    {
        // 1. 死人或已经在逃跑/战斗的人不需要再反应 (避免逻辑打架)
        if (currentState == AIState.Dead) return;

        // 2. 距离检测
        float distToShot = Vector3.Distance(transform.position, shotPosition);
        if (distToShot > hearingRange) return;

        // 3. 根据身份反应
        switch (npcType)
        {
            case NpcType.Guard:
                // 守卫：听到枪声 -> 战斗
                if (currentState != AIState.Combat)
                {
                    Debug.Log($"{gameObject.name} (Guard): 听到枪声，进入战斗！");
                    EnterState(AIState.Combat);
                }
                break;

            case NpcType.Bandit:
                // 土匪：听到枪声 -> 战斗 (支援队友)
                if (currentState != AIState.Combat)
                {
                    Debug.Log($"{gameObject.name} (Bandit): 听到枪声，加入战斗！");
                    EnterState(AIState.Combat);
                }
                break;

            case NpcType.Civilian:
                // 平民：听到枪声 -> 吓坏了 (逃跑或抱头)
                if (currentState != AIState.Flee && currentState != AIState.Cower)
                {
                    Debug.Log($"{gameObject.name} (Civilian): 听到枪声，吓坏了！");
                    EnterState(isCoward ? AIState.Flee : AIState.Cower);
                }
                break;

            case NpcType.Target:
                // === 关键修改：Target 的分化逻辑 ===
                if (currentState != AIState.Combat && currentState != AIState.Flee && currentState != AIState.Cower)
                {
                    if (isCoward)
                    {
                        // 胆小的目标 -> 逃跑 (Target通常比平民更惜命，所以优先Flee)
                        Debug.Log($"{gameObject.name} (Target): 听到枪声，逃跑！");
                        EnterState(AIState.Flee);
                    }
                    else
                    {
                        // 凶悍的目标 -> 反击
                        Debug.Log($"{gameObject.name} (Target): 听到枪声，反击！");
                        EnterState(AIState.Combat);

                        // 确保有枪 (虽然 Start 时可能已经生成，但这里是双保险)
                        if (weaponController != null)
                        {
                            // 这里假设 WeaponController 有个 CheckAndEquip 方法
                            // 或者你的 WeaponController 在 Start 就已经把枪生成好了
                        }
                    }
                }
                break;
        }
    }

    void Update()
    {
        if (!playerTarget) return;

        // 1. 全局感知
        float distToPlayer = Vector3.Distance(transform.position, playerTarget.position);
        bool canSeePlayer = distToPlayer < sightRange; // 这里可以加射线检测做视线遮挡

        // 2. 状态机流转 (核心决策大脑)
        switch (currentState)
        {
            case AIState.Idle:
                Logic_Idle(distToPlayer, canSeePlayer);
                break;
            case AIState.Alert:
                Logic_Alert(distToPlayer);
                break;
            case AIState.Combat:
                Logic_Combat(distToPlayer);
                break;
            case AIState.Flee:
                Logic_Flee(distToPlayer);
                break;
            case AIState.Cower:
                Logic_Cower(distToPlayer);
                break;
        }

        // 3. 执行当前状态行为
        ExecuteStateBehavior();
    }

    // =========================================================
    // 逻辑判定层 (Logic Layer) - 决定“是否切换状态”
    // =========================================================

    void Logic_Idle(float dist, bool see)
    {
        // 游荡逻辑在 Execute 里做，这里只管切换

        if (!see) return;

        // 1. 土匪：依然保留“看到就打”的野蛮逻辑
        if (npcType == NpcType.Bandit)
        {
            EnterState(AIState.Combat);
        }
        // 2. 守卫：看到玩家不再攻击，只做简单的注视（Alert），除非听到枪声（在事件里处理）
        else if (npcType == NpcType.Guard)
        {
            // 如果你想让守卫看到玩家拿枪就进入警戒(Alert)，保留这行
            // 如果你想让他完全无视，除非开枪，就删掉这行
            if (playerIsHoldingGun) EnterState(AIState.Alert);
        }
        // 3. 平民：完全移除距离判定
        // else if (npcType == NpcType.Civilian)... { } // 这一段被删除了
        // 平民现在看到玩家不会有任何反应，除非玩家开枪
    }

    void Logic_Alert(float dist)
    {
        // 守卫警戒中...
        // 如果玩家离得太近，或者开枪了(需扩展)，转为战斗
        if (dist > sightRange * 1.2f)
        {
            EnterState(AIState.Idle);
        }
    }

    void Logic_Combat(float dist)
    {
        // 战斗中... 
        // 如果玩家跑得没影了，变回 Idle (或者去最后消失点搜寻，这里简化)
        if (dist > sightRange * 1.5f)
        {
            EnterState(AIState.Idle);
        }
    }

    void Logic_Flee(float dist)
    {
        // 逃跑中... 跑得够远了就停下喘口气
        if (dist > safeDistance)
        {
            EnterState(AIState.Idle);
        }
    }

    void Logic_Cower(float dist)
    {
        // 抱头中... 玩家走远了才敢站起来
        if (dist > safeDistance) // 硬编码一个安全距离，或者复用 safeDistance
        {
            EnterState(AIState.Idle);
        }
    }

    // =========================================================
    // 行为执行层 (Execution Layer) - 真正控制身体
    // =========================================================

    void EnterState(AIState newState)
    {
        currentState = newState;
        stateTimer = 0f;

        if (anim != null) anim.currentShakeIntensity = 0f;

        switch (newState)
        {
            case AIState.Idle:
                agent.speed = walkSpeed;

                // === 修改：如果是站桩模式，直接停止移动 ===
                if (isStationary)
                {
                    agent.isStopped = true;
                    agent.velocity = Vector3.zero; // 彻底停下
                }
                else
                {
                    agent.isStopped = false;
                }
                // ==========================================

                agent.updateRotation = true;
                anim.SetAimTarget(null);
                anim.bodyBaseHeight = defaultBodyHeight;
                break;

            case AIState.Alert:
                agent.isStopped = true;
                agent.updateRotation = false;
                anim.SetAimTarget(null);
                anim.bodyBaseHeight = defaultBodyHeight;
                break;

            case AIState.Combat:
                agent.speed = runSpeed;
                agent.isStopped = false;
                agent.updateRotation = true;
                anim.SetAimTarget(playerTarget);
                anim.bodyBaseHeight = defaultBodyHeight;
                if (weaponController != null)
                {
                    weaponController.SetWeaponVisible(true);
                }
                break;

            case AIState.Flee:
                agent.speed = runSpeed * 1.2f;
                agent.isStopped = false;
                agent.updateRotation = true;
                anim.SetAimTarget(null);
                anim.bodyBaseHeight = defaultBodyHeight;
                break;

            case AIState.Cower:
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                anim.SetAimTarget(null);
                anim.bodyBaseHeight = defaultBodyHeight * 0.4f;
                // 开启发抖
                if (anim != null) anim.currentShakeIntensity = 0.2f;
                break;
            case AIState.Dead:
                // 已经在 HandleDeath 处理了，这里留空或做清理
                break;
        }
    }

    void ExecuteStateBehavior()
    {
        stateTimer += Time.deltaTime;

        switch (currentState)
        {
            case AIState.Idle:
                // === 新增：如果是站桩模式，直接跳过巡逻逻辑 ===
                if (isStationary)
                {
                    // 这里可以加一点随机旋转或者看来看去的逻辑，
                    // 但目前先让它老实站着
                    return;
                }
                // ==========================================

                // 简单的随机游荡 (原有逻辑)
                if (!agent.pathPending && agent.remainingDistance < 0.5f)
                {
                    if (stateTimer > 2f)
                    {
                        Vector3 randDest = GetRandomNavPoint(transform.position, wanderRadius);
                        if (randDest != Vector3.zero) agent.SetDestination(randDest);
                        stateTimer = 0f;
                    }
                }
                break;

            case AIState.Alert:
                RotateTowards(playerTarget.position);
                break;

            case AIState.Combat:
                float dist = Vector3.Distance(transform.position, playerTarget.position);

                // 1. 移动逻辑
                if (dist > attackRange * 0.8f)
                {
                    agent.isStopped = false;
                    agent.SetDestination(playerTarget.position);
                    agent.updateRotation = true;
                }
                else
                {
                    agent.isStopped = true;
                    agent.updateRotation = false;
                    RotateTowards(playerTarget.position);

                    // 2. === 新增：开火逻辑 ===
                    if (weaponController != null)
                    {
                        weaponController.TryShoot(playerTarget);
                    }
                }
                break;

            case AIState.Flee:
                if (stateTimer > 0.5f)
                {
                    Vector3 runDir = transform.position - playerTarget.position;
                    Vector3 fleePos = transform.position + runDir.normalized * 10f;
                    agent.SetDestination(fleePos);
                    stateTimer = 0f;
                }
                break;

            case AIState.Cower:
                // 保持发抖
                if (anim != null) anim.currentShakeIntensity = 0.2f;
                break;
        }
    }

    // =========================================================
    // 辅助工具方法
    // =========================================================

    // 在 NavMesh 上找随机点
    Vector3 GetRandomNavPoint(Vector3 origin, float dist)
    {
        Vector3 randDir = Random.insideUnitSphere * dist;
        randDir += origin;
        NavMeshHit navHit;
        if (NavMesh.SamplePosition(randDir, out navHit, dist, NavMesh.AllAreas))
        {
            return navHit.position;
        }
        return Vector3.zero; // 没找到
    }

    // 平滑旋转
    void RotateTowards(Vector3 targetPos)
    {
        Vector3 direction = (targetPos - transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            Quaternion lookRot = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Lerp(transform.rotation, lookRot, Time.deltaTime * 5f);
        }
    }
}