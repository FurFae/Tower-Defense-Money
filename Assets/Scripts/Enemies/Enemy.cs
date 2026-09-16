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

        public EnemyData Data { get; private set; }
        public float DistanceTraveled { get; private set; }

        private float currentHealth;
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

        public void Initialize(EnemyData data, Transform[] path)
        {
            Data = data;
            currentHealth = data.maxHealth;
            waypoints = path;
            waypointIndex = 0;
            DistanceTraveled = 0f;

            if (spriteRenderer != null && data.sprite != null)
                spriteRenderer.sprite = data.sprite;

            if (waypoints != null && waypoints.Length > 0)
                transform.position = waypoints[0].position;

            UpdateHealthBar();
        }

        private void Update()
        {
            if (waypoints == null || waypointIndex >= waypoints.Length) return;

            Transform target = waypoints[waypointIndex];
            Vector3 toTarget = target.position - transform.position;
            float step = Data.moveSpeed * Time.deltaTime;

            if (toTarget.magnitude <= step)
            {
                DistanceTraveled += toTarget.magnitude;
                transform.position = target.position;
                waypointIndex++;
                if (waypointIndex >= waypoints.Length)
                    ReachEnd();
            }
            else
            {
                transform.position += toTarget.normalized * step;
                DistanceTraveled += step;
            }
        }

        public void TakeDamage(float amount)
        {
            currentHealth -= amount;
            UpdateHealthBar();
            if (currentHealth <= 0f)
                Die();
        }

        private void UpdateHealthBar()
        {
            if (healthBarFill == null || Data == null) return;
            float ratio = Mathf.Clamp01(currentHealth / Data.maxHealth);
            Vector3 scale = healthBarFill.localScale;
            scale.x = ratio;
            healthBarFill.localScale = scale;
        }

        private void Die()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.AddCurrency(Data.currencyReward);
            OnDeath?.Invoke(this);
            Destroy(gameObject);
        }

        private void ReachEnd()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.DamageBase(Data.damageToBase);
            OnReachedEnd?.Invoke(this);
            Destroy(gameObject);
        }
    }
}
