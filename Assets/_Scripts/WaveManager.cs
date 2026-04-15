using UnityEngine;

public sealed class WaveManager : MonoBehaviour
{
    [Header("Spawn Setup")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private Transform playerTarget;
    [SerializeField] private float spawnInterval = 2f;

    private float spawnTimer;

    private void Awake()
    {
        spawnTimer = spawnInterval;
    }

    private void Update()
    {
        if (enemyPrefab == null || spawnPoints == null || spawnPoints.Length == 0)
        {
            return;
        }

        // Count down to the next spawn so waves can be paced from the inspector.
        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f)
        {
            return;
        }

        SpawnEnemy();
        spawnTimer = spawnInterval;
    }

    public void SpawnEnemy()
    {
        if (enemyPrefab == null || spawnPoints == null || spawnPoints.Length == 0)
        {
            return;
        }

        // Pick one of the scene-authored spawn anchors so level flow stays designer-driven.
        Transform selectedSpawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        if (selectedSpawnPoint == null)
        {
            return;
        }

        GameObject enemyObject = Instantiate(enemyPrefab, selectedSpawnPoint.position, selectedSpawnPoint.rotation);

        if (enemyObject.TryGetComponent(out EnemyAI enemyAI))
        {
            enemyAI.SetTarget(ResolvePlayerTarget());
        }
    }

    private Transform ResolvePlayerTarget()
    {
        if (playerTarget != null)
        {
            return playerTarget;
        }

        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null)
        {
            playerTarget = playerObject.transform;
        }

        return playerTarget;
    }

    private void OnValidate()
    {
        spawnInterval = Mathf.Max(0.1f, spawnInterval);
    }
}