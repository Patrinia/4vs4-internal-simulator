using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    [Header("Player Settings")]
    public List<GameObject> playerPrefabs;
    public List<Transform> playerSpawnPoints;

    [Header("Enemy Settings")]
    public List<GameObject> enemyPrefabPool;
    public List<Transform> enemySpawnPoints;

    void Start()
    {
        SpawnPlayers();
        SpawnEnemies();
    }

    void SpawnPlayers()
    {
        for (int i = 0; i < 4; i++)
        {
            Instantiate(playerPrefabs[i], playerSpawnPoints[i].position, Quaternion.identity);
        }
    }

    void SpawnEnemies()
    {
        for (int i = 0; i < 4; i++)
        {
            int randomIndex = Random.Range(0, enemyPrefabPool.Count);
            Instantiate(enemyPrefabPool[randomIndex], enemySpawnPoints[i].position, Quaternion.identity);
        }
    }
}