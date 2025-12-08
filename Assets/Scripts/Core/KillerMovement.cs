using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class KillerMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInputHandler inputHandler;

    [Header("Movement Stats")]
    [SerializeField] private float walkSpeed = 6.0f;
    [SerializeField] private float runSpeed = 12.0f;

    [Tooltip("瞄准时的移动速度倍率 (例如 0.5 表示半速)")]
    [Range(0.1f, 1f)][SerializeField] private float aimSpeedMultiplier = 0.5f;

    [SerializeField] private float acceleration = 100.0f;
    [SerializeField] private float deceleration = 100.0f;

    [Range(0f, 1f)][SerializeField] private float airControlMultiplier = 0.5f;

    [Header("Jumping & Gravity")]
    [SerializeField] private float jumpHeight = 2.0f;
    [SerializeField] private float jumpPostCooldown = 0.2f;
    [SerializeField] private float gravity = -40.0f;

    [Header("Ground Detection")]
    [SerializeField] private float groundCheckRadius = 0.25f;
    [SerializeField] private float groundCheckDistance = 0.15f;
    [SerializeField] private LayerMask groundLayer;

    // 内部状态
    private CharacterController _controller;
    private Vector3 _velocity;
    private Vector3 _horizontalVelocity;
    private bool _isGrounded;
    private float _jumpTimer = 0f;

    private void Start()
    {
        _controller = GetComponent<CharacterController>();
        _controller.minMoveDistance = 0f;
    }

    private void Update()
    {
        if (_jumpTimer > 0) _jumpTimer -= Time.deltaTime;

        HandleGroundDetection();
        HandleMovement();
        HandleGravity();

        Vector3 finalMove = _horizontalVelocity + Vector3.up * _velocity.y;
        _controller.Move(finalMove * Time.deltaTime);
    }

    private void HandleGroundDetection()
    {
        if (_jumpTimer > 0)
        {
            _isGrounded = false;
            return;
        }

        Vector3 sphereStart = transform.position + _controller.center + Vector3.down * (_controller.height * 0.5f - groundCheckRadius);
        bool hitGround = Physics.SphereCast(sphereStart, groundCheckRadius, Vector3.down, out RaycastHit hitInfo, groundCheckDistance, groundLayer);

        _isGrounded = hitGround;

        if (_isGrounded && _velocity.y < 0)
        {
            _velocity.y = -5f;
        }
    }

    private void HandleMovement()
    {
        // 计算目标速度
        float targetSpeed = inputHandler.Sprinting ? runSpeed : walkSpeed;

        if (inputHandler.Aiming)
        {
            targetSpeed *= aimSpeedMultiplier;
        }

        // 计算输入方向
        Vector3 inputDir = new Vector3(inputHandler.MoveInput.x, 0f, inputHandler.MoveInput.y).normalized;
        Vector3 targetDir = transform.TransformDirection(inputDir);

        // 计算水平速度
        Vector3 currentHorizontalVel = new Vector3(_controller.velocity.x, 0f, _controller.velocity.z);

        // 根据是否在地面上应用不同的加速度
        float accel = _isGrounded ? acceleration : acceleration * airControlMultiplier;

        // 应用加速度或减速度
        if (inputDir.magnitude > 0.1f) // 有输入
        {
            // 移动到目标速度
            _horizontalVelocity = Vector3.MoveTowards(currentHorizontalVel, targetDir * targetSpeed, accel * Time.deltaTime);
        }
        else
        {
            _horizontalVelocity = Vector3.MoveTowards(currentHorizontalVel, Vector3.zero, deceleration * Time.deltaTime);
        }

        if (_isGrounded && inputHandler.JumpTriggered)
        {
            _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            _jumpTimer = jumpPostCooldown;
            _isGrounded = false;
        }
    }

    private void HandleGravity()
    {
        if (!_isGrounded)
        {
            _velocity.y += gravity * Time.deltaTime;
        }
    }

    private void OnDrawGizmos()
    {
        if (_controller == null) return;
        Gizmos.color = _isGrounded ? Color.green : Color.red;
        Vector3 start = transform.position + _controller.center + Vector3.down * (_controller.height * 0.5f - groundCheckRadius);
        Vector3 end = start + Vector3.down * groundCheckDistance;
        Gizmos.DrawWireSphere(start, groundCheckRadius);
        Gizmos.DrawWireSphere(end, groundCheckRadius);
    }
}
