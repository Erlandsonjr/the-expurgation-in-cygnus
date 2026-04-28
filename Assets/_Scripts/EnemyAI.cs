using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class EnemyAI : MonoBehaviour
{
    private static readonly Color ArkanoBaseColor = new(1f, 1f, 1f, 1f);
    private static readonly Color DashWarningColor = new(1f, 0.64705884f, 0f, 1f);
    private const float DashPreparationDuration = 0.2f;

    private enum EnemyState
    {
        Idle,
        Chasing,
        PreparingDash,
        Dashing
    }

    [Header("Targeting")]
    [SerializeField] private Transform playerTarget;
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private bool continueLastDirectionWhenTargetLost;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float dashSpeed = 10f;
    [SerializeField] private float dashDuration = 0.3f;
    [SerializeField] private float dashCooldown = 1.25f;
    [SerializeField] private float attackRange = 3f;

    [Header("Combat")]
    [SerializeField] private float contactDamage = 1f;

    private EnemyState currentState;
    private Vector2 currentMoveDirection;
    private Vector2 dashTargetPosition;
    private Vector2 lastKnownDirection;
    private Color defaultColor = Color.white;
    private EnemyHealth enemyHealth;
    private float dashCooldownTimer;
    private float dashPreparationTimer;
    private float dashTimer;
    private Rigidbody2D rigidbody2d;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        rigidbody2d = GetComponent<Rigidbody2D>();
        enemyHealth = GetComponent<EnemyHealth>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        SetArkanoBaseColor();
    }

    private void OnEnable()
    {
        defaultColor = ArkanoBaseColor;
        currentMoveDirection = Vector2.zero;
        dashTargetPosition = Vector2.zero;
        dashCooldownTimer = 0f;
        dashPreparationTimer = 0f;
        dashTimer = 0f;
        lastKnownDirection = Vector2.zero;
        currentState = EnemyState.Idle;
        ResolvePlayerTarget();
        SetArkanoBaseColor();
        ApplyVisualState();
    }

    private void Update()
    {
        if (playerTarget == null)
        {
            GameObject playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null)
            {
                playerTarget = playerObject.transform;
            }
        }

        UpdateDashState();

        if (currentState != EnemyState.PreparingDash && currentState != EnemyState.Dashing)
        {
            UpdateState();
        }

        ApplyVisualState();
    }

    private void FixedUpdate()
    {
        if (rigidbody2d == null)
        {
            return;
        }

        if (currentState == EnemyState.PreparingDash || currentState == EnemyState.Idle)
        {
            return;
        }

        Vector2 nextPosition = currentState == EnemyState.Dashing
            ? Vector2.MoveTowards(rigidbody2d.position, dashTargetPosition, dashSpeed * Time.fixedDeltaTime)
            : GetChasePosition();

        rigidbody2d.MovePosition(nextPosition);

        if (currentState == EnemyState.Dashing && (dashTargetPosition - nextPosition).sqrMagnitude <= 0.0001f)
        {
            FinishDash();
        }
    }

    public void SetTarget(Transform target)
    {
        playerTarget = target;
    }

    private void SetArkanoBaseColor()
    {
        defaultColor = ArkanoBaseColor;

        if (enemyHealth != null)
        {
            enemyHealth.SetDefaultColor(defaultColor);
            return;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = defaultColor;
        }
    }

    private void UpdateState()
    {
        if (playerTarget == null)
        {
            currentMoveDirection = continueLastDirectionWhenTargetLost ? lastKnownDirection : Vector2.zero;
            currentState = currentMoveDirection.sqrMagnitude > 0.0001f ? EnemyState.Chasing : EnemyState.Idle;
            return;
        }

        Vector2 toPlayer = ((Vector2)playerTarget.position - (Vector2)transform.position).normalized;
        Vector2 rawToPlayer = playerTarget.position - transform.position;

        if (rawToPlayer.sqrMagnitude <= 0.0001f)
        {
            currentState = EnemyState.Idle;
            currentMoveDirection = Vector2.zero;
            return;
        }

        currentMoveDirection = toPlayer;
        lastKnownDirection = currentMoveDirection;
        currentState = EnemyState.Chasing;

        if (dashCooldownTimer <= 0f && rawToPlayer.sqrMagnitude <= attackRange * attackRange)
        {
            StartDashPreparation((Vector2)playerTarget.position);
        }
    }

    private void UpdateDashState()
    {
        dashCooldownTimer = Mathf.Max(0f, dashCooldownTimer - Time.deltaTime);

        if (currentState == EnemyState.PreparingDash)
        {
            dashPreparationTimer = Mathf.Max(0f, dashPreparationTimer - Time.deltaTime);
            if (dashPreparationTimer <= 0f)
            {
                BeginDash();
            }

            return;
        }

        if (currentState == EnemyState.Dashing)
        {
            dashTimer = Mathf.Max(0f, dashTimer - Time.deltaTime);
            if (dashTimer <= 0f)
            {
                FinishDash();
            }
        }
    }

    private void StartDashPreparation(Vector2 targetPosition)
    {
        dashTargetPosition = targetPosition;
        dashPreparationTimer = DashPreparationDuration;
        currentMoveDirection = Vector2.zero;
        currentState = EnemyState.PreparingDash;
    }

    private void BeginDash()
    {
        if (rigidbody2d == null)
        {
            FinishDash();
            return;
        }

        Vector2 dashDirection = dashTargetPosition - rigidbody2d.position;
        if (dashDirection.sqrMagnitude <= 0.0001f)
        {
            dashDirection = lastKnownDirection.sqrMagnitude > 0.0001f ? lastKnownDirection : Vector2.right;
            dashTargetPosition = rigidbody2d.position + dashDirection;
        }

        currentState = EnemyState.Dashing;
        dashTimer = dashDuration;
    }

    private void FinishDash()
    {
        if (currentState != EnemyState.Dashing && currentState != EnemyState.PreparingDash)
        {
            return;
        }

        dashTimer = 0f;
        dashPreparationTimer = 0f;
        dashCooldownTimer = dashCooldown;
        currentMoveDirection = Vector2.zero;
        currentState = EnemyState.Idle;
    }

    private void ResolvePlayerTarget()
    {
        if (playerTarget != null)
        {
            return;
        }

        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null)
        {
            playerTarget = playerObject.transform;
        }
    }

    private Vector2 GetChasePosition()
    {
        if (playerTarget != null)
        {
            return Vector2.MoveTowards(rigidbody2d.position, playerTarget.position, moveSpeed * Time.fixedDeltaTime);
        }

        return rigidbody2d.position + currentMoveDirection * moveSpeed * Time.fixedDeltaTime;
    }

    private void ApplyVisualState()
    {
        Color targetColor = currentState == EnemyState.PreparingDash || currentState == EnemyState.Dashing
            ? DashWarningColor
            : defaultColor;

        if (enemyHealth != null)
        {
            enemyHealth.SetBaseColor(targetColor);
            return;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = targetColor;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (contactDamage <= 0f)
        {
            return;
        }

        if (!other.CompareTag("Player"))
        {
            return;
        }

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player != null && !player.IsInvincible)
        {
            player.TakeDamage(contactDamage, transform.position);
        }
    }

    private void OnValidate()
    {
        detectionRange = Mathf.Max(0f, detectionRange);
        moveSpeed = Mathf.Max(0f, moveSpeed);
        dashSpeed = Mathf.Max(0f, dashSpeed);
        dashDuration = Mathf.Max(0.01f, dashDuration);
        dashCooldown = Mathf.Max(0f, dashCooldown);
        attackRange = Mathf.Max(0f, attackRange);
        contactDamage = Mathf.Max(0f, contactDamage);
    }
}