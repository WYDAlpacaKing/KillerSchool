using UnityEngine;

public class PlatformController : MonoBehaviour
{
    [Header("Enemies")]
    [SerializeField] private GameObject enemyContainer;

    [Header("UI Marker Configuration")]
    [SerializeField] private GameObject markerPrefab;
    [SerializeField] private string platformDisplayName = "SECTOR X";
    [SerializeField] private float markerHideDistance = 50f;
    [SerializeField] private float markerHeight = 15f;

    [Header("Scanner Style Settings")]
    [SerializeField] private float referenceScale = 0.15f;

    // 内部变量
    private GameObject currentMarker;
    private TextMesh markerText;
    private Transform playerTrans;
    private bool isMarkerActive = false;

    // === 关键修改：不再只在 Start 获取一次，而是动态维护 ===
    private Camera activeCamera;
    private Transform activeCamTrans;

    private void Start()
    {
        if (enemyContainer) enemyContainer.SetActive(false);

        // 依然获取 Player 引用用于计算物理距离 (因为人即使在车里，车也是 Player 的代理)
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player) playerTrans = player.transform;

        // 初始尝试获取一次相机
        RefreshCameraReference();
    }

    private void LateUpdate()
    {
        // 1. 每帧检查相机状态
        if (!IsCameraValid())
        {
            RefreshCameraReference();
        }

        // 2. 只有相机有效且 Marker 激活时才更新
        if (isMarkerActive && currentMarker != null && playerTrans != null && IsCameraValid())
        {
            UpdateMarkerBehavior();
        }
    }

    // === 辅助方法：检查当前相机是否还活着 ===
    private bool IsCameraValid()
    {
        // 如果相机引用没了，或者相机所在的物体被禁用了 (比如玩家上车后，玩家相机被Disable)
        return activeCamera != null && activeCamera.gameObject.activeInHierarchy;
    }

    // === 辅助方法：寻找新的主相机 ===
    private void RefreshCameraReference()
    {
        activeCamera = Camera.main; // 寻找当前 Tag 为 MainCamera 的相机
        if (activeCamera != null)
        {
            activeCamTrans = activeCamera.transform;
        }
    }

    public void ActivateEnemies()
    {
        if (enemyContainer) enemyContainer.SetActive(true);
    }

    public void EnableMarker(bool enable)
    {
        isMarkerActive = enable;

        if (enable)
        {
            if (currentMarker == null && markerPrefab != null)
            {
                currentMarker = Instantiate(markerPrefab, transform.position + Vector3.up * markerHeight, Quaternion.identity);
                markerText = currentMarker.GetComponentInChildren<TextMesh>();
            }
            if (currentMarker) currentMarker.SetActive(true);
        }
        else
        {
            if (currentMarker) currentMarker.SetActive(false);
        }
    }

    private void UpdateMarkerBehavior()
    {
        // === 使用 activeCamTrans (当前活跃相机) 进行所有计算 ===

        // 1. 真实距离判定
        // 注意：计算“距离文字”时，我们依然用玩家(或车)的位置，而不是相机位置，这样数值更人性化
        // 如果你希望以上车后的车为基准，可以使用 activeCamTrans.position 替代 playerTrans.position
        float realDist = Vector3.Distance(transform.position, activeCamTrans.position);

        if (realDist < markerHideDistance)
        {
            currentMarker.SetActive(false);
            return;
        }
        if (!currentMarker.activeSelf) currentMarker.SetActive(true);

        // 2. 智能防消失逻辑 (拉回到 activeCam 的视野边缘)
        float maxRenderDist = activeCamera.farClipPlane * 0.95f;
        Vector3 dirToTarget = (transform.position - activeCamTrans.position).normalized;

        // 计算 Marker 的物理位置
        // 这里计算距离要用相机距离，因为是针对渲染剔除的优化
        float distToCam = Vector3.Distance(transform.position, activeCamTrans.position);

        Vector3 targetPos = transform.position;
        if (distToCam > maxRenderDist)
        {
            targetPos = activeCamTrans.position + dirToTarget * maxRenderDist;
        }

        // 应用位置
        currentMarker.transform.position = targetPos + Vector3.up * markerHeight;

        // 3. 旋转对齐 (使用 Scanner 算法，但基于 activeCamTrans)
        currentMarker.transform.LookAt(
            currentMarker.transform.position + activeCamTrans.rotation * Vector3.forward,
            activeCamTrans.rotation * Vector3.up
        );

        // 4. 缩放算法
        Plane plane = new Plane(activeCamTrans.forward, activeCamTrans.position);
        float planeDist = plane.GetDistanceToPoint(currentMarker.transform.position);

        if (planeDist < 0.1f) planeDist = 0.1f;
        currentMarker.transform.localScale = Vector3.one * (referenceScale * planeDist);

        // 5. 更新文字
        if (markerText)
        {
            markerText.text = $"{platformDisplayName}\n<size=60%>{Mathf.CeilToInt(realDist)}m</size>";
        }
    }

    private void OnDestroy()
    {
        if (currentMarker != null) Destroy(currentMarker);
    }
}
