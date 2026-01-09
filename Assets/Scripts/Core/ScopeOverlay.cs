using UnityEngine;

public class ScopeOverlay : MonoBehaviour
{
    // 1. 创建单例，让全世界都能找到它
    public static ScopeOverlay Instance { get; private set; }

    // 引用黑框和橙色滤镜的父物体（或者就是Canvas本身）
    [SerializeField] private GameObject visualRoot;

    private void Awake()
    {
        // 初始化单例
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 2. 游戏开始时强制关闭 UI，防止穿帮
        if (visualRoot) visualRoot.SetActive(false);
    }

    /// <summary>
    /// 供外部调用的开关方法
    /// </summary>
    public void SetScopeActive(bool isActive)
    {
        if (visualRoot) visualRoot.SetActive(isActive);
    }
}
