using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public sealed class AuraDamage : MonoBehaviour
{
    private static readonly Color AuraVisualColor = new(0f, 1f, 1f, 0.4f);
    private const string AuraSpritePath = "UI/Skin/Knob.psd";
    private const string AuraSpriteFallbackPath = "UI/Skin/Background.psd";

    [SerializeField] private float damagePerTick = 1f;
    [SerializeField] private float tickInterval = 1f;

    private readonly Dictionary<Component, float> lastDamageTimes = new();
    private CircleCollider2D circleCollider;
    private GameObject playerObject;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        circleCollider = GetComponent<CircleCollider2D>();
        playerObject = (GetComponentInParent<IDamageable>() as Component)?.gameObject ?? transform.root.gameObject;
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
        IDamageable target = col.GetComponentInParent<IDamageable>();
        if (target == null || col.gameObject == playerObject)
        {
            return;
        }

        Component targetComponent = target as Component ?? col.transform;
        float currentTime = Time.time;
        if (lastDamageTimes.TryGetValue(targetComponent, out float lastDamageTime) && currentTime - lastDamageTime < tickInterval)
        {
            return;
        }

        lastDamageTimes[targetComponent] = currentTime;
        target.TakeDamage(damagePerTick);
    }

    private void OnTriggerExit2D(Collider2D col)
    {
        IDamageable target = col.GetComponentInParent<IDamageable>();
        if (target == null || col.gameObject == playerObject)
        {
            return;
        }

        Component targetComponent = target as Component ?? col.transform;
        lastDamageTimes.Remove(targetComponent);
    }

    private void SyncVisualToCollider()
    {
        if (circleCollider == null)
        {
            return;
        }

        transform.localScale = Vector3.one;

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = LoadAuraSprite() ?? spriteRenderer.sprite;
            spriteRenderer.color = AuraVisualColor;
        }
    }

    private static Sprite LoadAuraSprite()
    {
        return Resources.GetBuiltinResource<Sprite>(AuraSpritePath)
            ?? Resources.GetBuiltinResource<Sprite>(AuraSpriteFallbackPath);
    }
}