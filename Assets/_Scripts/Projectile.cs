using UnityEngine;

public sealed class Projectile : MonoBehaviour
{
    [SerializeField] private float lifetime = 3f;
    [SerializeField] private LayerMask damageLayers;
    [SerializeField] private LayerMask groundLayers;

    private float damage;
    private float speed;
    private float timer;
    private ProjectilePooler owningPooler;

    public void AssignPool(ProjectilePooler pooler)
    {
        owningPooler = pooler;
    }

    private void Awake()
    {
        ResolveDefaultLayerMasks();
    }

    public void Setup(float projectileSpeed, float projectileDamage)
    {
        speed = projectileSpeed;
        damage = Mathf.Max(0f, projectileDamage);
        timer = 0f;
        gameObject.SetActive(true);
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
        if (IsInLayerMask(collision.gameObject.layer, damageLayers))
        {
            IDamageable damageable = collision.GetComponentInParent<IDamageable>();
            damageable?.TakeDamage(damage);
            Deactivate();
            return;
        }

        if (IsInLayerMask(collision.gameObject.layer, groundLayers))
        {
            Deactivate();
        }
    }

    private void Deactivate()
    {
        gameObject.SetActive(false);
        owningPooler?.ReturnProjectile(gameObject);
    }

    private void OnValidate()
    {
        lifetime = Mathf.Max(0.01f, lifetime);

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