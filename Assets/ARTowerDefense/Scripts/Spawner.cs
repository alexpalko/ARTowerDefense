using System.Collections;
using ARTowerDefense;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    [SerializeField] private GameObject EnemyPrefab;
    private int m_RemainingWaves = 5;
    private float m_TimeBetweenWaves = 30;
    private float m_WaveCountdown = 2;
    private float m_TimeBetweenEnemies = 1;
    private int m_WaveSize = 1;
    private Transform m_AnchorTransform;

    private Transform m_SpawnerTransform;

    void Start()
    {
        m_AnchorTransform = Master.AnchorTransform;
        m_SpawnerTransform = Master.PathWaypoints[0];
    }

    void Update()
    {
        if (m_RemainingWaves == 0)
        {
            Master.LastWave = true;
            return;
        }

        if (m_WaveCountdown <= 0)
        {
            StartCoroutine(_SpawnWave());
            m_WaveCountdown = m_TimeBetweenWaves;
        }

        m_WaveCountdown -= Time.deltaTime;
    }

    private IEnumerator _SpawnWave()
    {
        m_RemainingWaves--;
        m_WaveSize += 2;
        Debug.Log($"Spawning a new wave of size: {m_WaveSize}");

        for (var i = 0; i < m_WaveSize; i++)
        {
            _SpawnEnemy();
            yield return new WaitForSeconds(m_TimeBetweenEnemies);
        }
    }

    private void _SpawnEnemy()
    {
        Instantiate(EnemyPrefab, m_SpawnerTransform.position, Quaternion.identity, m_AnchorTransform);
    }
}
