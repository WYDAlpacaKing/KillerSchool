using UnityEngine;

public class ProceduralWeaponAnimator : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private WeaponFeelConfig config;

    [Header("References")]
    [SerializeField] private Transform modelTransform;
    [SerializeField] private PlayerInputHandler inputHandler;

    private NoodleArmController noodleArm;
    private CrosshairController crosshair; // ����׼��

    [Header("Recoil Transfer")]
    [Range(0f, 1f)][SerializeField] private float armRecoilTransfer = 0.5f;

    
    private Spring posSpring;// λ�õ���
    private Spring rotSpring;// ��ת����
    private Vector3 swayPos;// �ڶ�λ��
    private Vector3 bobPos;// ҡ��λ��

    // 散布相关参数
    private float currentSpread = 0f;
    private float shootingSpread = 0f;

    // 半自动武器快速点射惩罚
    private float lastFireTime = 0f;
    private float rapidFirePenalty = 0f;

    // 水平后坐力模式相关
    private int shotCount = 0; // 连续射击计数（用于交替和模式）
    private int alternatingDirection = 1; // 交替方向：1=右，-1=左
    private float lastShotTime = 0f; // 上次射击时间（用于重置连射计数）

    private WeaponBase weaponBase;
    private FPSCameraController camController;

    private void Start()
    {
        posSpring = new Spring();
        rotSpring = new Spring();
        UpdateSpringParameters();
        posSpring.Init();
        rotSpring.Init();

        weaponBase = GetComponent<WeaponBase>();
        camController = Camera.main.GetComponent<FPSCameraController>();
        if (inputHandler == null) inputHandler = FindFirstObjectByType<PlayerInputHandler>();
        noodleArm = FindFirstObjectByType<NoodleArmController>();
        crosshair = FindFirstObjectByType<CrosshairController>();

        if (weaponBase != null) weaponBase.OnFire += OnWeaponFired;
    }

    private void OnDestroy()
    {
        if (weaponBase != null) weaponBase.OnFire -= OnWeaponFired;
    }

    private void LateUpdate()
    {
        if (config == null || modelTransform == null) return;

        float dt = Time.deltaTime;

#if UNITY_EDITOR
        UpdateSpringParameters();
#endif

        // ����ɢ��
        CalculateSpread(dt);

        // ���ɸ���
        CalculateSway(dt);
        CalculateBob(dt);

        Vector3 totalPosTarget = swayPos + bobPos;
        Vector3 totalRotTarget = new Vector3(-swayPos.y * 30f, swayPos.x * 30f, 0);

        posSpring.SetTarget(totalPosTarget);
        rotSpring.SetTarget(totalRotTarget);

        Vector3 finalPos = posSpring.Update(dt);
        Vector3 finalRot = rotSpring.Update(dt);

        // ������
        if (noodleArm != null)
        {
            Vector3 recoilOnly = finalPos - totalPosTarget;
            noodleArm.ApplyExternalRecoil(recoilOnly * armRecoilTransfer);
        }

        modelTransform.localPosition = finalPos;
        // �����ת�Ƕ�̫�󣬿��ܻ��������������⣬����ʹ�� Quaternion.Lerp ��ƽ����ת
        modelTransform.localEulerAngles = Vector3.Lerp(modelTransform.localEulerAngles, finalRot, dt * 15f);
        modelTransform.localRotation = Quaternion.Euler(finalRot);
    }

    /// <summary>
    /// 计算武器当前散布值，基于移动、瞄准和射击状态
    /// </summary>
    /// <param name="dt">帧间隔时间</param>
    private void CalculateSpread(float dt)
    {
        // 状态判定
        bool isMoving = inputHandler != null && inputHandler.MoveInput.magnitude > 0.1f;
        bool isAiming = false; // TODO: 获取真实瞄准状态

        // 计算基础散布
        float targetBase = config.baseSpread;
        if (isMoving) targetBase *= config.movementSpreadPenalty;
        if (isAiming) targetBase *= 0.2f; // 瞄准时散布减小

        // 射击散布恢复（已经在 OnWeaponFired 中处理）
        shootingSpread = Mathf.Lerp(shootingSpread, 0f, dt * config.spreadRecoverySpeed);

        // 半自动武器快速点射惩罚恢复
        rapidFirePenalty = Mathf.Lerp(rapidFirePenalty, 0f, dt * config.spreadRecoverySpeed * 0.5f);

        // 计算总散布值
        currentSpread = Mathf.Clamp(targetBase + shootingSpread + rapidFirePenalty, 0f, config.maxSpread);

        if (crosshair != null)
        {
            crosshair.UpdateSpread(currentSpread);
        }

        // 应用到武器脚本
        if (weaponBase != null)
        {
            weaponBase.currentSpread = currentSpread;
        }
    }

    private void OnWeaponFired()
    {
        // 枪身后坐力
        posSpring.AddForce(config.kickbackForce * 50f);
        rotSpring.AddForce(new Vector3(-config.kickbackForce.y * 300f, Random.Range(-10f, 10f), Random.Range(-20f, 20f)));

        // 基础射击散布增加
        shootingSpread += config.spreadPerShot;

        // 更新连射计数（如果距离上次射击超过一定时间则重置）
        float timeSinceLastShot = Time.time - lastShotTime;
        if (timeSinceLastShot > 0.5f) // 超过0.5秒视为新的一轮射击
        {
            shotCount = 0;
            alternatingDirection = 1;
        }
        shotCount++;
        lastShotTime = Time.time;

        // 计算后坐力值
        float vRecoil = Random.Range(config.verticalRecoil.x, config.verticalRecoil.y);
        float hRecoil = CalculateHorizontalRecoil();

        // 根据武器类型应用不同的后坐力逻辑
        if (camController != null && weaponBase != null)
        {
            bool isFullAuto = weaponBase.fireMode == WeaponBase.FireMode.FullAuto;

            if (isFullAuto)
            {
                // 全自动武器：混合真实后坐力和视觉后坐力
                float trueRatio = config.trueRecoilRatio;
                float visualRatio = 1f - trueRatio;

                // 真实后坐力（会真正改变视角，需要玩家压枪）
                if (trueRatio > 0)
                {
                    camController.ApplyTrueRecoil(
                        vRecoil * trueRatio,
                        hRecoil * trueRatio,
                        config.trueRecoilRecoveryDelay,
                        config.trueRecoilRecoverySpeed,
                        config.maxTrueRecoilAccumulation
                    );
                }

                // 视觉后坐力（自动回复，增加打击感）
                if (visualRatio > 0)
                {
                    camController.ApplyRecoil(
                        vRecoil * visualRatio,
                        hRecoil * visualRatio,
                        config.recoilRecoveryDuration,
                        config.recoilRecoveryCurve
                    );
                }
            }
            else
            {
                // 半自动武器：只使用视觉后坐力（自动回复）
                camController.ApplyRecoil(vRecoil, hRecoil, config.recoilRecoveryDuration, config.recoilRecoveryCurve);

                // 检测快速点射并增加散布惩罚
                float timeSinceLastFire = Time.time - lastFireTime;
                if (timeSinceLastFire < config.rapidFireThreshold)
                {
                    // 连续快速点射，增加散布惩罚
                    rapidFirePenalty = Mathf.Min(
                        rapidFirePenalty + config.rapidFireSpreadPenalty,
                        config.maxRapidFirePenalty
                    );
                }
            }

            // 通用效果：屏幕震动和FOV冲击
            camController.ApplyShake(config.shakeAmplitude, config.shakeFrequency, config.shakeDuration);
            camController.ApplyFOVKick(config.fovKick);
        }

        // 记录开火时间（用于半自动快速点射检测）
        lastFireTime = Time.time;
    }

    /// <summary>
    /// 根据配置的水平后坐力模式计算本次射击的水平后坐力
    /// </summary>
    private float CalculateHorizontalRecoil()
    {
        switch (config.horizontalMode)
        {
            case WeaponFeelConfig.HorizontalRecoilMode.Random:
                // 随机模式：在范围内随机
                return Random.Range(config.horizontalRecoil.x, config.horizontalRecoil.y);

            case WeaponFeelConfig.HorizontalRecoilMode.Alternating:
                // 交替模式：左右来回跳动
                float baseRecoil = config.alternatingRecoilAmount * alternatingDirection;
                float randomOffset = Random.Range(-config.alternatingRandomness, config.alternatingRandomness);
                alternatingDirection *= -1; // 翻转方向
                return baseRecoil + randomOffset;

            case WeaponFeelConfig.HorizontalRecoilMode.Pattern:
                // 模式序列：按预设顺序执行
                if (config.recoilPattern != null && config.recoilPattern.Length > 0)
                {
                    int patternIndex = (shotCount - 1) % config.recoilPattern.Length;
                    return config.recoilPattern[patternIndex];
                }
                return 0f;

            default:
                return Random.Range(config.horizontalRecoil.x, config.horizontalRecoil.y);
        }
    }

    /// <summary>
    /// Updates the parameters of the position and rotation springs based on the current configuration.
    /// </summary>
    /// <remarks>This method adjusts the stiffness, damping, and mass of both position and rotation springs.
    /// The mass is constrained to a minimum value of 0.01 to ensure stability.</remarks>
    private void UpdateSpringParameters()
    {
        if (posSpring == null) return;
        posSpring.stiffness = config.positionSpring.stiffness; posSpring.damping = config.positionSpring.damping; posSpring.mass = Mathf.Max(0.01f, config.positionSpring.mass);
        rotSpring.stiffness = config.rotationSpring.stiffness; rotSpring.damping = config.rotationSpring.damping; rotSpring.mass = Mathf.Max(0.01f, config.rotationSpring.mass);
    }

    /// <summary>
    /// Adjusts the sway position based on the current input and configuration settings.
    /// </summary>
    /// <remarks>This method calculates the sway effect by adjusting the position based on input values. The
    /// sway is clamped to a maximum value defined in the configuration.</remarks>
    /// <param name="dt">The delta time since the last frame, used to smooth the sway transition.</param>
    private void CalculateSway(float dt)
    {
        if (!inputHandler) return;
        Vector2 i = inputHandler.LookInput;
        Vector3 t = new Vector3(Mathf.Clamp(-i.x * config.swayAmount, -config.maxSwayAmount, config.maxSwayAmount), Mathf.Clamp(-i.y * config.swayAmount, -config.maxSwayAmount, config.maxSwayAmount), 0);
        swayPos = Vector3.Lerp(swayPos, t, dt * config.swaySmoothing);
    }

    /// <summary>
    /// Updates the bobbing position based on the current time and movement input.
    /// </summary>
    /// <remarks>The bobbing effect is calculated using a sine wave function influenced by the configured
    /// frequency and amplitude. If there is significant movement input, the bobbing effect is intensified.</remarks>
    /// <param name="dt">The delta time since the last update, used to adjust the bobbing calculation.</param>
    private void CalculateBob(float dt)
    {
        float w = Mathf.Sin(Time.time * config.bobFrequency) * config.bobAmplitude;
        if (inputHandler && inputHandler.MoveInput.magnitude > 0.1f) w *= 2f;
        bobPos = new Vector3(w * 0.5f, w, 0);
    }
}
