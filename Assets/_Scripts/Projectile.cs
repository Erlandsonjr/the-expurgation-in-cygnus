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

    public bool isExplosive = false;
    public float explosionRadius = 3f;
    public Sprite[] impactFrames;
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
        sr = GetComponent<SpriteRenderer>();
        defaultSprite = sr != null ? sr.sprite : null;
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

        if (sr != null)
        {
            sr.sprite = defaultSprite;
        }

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
        transform.Translate(Vector2.right * speed * Time.deltaTime);

        timer += Time.deltaTime;
        if (timer >= lifetime)
        {
            Deactivate();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        bool hitsDamageTarget = !string.IsNullOrEmpty(targetTag)
            ? collision.gameObject.CompareTag(targetTag)
            : IsInLayerMask(collision.gameObject.layer, damageLayers);

        if (hitsDamageTarget)
        {
            if (targetTag == "Player")
            {
                PlayerController player = collision.GetComponentInParent<PlayerController>();
                if (player != null && player.isDashing) return;
                player?.TakeDamage(damage, transform.position);
            }
            else if (isExplosive)
            {
                Collider2D[] blastHits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
                HashSet<IDamageable> damagedTargets = new HashSet<IDamageable>();

                foreach (Collider2D hit in blastHits)
                {
                    if (!IsInLayerMask(hit.gameObject.layer, damageLayers) && hit.GetComponentInParent<EnemyHealth>() == null)
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
            }
            else
            {
                IDamageable damageable = collision.GetComponentInParent<IDamageable>();
                damageable?.TakeDamage(damage);
            }

            if (isExplosive)
            {
                StartImpactAnimation();
                return;
            }

            Deactivate();
            return;
        }

        if (IsInLayerMask(collision.gameObject.layer, groundLayers))
        {
            if (isExplosive)
            {
                StartImpactAnimation();
                return;
            }

            Deactivate();
        }
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

        if (sr == null || impactFrames == null || impactFrames.Length < 2)
        {
            impactAnimationRoutine = null;
            Deactivate();
            yield break;
        }

        for (int i = 1; i < impactFrames.Length; i++)
        {
            sr.sprite = impactFrames[i];
            yield return new WaitForSeconds(0.02f);
        }

        impactAnimationRoutine = null;
        Deactivate();
    }

    private void Deactivate()
    {
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

    private static bool IsInLayerMask(int layer, LayerMask layerMask)
    {
        return (layerMask.value & (1 << layer)) != 0;
    }
}