using UnityEngine;

public class FPSCameraController : MonoBehaviour
{
    [Header("���ò���")]
    [SerializeField] private Transform playerBody;
    [SerializeField] private PlayerInputHandler inputHandler;
    [SerializeField] private Camera targetCamera; // ����������

    [Header("�ӽ������ò���")]
    [SerializeField] private float xSensitivity = 2.0f;
    [SerializeField] private float ySensitivity = 2.0f;
    [SerializeField] private float yClampMin = -85f;
    [SerializeField] private float yClampMax = 85f;

    [Header("�ƶ���б����")]
    [SerializeField] private float tiltAngle = 3.0f;
    [SerializeField] private float tiltSpeed = 8.0f;

    
    private float _xRotation = 0f;
    private float _currentTilt = 0f;

    private float recoilPitch = 0f;// 垂直后坐力（视觉层，自动回复）
    private float recoilYaw = 0f;// 水平后坐力（视觉层）

    private float recoilTimer = 0f;// 后坐力计时器
    private float recoilDuration = 0f;// 后坐力恢复时长
    private AnimationCurve recoilCurve;// 后坐力恢复曲线

    // 真实后坐力系统（影响实际视角，可压枪）
    private float trueRecoilAccumulated = 0f; // 累积的真实后坐力
    private float trueRecoilRecoveryTimer = 0f; // 真实后坐力恢复延迟计时器
    private float currentTrueRecoilRecoveryDelay = 0.3f; // 当前恢复延迟
    private float currentTrueRecoilRecoverySpeed = 3f; // 当前恢复速度
    private float currentMaxTrueRecoil = 12f; // 当前后坐力累积上限

    // 水平后坐力平滑系统
    private float targetHorizontalRecoil = 0f; // 目标水平后坐力偏移
    private float currentHorizontalRecoil = 0f; // 当前水平后坐力偏移（平滑过渡中）
    private float horizontalRecoilVelocity = 0f; // SmoothDamp 速度缓存
    private float horizontalRecoilSmoothTime = 0.08f; // 平滑时间

    //�����
    private float shakeTimer = 0f;
    private float currentShakeIntensity = 0f; // ��ǿ��ϵ��
    private Vector3 shakeOffset = Vector3.zero;

