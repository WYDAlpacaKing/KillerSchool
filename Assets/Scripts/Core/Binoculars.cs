using UnityEngine;

public class Binoculars : WeaponBase
{
    public static Binoculars Instance { get; private set; }
    public bool IsScanningMode { get; private set; } = false;

    [Header("Settings")]
    public float zoomedFOV = 30f;
    private float defaultFOV;


    private Camera mainCam;

    private void Awake()
    {
        Instance = this;
        mainCam = Camera.main;
        defaultFOV = mainCam.fieldOfView;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    protected override void FireLogic() { }

    private void Update()
    {
        bool isHolding = Input.GetMouseButton(1);

        if (isHolding != IsScanningMode)
        {
            IsScanningMode = isHolding;
            ToggleScope(IsScanningMode);
        }
    }

    private void ToggleScope(bool active)
    {
        // 1. 切换 FOV
        if (mainCam) mainCam.fieldOfView = active ? zoomedFOV : defaultFOV;

        // === 修改点 2：通过单例调用 UI ===
        if (ScopeOverlay.Instance != null)
        {
            ScopeOverlay.Instance.SetScopeActive(active);
        }
        else
        {
            // 防御性编程：如果没找到UI，报个错提醒自己
            Debug.LogWarning("场景里找不到 ScopeOverlay 脚本！请检查 Canvas 是否挂载了该脚本。");
        }
    }

    public override void OnUnequip()
    {
        base.OnUnequip();
        IsScanningMode = false;
        if (mainCam) mainCam.fieldOfView = defaultFOV;

        // 切枪时强制关闭
        if (ScopeOverlay.Instance != null)
        {
            ScopeOverlay.Instance.SetScopeActive(false);
        }
    }
}
