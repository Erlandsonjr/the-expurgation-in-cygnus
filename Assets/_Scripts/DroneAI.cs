using System.Collections;
using UnityEngine;

public sealed class DroneAI : MonoBehaviour
{
    [Header("Follow")]
    public Transform playerTransform;
    public Vector3 followOffset = new Vector3(-1.5f, 1.5f, 0f);
    public float smoothTime = 0.3f;

    [Header("Scanner")]
    public float scanRadius = 6f;

    [Header("Combat")]
    public ProjectilePooler projectilePooler;
    public GameObject projectilePrefab;
    public float fireRate = 1f;
    public float projectileDamage = 1f;
    public float projectileSpeed = 12f;

    private Transform currentTarget;
    private Vector3 followVelocity;
    private Coroutine scanRoutine;
    private Coroutine engageRoutine;

    private void Awake()
    {
        ResolvePlayerTransform();
    }

    private void OnEnable()
    {
        ResolvePlayerTransform();

        scanRoutine ??= StartCoroutine(ScanEnvironment());
        engageRoutine ??= StartCoroutine(EngageTarget());
    }

    private void OnDisable()
    {
        if (scanRoutine != null)
        {
            StopCoroutine(scanRoutine);
            scanRoutine = null;
        }

        if (engageRoutine != null)
        {
            StopCoroutine(engageRoutine);
            engageRoutine = null;
        }

        currentTarget = null;
        followVelocity = Vector3.zero;
    }

    private void LateUpdate()
    {
        ResolvePlayerTransform();
        if (playerTransform == null)
        {
            return;
        }

        Vector3 desiredPosition = playerTransform.position + followOffset;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref followVelocity, smoothTime);
    }

    private IEnumerator ScanEnvironment()
    {
        WaitForSeconds scanDelay = new WaitForSeconds(0.25f);

        while (true)
        {
            ResolvePlayerTransform();
            UpdateClosestTarget();
            yield return scanDelay;
        }
    }

    private IEnumerator EngageTarget()
    {
        while (true)
        {
            if (currentTarget != null)
            {
                FireAtTarget();
            }

            yield return new WaitForSeconds(GetShotInterval());
        }
    }

    private void UpdateClosestTarget()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, scanRadius);
        Transform closestTarget = null;
        float closestSqrDistance = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            if (hit == null)
            {
                continue;
            }

            IDamageable damageable = hit.GetComponentInParent<IDamageable>();
            Component targetComponent = damageable as Component;
            if (targetComponent == null)
            {
                continue;
            }

            Transform targetTransform = targetComponent.transform;
            if (playerTransform != null && targetTransform.root == playerTransform.root)
            {
                continue;
            }

            float sqrDistance = (targetTransform.position - transform.position).sqrMagnitude;
            if (sqrDistance < closestSqrDistance)
            {
                closestSqrDistance = sqrDistance;
                closestTarget = targetTransform;
            }
        }

        currentTarget = closestTarget;
    }

    private void FireAtTarget()
    {
        if (currentTarget == null)
        {
            return;
        }

        Vector3 aimDirection = currentTarget.position - transform.position;
        if (aimDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion projectileRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg);
        GameObject projectileObject = SpawnProjectile(transform.position, projectileRotation);
        if (projectileObject == null)
        {
            return;
        }

        projectileObject.transform.SetPositionAndRotation(transform.position, projectileRotation);

        if (projectileObject.TryGetComponent(out Rigidbody2D projectileRigidbody))
        {
            projectileRigidbody.linearVelocity = Vector2.zero;
            projectileRigidbody.angularVelocity = 0f;
            projectileRigidbody.position = transform.position;
            projectileRigidbody.rotation = projectileRotation.eulerAngles.z;
        }

        if (!projectileObject.TryGetComponent(out Projectile projectile))
        {
            if (projectilePooler != null)
            {
                projectilePooler.ReturnProjectile(projectileObject);
            }

            return;
        }

        projectile.Setup(projectileSpeed, projectileDamage);
    }

    private GameObject SpawnProjectile(Vector3 spawnPosition, Quaternion spawnRotation)
    {
        if (projectilePooler != null)
        {
            return projectilePooler.GetProjectile(spawnPosition, spawnRotation);
        }

        if (projectilePrefab != null)
        {
            return Instantiate(projectilePrefab, spawnPosition, spawnRotation);
        }

        return null;
    }

    private float GetShotInterval()
    {
        return fireRate > 0.01f ? 1f / fireRate : 100f;
    }

    private void ResolvePlayerTransform()
    {
        if (playerTransform == null)
        {
            playerTransform = FindAnyObjectByType<PlayerController>()?.transform;
        }
    }

    private void OnValidate()
    {
        smoothTime = Mathf.Max(0.01f, smoothTime);
        scanRadius = Mathf.Max(0f, scanRadius);
        fireRate = Mathf.Max(0.01f, fireRate);
        projectileDamage = Mathf.Max(0f, projectileDamage);
        projectileSpeed = Mathf.Max(0f, projectileSpeed);
    }
}