using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NavMeshBrain : MonoBehaviour
{
    [Header("Target")]
    public Transform target; // 玩家

    [Header("Settings")]
    public float updatePathRate = 0.2f; // 寻路刷新频率

    private NavMeshAgent agent;
    private Coroutine followCoroutine;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        // 关键设置：禁止 Agent 自动更新旋转，
        // 如果我们想要那种“面条人”独特的旋转手感，最好自己控制 Rotation
        // 但为了先跑通，我们先允许它自动旋转
        agent.updateRotation = true;
        agent.updatePosition = true;

        if (target != null)
        {
            StartChasing();
        }
    }

    public void StartChasing()
    {
        if (followCoroutine != null) StopCoroutine(followCoroutine);
        followCoroutine = StartCoroutine(ChaseRoutine());
    }

    IEnumerator ChaseRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(updatePathRate);

        while (target != null)
        {
            // === 核心代码就这一行 ===
            // SetDestination 会自动进行 A* 计算、路径平滑、避障计算
            agent.SetDestination(target.position);

            yield return wait;
        }
    }

    // 可以在这里获取速度，传递给之后的“程序化动画脚本”
    void Update()
    {
        // 例子：获取当前实际移动速度 (0 ~ Speed)
        // float currentSpeed = agent.velocity.magnitude;
        // proceduralAnimator.SetSpeed(currentSpeed);
    }
}
