using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class Projectile : MonoBehaviour
{
    [SerializeField] private float lifetime = 3f;
    [SerializeField] private LayerMask damageLayers;
    [SerializeField] private LayerMask groundLayers;

    [SerializeField] private float defaultSpeed = 10f;

    /// <summary>
    /// When non-empty, hit detection uses tag comparison instead of <see cref="damageLayers"/>.
    /// Set to "Player" on enemy projectiles, leave empty for player projectiles.
    /// </summary>
    public string targetTag = string.Empty;

    private float damage;
    private float speed;
    private float timer;
    private ProjectilePooler owningPooler;
    private Coroutine impactAnimationRoutine;
    private Sprite defaultSprite;
    private Collider2D[] hitColliders;
    private Rigidbody2D rb;
    private bool hasHit;
    private Vector3 defaultLocalScale;

    public bool isExplosive = false;
    public bool isCryo = false;
    public float explosionRadius = 3f;
    public Sprite[] impactFrames;
    public Sprite[] explosionFrames;
    private SpriteRenderer sr;

    /// <summary>Serialized default speed; readable by external spawners before Setup is called.</summary>
    public float Speed => defaultSpeed;

    public void AssignPool(ProjectilePooler pooler)
    {
        owningPooler = pooler;
    }

    private void Awake()
    {
        ResolveDefaultLayerMasks();
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        defaultSprite = sr != null ? sr.sprite : null;
        defaultLocalScale = transform.localScale;
        hitColliders = GetComponents<Collider2D>();

        // Safety net: if this prefab is instantiated without Setup() being called,
        // use the serialized defaultSpeed so bullets are never frozen at speed 0.
        if (speed == 0f)
        {
            speed = defaultSpeed;
        }
    }

    public void Setup(float projectileSpeed, float projectileDamage)
    {
        if (impactAnimationRoutine != null)
        {
            StopCoroutine(impactAnimationRoutine);
            impactAnimationRoutine = null;
        }

        speed = projectileSpeed;
        damage = Mathf.Max(0f, projectileDamage);
        timer = 0f;
        isExplosive = false;
        isCryo = false;
        hasHit = false;

        rb = GetComponent<Rigidbody2D>();

        if (sr != null)
        {
            sr.sprite = defaultSprite;
        }

        transform.localScale = defaultLocalScale;

        if (hitColliders != null)
        {
            foreach (Collider2D hitCollider in hitColliders)
            {
                if (hitCollider != null)
                {
                    hitCollider.enabled = true;
                }
            }
        }

        if (rb != null)
        {
            rb.angularVelocity = 0f;
            rb.linearVelocity = (Vector2)transform.right * speed;
        }

        gameObject.SetActive(true);
    }

    /// <summary>Call after Setup() to flag this projectile as explosive with an AOE blast on hit.</summary>
    public void SetExplosive(float radius)
    {
        isExplosive = true;
        explosionRadius = Mathf.Max(0f, radius);
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= lifetime)
        {
            Deactivate();
        }
    }

    private void FixedUpdate()
    {
        if (hasHit || speed <= 0f)
        {
            return;
        }

        float moveDistance = speed * Time.fixedDeltaTime;
        if (moveDistance <= 0f)
        {
            return;
        }

        RaycastHit2D[] hits = Physics2D.CircleCastAll(transform.position, 0.5f, transform.right, moveDistance);
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null || IsOwnedCollider(hit.collider) || !ShouldProcessSweepHit(hit.collider))
            {
                continue;
            }

            transform.position = hit.point;
            OnTriggerEnter2D(hit.collider);
            break;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasHit || collision == null)
        {
            return;
        }

        bool hitsDamageTarget = !string.IsNullOrEmpty(targetTag)
            ? collision.gameObject.CompareTag(targetTag)
            : IsInLayerMask(collision.gameObject.layer, damageLayers);
        bool hitsGround = IsWorldGeometry(collision);

        if (!hitsDamageTarget && !hitsGround)
        {
            return;
        }

        if (hitsDamageTarget)
        {
            if (targetTag == "Player")
            {
                PlayerController player = collision.GetComponentInParent<PlayerController>();
                if (player != null && player.isDashing) return;
            }

            hasHit = true;

            if (isCryo)
            {
                ApplyCryoDebuff(collision);
            }

            if (targetTag == "Player")
            {
                PlayerController player = collision.GetComponentInParent<PlayerController>();
                player?.TakeDamage(damage, transform.position);
            }
            else if (isExplosive)
            {
                Explode(collision.ClosestPoint(transform.position));
                return;
            }
            else
            {
                IDamageable damageable = collision.GetComponentInParent<IDamageable>();
                damageable?.TakeDamage(damage);
            }

            Deactivate();
            return;
        }

        if (hitsGround)
        {
            if (isExplosive)
            {
                Explode(collision.ClosestPoint(transform.position));
                return;
            }

            hasHit = true;
            Deactivate();
        }
    }

    private void Explode(Vector3 explosionPosition)
    {
        hasHit = true;
        transform.position = explosionPosition;

        Collider2D[] blastHits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        HashSet<IDamageable> damagedTargets = new HashSet<IDamageable>();

        foreach (Collider2D hit in blastHits)
        {
            bool blastHitsDamageTarget = !string.IsNullOrEmpty(targetTag)
                ? hit.gameObject.CompareTag(targetTag)
                : IsInLayerMask(hit.gameObject.layer, damageLayers);

            if (!blastHitsDamageTarget)
            {
                continue;
            }

            IDamageable damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable != null && damagedTargets.Add(damageable))
            {
                damageable.TakeDamage(damage);
            }
        }

        Debug.Log("BOOM!", this);
        StartImpactAnimation();
    }

    private void StartImpactAnimation()
    {
        if (impactAnimationRoutine != null)
        {
            return;
        }

        impactAnimationRoutine = StartCoroutine(PlayImpactAnimation());
    }

    private IEnumerator PlayImpactAnimation()
    {
        speed = 0f;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        if (hitColliders != null)
        {
            foreach (Collider2D hitCollider in hitColliders)
            {
                if (hitCollider != null)
                {
                    hitCollider.enabled = false;
                }
            }
        }

        Sprite[] framesToPlay = isExplosive && explosionFrames != null && explosionFrames.Length > 0
            ? explosionFrames
            : impactFrames;

        if (isExplosive)
        {
            float explosionDiameter = Mathf.Max(0.01f, explosionRadius * 2f);
            transform.localScale = new Vector3(explosionDiameter, explosionDiameter, 1f);
        }

        if (sr == null || framesToPlay == null || framesToPlay.Length == 0)
        {
            impactAnimationRoutine = null;
            Deactivate();
            yield break;
        }

        sr.sprite = framesToPlay[0];

        for (int i = 1; i < framesToPlay.Length; i++)
        {
            sr.sprite = framesToPlay[i];
            yield return new WaitForSeconds(0.02f);
        }

        impactAnimationRoutine = null;
        Deactivate();
    }

    private void Deactivate()
    {
        hasHit = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        transform.localScale = defaultLocalScale;

        gameObject.SetActive(false);
        owningPooler?.ReturnProjectile(gameObject);
    }

    private void OnValidate()
    {
        lifetime = Mathf.Max(0.01f, lifetime);
        explosionRadius = Mathf.Max(0.01f, explosionRadius);

        ResolveDefaultLayerMasks();
    }

    private void ResolveDefaultLayerMasks()
    {
        if (damageLayers.value == 0)
        {
            damageLayers = LayerMask.GetMask("Enemy");
        }

        if (groundLayers.value == 0)
        {
            groundLayers = LayerMask.GetMask("Ground");
        }
    }

    private bool IsOwnedCollider(Collider2D collider)
    {
        if (hitColliders != null)
        {
            foreach (Collider2D hitCollider in hitColliders)
            {
                if (hitCollider == collider)
                {
                    return true;
                }
            }
        }

        return collider.transform == transform || collider.transform.IsChildOf(transform);
    }

    private bool ShouldProcessSweepHit(Collider2D collider)
    {
        bool hitsDamageTarget = !string.IsNullOrEmpty(targetTag)
            ? collider.gameObject.CompareTag(targetTag)
            : IsInLayerMask(collider.gameObject.layer, damageLayers);

        return hitsDamageTarget || IsWorldGeometry(collider);
    }

    private bool IsWorldGeometry(Collider2D collider)
    {
        return IsInLayerMask(collider.gameObject.layer, groundLayers)
            || collider.gameObject.tag == "Ground"
            || collider.gameObject.tag == "Environment";
    }

    private static void ApplyCryoDebuff(Collider2D collision)
    {
        IColdAffectable coldAffectable = collision.GetComponentInParent<IColdAffectable>();
        if (coldAffectable != null)
        {
            coldAffectable.ApplyCold();
        }
    }

    private static bool IsInLayerMask(int layer, LayerMask layerMask)
    {
        return (layerMask.value & (1 << layer)) != 0;
    }
}