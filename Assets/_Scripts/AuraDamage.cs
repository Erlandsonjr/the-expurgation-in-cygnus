using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public sealed class AuraDamage : MonoBehaviour
{
    private static readonly Color AuraVisualColor = new(0f, 1f, 1f, 0.4f);

    [SerializeField] private float damagePerTick = 1f;
    [SerializeField] private float tickInterval = 1f;

    private readonly Dictionary<Component, float> lastDamageTimes = new();
    private CircleCollider2D circleCollider;
    private IDamageable ownerDamageable;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        circleCollider = GetComponent<CircleCollider2D>();
        ownerDamageable = GetComponentInParent<IDamageable>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (circleCollider != null)
        {
            circleCollider.isTrigger = true;
        }

        SyncVisualToCollider();
    }

    private void OnValidate()
    {
        damagePerTick = Mathf.Max(0f, damagePerTick);
        tickInterval = Mathf.Max(0.01f, tickInterval);

        circleCollider = GetComponent<CircleCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (circleCollider != null)
        {
            circleCollider.isTrigger = true;
        }

        SyncVisualToCollider();
    }

    private void OnDisable()
    {
        lastDamageTimes.Clear();
    }

    private void OnTriggerStay2D(Collider2D col)
    {
        IDamageable damageable = col.GetComponentInParent<IDamageable>();
        if (damageable == null || ReferenceEquals(damageable, ownerDamageable))
        {
            return;
        }

        Component damageableComponent = damageable as Component ?? col.transform;
        float currentTime = Time.time;
        if (lastDamageTimes.TryGetValue(damageableComponent, out float lastDamageTime) && currentTime - lastDamageTime < tickInterval)
        {
            return;
        }

        lastDamageTimes[damageableComponent] = currentTime;
        damageable.TakeDamage(damagePerTick);
    }

    private void OnTriggerExit2D(Collider2D col)
    {
        IDamageable damageable = col.GetComponentInParent<IDamageable>();
        if (damageable == null || ReferenceEquals(damageable, ownerDamageable))
        {
            return;
        }

        Component damageableComponent = damageable as Component ?? col.transform;
        lastDamageTimes.Remove(damageableComponent);
    }

    private void SyncVisualToCollider()
    {
        if (circleCollider == null)
        {
            return;
        }

        transform.localScale = Vector3.one * (circleCollider.radius * 2f);

        if (spriteRenderer != null)
        {
            spriteRenderer.color = AuraVisualColor;
        }
    }
}