    //FOV
    private float baseFOV;
    private float targetFOVOffset = 0f;// Ŀ��FOVƫ����

   

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (targetCamera == null) targetCamera = GetComponent<Camera>();
        baseFOV = targetCamera.fieldOfView;
    }

    private void LateUpdate()
    {
        // ����������� �������������
        if (DebuggerManager.Instance != null && DebuggerManager.Instance.IsVisible)
            return;


        if (inputHandler == null) return;

        // ��׽�������
        float mouseX = inputHandler.LookInput.x * xSensitivity;
        float mouseY = inputHandler.LookInput.y * ySensitivity;

        // �����ӽ�
        _xRotation -= mouseY;
        _xRotation = Mathf.Clamp(_xRotation, yClampMin, yClampMax);

        // 视觉后坐力恢复（自动回正）
        if (recoilTimer > 0)
        {
            recoilTimer -= Time.deltaTime;
            
            recoilPitch = Mathf.Lerp(recoilPitch, 0f, Time.deltaTime * 5f);// 平滑恢复
            recoilYaw = Mathf.Lerp(recoilYaw, 0f, Time.deltaTime * 5f);
        }

        // 真实后坐力恢复（延迟后自动回正）
        if (trueRecoilAccumulated > 0.01f)
        {
            trueRecoilRecoveryTimer -= Time.deltaTime;
            
            // 延迟结束后开始恢复
            if (trueRecoilRecoveryTimer <= 0 && currentTrueRecoilRecoverySpeed > 0)
            {
                float recoveryAmount = currentTrueRecoilRecoverySpeed * Time.deltaTime;
                trueRecoilAccumulated = Mathf.Max(0f, trueRecoilAccumulated - recoveryAmount);
                
                // 恢复真实视角（向下移动）
                _xRotation = Mathf.Lerp(_xRotation, _xRotation + recoveryAmount, 0.5f);
            }
        }
        else
        {
            trueRecoilAccumulated = 0f;
        }

        // 水平后坐力平滑处理
        // 使用 SmoothDamp 实现平滑过渡，而不是瞬间跳跃
        float previousHorizontal = currentHorizontalRecoil;
        currentHorizontalRecoil = Mathf.SmoothDamp(
            currentHorizontalRecoil, 
            targetHorizontalRecoil, 
            ref horizontalRecoilVelocity, 
            horizontalRecoilSmoothTime
        );
        
        // 计算本帧的增量并应用到角色旋转
        float horizontalDelta = currentHorizontalRecoil - previousHorizontal;
        if (Mathf.Abs(horizontalDelta) > 0.001f)
        {
            playerBody.Rotate(Vector3.up * horizontalDelta);
        }

        // 目标值缓慢衰减回零（水平后坐力自然回正）
        targetHorizontalRecoil = Mathf.Lerp(targetHorizontalRecoil, 0f, Time.deltaTime * 8f);

        // ������������
        if (shakeTimer > 0)
        {
            shakeTimer -= Time.deltaTime;
            // TODO: Ƶ�ʲ�����ʱд��20f
            float x = (Mathf.PerlinNoise(Time.time * 20f, 0f) - 0.5f) * 2f * currentShakeIntensity;
            float y = (Mathf.PerlinNoise(0f, Time.time * 20f) - 0.5f) * 2f * currentShakeIntensity;
            shakeOffset = new Vector3(x, y, 0);
        }
        else
        {
            shakeOffset = Vector3.zero;
        }

        // ��б
        float inputX = inputHandler.MoveInput.x;
        float targetTilt = -inputX * tiltAngle;
        _currentTilt = Mathf.Lerp(_currentTilt, targetTilt, Time.deltaTime * tiltSpeed);

        // Ӧ����ת!!! ( ���� + Recoil + Shake + Tilt)
        // ע������ Recoil �ǵ����� xRotation �ϵ� ������Ҫ�ٶ� xRotation ��������
        Quaternion finalRot = Quaternion.Euler(_xRotation - recoilPitch + shakeOffset.x, shakeOffset.y + recoilYaw, _currentTilt);
        transform.localRotation = finalRot;

        // ��ֱ������Ӧ��������� ˮƽ������Ӧ���ڽ�ɫ���� 
        playerBody.Rotate(Vector3.up * (mouseX + recoilYaw * Time.deltaTime));

        // FOV ����
        targetFOVOffset = Mathf.Lerp(targetFOVOffset, 0f, Time.deltaTime * 10f); // ���ٻص�
        targetCamera.fieldOfView = baseFOV + targetFOVOffset;
    }


    /// <summary>
    /// 应用视觉后坐力（自动回正，用于半自动武器或作为全自动武器的辅助效果）
    /// </summary>
    /// <param name="vertical">垂直后坐力量</param>
    /// <param name="horizontal">水平后坐力量</param>
    /// <param name="duration">恢复时长</param>
    /// <param name="curve">恢复曲线</param>
    public void ApplyRecoil(float vertical, float horizontal, float duration, AnimationCurve curve)
    {
        // 瞬间叠加后坐力
        recoilPitch += vertical;
        recoilYaw += horizontal; // 简单的水平偏移

        // 启动恢复逻辑
        recoilDuration = duration;
        recoilCurve = curve;
        recoilTimer = duration;
    }

    /// <summary>
    /// 应用真实后坐力（真正改变玩家视角，玩家可通过压枪抵消）
    /// 用于全自动武器，创造需要控制的后坐力手感
    /// </summary>
    /// <param name="vertical">垂直后坐力（向上抬起的角度）</param>
    /// <param name="horizontal">水平后坐力（左右偏移的角度）</param>
    /// <param name="recoveryDelay">停止射击后多久开始恢复</param>
    /// <param name="recoverySpeed">恢复速度</param>
    /// <param name="maxAccumulation">后坐力累积上限</param>
    /// <param name="horizontalSmoothTime">水平后坐力平滑时间</param>
    public void ApplyTrueRecoil(float vertical, float horizontal, float recoveryDelay, float recoverySpeed, float maxAccumulation, float horizontalSmoothTime = 0.08f)
    {
        // 更新当前上限
        currentMaxTrueRecoil = maxAccumulation;

        // 检查是否已达到累积上限
        float remainingRoom = currentMaxTrueRecoil - trueRecoilAccumulated;
        
        if (remainingRoom > 0.01f)
        {
            // 计算实际应用的垂直后坐力（不超过剩余空间）
            float actualVertical = Mathf.Min(vertical, remainingRoom);
            
            // 真正改变玩家的视角（向上抬）
            _xRotation -= actualVertical; // 减少 xRotation 会让视角向上抬
            _xRotation = Mathf.Clamp(_xRotation, yClampMin, yClampMax);
            
            // 记录累积的真实后坐力
            trueRecoilAccumulated += actualVertical;
        }
        // 如果达到上限，垂直后坐力不再生效，但水平后坐力仍然生效
        
        // 水平后坐力：设置目标值，在 LateUpdate 中平滑过渡
        targetHorizontalRecoil += horizontal;
        horizontalRecoilSmoothTime = horizontalSmoothTime;
        
        // 重置恢复计时器
        trueRecoilRecoveryTimer = recoveryDelay;
        currentTrueRecoilRecoveryDelay = recoveryDelay;
        currentTrueRecoilRecoverySpeed = recoverySpeed;
    }

    /// <summary>
    /// 重置真实后坐力累积值（当玩家通过压枪完全抵消后坐力时调用）
    /// </summary>
    public void ResetTrueRecoilAccumulation()
    {
        trueRecoilAccumulated = 0f;
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
        // Frequency�ڰ��������� ����20fд����
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
