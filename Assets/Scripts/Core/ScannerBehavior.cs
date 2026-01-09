using UnityEngine;

public class ScannerBehavior : MonoBehaviour
{
    [Header("Visual Components")]
    [SerializeField] private GameObject hiddenObj;   // 问号
    [SerializeField] private GameObject scanningObj; // 圈圈
    [SerializeField] private GameObject resultObj;   // 结果父物体
    [SerializeField] private TextMesh resultText;    // 结果文字

    [Header("Configuration")]
    [SerializeField] private float referenceScale = 0.02f;
    [SerializeField] private float alignDotThreshold = 0.995f;
    [SerializeField] private float scanDuration = 1.0f;

    [Header("Target Effect")]
    [SerializeField] private float flickerSpeed = 8.0f;

    // 内部状态
    private enum ScanState { None, Hidden, Scanning, Result }
    private ScanState currentState = ScanState.None;
    private ScanTargetData targetData;
    private Transform camTrans;

    // 两个核心状态标记
    private float scanTimer = 0f;
    private bool isScanComplete = false; // 标记：是否已经扫描过（永久记忆）
    private bool isLockedTarget = false; // 标记：是否是高价值目标（永久显示）

    private Camera mainCam;

    private void Start()
    {
        mainCam = Camera.main;
        camTrans = mainCam.transform;
        targetData = GetComponentInParent<ScanTargetData>();

        // 初始化
        if (hiddenObj) hiddenObj.SetActive(false);
        if (scanningObj) scanningObj.SetActive(false);
        if (resultObj) resultObj.SetActive(false);
        currentState = ScanState.Result;
        SetState(ScanState.None);
    }

    private void LateUpdate()
    {
        bool isBinocularActive = (Binoculars.Instance != null && Binoculars.Instance.IsScanningMode);

        // =========================================================
        // 优先级 1: 永久锁定的目标 (Target类型且已扫描)
        // =========================================================
        if (isLockedTarget)
        {
            if (IsOnScreen())
            {
                ProcessTransform(); // 依然需要计算位置和缩放
                SetState(ScanState.Result);
                HandleFlicker();    // 只有它会闪烁
            }
            else
            {
                SetState(ScanState.None); // 出屏幕就隐藏，节省性能
            }
            return; // 这里的 return 保证了只要是 Target，无视下面所有逻辑
        }

        // =========================================================
        // 优先级 2: 放下望远镜 -> 隐藏所有非Target
        // =========================================================
        if (!isBinocularActive)
        {
            // 关键修改：只重置视觉状态和未完成的进度，**保留 isScanComplete**
            SetState(ScanState.None);
            scanTimer = 0f;
            return;
        }

        // =========================================================
        // 优先级 3: 望远镜模式下的普通逻辑
        // =========================================================

        // 屏幕裁剪
        if (!IsOnScreen())
        {
            SetState(ScanState.None);
            return;
        }

        // 计算位置和Billboard
        ProcessTransform();

        // 核心分支：是否扫描过？
        if (isScanComplete)
        {
            // Case A: 以前扫描过 (记忆生效)
            // 不需要对准，不需要读条，只要在屏幕内就直接显示结果
            SetState(ScanState.Result);

            // 普通敌人不闪烁，确保物体是激活的
            if (resultObj && !resultObj.activeSelf) resultObj.SetActive(true);
        }
        else
        {
            // Case B: 还没扫描过 (执行扫描逻辑)
            ProcessScanningLogic();
        }
    }

    private void ProcessTransform()
    {
        Plane plane = new Plane(camTrans.forward, camTrans.position);
        float dist = plane.GetDistanceToPoint(transform.position);
        if (dist < 0.1f) dist = 0.1f;

        transform.localScale = Vector3.one * (referenceScale * dist);
        transform.LookAt(transform.position + camTrans.rotation * Vector3.forward, camTrans.rotation * Vector3.up);
    }

    private void ProcessScanningLogic()
    {
        Vector3 dirToTarget = (transform.position - camTrans.position).normalized;
        float dot = Vector3.Dot(camTrans.forward, dirToTarget);

        if (dot >= alignDotThreshold)
        {
            // 对准了 -> 读条
            SetState(ScanState.Scanning);
            if (scanningObj) scanningObj.transform.Rotate(0, 0, 360 * Time.deltaTime);

            scanTimer += Time.deltaTime;
            if (scanTimer >= scanDuration) CompleteScan();
        }
        else
        {
            // 没对准 -> 显示问号
            SetState(ScanState.Hidden);
            scanTimer = 0f; // 移开视线，进度归零
        }
    }

    private void HandleFlicker()
    {
        if (resultObj == null) return;
        float flash = Mathf.Sin(Time.time * flickerSpeed);
        resultObj.SetActive(flash > 0);
    }

    private void CompleteScan()
    {
        // 1. 永久标记为“已扫描”
        isScanComplete = true;

        if (targetData != null)
        {
            if (targetData.type == ScanTargetData.TargetType.Target)
            {
                // 2. 如果是Target，升级为“永久锁定”
                isLockedTarget = true;
                resultText.text = targetData.characterName.ToUpper();
                resultText.color = Color.red;
            }
            else if (targetData.type == ScanTargetData.TargetType.Guard)
            {
                resultText.text = "GUARD";
                resultText.color = Color.yellow;
            }
            else
            {
                resultText.text = "CIVILIAN";
                resultText.color = Color.white;
            }
        }
    }

    private void SetState(ScanState state)
    {
        if (currentState == state) return;

        if (hiddenObj) hiddenObj.SetActive(state == ScanState.Hidden);
        if (scanningObj) scanningObj.SetActive(state == ScanState.Scanning);

        // 注意：对于 Target，HandleFlicker 会接管 active 状态，
        // 但对于普通敌人，这里确保它是显示的
        if (resultObj)
        {
            // 如果切到 Result 状态，先强制显示（防止之前的闪烁把它关了）
            if (state == ScanState.Result) resultObj.SetActive(true);
            else resultObj.SetActive(false);
        }

        currentState = state;
    }

    private bool IsOnScreen()
    {
        Vector3 vp = mainCam.WorldToViewportPoint(transform.position);
        return vp.z > 0 && vp.x > 0 && vp.x < 1 && vp.y > 0 && vp.y < 1;
    }

    public void OnEnemyDeath()
    {
        this.enabled = false;
        if (hiddenObj) hiddenObj.SetActive(false);
        if (scanningObj) scanningObj.SetActive(false);
        if (resultObj) resultObj.SetActive(false);
    }
}
