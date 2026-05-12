using System.Collections;
using UnityEngine;

/// <summary>
/// Flying Arkano Sniper: drifts toward the player's X position and hovers at a
/// randomized height while oscillating sinusoidally. Fires on a time-based interval.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class ArkanoSniperAI : MonoBehaviour, IColdAffectable
{
    private static readonly Color ColdColor = new(0.68f, 0.87f, 1f, 1f);
    private const float ColdSlowMultiplier = 0.5f;

    [Header("Targeting")]
    [SerializeField] private Transform playerTarget;

    [Header("Movement")]
    [SerializeField] private float horizontalSmoothSpeed = 2f;
    [SerializeField] private float waveFrequency = 1.5f;
    [SerializeField] private float waveAmplitude = 1.0f;
    [SerializeField] private float roamSmoothTime = 1.5f;

    // Per-instance randomized values — set in Start() so each sniper is unique.
    private float individualYOffset;
    private float randomPhase;
    private float sinWaveTimer;

    // Roaming X: the sniper picks a new X offset every 2-3 s and drifts toward it.
    private float roamTimer;
    private float roamInterval;
    private float currentXOffset;     // where the sniper is drifting toward
    private float xVelocity;          // used by SmoothDamp

    [Header("Combat")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float fireRate = 3f;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float sniperDamage = 1f;

    private Rigidbody2D rb;
    private EnemyHealth enemyHealth;
    private SpriteRenderer spriteRenderer;
    private Color defaultColor = Color.white;
    private float baseHorizontalSmoothSpeed;
    private float baseRoamSmoothTime;
    private bool isCold;
    private Coroutine coldRoutine;
    // Counts down to zero; fires when it reaches 0 and resets to fireRate.
    private float fireTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        enemyHealth = GetComponent<EnemyHealth>();
        spriteRenderer = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
        defaultColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        baseHorizontalSmoothSpeed = horizontalSmoothSpeed;
        baseRoamSmoothTime = roamSmoothTime;
    }

    private void OnEnable()
    {
        horizontalSmoothSpeed = baseHorizontalSmoothSpeed > 0f ? baseHorizontalSmoothSpeed : horizontalSmoothSpeed;
        roamSmoothTime = baseRoamSmoothTime > 0f ? baseRoamSmoothTime : roamSmoothTime;
        isCold = false;
        coldRoutine = null;
        ApplyVisualState();
    }

    private void Start()
    {
        if (playerTarget == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                playerTarget = playerObj.transform;
            }
        }

        // Start with a full interval delay so the sniper doesn't fire the instant it spawns.
        fireTimer = fireRate;

        // Randomize per-instance flight values so each sniper occupies a unique position
        // and oscillates out of phase with its siblings.
        individualYOffset = Random.Range(8f, 11f);
        randomPhase       = Random.Range(0f, Mathf.PI * 2f);

        // Seed roam values so the sniper starts drifting immediately.
        currentXOffset = Random.Range(-10f, 10f);
        roamInterval   = Random.Range(2f, 3f);
        roamTimer      = roamInterval;
    }

    public void SetTarget(Transform target)
    {
        playerTarget = target;
    }

    public void ApplyCold()
    {
        if (baseHorizontalSmoothSpeed <= 0f)
        {
            baseHorizontalSmoothSpeed = horizontalSmoothSpeed;
        }

        if (baseRoamSmoothTime <= 0f)
        {
            baseRoamSmoothTime = roamSmoothTime;
        }

        if (!isCold)
        {
            horizontalSmoothSpeed = baseHorizontalSmoothSpeed * ColdSlowMultiplier;
            roamSmoothTime = baseRoamSmoothTime / ColdSlowMultiplier;
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
        if (playerTarget == null || projectilePrefab == null)
        {
            return;
        }

        fireTimer -= Time.deltaTime;
        if (fireTimer <= 0f)
        {
            FireProjectile();
            fireTimer = fireRate;
        }
    }

    private void FixedUpdate()
    {
        if (playerTarget == null)
        {
            return;
        }

        // X: roaming — pick a new offset every roamInterval seconds and SmoothDamp toward it.
        roamTimer -= Time.fixedDeltaTime;
        if (roamTimer <= 0f)
        {
            currentXOffset = Random.Range(-10f, 10f);
            roamInterval   = Random.Range(2f, 3f);
            roamTimer      = roamInterval;
        }
        float targetX = playerTarget.position.x + currentXOffset;
        float newX    = Mathf.SmoothDamp(rb.position.x, targetX, ref xVelocity, roamSmoothTime);

        // Y: base hover height + sinusoidal oscillation with per-instance phase offset.
        sinWaveTimer += Time.fixedDeltaTime;
        float targetY = (playerTarget.position.y + individualYOffset)
                      + Mathf.Sin((Time.time + randomPhase) * waveFrequency) * waveAmplitude;
        float newY = Mathf.Lerp(rb.position.y, targetY, horizontalSmoothSpeed * Time.fixedDeltaTime);

        rb.MovePosition(new Vector2(newX, newY));
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

    private IEnumerator ColdRoutine()
    {
        yield return new WaitForSeconds(2f);

        horizontalSmoothSpeed = baseHorizontalSmoothSpeed;
        roamSmoothTime = baseRoamSmoothTime;
        isCold = false;
        coldRoutine = null;
        ApplyVisualState();
    }

    private void FireProjectile()
    {
        Vector3 origin = firePoint != null ? firePoint.position : transform.position;
        Vector2 direction = ((Vector2)(playerTarget.position - origin)).normalized;

        // Rotate so the projectile's local +right points toward the player.
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.Euler(0f, 0f, angle);

        GameObject shot = Instantiate(projectilePrefab, origin, rotation);

        if (shot.TryGetComponent(out Projectile projectileScript))
        {
            // Setup with the prefab's authored default speed so the Projectile
            // drives itself via transform.Translate in its own Update loop.
            projectileScript.Setup(projectileScript.Speed, sniperDamage);
            // This is an enemy projectile — it must damage the Purger, not other enemies.
            projectileScript.targetTag = "Player";
        }
    }
}
