using System.Collections;
using UnityEngine;

public class HoverCarController : MonoBehaviour
{
    [Header("=== 核心组件 ===")]
    public Transform visualPart;
    public AudioSource engineAudio;

    [Header("=== 驾驶参数 ===")]
    public float moveSpeed = 15f;
    public float boostMultiplier = 2.5f;
    public float rotationSpeed = 100f;
    public float inertiaDamping = 3f;
    public float inputSmoothing = 5f;

    [Header("=== 悬浮感 ===")]
    public float bobFrequency = 1.5f;
    public float bobAmplitude = 0.2f;
    public float tiltAmount = 10f;

    // --- 状态 ---
    [HideInInspector] public bool isPlayerControlling = false;
    [HideInInspector] public bool isEngineRunning = false;
    [HideInInspector] public bool isDocked = false;

    // --- 停靠交互 ---
    [HideInInspector] public bool canDock = false;
    // === 新增：记录当前是哪个平台发出的邀请 ===
    [HideInInspector] public Transform currentDockingPlatform;
    private Vector3 validDockPosition;
    private Quaternion validDockRotation;

    // 内部变量
    private Vector3 currentVelocity;
    private float currentHInput;
    private float currentVInput;
    private float originalHeight;
    private bool isBoosting;
    private bool isDockingProcess = false;

    void Start()
    {
        originalHeight = transform.position.y;

        if (engineAudio != null)
        {
            engineAudio.loop = true; // 强制开启循环
                                     // 如果想一开始是静音的（熄火状态），可以先暂停，或者由 Play On Awake 决定
            if (!engineAudio.isPlaying && isEngineRunning)
            {
                engineAudio.Play();
            }
        }
    }

