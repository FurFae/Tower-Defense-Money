using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerDefense
{
    [Serializable]
    public class SpawnEntry
    {
        public EnemyData enemy;
        public int count = 5;
        public float interval = 0.75f;
    }

    [Serializable]
    public class Wave
    {
        public string name = "Wave";
        public float delayBeforeWave = 5f;
        public List<SpawnEntry> entries = new List<SpawnEntry>();
    }

    public class WaveManager : MonoBehaviour
    {
        [SerializeField] private PathHolder path;
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private List<Wave> waves = new List<Wave>();

        public int CurrentWaveNumber { get; private set; }
        public int TotalWaves => waves.Count;
        public bool WaveInProgress { get; private set; }

        public event Action<int, int> OnWaveChanged;
        public event Action OnAllWavesCleared;

        private int aliveCount;
        private bool skipDelayRequested;

        private void Start()
        {
            StartCoroutine(RunWaves());
        }

        private IEnumerator RunWaves()
        {
            for (int i = 0; i < waves.Count; i++)
            {
                CurrentWaveNumber = i + 1;
                OnWaveChanged?.Invoke(CurrentWaveNumber, TotalWaves);

                skipDelayRequested = false;
                float timer = 0f;
                while (timer < waves[i].delayBeforeWave && !skipDelayRequested)
                {
                    timer += Time.deltaTime;
                    yield return null;
                }

                WaveInProgress = true;
                yield return StartCoroutine(SpawnWave(waves[i]));

                while (aliveCount > 0)
                    yield return null;

                WaveInProgress = false;
            }

            OnAllWavesCleared?.Invoke();
            if (GameManager.Instance != null)
                GameManager.Instance.EndGame(true);
        }

        private IEnumerator SpawnWave(Wave wave)
        {
            foreach (SpawnEntry entry in wave.entries)
            {
                for (int i = 0; i < entry.count; i++)
                {
                    SpawnEnemy(entry.enemy);
                    yield return new WaitForSeconds(entry.interval);
                }
            }
        }

        private void SpawnEnemy(EnemyData data)
        {
            if (path == null || path.Waypoints.Length == 0 || enemyPrefab == null || data == null) return;

            GameObject go = Instantiate(enemyPrefab, path.Waypoints[0].position, Quaternion.identity);
            Enemy enemy = go.GetComponent<Enemy>();
            enemy.Initialize(data, path.Waypoints);
            enemy.OnDeath += HandleEnemyRemoved;
            enemy.OnReachedEnd += HandleEnemyRemoved;
            aliveCount++;
        }

        private void HandleEnemyRemoved(Enemy enemy)
        {
            aliveCount--;
        }

        public void RequestSkipDelay()
        {
            skipDelayRequested = true;
        }
    }
}
