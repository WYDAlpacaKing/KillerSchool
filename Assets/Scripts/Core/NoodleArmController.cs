using System.Collections.Generic;
using UnityEngine;

public class NoodleArmController : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private PlayerInputHandler inputHandler;
    [Tooltip("必须赋值！通常是 Main Camera")]
    [SerializeField] private Transform cameraTransform;

    //调试状态枚举
    public enum DebugPoseState
    {
        Gameplay_Default = 0,
        Force_Empty = 1,
        Force_Hip = 2,
        Force_Aim = 3
    }

    [Header("Debug Workflow")]
    public DebugPoseState debugState = DebugPoseState.Gameplay_Default;

    [Header("Global Settings")]
    public bool startWithWeapon = false;
    public float transitionSpeed = 10f;

    [Header("Materials")]
    [SerializeField] private Material armMat;
    [SerializeField] private Material fingerMat;

    // 武器预制体
    [SerializeField] private WeaponBase startingWeaponPrefab;
    private WeaponBase activeWeapon;

    // --- 数据结构 ---
    [System.Serializable]
    public class ArmPose
    {
        [Header("1. Position (Local)")]
        public Vector3 shoulderPos = new Vector3(0.35f, -0.2f, 0.1f);
        public Vector3 wristPos = new Vector3(0.25f, -0.25f, 0.4f);

        [Header("2. Hand Shape")]
        public float wristGap = 0.05f;
        public Vector3 handDirection = new Vector3(10f, -15f, 0f);
        public float handLength = 0.08f;

        [Header("3. Visual Thickness")]
        public float armThickness = 0.02f;
        public float handThickness = 0.015f;

        [Header("4. Gun Settings")]
        public Vector3 gunRotation = new Vector3(5f, -5f, 0f);
        public Vector3 gunPosOffset = Vector3.zero;

        [Header("5. Physics & Wiggle")]
        public float swaySpeed = 15f;
        public float swayAmountX = 0.03f;
        public float swayAmountY = 0.02f;
    }

    [System.Serializable]
    public class ArmConfig
    {
        public string name = "Arm";
        [Header("--- States ---")]
        public ArmPose emptyPose;
        public ArmPose hipPose;
        public ArmPose aimPose;
    }

    [Header("Configuration")]
    [SerializeField] private ArmConfig rightArm;
    [SerializeField] private ArmConfig leftArm;

    // 中间态数据
    private Transform[] armTransforms = new Transform[2];
    private Transform[] fingerTransforms = new Transform[2];
    private LineRenderer[] armLines = new LineRenderer[2];
    private LineRenderer[] fingerLines = new LineRenderer[2];

    private ArmPose currentRightPose = new ArmPose();
    private ArmPose currentLeftPose = new ArmPose();
    private Vector3[] rightFingerLocalPos = new Vector3[2];

    private float[] swayTimeAccumulator = new float[2];

    private bool hasWeaponEquipped = false;
    private bool isAiming = false;
    private bool isShooting = false;

    // 射击计时器
    private float shootTimer = 0f;

    // 接收外部震动
    private Vector3 externalRecoilOffset = Vector3.zero;

    private void Start()
    {
        //注册监听
        DebuggerManager.RegisterWatcher(DebuggerModuleType.NoodleArm, "Is Aiming", () => isAiming);
        DebuggerManager.RegisterWatcher(DebuggerModuleType.NoodleArm, "Is Shooting", () => isShooting);
        DebuggerManager.RegisterWatcher(DebuggerModuleType.NoodleArm, "Has Weapon", () => hasWeaponEquipped);


        if (cameraTransform == null) cameraTransform = this.transform;
        if (armMat == null || fingerMat == null) { Debug.LogError("Assign Materials!"); return; }

        SetupLine(ref armLines[0], ref armTransforms[0], "RightArm", armMat);
        SetupLine(ref armLines[1], ref armTransforms[1], "LeftArm", armMat);
        SetupLine(ref fingerLines[0], ref fingerTransforms[0], "RightFinger", fingerMat);
        SetupLine(ref fingerLines[1], ref fingerTransforms[1], "LeftFinger", fingerMat);

        if (startingWeaponPrefab != null)
        {
            GameObject gunObj = Instantiate(startingWeaponPrefab.gameObject, cameraTransform);
            gunObj.transform.localPosition = Vector3.zero;
            gunObj.transform.localRotation = Quaternion.identity;

            activeWeapon = gunObj.GetComponent<WeaponBase>();
            if (activeWeapon != null)
            {
                activeWeapon.OnFire += OnWeaponFired;
                hasWeaponEquipped = startWithWeapon;
                if (hasWeaponEquipped) activeWeapon.OnEquip();
                else activeWeapon.OnUnequip();
            }
        }
    }

    private void SetupLine(ref LineRenderer lr, ref Transform container, string name, Material mat)
    {
        GameObject go = new GameObject(name);
        container = go.transform;
        go.transform.SetParent(cameraTransform, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;

        lr = go.AddComponent<LineRenderer>();
        lr.material = mat;
        lr.positionCount = 2;
        lr.useWorldSpace = false;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
    }

    private void LateUpdate()
    {
        if (cameraTransform != null)
        {
            ResetTransformToCamera(armTransforms[0]);
            ResetTransformToCamera(armTransforms[1]);
            ResetTransformToCamera(fingerTransforms[0]);
            ResetTransformToCamera(fingerTransforms[1]);
        }

        HandleInput();

        UpdateCurrentPose(rightArm, ref currentRightPose, 0);
        UpdateCurrentPose(leftArm, ref currentLeftPose, 1);

        CalculateArmPhysics(0, currentRightPose);
        CalculateArmPhysics(1, currentLeftPose);

        UpdateGunTransform();
    }

    private void HandleInput()
    {
        if (inputHandler == null) return;

        if (inputHandler.SwitchWeaponTriggered && activeWeapon != null)
        {
            ToggleWeapon();
        }

        if (debugState == DebugPoseState.Gameplay_Default)
        {
            isAiming = hasWeaponEquipped && inputHandler.Aiming;

            if (hasWeaponEquipped && activeWeapon != null)
            {
                // 将判断权移交给自己手中的武器
                activeWeapon.HandleFiringInput(inputHandler.FireTriggered, inputHandler.FireHeld);
            }
        }
        else
        {
            isAiming = (debugState == DebugPoseState.Force_Aim);
            bool debugHasGun = (debugState == DebugPoseState.Force_Hip || debugState == DebugPoseState.Force_Aim);
            if (debugHasGun && activeWeapon != null)
            {
                activeWeapon.HandleFiringInput(inputHandler.FireTriggered, inputHandler.FireHeld);
            }
        }

        if (shootTimer > 0) shootTimer -= Time.deltaTime;
        isShooting = shootTimer > 0;
    }

    private void ToggleWeapon()
    {
        hasWeaponEquipped = !hasWeaponEquipped;
        if (hasWeaponEquipped) activeWeapon.OnEquip();
        else activeWeapon.OnUnequip();
    }

    private void OnWeaponFired()
    {
        shootTimer = 0.1f;
        isShooting = true;
    }

    private void ResetTransformToCamera(Transform t)
    {
        if (t == null) return;
        if (t.parent != cameraTransform) t.SetParent(cameraTransform);
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;
    }

    private void UpdateCurrentPose(ArmConfig config, ref ArmPose runtimePose, int sideIndex)
    {
        ArmPose targetPose;

        if (debugState == DebugPoseState.Force_Empty) targetPose = config.emptyPose;
        else if (debugState == DebugPoseState.Force_Hip) targetPose = config.hipPose;
        else if (debugState == DebugPoseState.Force_Aim) targetPose = config.aimPose;
        else
        {
            if (!hasWeaponEquipped) targetPose = config.emptyPose;
            else if (isAiming) targetPose = config.aimPose;
            else targetPose = config.hipPose;
        }

        float speed = (debugState != DebugPoseState.Gameplay_Default) ? 50f : transitionSpeed;
        float t = Time.deltaTime * speed;

        runtimePose.shoulderPos = Vector3.Lerp(runtimePose.shoulderPos, targetPose.shoulderPos, t);
        runtimePose.wristPos = Vector3.Lerp(runtimePose.wristPos, targetPose.wristPos, t);
        runtimePose.wristGap = Mathf.Lerp(runtimePose.wristGap, targetPose.wristGap, t);
        runtimePose.handDirection = Vector3.Lerp(runtimePose.handDirection, targetPose.handDirection, t);
        runtimePose.handLength = Mathf.Lerp(runtimePose.handLength, targetPose.handLength, t);
        runtimePose.armThickness = Mathf.Lerp(runtimePose.armThickness, targetPose.armThickness, t);
        runtimePose.handThickness = Mathf.Lerp(runtimePose.handThickness, targetPose.handThickness, t);
        runtimePose.gunRotation = Vector3.Lerp(runtimePose.gunRotation, targetPose.gunRotation, t);
        runtimePose.gunPosOffset = Vector3.Lerp(runtimePose.gunPosOffset, targetPose.gunPosOffset, t);

        runtimePose.swaySpeed = Mathf.Lerp(runtimePose.swaySpeed, targetPose.swaySpeed, t);
        runtimePose.swayAmountX = Mathf.Lerp(runtimePose.swayAmountX, targetPose.swayAmountX, t);
        runtimePose.swayAmountY = Mathf.Lerp(runtimePose.swayAmountY, targetPose.swayAmountY, t);
    }

    private void CalculateArmPhysics(int index, ArmPose pose)
    {
        armLines[index].startWidth = pose.armThickness;
        armLines[index].endWidth = pose.armThickness;
        fingerLines[index].startWidth = pose.handThickness;
        fingerLines[index].endWidth = pose.handThickness;

        Vector3 shoulderLocal = pose.shoulderPos;
        Vector3 wristLocal = pose.wristPos;

        // 晃动
        swayTimeAccumulator[index] += Time.deltaTime * pose.swaySpeed;
        float currentTime = swayTimeAccumulator[index];

        float moveIntensity = (inputHandler != null && inputHandler.MoveInput.magnitude > 0.1f) ? 1.5f : 0.5f;
        if (debugState != DebugPoseState.Gameplay_Default) moveIntensity = 0.5f;

        float phase = index * 10f;
        float wiggleX = Mathf.Sin(currentTime + phase) * pose.swayAmountX * moveIntensity;
        float wiggleY = Mathf.Cos(currentTime * 0.8f + phase) * pose.swayAmountY * moveIntensity;

        wristLocal += new Vector3(wiggleX, wiggleY, 0);

        // GPT修复：坐标系对齐
        if (index == 0) // 仅右手处理后坐力
        {
            // 获取枪械当前的期望旋转 (Hip/Aim/Lerp 中的状态)
            Quaternion gunRotation = Quaternion.Euler(pose.gunRotation);

            // 将“枪械局部震动”(externalRecoilOffset) 旋转到“摄像机空间”
            // 原理：如果枪歪了 45 度，后退的震动也应该歪 45 度
            Vector3 alignedRecoil = gunRotation * externalRecoilOffset;

            // 叠加到位移上
            wristLocal += alignedRecoil;
        }

        // 手指位置计算
        Quaternion handRot = Quaternion.Euler(pose.handDirection);
        Vector3 fingerDir = handRot * Vector3.forward;

        Vector3 fingerStartLocal = wristLocal + (fingerDir * pose.wristGap);
        Vector3 fingerEndLocal = fingerStartLocal + (fingerDir * pose.handLength);

        // 应用到线段
        armLines[index].SetPosition(0, shoulderLocal);
        armLines[index].SetPosition(1, wristLocal);
        fingerLines[index].SetPosition(0, fingerStartLocal);
        fingerLines[index].SetPosition(1, fingerEndLocal);

        // 记录右手手指位置供枪械使用
        if (index == 0)
        {
            rightFingerLocalPos[0] = fingerStartLocal;
            rightFingerLocalPos[1] = fingerEndLocal;
        }
    }


    /// <summary>
    /// 更新枪械位置和旋转
    /// </summary>
    private void UpdateGunTransform()
    {
        if (activeWeapon == null) return;

        if (debugState != DebugPoseState.Gameplay_Default)
        {
            bool show = (debugState != DebugPoseState.Force_Empty);
            if (activeWeapon.gameObject.activeSelf != show) activeWeapon.gameObject.SetActive(show);
        }

        if (!activeWeapon.gameObject.activeSelf) return;

        //保证静态偏移也是跟随枪身方向的
        Quaternion currentGunRot = Quaternion.Euler(currentRightPose.gunRotation);
        Vector3 finalPos = rightFingerLocalPos[1] + (currentGunRot * currentRightPose.gunPosOffset);

        activeWeapon.transform.localPosition = finalPos;

        Quaternion targetRot = currentGunRot;
        activeWeapon.transform.localRotation = Quaternion.Lerp(activeWeapon.transform.localRotation, targetRot, Time.deltaTime * 20f);
    }

    [ContextMenu(">>> Mirror Right Arm to Left Arm <<<")]
    private void MirrorRightToLeft()
    {
        leftArm.emptyPose = CreateMirroredPose(rightArm.emptyPose);
        leftArm.hipPose = CreateMirroredPose(rightArm.hipPose);
        leftArm.aimPose = CreateMirroredPose(rightArm.aimPose);
        Debug.Log("<color=green>NoodleArm: 镜像完成。</color>");
    }

    private ArmPose CreateMirroredPose(ArmPose source)
    {
        ArmPose mirror = new ArmPose();
        mirror.shoulderPos = new Vector3(-source.shoulderPos.x, source.shoulderPos.y, source.shoulderPos.z);
        mirror.wristPos = new Vector3(-source.wristPos.x, source.wristPos.y, source.wristPos.z);
        mirror.handDirection = new Vector3(source.handDirection.x, -source.handDirection.y, source.handDirection.z);
        mirror.gunRotation = new Vector3(source.gunRotation.x, -source.gunRotation.y, source.gunRotation.z);

        mirror.wristGap = source.wristGap;
        mirror.handLength = source.handLength;
        mirror.armThickness = source.armThickness;
        mirror.handThickness = source.handThickness;
        mirror.gunPosOffset = new Vector3(-source.gunPosOffset.x, source.gunPosOffset.y, source.gunPosOffset.z);

        mirror.swaySpeed = source.swaySpeed;
        mirror.swayAmountX = source.swayAmountX;
        mirror.swayAmountY = source.swayAmountY;
        return mirror;
    }

    public void ApplyExternalRecoil(Vector3 offset)
    {
        // 接收原始的枪系震动向量 (例如 (0, 0, -0.1))
        externalRecoilOffset = offset;
    }
}
