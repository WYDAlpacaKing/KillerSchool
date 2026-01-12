using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;


[System.Serializable]
public class MissionData
{
    // 这些是静态配置，游戏运行过程中绝对不要改它们！
    public string missionID;      // 比如 "M01"
    public string targetName;
    public string locationName;
    public int reward;

    // 如果是场景里的物体，引用没问题，但不要反向在物体里改这个Data
    public PlatformController targetPlatform;
}
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Base Platform")]
    public PlatformController homeBasePlatform;

    [Header("Mission Settings")]
    public MissionData[] allMissions;

    [Header("Runtime State (Dynamic)")]
    public string currentMissionID = ""; // 只存ID
    public List<string> completedMissionIDs = new List<string>(); // 已完成列表

    // === 关键属性：通过 ID 动态获取当前任务的配置 ===
    public MissionData CurrentMissionConfig
    {
        get
        {
            if (string.IsNullOrEmpty(currentMissionID)) return null;
            return allMissions.FirstOrDefault(m => m.missionID == currentMissionID);
        }
    }

    public bool hasQuestItem = false;

    [Header("Economy")]
    public int currentMoney = 0;
    public int targetMoney = 900;

    [Header("Game Over Settings")]
    public GameObject gameOverUIPanel;
    public GameObject PlayerDeadUIPanel;
    public bool canEscape = false;

    [Header("UI References")]
    public string defaultTargetText = "NO TARGET";
    public string defaultLocationText = "WAITING FOR ORDERS";

    public System.Action OnGameWin; // 如果有其他脚本订阅这个事件

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private void Start()
    {
        // 每次开始游戏（或重载场景）都强制清空状态
        currentMissionID = "";
        completedMissionIDs.Clear();
        UpdateAllHUD();
    }

    // 1. 接取任务
    public bool AcceptMission(string missionID)
    {
        if (!string.IsNullOrEmpty(currentMissionID))
        {
            Debug.Log("已有任务在身！");
            return false;
        }

        if (completedMissionIDs.Contains(missionID))
        {
            Debug.Log("这任务做过了！");
            return false;
        }

        MissionData config = allMissions.FirstOrDefault(m => m.missionID == missionID);
        if (config == null)
        {
            Debug.LogError("找不到ID为 " + missionID + " 的任务配置！");
            return false;
        }

        // 记录 ID
        currentMissionID = missionID;

        // 设置 UI 和 场景标记
        SetMissionInfo(config.targetName, config.locationName);
        if (config.targetPlatform != null)
        {
            config.targetPlatform.ActivateEnemies();
            config.targetPlatform.EnableMarker(true);
        }

        return true;
    }

    // === 【修复重点】 2. 拾取任务物品 ===
    public void CollectQuestItem()
    {
        // 第一步：通过属性获取当前任务的配置数据
        var config = CurrentMissionConfig;

        // 如果 config 为空，说明当前根本没有任务，直接返回
        if (config == null) return;

        hasQuestItem = true;
        Debug.Log("获得任务物品！请返回交付。");

        SetMissionInfo("RETURN TO BASE", "CLAIM BOUNTY");

        // 使用 config 访问 targetPlatform
        if (config.targetPlatform != null)
        {
            config.targetPlatform.EnableMarker(false);
        }

        if (homeBasePlatform != null)
        {
            homeBasePlatform.EnableMarker(true);
        }
    }

    // 3. 交付任务
    public bool CompleteCurrentMission()
    {
        var config = CurrentMissionConfig;

        if (config == null || !hasQuestItem) return false;

        AddMoney(config.reward);

        // 记录到完成列表
        completedMissionIDs.Add(currentMissionID);

        // 重置状态
        currentMissionID = "";
        hasQuestItem = false;

        SetMissionInfo(defaultTargetText, defaultLocationText);

        if (config.targetPlatform != null) config.targetPlatform.EnableMarker(false);
        if (homeBasePlatform != null) homeBasePlatform.EnableMarker(false);

        // === 【补全逻辑】 之前漏掉了这一步，导致无法触发逃离 ===
        CheckWinCondition();

        return true;
    }

    private void CheckWinCondition()
    {
        if (currentMoney >= targetMoney)
        {
            Debug.Log("获得了足够的钱！前往撤离点离开城市！");
            canEscape = true;
            SetMissionInfo("ESCAPE CITY", "GO TO GATE");

            // 如果有其他系统订阅了胜利事件
            OnGameWin?.Invoke();
        }
    }

    public void UpdateAllHUD()
    {
        if (HUDController.Instance == null) return;
        HUDController.Instance.UpdateMissionUI(defaultTargetText, defaultLocationText);
        HUDController.Instance.UpdateMoneyUI(currentMoney, targetMoney);
    }

    public void AddMoney(int amount)
    {
        currentMoney += amount;
        HUDController.Instance.UpdateMoneyUI(currentMoney, targetMoney);
    }

    public void SetMissionInfo(string target, string location)
    {
        HUDController.Instance.UpdateMissionUI(target, location);
    }

    public void TriggerGameOver()
    {
        Debug.Log("游戏结束逻辑触发...");

        if (gameOverUIPanel != null)
        {
            gameOverUIPanel.SetActive(true);
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void PlayerDead()
    {
        if (PlayerDeadUIPanel != null)
        {
            PlayerDeadUIPanel.SetActive(true);
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
