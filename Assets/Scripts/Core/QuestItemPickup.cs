using UnityEngine;

public class QuestItemPickup : MonoBehaviour
{
    [Header("设置")]
    [Tooltip("玩家走进几米范围内显示提示")]
    [SerializeField] private float detectionRadius = 3.0f;

    private Transform playerTransform;

    private void Start()
    {
        // 获取玩家引用
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player) playerTransform = player.transform;
    }

    private void Update()
    {
        if (playerTransform == null) return;

        // 1. 计算距离
        float dist = Vector3.Distance(transform.position, playerTransform.position);

        // 2. 如果在范围内
        if (dist < detectionRadius)
        {
            // A. 持续刷新提示
            // 只要玩家在范围内，每一帧都发送 0.2秒 的提示。
            // 这样提示会一直留在屏幕上；一旦离开，0.2秒后提示自动消失。
            HUDController.Instance.ShowNotification("[E] PICK UP PROOF", 0.2f);

            // B. 检测按键输入
            if (Input.GetKeyDown(KeyCode.E))
            {
                PickUp();
            }
        }
    }

    private void PickUp()
    {
        // 1. 通知 GameManager (逻辑核心)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CollectQuestItem();
        }

        // 2. 显示拾取成功的反馈 (覆盖掉刚才的 "Press E" 提示)
        // 给它 2秒 显示时间，告诉玩家拿到东西了
        if (HUDController.Instance != null)
        {
            HUDController.Instance.ShowNotification("QUEST ITEM COLLECTED!", 2f);
        }

        // 3. 播放音效 (可选)
        // AudioSource.PlayClipAtPoint(pickupSound, transform.position);

        // 4. 销毁物体
        Destroy(gameObject);
    }

    // 编辑器辅助显示范围
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }

}
