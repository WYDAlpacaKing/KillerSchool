using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    [Header("Input Settings")]
    [SerializeField] private float mouseSensitivity = 1.0f;

    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    public bool JumpTriggered { get; private set; }
    public bool Sprinting { get; private set; }
    public bool CrouchTriggered { get; private set; }
    public bool InteractTriggered { get; private set; }

    // 武器
    public bool FireTriggered { get; private set; } 
    public bool FireHeld { get; private set; }   
    public bool Aiming { get; private set; }
    public bool SwitchWeaponTriggered { get; private set; }

    private void Update()
    {
        // 如果调试面板打开，则忽略所有输入
        if (DebuggerManager.Instance != null && DebuggerManager.Instance.IsVisible)
        {
            // 重置所有输入
            MoveInput = Vector2.zero;
            LookInput = Vector2.zero;
            JumpTriggered = false;
            Sprinting = false;
            CrouchTriggered = false;
            FireTriggered = false;
            FireHeld = false;
            Aiming = false;
            SwitchWeaponTriggered = false;
            InteractTriggered = false;
            return;
        }
        InteractTriggered = Input.GetKeyDown(KeyCode.E);
        MoveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        LookInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * mouseSensitivity;

        JumpTriggered = Input.GetButtonDown("Jump");
        Sprinting = Input.GetKey(KeyCode.LeftShift);
        CrouchTriggered = Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.LeftControl);

        // 战斗输入
        FireTriggered = Input.GetMouseButtonDown(0);
        FireHeld = Input.GetMouseButton(0); 

        Aiming = Input.GetMouseButton(1);
        SwitchWeaponTriggered = Input.GetKeyDown(KeyCode.Q);
    }
}
