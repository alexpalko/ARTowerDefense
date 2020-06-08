using System.Collections;
using ARTowerDefense;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    [SerializeField] private GameObject EnemyPrefab;

    private int m_RemainingWaves = 6;
    private float m_TimeBetweenWaves = 20;
    private float m_WaveCountdown = 10;
    private float m_TimeBetweenEnemies = .35f;
    private int m_WaveSize = 3;
    private int m_WaveSizeIncreaseMin = 1;
    private int m_WaveSizeIncreaseMax = 5;
    private int m_RemainingEnemiesToSpawn;

    private Transform m_AnchorTransform;
    private Transform m_SpawnerTransform;

    void Start()
    {
        m_AnchorTransform = GameObject.Find("Master").GetComponent<Master>().AnchorTransform;
        m_SpawnerTransform = Master.PathWayPoints[0];
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

        if (m_RemainingEnemiesToSpawn == 0)
        {
            m_WaveCountdown -= Time.deltaTime;
        }
    }

    private IEnumerator _SpawnWave()
    {
        m_RemainingWaves--;
        m_WaveSize += Random.Range(m_WaveSizeIncreaseMin, m_WaveSizeIncreaseMax);
        m_WaveSizeIncreaseMin += Random.Range(1, 4);
        m_WaveSizeIncreaseMax += Random.Range(4, 7);
        m_RemainingEnemiesToSpawn = m_WaveSize;
        m_TimeBetweenWaves += 5;
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
        m_RemainingEnemiesToSpawn--;
    }
}
