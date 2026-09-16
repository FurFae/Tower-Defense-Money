using UnityEngine;

namespace TowerDefense
{
    public class Tower : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer baseRenderer;
        [SerializeField] private SpriteRenderer weaponRenderer;
        [SerializeField] private Transform weaponPivot;
        [SerializeField] private Transform firePoint;

        private TowerData data;
        private float cooldown;

        public void Initialize(TowerData towerData)
        {
            data = towerData;
            if (baseRenderer != null) baseRenderer.sprite = data.baseSprite;
            if (weaponRenderer != null) weaponRenderer.sprite = data.weaponSprite;
            cooldown = 0f;
        }

        private void Update()
        {
            if (data == null) return;

            Enemy target = FindTarget();
            if (target == null) return;

            AimAt(target.transform.position);

            cooldown -= Time.deltaTime;
            if (cooldown <= 0f)
            {
                Fire(target);
                cooldown = 1f / Mathf.Max(0.01f, data.fireRate);
            }
        }

        private Enemy FindTarget()
        {
            Enemy best = null;
            float bestProgress = -1f;
            Vector3 origin = firePoint != null ? firePoint.position : transform.position;

            foreach (Enemy enemy in Enemy.Active)
            {
                if (enemy == null) continue;
                float dist = Vector3.Distance(origin, enemy.transform.position);
                if (dist <= data.range && enemy.DistanceTraveled > bestProgress)
                {
                    bestProgress = enemy.DistanceTraveled;
                    best = enemy;
                }
            }
            return best;
        }

        private void AimAt(Vector3 worldPos)
        {
            if (weaponPivot == null) return;
            Vector3 dir = worldPos - weaponPivot.position;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            weaponPivot.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void Fire(Enemy target)
        {
            if (data.projectilePrefab == null) return;
            Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
            GameObject go = Instantiate(data.projectilePrefab, spawnPos, Quaternion.identity);
            Projectile projectile = go.GetComponent<Projectile>();
            if (projectile != null)
                projectile.Initialize(target, data.damage, data.projectileSpeed);
        }

        private void OnDrawGizmosSelected()
        {
            if (data == null) return;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, data.range);
        }
    }
}
