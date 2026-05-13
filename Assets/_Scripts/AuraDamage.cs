using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public sealed class AuraDamage : MonoBehaviour
{
    [SerializeField] private float damagePerTick = 1f;
    [SerializeField] private float tickInterval = 1f;

    private readonly Dictionary<Component, float> lastDamageTimes = new();
    private CircleCollider2D circleCollider;
    private GameObject playerObject;

    private void Awake()
    {
        circleCollider = GetComponent<CircleCollider2D>();
        playerObject = (GetComponentInParent<IDamageable>() as Component)?.gameObject ?? transform.root.gameObject;

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
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        CircleCollider2D col = GetComponent<CircleCollider2D>();

        if (sr != null && col != null)
        {
            transform.localScale = new Vector3(35f, 35f, 1f);

            float targetWorldRadius = 2.5f;
            col.radius = targetWorldRadius / 35f;

            sr.color = new Color(0f, 1f, 1f, 0.08f);
            sr.sortingOrder = -2;
        }
    }
}