using UnityEngine;

public class LandingPlatform : MonoBehaviour
{
    [Header("配置")]
    public Transform dockPoint; // 停靠点 (红色球)
    public float detectionRadius = 8f; // 半径稍微给大一点，方便停靠
    [Tooltip("允许的高度差范围 (车比平台高多少以内算有效)")]
    public float maxVerticalDistance = 10f;

    private HoverCarController playerCar;
    private bool isPlayerNearby = false;

    void Start()
    {
        playerCar = FindFirstObjectByType<HoverCarController>();
    }

    void Update()
    {
        if (playerCar == null) return;

        // ... [圆柱体计算逻辑保持不变] ...
        Vector3 platformPosFlat = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 carPosFlat = new Vector3(playerCar.transform.position.x, 0, playerCar.transform.position.z);
        float horizontalDist = Vector3.Distance(platformPosFlat, carPosFlat);
        float verticalDist = Mathf.Abs(transform.position.y - playerCar.transform.position.y);

        bool isInsideCylinder = horizontalDist < detectionRadius && verticalDist < maxVerticalDistance;

        isPlayerNearby = isInsideCylinder;

        if (isInsideCylinder)
        {
            // === 修改：传入 this.transform ===
            // 告诉车：是我（这个平台）允许你停靠的
            playerCar.SetDockableStatus(true, dockPoint.position, dockPoint.rotation, transform);
        }
        else
        {
            if (!playerCar.isDocked)
            {
                // === 修改：传入 this.transform ===
                // 告诉车：我（这个平台）不再允许你停靠了
                // (如果车子当前记录的是别的平台，车子会无视这句话)
                playerCar.SetDockableStatus(false, Vector3.zero, Quaternion.identity, transform);
            }
        }
    }

    // === 这里的 Gizmos 极其重要，请回到 Scene 窗口查看 ===
    void OnDrawGizmos()
    {
        // 1. 画出检测范围 (绿色=激活，黄色=未激活)
        Gizmos.color = isPlayerNearby ? Color.green : Color.yellow;

        // 画两个圈，形成圆柱体的视觉提示
        Vector3 top = transform.position + Vector3.up * maxVerticalDistance;
        Vector3 bottom = transform.position + Vector3.down * maxVerticalDistance;

        // 中心圆圈
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        // 顶部边界提示
        Gizmos.DrawWireSphere(top, detectionRadius * 0.5f);
        // 连线
        Gizmos.DrawLine(top, bottom);

        if (dockPoint)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(dockPoint.position, 0.3f);
        }
    }
}
