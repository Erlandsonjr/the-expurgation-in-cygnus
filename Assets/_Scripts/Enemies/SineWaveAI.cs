using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class SineWaveAI : MonoBehaviour, IColdAffectable
{
    private static readonly Color ColdColor = new(0.68f, 0.87f, 1f, 1f);
    private const float ColdSlowMultiplier = 0.5f;

    [Header("Targeting")]
    [SerializeField] private Transform playerTarget;
    [SerializeField] private float detectionRange = 20f;

    [Header("Movement")]
    public float moveSpeed = 2.5f;
    public float waveFrequency = 2f;
    public float waveAmplitude = 1.5f;

    [Header("Combat")]
    [SerializeField] private GameObject enemyProjectilePrefab;
    [SerializeField] private Transform firePoint;
    public float fireRate = 2f;
    [SerializeField] private float projectileDamage = 1f;
    [SerializeField] private float projectileSpeed = 10f;
    public float contactDamage = 1f;

    private Coroutine coldRoutine;
    private Rigidbody2D rb;
    private EnemyHealth enemyHealth;
    private SpriteRenderer spriteRenderer;
    private Color defaultColor = Color.white;
    private float baseMoveSpeed;
    private float lastContactTime = -1f;
    private bool isAttacking = false;
    private bool isCold;

    private void Awake()
    {
        if (GetComponent<EnemyArenaGate>() == null)
        {
            gameObject.AddComponent<EnemyArenaGate>();
        }

        gameObject.tag = "Enemy";

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer != -1)
        {
            gameObject.layer = enemyLayer;
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        if (moveSpeed <= 0.1f) moveSpeed = 2.5f;
        if (waveFrequency <= 0.1f) waveFrequency = 2f;
        if (waveAmplitude <= 0.1f) waveAmplitude = 1.5f;

        enemyHealth = GetComponent<EnemyHealth>();
        spriteRenderer = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
        defaultColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        baseMoveSpeed = moveSpeed;
    }

    private void OnEnable()
    {
        moveSpeed = baseMoveSpeed > 0f ? baseMoveSpeed : moveSpeed;
        lastContactTime = -1f;
        isAttacking = false;
        isCold = false;
        ResolvePlayerTarget();
        ApplyVisualState();
    }

    private void OnDisable()
    {
        if (coldRoutine != null)
        {
            StopCoroutine(coldRoutine);
            coldRoutine = null;
        }

        isAttacking = false;
        isCold = false;
        moveSpeed = baseMoveSpeed;
    }

    public void SetTarget(Transform target)
    {
        playerTarget = target;
    }

    public void ApplyCold()
    {
        if (baseMoveSpeed <= 0f)
        {
            baseMoveSpeed = moveSpeed;
        }

        if (!isCold)
        {
            moveSpeed = baseMoveSpeed * ColdSlowMultiplier;
        }

        isCold = true;

        if (coldRoutine != null)
        {
            StopCoroutine(coldRoutine);
        }

        ApplyVisualState();
        coldRoutine = StartCoroutine(ColdRoutine());
    }

    private void Update()
    {
        ResolvePlayerTarget();

        if (!isAttacking && playerTarget != null && enemyProjectilePrefab != null)
        {
            StartCoroutine(EngageTarget());
        }

        if (playerTarget != null)
        {
            Vector3 direction = (playerTarget.position - transform.position).normalized;

            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = direction.x < 0f;
            }

            Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0f);
            Vector3 waveOffset = perpendicular * Mathf.Sin(Time.time * waveFrequency) * waveAmplitude;
            transform.position += (direction * moveSpeed + waveOffset) * Time.deltaTime;
        }
    }

    private void FixedUpdate()
    {
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player") && Time.time > lastContactTime + 1f)
        {
            IDamageable damageable = other.GetComponent<IDamageable>() ?? other.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(contactDamage);
                lastContactTime = Time.time;
            }
        }
    }

    private IEnumerator EngageTarget()
    {
        isAttacking = true;

        if (playerTarget != null && enemyProjectilePrefab != null)
        {
            FireProjectile();
        }

        yield return new WaitForSeconds(fireRate);
        isAttacking = false;
    }

    private IEnumerator ColdRoutine()
    {
        yield return new WaitForSeconds(2f);

        moveSpeed = baseMoveSpeed;
        isCold = false;
        coldRoutine = null;
        ApplyVisualState();
    }

    private void FireProjectile()
    {
        Vector3 origin = firePoint != null ? firePoint.position : transform.position;
        Vector2 toPlayer = (Vector2)playerTarget.position - (Vector2)origin;
        if (toPlayer.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Vector2 direction = toPlayer.normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
        GameObject shot = Instantiate(enemyProjectilePrefab, origin, rotation);

        if (shot.TryGetComponent(out Projectile projectile))
        {
            projectile.Setup(projectileSpeed, projectileDamage);
            projectile.targetTag = "Player";
        }
    }

    private void ResolvePlayerTarget()
    {
        if (playerTarget == null)
        {
            playerTarget = FindAnyObjectByType<PlayerController>()?.transform;
        }
    }

    private void ApplyVisualState()
    {
        Color targetColor = isCold ? ColdColor : defaultColor;

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

    private void OnValidate()
    {
        detectionRange = Mathf.Max(0f, detectionRange);
        moveSpeed = Mathf.Max(0f, moveSpeed);
        waveFrequency = Mathf.Max(0f, waveFrequency);
        waveAmplitude = Mathf.Max(0f, waveAmplitude);
        fireRate = Mathf.Max(0.01f, fireRate);
        projectileDamage = Mathf.Max(0f, projectileDamage);
        projectileSpeed = Mathf.Max(0f, projectileSpeed);
        contactDamage = Mathf.Max(0f, contactDamage);
    }
}