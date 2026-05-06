using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public sealed class AuraDamage : MonoBehaviour
{
    [SerializeField] private float damagePerTick = 1f;
    [SerializeField] private float tickInterval = 1f;

    private readonly Dictionary<int, float> lastDamageTimes = new();

    private void Awake()
    {
        if (TryGetComponent(out CircleCollider2D circleCollider))
        {
            circleCollider.isTrigger = true;
        }
    }

    private void OnDisable()
    {
        lastDamageTimes.Clear();
    }

    private void OnTriggerStay2D(Collider2D col)
    {
        if (!col.CompareTag("Enemy"))
        {
            return;
        }

        IDamageable damageable = col.GetComponentInParent<IDamageable>();
        if (damageable == null)
        {
            return;
        }

        int enemyId = damageable is Component component ? component.gameObject.GetInstanceID() : col.gameObject.GetInstanceID();
        float currentTime = Time.time;
        if (lastDamageTimes.TryGetValue(enemyId, out float lastDamageTime) && currentTime - lastDamageTime < tickInterval)
        {
            return;
        }

        lastDamageTimes[enemyId] = currentTime;
        damageable.TakeDamage(damagePerTick);
    }

    private void OnTriggerExit2D(Collider2D col)
    {
        IDamageable damageable = col.GetComponentInParent<IDamageable>();
        int enemyId = damageable is Component component ? component.gameObject.GetInstanceID() : col.gameObject.GetInstanceID();
        lastDamageTimes.Remove(enemyId);
    }
}