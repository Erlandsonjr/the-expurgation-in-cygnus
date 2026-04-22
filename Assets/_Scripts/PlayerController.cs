using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class PlayerController : MonoBehaviour, IDamageable
{
    private static readonly int SpeedParameterHash = Animator.StringToHash("Speed");
    private const float InvincibilityFlickerInterval = 0.08f;

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
    [FormerlySerializedAs("pivot")]
    [SerializeField] private Transform aimPivot;

    [Header("Presentation")]
    [SerializeField] private Animator bodyAnimator;
    [SerializeField] private SpriteRenderer bodySpriteRenderer;

    [Header("Health")]
    [SerializeField] private float maxHealth = 5f;
    [SerializeField] private float invincibilityDuration = 1f;
    [SerializeField] private float knockbackForce = 9f;
    [SerializeField] private float knockbackTotalTime = 0.2f;

    [Header("Combat")]
    [SerializeField] private WeaponData activeWeapon;
    [SerializeField] private ProjectilePooler projectilePooler;

    private BoxCollider2D boxCollider;
    private Color bodyDefaultColor = Color.white;
    private Camera mainCamera;
    private float currentHealth;
    private Coroutine invincibilityRoutine;
    private float knockbackCounter;
    private Rigidbody2D rigidbody2d;

    private float coyoteCounter;
    private float jumpBufferCounter;
    private float shotCooldownTimer;
    private bool isInvincible;
    private Vector2 moveInput;
    private bool isGrounded;
    private bool jumpHeld;

    public event Action<float, float> HealthChanged;

    public bool IsInvincible => isInvincible;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

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
        aimPivot ??= transform.Find("Pivot");

        Transform bodyTransform = transform.Find("Body");
        if (bodyTransform != null)
        {
            bodyAnimator ??= bodyTransform.GetComponent<Animator>();
            bodySpriteRenderer ??= bodyTransform.GetComponent<SpriteRenderer>();
        }

        bodyAnimator ??= GetComponentInChildren<Animator>();
        bodySpriteRenderer ??= GetComponentInChildren<SpriteRenderer>();

        maxHealth = maxHealth > 0f ? maxHealth : 5f;
        invincibilityDuration = Mathf.Max(0f, invincibilityDuration);
        knockbackForce = Mathf.Max(0f, knockbackForce);
        currentHealth = maxHealth;
        isInvincible = false;

        if (bodySpriteRenderer != null)
        {
            bodyDefaultColor = bodySpriteRenderer.color;
        }

        NotifyHealthChanged();
    }

    private void Update()
    {
        if (knockbackCounter > 0f)
        {
            knockbackCounter = Mathf.Max(0f, knockbackCounter - Time.deltaTime);
            moveInput = Vector2.zero;
        }
        else
        {
            moveInput = ReadMoveInput();
        }

        jumpHeld = IsJumpHeld();
        UpdateAnimation();

        if (WasJumpPressedThisFrame())
        {
            jumpBufferCounter = jumpBuffer;
        }
        else
        {
            jumpBufferCounter = Mathf.Max(0f, jumpBufferCounter - Time.deltaTime);
        }

        shotCooldownTimer = Mathf.Max(0f, shotCooldownTimer - Time.deltaTime);
        UpdateAim();
        HandlePrimaryFire();
    }

    private void FixedUpdate()
    {
        isGrounded = rigidbody2d.linearVelocity.y > 0.01f ? false : CheckGrounded();
        coyoteCounter = isGrounded ? coyoteTime : Mathf.Max(0f, coyoteCounter - Time.fixedDeltaTime);

        if (knockbackCounter <= 0f)
        {
            ApplyHorizontalMovement();
        }

        TryConsumeJump();
        ApplyVariableGravity();
    }

    private void ApplyHorizontalMovement()
    {
        float targetSpeed = moveInput.x * moveSpeed;
        float speedDelta = Mathf.Abs(targetSpeed) > 0.01f ? acceleration : deceleration;
        float nextVelocityX = Mathf.MoveTowards(rigidbody2d.linearVelocity.x, targetSpeed, speedDelta * Time.fixedDeltaTime);

        rigidbody2d.linearVelocity = new Vector2(nextVelocityX, rigidbody2d.linearVelocity.y);
    }

    private void TryConsumeJump()
    {
        if (jumpBufferCounter <= 0f || coyoteCounter <= 0f)
        {
            return;
        }

        Vector2 nextVelocity = rigidbody2d.linearVelocity;
        nextVelocity.y = jumpForce;
        rigidbody2d.linearVelocity = nextVelocity;

        coyoteCounter = 0f;
        jumpBufferCounter = 0f;
        isGrounded = false;
    }

    private void ApplyVariableGravity()
    {
        if (rigidbody2d.linearVelocity.y < 0f)
        {
            rigidbody2d.gravityScale = baseGravityScale * fallGravityMultiplier;
            return;
        }

        if (rigidbody2d.linearVelocity.y > 0f && !jumpHeld)
        {
            rigidbody2d.gravityScale = baseGravityScale * lowJumpGravityMultiplier;
            return;
        }

        rigidbody2d.gravityScale = baseGravityScale;
    }

    private void UpdateAim()
    {
        if (aimPivot == null || Mouse.current == null)
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
        mouseScreenPosition.z = Mathf.Abs(mainCamera.transform.position.z - aimPivot.position.z);

        Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(mouseScreenPosition);
        if (bodySpriteRenderer != null)
        {
            bodySpriteRenderer.flipX = mouseWorldPosition.x < transform.position.x;
        }

        Vector2 aimDirection = mouseWorldPosition - aimPivot.position;

        if (aimDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        float aimAngle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
        aimPivot.rotation = Quaternion.Euler(0f, 0f, aimAngle);
    }

    private void UpdateAnimation()
    {
        if (bodyAnimator == null)
        {
            return;
        }

        bodyAnimator.SetFloat(SpeedParameterHash, moveInput.magnitude);
    }

    private void HandlePrimaryFire()
    {
        if (!IsPrimaryFireHeld())
        {
            return;
        }

        TryFireProjectile();
    }

    private void TryFireProjectile()
    {
        if (activeWeapon == null || projectilePooler == null || aimPivot == null || shotCooldownTimer > 0f)
        {
            return;
        }

        GameObject projectileObject = projectilePooler.GetProjectile(aimPivot.position, aimPivot.rotation);
        if (projectileObject == null)
        {
            return;
        }

        Vector3 spawnPosition = aimPivot.position;
        Quaternion spawnRotation = aimPivot.rotation;

        projectileObject.transform.SetPositionAndRotation(spawnPosition, spawnRotation);

        if (projectileObject.TryGetComponent(out Rigidbody2D projectileRigidbody))
        {
            projectileRigidbody.linearVelocity = Vector2.zero;
            projectileRigidbody.angularVelocity = 0f;
            projectileRigidbody.position = spawnPosition;
            projectileRigidbody.rotation = spawnRotation.eulerAngles.z;
        }

        if (!projectileObject.TryGetComponent(out Projectile projectile))
        {
            projectilePooler.ReturnProjectile(projectileObject);
            return;
        }

        projectile.Setup(activeWeapon.ProjectileSpeed, activeWeapon.Damage);
        shotCooldownTimer = activeWeapon.ShotInterval;
    }

    public void TakeDamage(float damage)
    {
        TakeDamage(damage, transform.position);
    }

    public void TakeDamage(float damage, Vector2 sourcePosition)
    {
        if (damage <= 0f || isInvincible)
        {
            return;
        }

        currentHealth = Mathf.Max(0f, currentHealth - damage);
        knockbackCounter = knockbackTotalTime;
        NotifyHealthChanged();

        StartInvincibilityFrames();
        ApplyKnockback(sourcePosition);
    }

    public void UpdateUI()
    {
        NotifyHealthChanged();
    }

    private void ApplyKnockback(Vector2 sourcePosition)
    {
        if (knockbackForce <= 0f)
        {
            return;
        }

        Vector2 knockbackDirection = (Vector2)transform.position - sourcePosition;
        if (knockbackDirection.sqrMagnitude <= 0.0001f)
        {
            knockbackDirection = Vector2.right;
        }

        knockbackDirection = knockbackDirection.normalized;
        rigidbody2d.linearVelocity = Vector2.zero;
        rigidbody2d.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);
    }

    private void StartInvincibilityFrames()
    {
        if (invincibilityRoutine != null)
        {
            StopCoroutine(invincibilityRoutine);
        }

        invincibilityRoutine = StartCoroutine(InvincibilityRoutine());
    }

    private IEnumerator InvincibilityRoutine()
    {
        isInvincible = true;
        float elapsed = 0f;
        bool faded = false;

        while (elapsed < invincibilityDuration)
        {
            SetBodyAlpha(faded ? 1f : 0.2f);
            faded = !faded;

            float waitDuration = Mathf.Min(InvincibilityFlickerInterval, invincibilityDuration - elapsed);
            yield return new WaitForSeconds(waitDuration);
            elapsed += waitDuration;
        }

        SetBodyAlpha(1f);
        isInvincible = false;
        invincibilityRoutine = null;
    }

    private void SetBodyAlpha(float alpha)
    {
        if (bodySpriteRenderer == null)
        {
            return;
        }

        Color color = bodyDefaultColor;
        color.a = alpha;
        bodySpriteRenderer.color = color;
    }

    private void NotifyHealthChanged()
    {
        HealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private bool CheckGrounded()
    {
        Bounds bounds = boxCollider.bounds;
        Vector2 boxSize = new Vector2(bounds.size.x * 0.95f, bounds.size.y * 0.98f);

        RaycastHit2D hit = Physics2D.BoxCast(bounds.center, boxSize, 0f, Vector2.down, groundCheckDistance, groundLayers);
        return hit.collider != null;
    }

    private static Vector2 ReadMoveInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return Vector2.zero;
        }

        float horizontalInput = 0f;

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
        {
            horizontalInput -= 1f;
        }

        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
        {
            horizontalInput += 1f;
        }

        return new Vector2(horizontalInput, 0f);
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

    private static bool IsPrimaryFireHeld()
    {
        Mouse mouse = Mouse.current;
        return mouse != null && mouse.leftButton.isPressed;
    }

    private void OnDisable()
    {
        if (invincibilityRoutine != null)
        {
            StopCoroutine(invincibilityRoutine);
            invincibilityRoutine = null;
        }

        isInvincible = false;

        if (bodySpriteRenderer != null)
        {
            bodySpriteRenderer.color = bodyDefaultColor;
        }
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
        maxHealth = Mathf.Max(0.01f, maxHealth);
        invincibilityDuration = Mathf.Max(0f, invincibilityDuration);
        knockbackForce = Mathf.Max(0f, knockbackForce);
        knockbackTotalTime = Mathf.Max(0f, knockbackTotalTime);
    }
}