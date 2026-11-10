using UnityEngine;

namespace TowerDefense
{
    [CreateAssetMenu(menuName = "Tower Defense/Enemy Data", fileName = "NewEnemyData")]
    public class EnemyData : ScriptableObject
    {
        public string displayName = "Enemy";
        public float maxHealth = 20f;
        public float moveSpeed = 2f;
        public int currencyReward = 5;
        public int damageToBase = 1;
        public Sprite sprite;
        public RuntimeAnimatorController animatorController;

        // ---- Roles (item 5 of the roadmap) ----
        [Header("Roles")]
        [Tooltip("Multiply damage from arrow/ballista projectiles (1 = no armor).")]
        [Range(0.05f, 1f)] public float arrowDamageMultiplier = 1f;
        public bool isFlying;
        [Tooltip("Heals nearby enemies for this much HP per second; 0 = not a healer.")]
        public float healPerSecond;
        public float healRadius = 2f;
        [Tooltip("Spawns this many copies of splitInto on death; 0 = no splitting.")]
        public int splitCount;
        public EnemyData splitInto;
        [Tooltip("Seconds between self-cleanses of poison/slow/burn; 0 = never.")]
        public float cleanseInterval;
        public bool isBoss;
    }
}
