using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float acceleration = 90f;
    [SerializeField] private float deceleration = 110f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 15f;
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBuffer = 0.12f;
    [SerializeField] private float baseGravityScale = 4f;
    [SerializeField] private float fallGravityMultiplier = 2.2f;
    [SerializeField] private float lowJumpGravityMultiplier = 1.8f;

    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundLayers = ~0;
    [SerializeField] private float groundCheckDistance = 0.08f;

    [Header("Aim")]
    [SerializeField] private Transform pivot;

    private BoxCollider2D boxCollider;
    private Camera mainCamera;
    private Rigidbody2D rigidbody2d;

    private float coyoteCounter;
    private float jumpBufferCounter;
    private float moveInput;
    private bool isGrounded;
    private bool jumpHeld;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        rigidbody2d = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;

        if (baseGravityScale <= 0f)
        {
            baseGravityScale = rigidbody2d.gravityScale > 0f ? rigidbody2d.gravityScale : 1f;
        }

        rigidbody2d.gravityScale = baseGravityScale;
        pivot ??= transform.Find("Pivot");
    }

    private void Update()
    {
        moveInput = ReadHorizontalInput();
        jumpHeld = IsJumpHeld();

        if (WasJumpPressedThisFrame())
        {
            jumpBufferCounter = jumpBuffer;
        }
        else
        {
            jumpBufferCounter = Mathf.Max(0f, jumpBufferCounter - Time.deltaTime);
        }

        UpdateAim();
    }

    private void FixedUpdate()
    {
        isGrounded = rigidbody2d.velocity.y > 0.01f ? false : CheckGrounded();
        coyoteCounter = isGrounded ? coyoteTime : Mathf.Max(0f, coyoteCounter - Time.fixedDeltaTime);

        ApplyHorizontalMovement();
        TryConsumeJump();
        ApplyVariableGravity();
    }

    private void ApplyHorizontalMovement()
    {
        float targetSpeed = moveInput * moveSpeed;
        float speedDelta = Mathf.Abs(targetSpeed) > 0.01f ? acceleration : deceleration;
        float nextVelocityX = Mathf.MoveTowards(rigidbody2d.velocity.x, targetSpeed, speedDelta * Time.fixedDeltaTime);

        rigidbody2d.velocity = new Vector2(nextVelocityX, rigidbody2d.velocity.y);
    }

    private void TryConsumeJump()
    {
        if (jumpBufferCounter <= 0f || coyoteCounter <= 0f)
        {
            return;
        }

        Vector2 nextVelocity = rigidbody2d.velocity;
        nextVelocity.y = jumpForce;
        rigidbody2d.velocity = nextVelocity;

        coyoteCounter = 0f;
        jumpBufferCounter = 0f;
        isGrounded = false;
    }

    private void ApplyVariableGravity()
    {
        if (rigidbody2d.velocity.y < 0f)
        {
            rigidbody2d.gravityScale = baseGravityScale * fallGravityMultiplier;
            return;
        }

        if (rigidbody2d.velocity.y > 0f && !jumpHeld)
        {
            rigidbody2d.gravityScale = baseGravityScale * lowJumpGravityMultiplier;
            return;
        }

        rigidbody2d.gravityScale = baseGravityScale;
    }

    private void UpdateAim()
    {
        if (pivot == null || Mouse.current == null)
        {
            return;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            return;
        }

        Vector3 mouseScreenPosition = Mouse.current.position.ReadValue();
        mouseScreenPosition.z = Mathf.Abs(mainCamera.transform.position.z - pivot.position.z);

        Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(mouseScreenPosition);
        Vector2 aimDirection = mouseWorldPosition - pivot.position;

        if (aimDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        float aimAngle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
        pivot.rotation = Quaternion.Euler(0f, 0f, aimAngle);
    }

    private bool CheckGrounded()
    {
        Bounds bounds = boxCollider.bounds;
        Vector2 boxSize = new Vector2(bounds.size.x * 0.95f, bounds.size.y * 0.98f);

        RaycastHit2D hit = Physics2D.BoxCast(bounds.center, boxSize, 0f, Vector2.down, groundCheckDistance, groundLayers);
        return hit.collider != null;
    }

    private static float ReadHorizontalInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return 0f;
        }

        float input = 0f;

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
        {
            input -= 1f;
        }

        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
        {
            input += 1f;
        }

        return input;
    }

    private static bool IsJumpHeld()
    {
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.spaceKey.isPressed;
    }

    private static bool WasJumpPressedThisFrame()
    {
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.spaceKey.wasPressedThisFrame;
    }

    private void OnValidate()
    {
        moveSpeed = Mathf.Max(0f, moveSpeed);
        acceleration = Mathf.Max(0f, acceleration);
        deceleration = Mathf.Max(0f, deceleration);
        jumpForce = Mathf.Max(0f, jumpForce);
        coyoteTime = Mathf.Max(0f, coyoteTime);
        jumpBuffer = Mathf.Max(0f, jumpBuffer);
        baseGravityScale = Mathf.Max(0.01f, baseGravityScale);
        fallGravityMultiplier = Mathf.Max(1f, fallGravityMultiplier);
        lowJumpGravityMultiplier = Mathf.Max(1f, lowJumpGravityMultiplier);
        groundCheckDistance = Mathf.Max(0.01f, groundCheckDistance);
    }
}