    void Update()
    {
        // 1. 自动归位中：全权交给协程，Update 不干涉
        if (isDockingProcess) return;

        // 2. 始终悬浮 (除非在归位)
        HandleBobbing();

        // 3. 没人开：惯性停车
        if (!isPlayerControlling)
        {
            HandleInertiaStop();
            return;
        }

        // 4. 有人开：
        //    这里是修复"无法开动"的关键。我们不能因为 isDocked 就直接 return。

        if (isDocked)
        {
            // === 修复点：在停靠状态下，检测点火输入 ===
            // 如果按下了 W 或 空格，解除停靠，启动引擎
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space))
            {
                Undock();
            }
            // 停靠时强制静止，不计算惯性
            currentVelocity = Vector3.zero;
        }
        else
        {
            // === 正常驾驶状态 ===
            HandleInput(); // 处理输入

            if (isEngineRunning)
            {
                HandleMovement();
                HandleVisuals();
                UpdateAudio();
            }
            else
            {
                // 未停靠但引擎熄火（极少情况，或者是刚下落）
                if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space))
                {
                    StartEngine();
                }
            }
        }
    }

    // === 核心修复：平滑归位协程 (包含视觉回正) ===
    public void StartAutoDocking()
    {
        if (!canDock) return;
        StartCoroutine(SmoothDockRoutine());
    }

    IEnumerator SmoothDockRoutine()
    {
        isDockingProcess = true;
        isPlayerControlling = false;
        isEngineRunning = false;

        float duration = 2.0f;
        float elapsed = 0f;

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        // === 修复点：记录当前倾斜角度 ===
        Quaternion startVisualRot = visualPart.localRotation;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = Mathf.SmoothStep(0f, 1f, t);

            // 1. 归位位置和旋转
            transform.position = Vector3.Lerp(startPos, validDockPosition, t);
            transform.rotation = Quaternion.Lerp(startRot, validDockRotation, t);

            // 2. === 修复点：视觉倾斜归零 ===
            // 让车身平滑地回正到 0 度
            visualPart.localRotation = Quaternion.Lerp(startVisualRot, Quaternion.identity, t);

            // 3. 声音淡出
            if (engineAudio) engineAudio.pitch = Mathf.Lerp(engineAudio.pitch, 0f, t);

            yield return null;
        }

        // 强制对齐
        transform.position = validDockPosition;
        transform.rotation = validDockRotation;
        visualPart.localRotation = Quaternion.identity; // 确保完全回正

        // 更新基准高度
        originalHeight = validDockPosition.y;

        // 重置物理量
        currentVelocity = Vector3.zero;
        currentHInput = 0;
        currentVInput = 0;

        isDocked = true;         // 标记为已停靠
        isDockingProcess = false; // 释放控制权给 Update
    }

    // ... [以下标准方法保持不变] ...

    void HandleBobbing()
    {
        float bobOffset = Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
        Vector3 pos = transform.position;
        pos.y = originalHeight + bobOffset;
        transform.position = pos;
    }

    void HandleInput()
    {
        float hTarget = Input.GetAxis("Horizontal");
        float vTarget = Input.GetAxis("Vertical");
        isBoosting = Input.GetKey(KeyCode.LeftShift);

        currentHInput = Mathf.Lerp(currentHInput, hTarget, inputSmoothing * Time.deltaTime);
        currentVInput = Mathf.Lerp(currentVInput, vTarget, inputSmoothing * Time.deltaTime);
    }

    void HandleMovement()
    {
        float turnAmount = currentHInput * rotationSpeed * Time.deltaTime;
        if (currentVInput < -0.1f) turnAmount *= -1;
        transform.Rotate(0, turnAmount, 0);

        float targetSpeed = currentVInput * moveSpeed;
        if (isBoosting) targetSpeed *= boostMultiplier;

        Vector3 targetVelocity = transform.forward * targetSpeed;
        float lerpSpeed = isBoosting ? 2f : inertiaDamping;
        currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, lerpSpeed * Time.deltaTime);

        Vector3 displacement = currentVelocity * Time.deltaTime;
        transform.position += new Vector3(displacement.x, 0, displacement.z);
    }

    void HandleInertiaStop()
    {
        currentVelocity = Vector3.Lerp(currentVelocity, Vector3.zero, Time.deltaTime * 2f);
        if (currentVelocity.magnitude > 0.01f)
        {
            Vector3 displacement = currentVelocity * Time.deltaTime;
            transform.position += new Vector3(displacement.x, 0, displacement.z);
        }

        // 视觉回正 (无人驾驶时自动回正倾斜)
        if (visualPart) visualPart.localRotation = Quaternion.Lerp(visualPart.localRotation, Quaternion.identity, Time.deltaTime * 2f);

        if (engineAudio) engineAudio.Stop();
    }

    void HandleVisuals()
    {
        if (visualPart == null) return;
        float targetPitch = currentVInput * 5f;
        float targetRoll = -currentHInput * tiltAmount;
        Quaternion targetRot = Quaternion.Euler(targetPitch, 0, targetRoll);
        visualPart.localRotation = Quaternion.Lerp(visualPart.localRotation, targetRot, 5f * Time.deltaTime);
    }

    void UpdateAudio()
    {
        if (engineAudio == null) return;
        float targetPitch = 1.0f + (currentVelocity.magnitude * 0.05f);
        if (isBoosting) targetPitch += 0.5f;
        engineAudio.pitch = Mathf.Lerp(engineAudio.pitch, targetPitch, Time.deltaTime * 2f);
    }

    // 接口
    public void StartEngine()
    {
        isEngineRunning = true;

        if (engineAudio != null && !engineAudio.isPlaying)
        {
            engineAudio.Play();
        }
    }

    public void Undock()
    {
        isDocked = false; // 解除停靠锁
        StartEngine();    // 点火
    }

    public void SetDockableStatus(bool status, Vector3 pos, Quaternion rot, Transform requestingPlatform)
    {
        // 情况 1: 某个平台说 "可以停靠" (status = true)
        if (status)
        {
            canDock = true;
            validDockPosition = pos;
            validDockRotation = rot;
            currentDockingPlatform = requestingPlatform; // 记录：是这个平台邀请我的
        }
        // 情况 2: 某个平台说 "取消停靠" (status = false)
        else
        {
            // === 核心逻辑保护 ===
            // 只有当“发出取消请求的平台”等于“当前记录的平台”时，才允许取消
            // 防止远处的平台 B 取消了平台 A 的邀请
            if (currentDockingPlatform == requestingPlatform)
            {
                canDock = false;
                currentDockingPlatform = null;
            }
        }
    }
}
