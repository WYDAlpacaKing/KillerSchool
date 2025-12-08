using UnityEngine;

public class ProceduralWeaponAnimator : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private WeaponFeelConfig config;

    [Header("References")]
    [SerializeField] private Transform modelTransform;
    [SerializeField] private PlayerInputHandler inputHandler;

    private NoodleArmController noodleArm;
    private CrosshairController crosshair; // 引用准星

    [Header("Recoil Transfer")]
    [Range(0f, 1f)][SerializeField] private float armRecoilTransfer = 0.5f;

    
    private Spring posSpring;// 位置弹簧
    private Spring rotSpring;// 旋转弹簧
    private Vector3 swayPos;// 摆动位置
    private Vector3 bobPos;// 摇晃位置

    //散步相关参数
    private float currentSpread = 0f;
    private float shootingSpread = 0f;

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

        // 计算散步
        CalculateSpread(dt);

        // 弹簧更新
        CalculateSway(dt);
        CalculateBob(dt);

        Vector3 totalPosTarget = swayPos + bobPos;
        Vector3 totalRotTarget = new Vector3(-swayPos.y * 30f, swayPos.x * 30f, 0);

        posSpring.SetTarget(totalPosTarget);
        rotSpring.SetTarget(totalRotTarget);

        Vector3 finalPos = posSpring.Update(dt);
        Vector3 finalRot = rotSpring.Update(dt);

        // 传递震动
        if (noodleArm != null)
        {
            Vector3 recoilOnly = finalPos - totalPosTarget;
            noodleArm.ApplyExternalRecoil(recoilOnly * armRecoilTransfer);
        }

        modelTransform.localPosition = finalPos;
        modelTransform.localRotation = Quaternion.Euler(finalRot);
    }

    /// <summary>
    /// Calculates the current spread of the weapon based on movement, aiming, and shooting conditions.
    /// </summary>
    /// <remarks>This method adjusts the weapon's spread dynamically by considering the player's movement and
    /// aiming status, as well as the recovery from shooting spread over time. The calculated spread is then applied to
    /// both the crosshair UI and the weapon's accuracy.</remarks>
    /// <param name="dt"></param>
    private void CalculateSpread(float dt)
    {
        // 状态判断
        bool isMoving = inputHandler != null && inputHandler.MoveInput.magnitude > 0.1f;
       
        bool isAiming = false; // TODO: 获取真实瞄准状态

        // 计算基础散布
        float targetBase = config.baseSpread;
        if (isMoving) targetBase *= config.movementSpreadPenalty;
        if (isAiming) targetBase *= 0.2f; // 瞄准时散布极小

        // 射击额外散布已经在 OnWeaponFired 中处理 
        shootingSpread = Mathf.Lerp(shootingSpread, 0f, dt * config.spreadRecoverySpeed);

        // 整合散布值
        currentSpread = Mathf.Clamp(targetBase + shootingSpread, 0f, config.maxSpread);

        
        if (crosshair != null)
        {
            crosshair.UpdateSpread(currentSpread);
        }

        // 应用到武器精度
        if (weaponBase != null)
        {
            weaponBase.currentSpread = currentSpread;
        }
    }

    private void OnWeaponFired()
    {
        // 物理反馈
        posSpring.AddForce(config.kickbackForce * 50f);
        rotSpring.AddForce(new Vector3(-config.kickbackForce.y * 300f, Random.Range(-10f, 10f), Random.Range(-20f, 20f)));

        // 射击散布增加
        shootingSpread += config.spreadPerShot;

        // 相机震动和后坐力
        if (camController != null)
        {
            float vRecoil = Random.Range(config.verticalRecoil.x, config.verticalRecoil.y);
            float hRecoil = Random.Range(config.horizontalRecoil.x, config.horizontalRecoil.y);
            camController.ApplyRecoil(vRecoil, hRecoil, config.recoilRecoveryDuration, config.recoilRecoveryCurve);
            camController.ApplyShake(config.shakeAmplitude, config.shakeFrequency, config.shakeDuration);
            camController.ApplyFOVKick(config.fovKick);
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
