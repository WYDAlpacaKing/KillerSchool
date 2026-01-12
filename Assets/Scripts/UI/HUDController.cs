using TMPro;
using UnityEngine;

public class HUDController : MonoBehaviour
{
    [Header("=== References: Top Left ===")]
    [SerializeField] private TextMeshProUGUI targetText;
    [SerializeField] private TextMeshProUGUI locationText;

    [Header("=== References: Top Right (Inventory) ===")]
    [SerializeField] private TextMeshProUGUI[] weaponSlotTexts;
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color inactiveColor = Color.gray;

    [Header("=== References: Bottom Left ===")]
    [SerializeField] private TextMeshProUGUI moneyText;

    [Header("=== References: Bottom Right ===")]
    [SerializeField] private TextMeshProUGUI healthText;

    [Header("=== References: Notification (New) ===")]
    [SerializeField] private TextMeshProUGUI notificationText; // 新增：提示信息文本槽位

    [Header("=== Layout Settings ===")]
    [SerializeField] private GameObject topRightPanel;

    // 单例模式
    public static HUDController Instance { get; private set; }

    // 内部计时器
    private float notificationTimer = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private void Start()
    {
        // 游戏开始时清空提示
        if (notificationText != null)
        {
            notificationText.text = "";
        }
    }

    private void Update()
    {
        // --- 处理提示信息的倒计时 ---
        if (notificationTimer > 0)
        {
            notificationTimer -= Time.deltaTime;

            // 时间到了，清空文本
            if (notificationTimer <= 0)
            {
                if (notificationText != null)
                {
                    notificationText.text = "";
                }
            }
        }
    }

    // --- 新增功能：显示提示信息 ---
    /// <summary>
    /// 在屏幕上显示一段临时提示信息
    /// </summary>
    /// <param name="message">要显示的内容</param>
    /// <param name="duration">显示持续时间（秒）</param>
    public void ShowNotification(string message, float duration)
    {
        if (notificationText == null) return;

        notificationText.text = message;
        notificationTimer = duration; // 设置倒计时，如果已有信息则重置时间
    }

    // --- 1. 更新背包显示 ---
    public void UpdateInventoryUI(WeaponBase[] slots, int currentIndex)
    {
        if (topRightPanel != null && !topRightPanel.activeSelf) return;

        for (int i = 0; i < weaponSlotTexts.Length; i++)
        {
            if (i >= slots.Length) break;

            WeaponBase weapon = slots[i];
            string displayName = "EMPTY";

            if (weapon != null)
            {
                displayName = weapon.weaponName;
            }
            else if (i == 2)
            {
                displayName = "Scanner";
            }

            string finalString = displayName;
            bool isSelected = (i == currentIndex);

            if (isSelected)
            {
                weaponSlotTexts[i].color = activeColor;
                weaponSlotTexts[i].text = $"{finalString} <";
            }
            else
            {
                weaponSlotTexts[i].color = inactiveColor;
                int nextIndex = (currentIndex + 1) % 3;
                if (i == nextIndex)
                {
                    weaponSlotTexts[i].text = $"{finalString} [Q]";
                }
                else
                {
                    weaponSlotTexts[i].text = finalString;
                }
            }
        }
    }

    // --- 2. 更新血量 ---
    public void UpdateHealthUI(float currentHealth)
    {
        healthText.text = $"HEALTH   {Mathf.CeilToInt(currentHealth)}";
        if (currentHealth < 30) healthText.color = Color.red;
        else healthText.color = Color.white;
    }

    // --- 3. 更新金钱 ---
    public void UpdateMoneyUI(int currentMoney, int maxMoney)
    {
        moneyText.text = $"$ {currentMoney}/{maxMoney}";
    }

    // --- 4. 更新任务信息 ---
    public void UpdateMissionUI(string targetName, string locationName)
    {
        targetText.text = $"TARGET: {targetName}";
        locationText.text = $"LOCATION: {locationName}";
    }

    // --- 5. 开车状态切换 ---
    public void SetDrivingMode(bool isDriving)
    {
        if (topRightPanel != null)
        {
            topRightPanel.SetActive(!isDriving);
        }
    }
}
