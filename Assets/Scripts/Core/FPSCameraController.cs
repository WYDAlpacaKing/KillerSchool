using UnityEngine;

public class FPSCameraController : MonoBehaviour
{
    [Header("引用参数")]
    [SerializeField] private Transform playerBody;
    [SerializeField] private PlayerInputHandler inputHandler;
    [SerializeField] private Camera targetCamera; // 拖入相机组件

    [Header("视角轴设置参数")]
    [SerializeField] private float xSensitivity = 2.0f;
    [SerializeField] private float ySensitivity = 2.0f;
    [SerializeField] private float yClampMin = -85f;
    [SerializeField] private float yClampMax = 85f;

    [Header("移动倾斜参数")]
    [SerializeField] private float tiltAngle = 3.0f;
    [SerializeField] private float tiltSpeed = 8.0f;

    
    private float _xRotation = 0f;
    private float _currentTilt = 0f;

    private float recoilPitch = 0f;// 垂直后坐力
    private float recoilYaw = 0f;// 水平后坐力

    private float recoilTimer = 0f;// 后坐力计时器
    private float recoilDuration = 0f;// 后坐力持续时间
    private AnimationCurve recoilCurve;// 后坐力恢复曲线

    //相机震动
    private float shakeTimer = 0f;
    private float currentShakeIntensity = 0f; // 震动强度系数
    private Vector3 shakeOffset = Vector3.zero;

    //FOV
    private float baseFOV;
    private float targetFOVOffset = 0f;// 目标FOV偏移量

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (targetCamera == null) targetCamera = GetComponent<Camera>();
        baseFOV = targetCamera.fieldOfView;
    }

    private void LateUpdate()
    {
        // 如果调试面板打开 则忽略所有输入
        if (DebuggerManager.Instance != null && DebuggerManager.Instance.IsVisible)
            return;


        if (inputHandler == null) return;

        // 捕捉鼠标输入
        float mouseX = inputHandler.LookInput.x * xSensitivity;
        float mouseY = inputHandler.LookInput.y * ySensitivity;

        // 上下视角
        _xRotation -= mouseY;
        _xRotation = Mathf.Clamp(_xRotation, yClampMin, yClampMax);

        // 处理后坐力回复
        if (recoilTimer > 0)
        {
            recoilTimer -= Time.deltaTime;
            
            recoilPitch = Mathf.Lerp(recoilPitch, 0f, Time.deltaTime * 5f);// 平滑回复
            recoilYaw = Mathf.Lerp(recoilYaw, 0f, Time.deltaTime * 5f);
        }

        // 柏林噪声做震动
        if (shakeTimer > 0)
        {
            shakeTimer -= Time.deltaTime;
            // TODO: 频率参数暂时写死20f
            float x = (Mathf.PerlinNoise(Time.time * 20f, 0f) - 0.5f) * 2f * currentShakeIntensity;
            float y = (Mathf.PerlinNoise(0f, Time.time * 20f) - 0.5f) * 2f * currentShakeIntensity;
            shakeOffset = new Vector3(x, y, 0);
        }
        else
        {
            shakeOffset = Vector3.zero;
        }

        // 倾斜
        float inputX = inputHandler.MoveInput.x;
        float targetTilt = -inputX * tiltAngle;
        _currentTilt = Mathf.Lerp(_currentTilt, targetTilt, Time.deltaTime * tiltSpeed);

        // 应用旋转!!! ( 基础 + Recoil + Shake + Tilt)
        // 注意这里 Recoil 是叠加在 xRotation 上的 后续不要再对 xRotation 做处理了
        Quaternion finalRot = Quaternion.Euler(_xRotation - recoilPitch + shakeOffset.x, shakeOffset.y + recoilYaw, _currentTilt);
        transform.localRotation = finalRot;

        // 竖直后座力应用在相机上 水平后坐力应用在角色身上 
        playerBody.Rotate(Vector3.up * (mouseX + recoilYaw * Time.deltaTime));

        // FOV 处理
        targetFOVOffset = Mathf.Lerp(targetFOVOffset, 0f, Time.deltaTime * 10f); // 快速回弹
        targetCamera.fieldOfView = baseFOV + targetFOVOffset;
    }


    /// <summary>
    /// Applies recoil to the current object, affecting its vertical and horizontal orientation.
    /// </summary>
    /// <remarks>This method modifies the object's orientation by adding the specified vertical and horizontal
    /// recoil values. The recoil effect is applied over the specified duration using the provided animation
    /// curve.</remarks>
    /// <param name="vertical">The amount of vertical recoil to apply.</param>
    /// <param name="horizontal">The amount of horizontal recoil to apply.</param>
    /// <param name="duration">The duration over which the recoil effect will be applied.</param>
    /// <param name="curve">An <see cref="AnimationCurve"/> that defines the recoil effect over time.</param>
    public void ApplyRecoil(float vertical, float horizontal, float duration, AnimationCurve curve)
    {
        // 瞬间加上后坐力
        recoilPitch += vertical;
        recoilYaw += horizontal; // 简单的水平偏移

        // 启动回复逻辑
        recoilDuration = duration;
        recoilCurve = curve;
        recoilTimer = duration;
    }

    /// <summary>
    /// Applies a shake effect to the object with the specified amplitude, frequency, and duration.
    /// </summary>
    /// <remarks>The shake effect is applied immediately and will last for the specified duration. The
    /// frequency parameter is not currently utilized in the implementation.</remarks>
    /// <param name="amplitude">The intensity of the shake effect. Must be a non-negative value.</param>
    /// <param name="frequency">The frequency of the shake effect. This parameter is currently not used but can be stored for future use.</param>
    /// <param name="duration">The duration, in seconds, for which the shake effect will be applied. Must be a non-negative value.</param>
    public void ApplyShake(float amplitude, float frequency, float duration)
    {
        currentShakeIntensity = amplitude;
        shakeTimer = duration;
        // TODO:
        // Frequency在柏噪里面用 先用20f写死了
    }

    /// <summary>
    /// Applies a field of view (FOV) kick effect by adjusting the FOV offset.
    /// </summary>
    /// <param name="amount">The magnitude of the FOV kick. A positive value decreases the FOV, creating a zoom-in effect.</param>
    public void ApplyFOVKick(float amount)
    {
        targetFOVOffset = -amount;
    }

}
