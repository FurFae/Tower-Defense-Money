using System.Collections.Generic;
using UnityEngine;

namespace TowerDefense
{
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float maxLifetime = 5f;
        [SerializeField] private float hitDistance = 0.1f;
        [SerializeField] private float pierceHitDistance = 0.35f;

        private Enemy target;
        private float damage;
        private float speed;
        private float splashRadius;
        private float splashDamageMultiplier;
        private float damageOverTimePerSecond;
        private float damageOverTimeDuration;
        private GameObject impactPrefab;
        private int pierceCount;
        private int chainCount;
        private float chainRadius;
        private float chainDamageMultiplier;
        private GameObject chainArcPrefab;
        private float slowDuration;
        private float slowMultiplier;
        private float burnDamagePerSecond;
        private float burnDuration;
        private bool isArrowShot;
        private bool straightFlight;
        private Vector3 flightDirection;
        private readonly HashSet<Enemy> hitTargets = new HashSet<Enemy>();
        private float lifetime;

        public void Initialize(Enemy targetEnemy, float dmg, float projectileSpeed,
            float areaRadius = 0f, float areaDamageMultiplier = 1f,
            float dotPerSecond = 0f, float dotDuration = 0f, GameObject hitEffectPrefab = null,
            int pierce = 0, int chain = 0, float chainDistance = 0f, float chainMultiplier = 1f,
            GameObject arcPrefab = null, float freezeDuration = 0f, float freezeMultiplier = 1f,
            float firePerSecond = 0f, float fireDuration = 0f, bool arrow = false)
        {
            isArrowShot = arrow;
            target = targetEnemy;
            damage = dmg;
            speed = projectileSpeed;
            splashRadius = areaRadius;
            splashDamageMultiplier = areaDamageMultiplier;
            damageOverTimePerSecond = dotPerSecond;
            damageOverTimeDuration = dotDuration;
            impactPrefab = hitEffectPrefab;
            pierceCount = Mathf.Max(0, pierce);
            chainCount = Mathf.Max(0, chain);
            chainRadius = Mathf.Max(0f, chainDistance);
            chainDamageMultiplier = Mathf.Max(0f, chainMultiplier);
            chainArcPrefab = arcPrefab;
            slowDuration = freezeDuration;
            slowMultiplier = freezeMultiplier;
            burnDamagePerSecond = firePerSecond;
            burnDuration = fireDuration;
            straightFlight = false;
            flightDirection = Vector3.zero;
            hitTargets.Clear();
        }

        private void Update()
        {
            lifetime += Time.deltaTime;
            if (lifetime >= maxLifetime
                || (GameManager.Instance != null && GameManager.Instance.IsGameOver))
            {
                Finish();
                return;
            }

            if (straightFlight)
                MoveStraight();
            else
                MoveHoming();
        }

        private void MoveHoming()
        {
            if (target == null)
            {
                if (pierceCount > 0)
                    StartStraightFlight(transform.up);
                else
                    Finish();
                return;
            }

            Vector3 toTarget = target.transform.position - transform.position;
            float step = speed * Time.deltaTime;

            if (toTarget.magnitude <= Mathf.Max(step, hitDistance))
            {
                HitTarget(target);
                if (pierceCount > 0)
                {
                    pierceCount--;
                    StartStraightFlight(toTarget.normalized);
                }
                else
                {
                    Finish();
                }
                return;
            }

            transform.position += toTarget.normalized * step;
            Face(toTarget);
        }

        private void MoveStraight()
        {
            transform.position += flightDirection.normalized * speed * Time.deltaTime;

            foreach (Enemy enemy in Enemy.Active)
            {
                if (enemy == null || hitTargets.Contains(enemy)) continue;
                if (Vector3.Distance(enemy.transform.position, transform.position) > pierceHitDistance) continue;

                HitTarget(enemy);
                if (pierceCount > 0)
                {
                    pierceCount--;
                    break;
                }

                Finish();
                return;
            }
        }

        private void StartStraightFlight(Vector3 direction)
        {
            target = null;
            straightFlight = true;
            flightDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.up;
            Face(flightDirection);
        }

        private void Face(Vector3 direction)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
        }

        // Returns the projectile to the pool instead of destroying it (pooled by Tower.Fire).
        private void Finish()
        {
            SimplePool.Release(gameObject);
        }

        private void HitTarget(Enemy enemy)
        {
            if (!hitTargets.Add(enemy)) return;
            SpawnImpact(enemy.transform.position);

            enemy.TakeDamage(damage, isArrowShot);
            if (damageOverTimePerSecond > 0f && damageOverTimeDuration > 0f)
                enemy.ApplyPoison(damageOverTimePerSecond, damageOverTimeDuration);
            if (burnDamagePerSecond > 0f && burnDuration > 0f)
                enemy.ApplyBurn(burnDamagePerSecond, burnDuration);
            if (slowDuration > 0f && slowMultiplier < 1f)
                enemy.ApplySlow(slowDuration, slowMultiplier);
            if (splashRadius > 0f)
                Splash(enemy);
            if (chainCount > 0 && chainRadius > 0f)
                ChainFrom(enemy);
        }

        private void Splash(Enemy enemy)
        {
            GameSfx.Play(GameSfx.Event.Explosion, enemy.transform.position);
            foreach (Enemy other in Enemy.Active)
            {
                if (other == null || other == enemy || hitTargets.Contains(other)) continue;
                if (other.IsFlying) continue; // наземный взрыв не задевает летающих
                if (Vector3.Distance(other.transform.position, enemy.transform.position) <= splashRadius)
                    other.TakeDamage(damage * splashDamageMultiplier);
            }
        }

        private void ChainFrom(Enemy source)
        {
            Enemy current = source;
            int remaining = chainCount;
            while (remaining > 0)
            {
                Enemy next = FindNearestChainTarget(current.transform.position);
                if (next == null) break;
                hitTargets.Add(next);
                SpawnArc(current.transform.position, next.transform.position);
                SpawnImpact(next.transform.position);
                next.TakeDamage(damage * chainDamageMultiplier);
                current = next;
                remaining--;
            }
        }

        private Enemy FindNearestChainTarget(Vector3 origin)
        {
            Enemy best = null;
            float bestDistance = chainRadius;
            foreach (Enemy enemy in Enemy.Active)
            {
                if (enemy == null || hitTargets.Contains(enemy)) continue;
                float distance = Vector3.Distance(enemy.transform.position, origin);
                if (distance <= bestDistance)
                {
                    bestDistance = distance;
                    best = enemy;
                }
            }
            return best;
        }

        private void SpawnImpact(Vector3 position)
        {
            if (impactPrefab != null)
                Instantiate(impactPrefab, position, Quaternion.identity);
        }

        private void SpawnArc(Vector3 from, Vector3 to)
        {
            if (chainArcPrefab == null) return;
            GameObject arcObject = Instantiate(chainArcPrefab, Vector3.zero, Quaternion.identity);
            if (arcObject.TryGetComponent(out LightningArcEffect arc))
                arc.SetEndpoints(from, to);
        }
    }
}
