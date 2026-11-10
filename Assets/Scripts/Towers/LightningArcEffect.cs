using UnityEngine;

namespace TowerDefense
{
    // Stretches a lightning strip between two world points and fades it out.
    public class LightningArcEffect : MonoBehaviour
    {
        [SerializeField] private float lifetime = 0.18f;

        private SpriteRenderer spriteRenderer;
        private Vector3 baseScale;
        private float age;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            baseScale = transform.localScale;
        }

        public void SetEndpoints(Vector3 from, Vector3 to)
        {
            Vector3 delta = to - from;
            float distance = delta.magnitude;
            transform.position = (from + to) * 0.5f;

            if (distance > 0.0001f)
            {
                float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }

            float spriteWidth = spriteRenderer != null && spriteRenderer.sprite != null
                ? spriteRenderer.sprite.bounds.size.x : 1f;
            transform.localScale = new Vector3(
                baseScale.x * distance / Mathf.Max(0.0001f, spriteWidth),
                baseScale.y,
                baseScale.z);
        }

        private void Update()
        {
            age += Time.deltaTime;
            float progress = lifetime <= 0f ? 1f : Mathf.Clamp01(age / lifetime);
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
