using UnityEngine;

namespace TowerDefense
{
    // Short-lived sprite effect spawned by projectiles on impact.
    public class ImpactEffect : MonoBehaviour
    {
        [SerializeField] private float lifetime = 0.45f;
        [SerializeField] private float scaleGrowth = 0.35f;

        private SpriteRenderer spriteRenderer;
        private Vector3 initialScale;
        private float age;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            initialScale = transform.localScale;
        }

        private void Update()
        {
            age += Time.deltaTime;
            float progress = lifetime <= 0f ? 1f : Mathf.Clamp01(age / lifetime);
            transform.localScale = initialScale * (1f + scaleGrowth * progress);

            if (spriteRenderer != null)
            {
                Color color = spriteRenderer.color;
                color.a = 1f - progress;
                spriteRenderer.color = color;
            }

            if (age >= lifetime)
                Destroy(gameObject);
        }
    }
}
