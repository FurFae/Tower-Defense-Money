using System;
using System.Collections.Generic;
using UnityEngine;

namespace TowerDefense
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class Enemy : MonoBehaviour
    {
        // Towers scan this instead of doing FindObjectsOfType every frame.
        public static readonly HashSet<Enemy> Active = new HashSet<Enemy>();

        [SerializeField] private Transform healthBarFill;
        [SerializeField] private Animator animator;

        public EnemyData Data { get; private set; }
        public float DistanceTraveled { get; private set; }
        public bool IsPoisoned => poisonRemaining > 0f;
        public bool IsSlowed => slowRemaining > 0f;
        public bool IsBurned => burnRemaining > 0f;
        public bool IsFlying => Data != null && Data.isFlying;
        public bool IsBoss => Data != null && Data.isBoss;
        public bool IsDead => isDead;
        public int NextWaypointIndex => waypointIndex;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public float MoveSpeed => Data != null ? Data.moveSpeed : 0f;

        // Raised when a splitting enemy dies; the spawner instantiates the copies.
        public static event Action<Enemy> OnSplitRequest;
        // Raised on every damage/heal so status icons and HP bars stay fresh.
        public event Action OnStateChanged;

        private float currentHealth;
        private bool isDead;
        private float poisonRemaining;
        private float poisonDamagePerSecond;
        private float burnRemaining;
        private float burnDamagePerSecond;
        private float slowRemaining;
        private float slowMultiplier = 1f;
        private float CurrentSpeedMultiplier => slowRemaining > 0f ? slowMultiplier : 1f;
        private Transform[] waypoints;
        private int waypointIndex;
        private SpriteRenderer spriteRenderer;

        public event Action<Enemy> OnDeath;
        public event Action<Enemy> OnReachedEnd;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void OnEnable() => Active.Add(this);
        private void OnDisable() => Active.Remove(this);

        private float maxHealth;
        private float cleanseTimer;
        private float healPulse;
        // Small status icons above the head (poison/slow/burn), built once at runtime.
        private SpriteRenderer[] statusIcons;
        private static Sprite iconSprite;

        public void Initialize(EnemyData data, Transform[] path, float healthMultiplier = 1f, Vector3? spawnPosition = null)
        {
            Data = data;
            maxHealth = data.maxHealth * healthMultiplier;
            currentHealth = maxHealth;
            isDead = false;
            cleanseTimer = data.cleanseInterval;
            poisonRemaining = 0f;
            poisonDamagePerSecond = 0f;
            burnRemaining = 0f;
            burnDamagePerSecond = 0f;
            slowRemaining = 0f;
            slowMultiplier = 1f;
            UpdateStatusVisual();
            waypoints = path;
            waypointIndex = 0;
            DistanceTraveled = 0f;

            if (spawnPosition.HasValue && waypoints != null)
            {
                // Split copies appear mid-route: resume from the closest waypoint ahead.
                float best = float.MaxValue;
                for (int i = 0; i < waypoints.Length; i++)
                {
                    float d = Vector3.Distance(waypoints[i].position, spawnPosition.Value);
                    if (d < best)
                    {
                        best = d;
                        waypointIndex = Mathf.Min(i + 1, waypoints.Length - 1);
                        DistanceTraveled = i;
                        for (int j = 0; j < i; j++)
                            DistanceTraveled += Vector3.Distance(waypoints[j].position, waypoints[j + 1].position);
                    }
                }
            }

            if (spriteRenderer != null && data.sprite != null)
                spriteRenderer.sprite = data.sprite;

            transform.localScale = data.isBoss ? new Vector3(1.6f, 1.6f, 1f) : Vector3.one;
            EnsureStatusIcons();
            UpdateStatusIcons();

            if (waypoints != null && waypoints.Length > 0)
                transform.position = spawnPosition ?? waypoints[0].position;

            if (animator != null)
                animator.runtimeAnimatorController = Data.animatorController;

            UpdateHealthBar();
        }

        public void ResumePath(int nextWaypoint, float distance)
        {
            waypointIndex = Mathf.Clamp(nextWaypoint, 0, waypoints.Length - 1);
            DistanceTraveled = distance;
        }

        private void Update()
        {
            if (isDead) return;
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;
            UpdatePoison();
            if (isDead) return;
            UpdateBurn();
            if (isDead) return;
            UpdateSlow();
            UpdateCleanse();
            UpdateHealAura();
            UpdateStatusIcons();

            if (waypoints == null || waypointIndex >= waypoints.Length) return;
            Transform target = waypoints[waypointIndex];
            Vector3 toTarget = target.position - transform.position;
            float step = Data.moveSpeed * CurrentSpeedMultiplier * Time.deltaTime;

            if (toTarget.magnitude <= step)
            {
                DistanceTraveled += toTarget.magnitude;
                transform.position = target.position;
                waypointIndex++;
                if (waypointIndex >= waypoints.Length)
                {
                    ReachEnd();
                    return;
                }
                UpdateAnimation(waypoints[waypointIndex].position - transform.position);
            }
            else
            {
                transform.position += toTarget.normalized * step;
                DistanceTraveled += step;
            }
        }

        public void TakeDamage(float amount, bool isArrow = false)
        {
            if (isDead || amount <= 0f) return;
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;
            if (isArrow && Data != null) amount *= Data.arrowDamageMultiplier;
            currentHealth -= amount;
            UpdateHealthBar();
            OnStateChanged?.Invoke();
            if (currentHealth <= 0f)
                Die();
        }

        public void Heal(float amount)
        {
            if (isDead || amount <= 0f || currentHealth >= maxHealth) return;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            UpdateHealthBar();
        }

        private void UpdateCleanse()
        {
            if (Data == null || Data.cleanseInterval <= 0f) return;
            cleanseTimer -= Time.deltaTime;
            if (cleanseTimer > 0f) return;
            cleanseTimer = Data.cleanseInterval;
            bool hadEffect = poisonRemaining > 0f || slowRemaining > 0f || burnRemaining > 0f;
            poisonRemaining = 0f; poisonDamagePerSecond = 0f;
            burnRemaining = 0f; burnDamagePerSecond = 0f;
            slowRemaining = 0f; slowMultiplier = 1f;
            if (hadEffect) UpdateStatusVisual();
        }

        private void UpdateHealAura()
        {
            if (Data == null || Data.healPerSecond <= 0f) return;
            healPulse -= Time.deltaTime;
            if (healPulse > 0f) return;
            healPulse = 0.5f;
            foreach (Enemy other in Active)
            {
                if (other == null || other == this) continue;
                if (Vector3.Distance(other.transform.position, transform.position) <= Data.healRadius)
                    other.Heal(Data.healPerSecond * 0.5f);
            }
        }

        public void ApplyPoison(float damagePerSecond, float duration)
        {
            if (isDead || damagePerSecond <= 0f || duration <= 0f) return;
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;
            poisonDamagePerSecond = Mathf.Max(poisonDamagePerSecond, damagePerSecond);
            poisonRemaining = Mathf.Max(poisonRemaining, duration);
            UpdateStatusVisual();
        }

        public void ApplySlow(float duration, float multiplier)
        {
            if (isDead || duration <= 0f || multiplier <= 0f || multiplier >= 1f) return;
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;
            slowRemaining = Mathf.Max(slowRemaining, duration);
            slowMultiplier = Mathf.Min(slowMultiplier, multiplier);
            UpdateStatusVisual();
        }

        public void ApplyBurn(float damagePerSecond, float duration)
        {
            if (isDead || damagePerSecond <= 0f || duration <= 0f) return;
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;
            burnDamagePerSecond = Mathf.Max(burnDamagePerSecond, damagePerSecond);
            burnRemaining = Mathf.Max(burnRemaining, duration);
            UpdateStatusVisual();
        }

        private void UpdatePoison()
        {
            if (poisonRemaining <= 0f) return;
            float damageTime = Mathf.Min(poisonRemaining, Time.deltaTime);
            poisonRemaining -= damageTime;
            currentHealth -= poisonDamagePerSecond * damageTime;
            UpdateHealthBar();

            if (currentHealth <= 0f)
            {
                Die();
                return;
            }
            if (poisonRemaining <= 0f)
            {
                poisonDamagePerSecond = 0f;
                UpdateStatusVisual();
            }
        }

        private void UpdateBurn()
        {
            if (burnRemaining <= 0f) return;
            float damageTime = Mathf.Min(burnRemaining, Time.deltaTime);
            burnRemaining -= damageTime;
            currentHealth -= burnDamagePerSecond * damageTime;
            UpdateHealthBar();

            if (currentHealth <= 0f)
            {
                Die();
                return;
            }
            if (burnRemaining <= 0f)
            {
                burnDamagePerSecond = 0f;
                UpdateStatusVisual();
            }
        }

        private void UpdateSlow()
        {
            if (slowRemaining <= 0f) return;
            slowRemaining -= Time.deltaTime;
            if (slowRemaining <= 0f)
            {
                slowRemaining = 0f;
                slowMultiplier = 1f;
                UpdateStatusVisual();
            }
        }

        private void UpdateStatusIcons()
        {
            if (statusIcons == null) return;
            statusIcons[0].enabled = poisonRemaining > 0f;
            statusIcons[1].enabled = slowRemaining > 0f;
            statusIcons[2].enabled = burnRemaining > 0f;
        }

        // Poison / slow / burn dots above the head so stacked states read at a glance.
        private void EnsureStatusIcons()
        {
            if (statusIcons != null) return;
            Sprite sprite = IconSprite.Get();
            if (sprite == null) return;
            statusIcons = new SpriteRenderer[3];
            for (int i = 0; i < 3; i++)
            {
                GameObject icon = new GameObject($"Status{i}");
                icon.transform.SetParent(transform, false);
                icon.transform.localPosition = new Vector3((i - 1) * 0.35f, 1.1f, 0f);
                SpriteRenderer renderer = icon.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = 40;
                renderer.enabled = false;
                statusIcons[i] = renderer;
            }
            statusIcons[0].color = new Color(0.45f, 0.95f, 0.35f); // яд
            statusIcons[1].color = new Color(0.55f, 0.85f, 1f);    // замедление
            statusIcons[2].color = new Color(1f, 0.55f, 0.2f);    // поджог
        }

        private void UpdateStatusVisual()
        {
            if (spriteRenderer == null) return;
            if (burnRemaining > 0f)
                spriteRenderer.color = new Color(1f, 0.62f, 0.32f);
            else if (poisonRemaining > 0f)
                spriteRenderer.color = new Color(0.62f, 1f, 0.62f);
            else if (slowRemaining > 0f)
                spriteRenderer.color = new Color(0.65f, 0.86f, 1f);
            else
                spriteRenderer.color = Color.white;
        }

        private void UpdateAnimation(Vector3 dir)
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;
            if (dir.y > Mathf.Abs(dir.x))
                animator.Play("U_Walk");
            else if (-dir.y > Mathf.Abs(dir.x))
                animator.Play("D_Walk");
            else
            {
                animator.Play("S_Walk");
                spriteRenderer.flipX = dir.x < 0;
            }
        }

        private void UpdateHealthBar()
        {
            if (healthBarFill == null || Data == null) return;
            float ratio = Mathf.Clamp01(currentHealth / maxHealth);
            Vector3 scale = healthBarFill.localScale;
            scale.x = ratio;
            healthBarFill.localScale = scale;
        }

        private void Die()
        {
            if (isDead) return;
            isDead = true;
            if (GameManager.Instance != null)
                GameManager.Instance.AddCurrency(Data.currencyReward);
            RetentionSystem.Report(RetentionStat.Kills);
            if (Data != null && Data.splitCount > 0 && Data.splitInto != null)
                OnSplitRequest?.Invoke(this);
            GameSfx.Play(IsBoss ? GameSfx.Event.BossDeath : GameSfx.Event.EnemyDeath, transform.position);
            OnDeath?.Invoke(this);
            Destroy(gameObject);
        }

        private void ReachEnd()
        {
            if (isDead) return;
            isDead = true;
            if (GameManager.Instance != null)
                GameManager.Instance.DamageBase(Data.damageToBase);
            OnReachedEnd?.Invoke(this);
            Destroy(gameObject);
        }
    }
}
