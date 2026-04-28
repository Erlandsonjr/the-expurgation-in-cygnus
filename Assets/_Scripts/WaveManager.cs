using UnityEngine;

[System.Serializable]
public struct WaveData
{
    public int enemyCount;
    public float spawnRate;
    /// <summary>If non-empty, one of these is chosen at random each spawn.
    /// Falls back to WaveManager.enemyPrefab when empty.</summary>
    public GameObject[] allowedEnemyPrefabs;
}

public sealed class WaveManager : MonoBehaviour
{
    [Header("Wave Configuration")]
    [SerializeField] private WaveData[] waves = new WaveData[10]
    {
        new WaveData { enemyCount = 7,  spawnRate = 2.0f },
        new WaveData { enemyCount = 4,  spawnRate = 1.8f },
        new WaveData { enemyCount = 5,  spawnRate = 1.6f },
        new WaveData { enemyCount = 6,  spawnRate = 1.5f },
        new WaveData { enemyCount = 7,  spawnRate = 1.4f },
        new WaveData { enemyCount = 8,  spawnRate = 1.3f },
        new WaveData { enemyCount = 9,  spawnRate = 1.2f },
        new WaveData { enemyCount = 10, spawnRate = 1.1f },
        new WaveData { enemyCount = 12, spawnRate = 1.0f },
        new WaveData { enemyCount = 15, spawnRate = 0.8f },
    };

    [Header("Spawn Setup")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private Transform playerTarget;

    private int currentWaveIndex;
    private int spawnedThisWave;
    private float spawnTimer;
    private bool waveActive;
    private bool allWavesDone;

    public int CurrentWave => currentWaveIndex + 1;

    private void Awake()
    {
        StartWave(0);
    }

    private void Update()
    {
        if (allWavesDone || !waveActive)
        {
            return;
        }

        WaveData wave = waves[currentWaveIndex];

        // Still have enemies to spawn in this wave.
        if (spawnedThisWave < wave.enemyCount)
        {
            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f)
            {
                SpawnEnemy();
                spawnedThisWave++;
                spawnTimer = wave.spawnRate;
            }
            return;
        }

        // Quota reached — wait for all active enemies to be cleared.
        int livingEnemies = CountLivingEnemies();
        if (livingEnemies == 0)
        {
            OnWaveCleared();
        }
    }

    private void StartWave(int index)
    {
        currentWaveIndex = index;
        spawnedThisWave = 0;
        spawnTimer = 0f;   // spawn first enemy immediately
        waveActive = true;
        Debug.Log($"WAVE {index + 1} START — {waves[index].enemyCount} enemies, rate {waves[index].spawnRate}s");
    }

    private void OnWaveCleared()
    {
        waveActive = false;
        Debug.Log($"WAVE {currentWaveIndex + 1} COMPLETE");
        Time.timeScale = 0f;

        if (UpgradeManager.Instance != null)
        {
            // Wave 3 (index 2) → weapon selection; all other waves → general stat upgrades.
            if (currentWaveIndex == 2)
                UpgradeManager.Instance.ShowWeaponUpgrades();
            else
                UpgradeManager.Instance.ShowGeneralUpgrades();
        }
        else
        {
            AdvanceToNextWave();
        }
    }

    /// <summary>Called externally (e.g., from a "Next Wave" UI button) to resume play.</summary>
    public void AdvanceToNextWave()
    {
        if (currentWaveIndex + 1 >= waves.Length)
        {
            allWavesDone = true;
            Debug.Log("ALL WAVES COMPLETE");
            return;
        }

        Time.timeScale = 1f;
        StartWave(currentWaveIndex + 1);
    }

    public void SpawnEnemy()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            return;
        }

        // Choose prefab: wave override list first, then global fallback.
        WaveData wave = waves[currentWaveIndex];
        GameObject prefabToSpawn = null;
        if (wave.allowedEnemyPrefabs != null && wave.allowedEnemyPrefabs.Length > 0)
        {
            prefabToSpawn = wave.allowedEnemyPrefabs[Random.Range(0, wave.allowedEnemyPrefabs.Length)];
        }
        else
        {
            prefabToSpawn = enemyPrefab;
        }

        if (prefabToSpawn == null)
        {
            return;
        }

        bool isSniper = prefabToSpawn.GetComponent<ArkanoSniperAI>() != null;
        Transform selectedSpawnPoint = PickSpawnPoint(isSniper);
        if (selectedSpawnPoint == null)
        {
            return;
        }

        GameObject enemyObject = Instantiate(prefabToSpawn, selectedSpawnPoint.position, selectedSpawnPoint.rotation);

        if (enemyObject.TryGetComponent(out EnemyAI enemyAI))
        {
            enemyAI.SetTarget(ResolvePlayerTarget());
        }

        if (enemyObject.TryGetComponent(out ArkanoSniperAI sniperAI))
        {
            sniperAI.SetTarget(ResolvePlayerTarget());
        }
    }

    /// <summary>
    /// Picks a random spawn point. For sniper enemies, excludes points whose
    /// name contains 'South' or that have the lowest Y position in the array.
    /// </summary>
    private Transform PickSpawnPoint(bool excludeSouth)
    {
        if (!excludeSouth)
        {
            return spawnPoints[Random.Range(0, spawnPoints.Length)];
        }

        // Build a filtered list: no 'South' in name and not the lowest-Y point.
        float minY = float.MaxValue;
        foreach (Transform sp in spawnPoints)
        {
            if (sp != null && sp.position.y < minY)
            {
                minY = sp.position.y;
            }
        }

        System.Collections.Generic.List<Transform> filtered = new System.Collections.Generic.List<Transform>();
        foreach (Transform sp in spawnPoints)
        {
            if (sp == null) continue;
            bool isSouth = sp.name.IndexOf("South", System.StringComparison.OrdinalIgnoreCase) >= 0;
            bool isLowest = Mathf.Approximately(sp.position.y, minY);
            if (!isSouth && !isLowest)
            {
                filtered.Add(sp);
            }
        }

        // Fall back to any point if all were filtered out.
        if (filtered.Count == 0)
        {
            return spawnPoints[Random.Range(0, spawnPoints.Length)];
        }

        return filtered[Random.Range(0, filtered.Count)];
    }

    private static int CountLivingEnemies()
    {
        // EnemyHealth is on every enemy; count active ones in the scene.
        return FindObjectsByType<EnemyHealth>().Length;
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
        for (int i = 0; i < waves.Length; i++)
        {
            waves[i].enemyCount  = Mathf.Max(1, waves[i].enemyCount);
            waves[i].spawnRate   = Mathf.Max(0.1f, waves[i].spawnRate);
        }
    }
}
