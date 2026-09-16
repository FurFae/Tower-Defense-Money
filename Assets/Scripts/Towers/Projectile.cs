using UnityEngine;

namespace TowerDefense
{
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float maxLifetime = 5f;
        [SerializeField] private float hitDistance = 0.1f;

        private Enemy target;
        private float damage;
        private float speed;
        private float lifetime;

        public void Initialize(Enemy targetEnemy, float dmg, float projectileSpeed)
        {
            target = targetEnemy;
            damage = dmg;
            speed = projectileSpeed;
        }

        private void Update()
        {
            lifetime += Time.deltaTime;
            if (target == null || lifetime >= maxLifetime)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 toTarget = target.transform.position - transform.position;
            float step = speed * Time.deltaTime;

            if (toTarget.magnitude <= Mathf.Max(step, hitDistance))
            {
                target.TakeDamage(damage);
                Destroy(gameObject);
                return;
            }

            transform.position += toTarget.normalized * step;

            float angle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
