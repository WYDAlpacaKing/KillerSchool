using UnityEngine;

public class VehicleInteraction : MonoBehaviour
{
    [Header("=== 配置 ===")]
    public Transform entryPoint; // 玩家检测上车的位置
    public Transform exitPoint;  // 玩家下车的位置
    public float interactDistance = 3f;
    public GameObject carCamera; // 车载摄像机
    public GameObject driveUI;   // 驾驶时的UI提示 (比如 "Press Shift to Boost")

    [Header("=== 运行时引用 ===")]
    public GameObject playerObject; // 场景里的 FPS Player
    private HoverCarController carController;
    private bool playerInRange = false;

    void Start()
    {
        carController = GetComponent<HoverCarController>();
        if (carCamera) carCamera.SetActive(false);
        if (driveUI) driveUI.SetActive(false);
    }

    void Update()
    {
        // 1. 还没上车：检测玩家距离
        if (!carController.isPlayerControlling)
        {
            float dist = Vector3.Distance(playerObject.transform.position, entryPoint.position);
            playerInRange = dist < interactDistance;
            if (playerInRange)
            {
                // 这里可以加一个 UI 提示： "Press E to Enter Vehicle"
                HUDController.Instance.ShowNotification("Press E to Enter Vehicle", 0.1f);
            }
            if (playerInRange && Input.GetKeyDown(KeyCode.E))
            {
                EnterVehicle();
            }
        }
        // 2. 在车上：检测下车
        else
        {
            // === 核心修改：仅当允许停靠时，才允许按 E 下车 ===
            if (carController.canDock)
            {
                // 这里可以加一个 UI 提示： "Press E to Dock & Exit"
                HUDController.Instance.ShowNotification("Press E to Dock & Exit", 0.1f);
                Debug.Log("检测到 E 键，尝试下车！");
                if (Input.GetKeyDown(KeyCode.E))
                {
                    ExitVehicleAndDock();
                }
            }
            else
            {
                // 这里可以加一个 UI 提示： "Cannot Exit Here" (提示玩家还没到站)
                HUDController.Instance.ShowNotification("Cannot Exit Here", 0.1f);
            }
        }
    }

    void EnterVehicle()
    {
        playerObject.SetActive(false);
        carCamera.SetActive(true);
        if (driveUI) driveUI.SetActive(true);

        carController.isPlayerControlling = true;
        if (HUDController.Instance != null)
        {
            HUDController.Instance.SetDrivingMode(true);
        }
        // 注意：这里我们不再调用 carController.StartEngine()
        // 而是保持 isEngineRunning = false;
        // 这样玩家上车后，必须按 W 才能触发 Undock() 或 StartEngine()
        // 这就形成了完美的“上车 -> 停顿 -> 点火起飞”的节奏
    }

    // 修改为：下车并泊车
    public void ExitVehicleAndDock()
    {
        // 1. 触发车子的平滑归位
        carController.StartAutoDocking();

        // 2. 处理玩家下车
        playerObject.transform.position = exitPoint.position;
        playerObject.transform.rotation = transform.rotation;

        playerObject.SetActive(true);
        carCamera.SetActive(false);
        if (driveUI) driveUI.SetActive(false);

        if (HUDController.Instance != null)
        {
            HUDController.Instance.SetDrivingMode(false);
        }
    }
}
