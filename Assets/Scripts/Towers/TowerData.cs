using UnityEngine;

namespace TowerDefense
{
    [CreateAssetMenu(menuName = "Tower Defense/Tower Data", fileName = "NewTowerData")]
    public class TowerData : ScriptableObject
    {
        public string displayName = "Tower";
        public int cost = 50;
        public float damage = 5f;
        public float range = 3f;
        public float fireRate = 1f; // shots per second
        public float projectileSpeed = 8f;
        public float splashRadius;
        [Range(0f, 1f)] public float splashDamageMultiplier = 0.65f;
        // How many extra enemies a projectile passes through after the first hit.
        public int pierceCount;
        // How many times a hit jumps to another nearby enemy.
        public int chainCount;
        public float chainRadius = 2f;
        [Range(0f, 1f)] public float chainDamageMultiplier = 0.75f;
        public GameObject chainArcPrefab;
        public Sprite baseSprite;
        // Full-tower art per level (index = level - 1), for towers without separate weapons.
        public Sprite[] levelSprites = new Sprite[8];
        // Per tower level (index = level - 1); towers without them use weaponSprite.
        public WeaponData[] weapons = new WeaponData[7];
        public Sprite weaponSprite;
        public GameObject projectilePrefab;
        public float damageOverTimePerSecond;
        public float damageOverTimeDuration;
        public bool preferUnpoisonedTarget;
        public float slowDuration;
        [Range(0f, 1f)] public float slowMultiplier = 0.55f;
        public float burnDamagePerSecond;
        public float burnDuration;
        public GameObject impactPrefab;
        public RuntimeAnimatorController[] animatorControllers = new RuntimeAnimatorController[7];
        [Header("Targeting")]
        [Tooltip("False means the tower cannot hit flying enemies (cannon, splash, ground effects).")]
        public bool canHitFlying = true;
        [Tooltip("True for arrow/ballista shots, reduced by enemy armor.")]
        public bool projectileIsArrow;
    }
}
