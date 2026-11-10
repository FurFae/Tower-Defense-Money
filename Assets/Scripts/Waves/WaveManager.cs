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
        // True while the pre-wave delay ticks and the wave can be called early for a mana bonus.
        public bool IsInDelay { get; private set; }
        public const int NextWaveBonus = 15;

        public event Action<int, int> OnWaveChanged;
        public event Action OnAllWavesCleared;
        public event Action OnWavesUpdated;

        private int aliveCount;
        private bool skipDelayRequested;

        private float healthMultiplier = 1f;

        private static bool IsGameOver => GameManager.Instance != null && GameManager.Instance.IsGameOver;

        private void Start()
        {
            ApplyLevel(Levels.Current);
            Enemy.OnSplitRequest += HandleSplitRequest;
            StartCoroutine(RunWaves());
        }

        private void OnDestroy()
        {
            Enemy.OnSplitRequest -= HandleSplitRequest;
        }

        // Splitting enemies raise this on death; copies join the current wave alive-count.
        private void HandleSplitRequest(EnemyData splitInto, Vector3 position)
        {
            if (IsGameOver || enemyPrefab == null || splitInto == null) return;
            GameObject go = Instantiate(enemyPrefab, position, Quaternion.identity);
            Enemy enemy = go.GetComponent<Enemy>();
            enemy.Initialize(splitInto, path.Waypoints, healthMultiplier, position);
            enemy.OnDeath += HandleEnemyRemoved;
            enemy.OnReachedEnd += HandleEnemyRemoved;
            aliveCount++;
        }

        // Состав следующей волны для предпросмотра. Пока идёт задержка перед волной,
        // показываем эту волну; во время боя — следующую.
        public string DescribeNextWave()
        {
            int index = WaveInProgress ? CurrentWaveNumber : CurrentWaveNumber - 1;
            if (index < 0 || index >= waves.Count) return null;
            Wave wave = waves[index];
            List<string> parts = new List<string>();
            foreach (SpawnEntry entry in wave.entries)
            {
                if (entry.enemy == null || entry.count <= 0) continue;
                string name = entry.enemy.displayName;
                string tag = entry.enemy.isBoss ? " <b>БОСС</b>" : entry.enemy.isFlying ? " (летит)" : "";
                parts.Add($"{name}{tag} ×{entry.count}");
            }
            return parts.Count == 0 ? null : string.Join(", ", parts);
        }

        // The scene holds the base waves; the level scales them and adds more at the end.
        private void ApplyLevel(LevelDefinition level)
        {
            if (level == null || waves.Count == 0) return;
            healthMultiplier = level.healthMultiplier;

            Wave last = waves[waves.Count - 1];
            List<Wave> result = new List<Wave>();
            foreach (Wave wave in waves)
                result.Add(Scaled(wave, wave.name, level.countMultiplier));
            for (int i = 1; i <= level.extraWaves; i++)
                result.Add(Scaled(last, $"Wave {waves.Count + i}", level.countMultiplier * (1f + 0.25f * i)));
            waves = result;

            InjectRoleWaves(level);
            OnWavesUpdated?.Invoke();
        }

        // Разнообразие врагов по уровням: новые роли подмешиваются к волнам карты,
        // на третьей карте в финал добавляется босс.
        private void InjectRoleWaves(LevelDefinition level)
        {
            EnemyData armored = Resources.Load<EnemyData>("Enemies/Enemy_ArmoredBug");
            EnemyData swift = Resources.Load<EnemyData>("Enemies/Enemy_Swift");
            EnemyData healer = Resources.Load<EnemyData>("Enemies/Enemy_Healer");
            EnemyData splitter = Resources.Load<EnemyData>("Enemies/Enemy_Splitter");
            EnemyData cleanse = Resources.Load<EnemyData>("Enemies/Enemy_Cleanse");
            EnemyData flyer = Resources.Load<EnemyData>("Enemies/Enemy_Flyer");
            EnemyData boss = Resources.Load<EnemyData>("Enemies/Enemy_Boss");

            void Add(int waveIndex, EnemyData data, int count, float interval = 1.2f)
            {
                if (data == null || waveIndex >= waves.Count) return;
                waves[waveIndex].entries.Add(new SpawnEntry { enemy = data, count = count, interval = interval });
            }

            int mid = Mathf.Clamp(waves.Count / 2, 0, waves.Count - 1);
            int late = Mathf.Clamp(waves.Count - 2, 0, waves.Count - 1);
            switch (Mathf.Clamp(level.number, 1, 3))
            {
                case 1:
                    Add(1, armored, 3);
                    Add(mid, swift, 4);
                    if (healer != null) Add(late, healer, 1);
                    break;
                case 2:
                    Add(0, swift, 5);
                    Add(1, armored, 5);
                    Add(mid, flyer, 4);
                    if (splitter != null) Add(mid + 1 < waves.Count ? mid + 1 : mid, splitter, 3);
                    if (healer != null) Add(late, healer, 2);
                    break;
                case 3:
                    Add(0, flyer, 4);
                    Add(1, armored, 6);
                    Add(mid, cleanse, 3);
                    if (splitter != null) Add(mid, splitter, 4);
                    if (healer != null) Add(late, healer, 2);
                    Add(waves.Count - 1, boss, 1, 0.5f);
                    break;
            }
        }

        private static Wave Scaled(Wave wave, string name, float countMultiplier)
        {
            Wave copy = new Wave { name = name, delayBeforeWave = wave.delayBeforeWave };
            foreach (SpawnEntry entry in wave.entries)
            {
                copy.entries.Add(new SpawnEntry
                {
                    enemy = entry.enemy,
                    count = Mathf.CeilToInt(entry.count * countMultiplier),
                    interval = entry.interval,
                });
            }
            return copy;
        }

        private IEnumerator RunWaves()
        {
            for (int i = 0; i < waves.Count; i++)
            {
                if (IsGameOver) yield break;
                CurrentWaveNumber = i + 1;
                OnWaveChanged?.Invoke(CurrentWaveNumber, TotalWaves);

                skipDelayRequested = false;
                IsInDelay = true;
                float timer = 0f;
                while (timer < waves[i].delayBeforeWave && !skipDelayRequested)
                {
                    if (IsGameOver) yield break;
                    timer += Time.deltaTime;
                    yield return null;
                }
                IsInDelay = false;
                if (skipDelayRequested && !IsGameOver)
                {
                    // Ранняя волна — небольшой бонус маны.
                    GameManager.Instance?.AddCurrency(NextWaveBonus);
                }

                WaveInProgress = true;
                GameSfx.Play(GameSfx.Event.WaveStart);
                yield return StartCoroutine(SpawnWave(waves[i]));

                while (aliveCount > 0)
                {
                    if (IsGameOver) break;
                    yield return null;
                }

                WaveInProgress = false;
                if (IsGameOver) yield break;
                RetentionSystem.Report(RetentionStat.WavesCleared);
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
                    if (IsGameOver) yield break;
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
            enemy.Initialize(data, path.Waypoints, healthMultiplier);
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
