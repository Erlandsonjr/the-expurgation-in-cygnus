using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class EnemyAI : MonoBehaviour
{
    private enum EnemyState
    {
        Idle,
        Chasing
    }

    [Header("Targeting")]
    [SerializeField] private Transform playerTarget;
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float horizontalStoppingDistance = 0.1f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;

    [Header("Health")]
    [SerializeField] private int maxHealth = 3;

    private EnemyState currentState;
    private int currentHealth;
    private float horizontalDirection;
    private Rigidbody2D rigidbody2d;

    private void Awake()
    {
        rigidbody2d = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        currentHealth = maxHealth;
        horizontalDirection = 0f;
        currentState = EnemyState.Idle;
        ResolvePlayerTarget();

        if (rigidbody2d != null)
        {
            rigidbody2d.linearVelocity = Vector2.zero;
        }
    }

    private void Update()
    {
        ResolvePlayerTarget();
        UpdateState();
    }

    private void FixedUpdate()
    {
        float targetVelocityX = currentState == EnemyState.Chasing ? horizontalDirection * moveSpeed : 0f;
        rigidbody2d.linearVelocity = new Vector2(targetVelocityX, rigidbody2d.linearVelocity.y);
    }

    public void SetTarget(Transform target)
    {
        playerTarget = target;
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= Mathf.Max(0, amount);
        if (currentHealth <= 0)
        {
            DeactivateEnemy();
        }
    }

    private void UpdateState()
    {
        if (playerTarget == null)
        {
            currentState = EnemyState.Idle;
            horizontalDirection = 0f;
            return;
        }

        Vector2 toPlayer = playerTarget.position - transform.position;
        bool playerInRange = toPlayer.sqrMagnitude <= detectionRange * detectionRange;

        currentState = playerInRange ? EnemyState.Chasing : EnemyState.Idle;

        if (currentState == EnemyState.Idle || Mathf.Abs(toPlayer.x) <= horizontalStoppingDistance)
        {
            horizontalDirection = 0f;
            return;
        }

        horizontalDirection = Mathf.Sign(toPlayer.x);
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

    private void DeactivateEnemy()
    {
        if (rigidbody2d != null)
        {
            rigidbody2d.linearVelocity = Vector2.zero;
        }

        gameObject.SetActive(false);
    }

    private void OnValidate()
    {
        detectionRange = Mathf.Max(0f, detectionRange);
        horizontalStoppingDistance = Mathf.Max(0f, horizontalStoppingDistance);
        moveSpeed = Mathf.Max(0f, moveSpeed);
        maxHealth = Mathf.Max(1, maxHealth);
    }
}