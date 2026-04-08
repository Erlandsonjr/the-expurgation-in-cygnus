using System.Collections.Generic;
using UnityEngine;

public sealed class ProjectilePooler : MonoBehaviour
{
    [Header("Pool Setup")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private int initialPoolSize = 16;
    [SerializeField] private bool canExpand = true;
    [SerializeField] private Transform poolRoot;

    private readonly Queue<GameObject> availableProjectiles = new();
    private readonly HashSet<GameObject> queuedProjectiles = new();
    private int totalProjectilesCreated;

    private void Awake()
    {
        if (poolRoot == null)
        {
            poolRoot = transform;
        }

        WarmPool(initialPoolSize);
    }

    public void WarmPool(int count)
    {
        if (projectilePrefab == null)
        {
            return;
        }

        int projectilesToCreate = Mathf.Max(0, count - totalProjectilesCreated);
        for (int index = 0; index < projectilesToCreate; index++)
        {
            CreateProjectile();
        }
    }

    public GameObject GetProjectile(Vector3 position, Quaternion rotation)
    {
        if (availableProjectiles.Count == 0)
        {
            if (!canExpand || projectilePrefab == null)
            {
                return null;
            }

            CreateProjectile();
        }

        GameObject projectile = availableProjectiles.Dequeue();
        queuedProjectiles.Remove(projectile);
        projectile.transform.SetParent(null);
        projectile.transform.SetPositionAndRotation(position, rotation);
        projectile.SetActive(true);
        return projectile;
    }

    public void ReturnProjectile(GameObject projectile)
    {
        if (projectile == null || !queuedProjectiles.Add(projectile))
        {
            return;
        }

        if (projectile.activeSelf)
        {
            projectile.SetActive(false);
        }

        projectile.transform.SetParent(poolRoot);
        availableProjectiles.Enqueue(projectile);
    }

    private GameObject CreateProjectile()
    {
        GameObject projectile = Instantiate(projectilePrefab, poolRoot);

        if (projectile.TryGetComponent(out Projectile projectileComponent))
        {
            projectileComponent.AssignPool(this);
        }

        totalProjectilesCreated++;
        ReturnProjectile(projectile);
        return projectile;
    }

    private void OnValidate()
    {
        initialPoolSize = Mathf.Max(0, initialPoolSize);
    }
}