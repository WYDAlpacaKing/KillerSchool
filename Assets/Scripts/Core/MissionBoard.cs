using UnityEngine;

public class MissionBoard : MonoBehaviour
{
    [Header("配置")]
    [Tooltip("对应 GameManager 中 allMissions 的索引 (0, 1, 2)")]
    [SerializeField] private int missionIndex = 0;

    [Header("检测设置")]
    [SerializeField] private float detectionRadius = 4.0f;

    // 内部变量
    private Transform playerTransform;
    private bool isPlayerInRange = false;

    private void Start()
    {
        // 1. 找玩家
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player)
        {
            playerTransform = player.transform;
            Debug.Log($"[MissionBoard] 面板 {gameObject.name} 成功找到玩家: {player.name}");
        }
        else
        {
            Debug.LogError($"[MissionBoard] 面板 {gameObject.name} 找不到 Tag 为 'Player' 的物体！请检查设置！");
        }
    }

    private void Update()
    {
        if (playerTransform == null) return;

        // 2. 算距离
        float dist = Vector3.Distance(transform.position, playerTransform.position);

        // 状态切换检测
        bool currentlyInRange = dist < detectionRadius;

        if (currentlyInRange != isPlayerInRange)
        {
            isPlayerInRange = currentlyInRange;
            if (isPlayerInRange) HUDController.Instance.ShowNotification("Press E to Recieve Task!",2f);
            else Debug.Log($"[MissionBoard] <<< 离开面板 {missionIndex} 范围");
        }

        // 3. 核心交互逻辑
        if (isPlayerInRange)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                Debug.Log($"[MissionBoard] 检测到 E 键按下！正在判定任务逻辑...");
                HandleInteraction();
            }
        }
    }

    private void HandleInteraction()
    {
        // 防空检查
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager 实例不存在！");
            return;
        }

        // 1. 获取当前面板代表的任务配置 (菜单上的菜)
        if (missionIndex >= GameManager.Instance.allMissions.Length)
        {
            Debug.LogError("MissionBoard 索引越界！请检查 Inspector 设置的数字是否过大。");
            return;
        }
        MissionData boardMission = GameManager.Instance.allMissions[missionIndex];

        // 2. 获取主角当前正在做的任务配置 (手里的账单)
        // 注意：这里改用了 CurrentMissionConfig
        MissionData activeMission = GameManager.Instance.CurrentMissionConfig;

        // 3. 检查这个面板的任务是否已经做完了
        // 注意：这里不再访问 isCompleted，而是去列表里查 ID
        bool isThisMissionFinished = GameManager.Instance.completedMissionIDs.Contains(boardMission.missionID);

        // --- 逻辑分支 ---

        // 分支 A: 尝试交付
        // 判定条件：手上有任务 + 手上的任务ID等于面板的任务ID + 有物品
        if (activeMission != null &&
            activeMission.missionID == boardMission.missionID &&
            GameManager.Instance.hasQuestItem)
        {
            Debug.Log("条件满足：尝试交付任务...");
            if (GameManager.Instance.CompleteCurrentMission())
            {
                Debug.Log("<color=green>任务交付成功！收到钱了！</color>");
                HUDController.Instance.ShowNotification("Completed! +300$", 4f);
                AudioManager.Instance.PlaySound2D(AudioManager.Instance.Money, 1f);
            }
        }
        // 分支 B: 尝试接取
        // 判定条件：手上没任务 + 这个任务没做过
        else if (activeMission == null && !isThisMissionFinished)
        {
            Debug.Log("条件满足：尝试接取任务...");

            // 注意：这里现在只传 ID (String)，不传对象
            if (GameManager.Instance.AcceptMission(boardMission.missionID))
            {
                Debug.Log($"<color=cyan>任务接取成功：{boardMission.targetName}</color>");
                HUDController.Instance.ShowNotification("Got the mission", 4f);
            }
        }
        // 分支 C: 各种失败原因分析
        else
        {
            Debug.LogWarning("交互无效，原因分析：");

            if (isThisMissionFinished)
                HUDController.Instance.ShowNotification("This Quest already completed!", 1f);

            else if (activeMission != null && activeMission.missionID != boardMission.missionID)
                HUDController.Instance.ShowNotification("You already got a mission!", 1f); 

            else if (activeMission != null && activeMission.missionID == boardMission.missionID && !GameManager.Instance.hasQuestItem)
                HUDController.Instance.ShowNotification("Go and take the quest item back!", 1f);

            else
                Debug.Log("- 未知状态。");
        }
    }

    // 画圈圈，方便你看范围
    private void OnDrawGizmos()
    {
        Gizmos.color = isPlayerInRange ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
