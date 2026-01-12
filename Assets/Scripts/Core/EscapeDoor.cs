using UnityEngine;

public class EscapeDoor : MonoBehaviour
{
    [Header("配置")]
    [Tooltip("玩家进入多少米范围内显示提示")]
    [SerializeField] private float detectionRadius = 5.0f;

    [Header("UI")]
    [Tooltip("门上方的 3D 文字 (TextMesh)")]
    [SerializeField] private TextMesh promptText;

    private Transform playerTransform;
    private bool isPlayerInRange = false;

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player) playerTransform = player.transform;

        if (promptText) promptText.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (playerTransform == null) return;

        // 1. 距离检测
        float dist = Vector3.Distance(transform.position, playerTransform.position);
        bool wasInRange = isPlayerInRange;
        isPlayerInRange = dist < detectionRadius;

        // 2. 状态变化时开关文字
        if (isPlayerInRange != wasInRange)
        {
            if (promptText) promptText.gameObject.SetActive(isPlayerInRange);
        }

        // 3. 范围内的实时逻辑
        if (isPlayerInRange)
        {
            UpdateTextContent();
            HandleInput();
        }
    }

    private void UpdateTextContent()
    {
        if (promptText == null) return;

        // 检查是否满足逃离条件 (直接问 GameManager)
        // 也可以用 GameManager.Instance.canEscape 变量
        bool canEscape = GameManager.Instance.currentMoney >= GameManager.Instance.targetMoney;

        if (canEscape)
        {
            promptText.text = "<color=green>ESCAPE CITY</color>\n[PRESS E]";
            promptText.color = Color.green;
        }
        else
        {
            int lacking = GameManager.Instance.targetMoney - GameManager.Instance.currentMoney;
            promptText.text = $"<color=red>LOCKED</color>\nNeed ${lacking}";
            promptText.color = Color.red;
        }

        // 可选：让文字始终朝向玩家 (Billboard)
        promptText.transform.rotation = Quaternion.LookRotation(promptText.transform.position - Camera.main.transform.position);
    }

    private void HandleInput()
    {
        // 只有在范围内按下 E 键
        if (Input.GetKeyDown(KeyCode.E))
        {
            bool canEscape = GameManager.Instance.canEscape;

            if (canEscape)
            {
                // 触发结局
                GameManager.Instance.TriggerGameOver();
            }
            else
            {
                // 钱不够时的反馈 (比如播放个错误音效)
                HUDController.Instance.ShowNotification("You Dont have enough credit to leave this school", 4f);
                Debug.Log("钱不够，无法撤离！");
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = isPlayerInRange ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
