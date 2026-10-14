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
        public Sprite baseSprite;
        // Per tower level (index = level - 1); towers without them use weaponSprite.
        public WeaponData[] weapons = new WeaponData[7];
        public Sprite weaponSprite;
        public GameObject projectilePrefab;
        public RuntimeAnimatorController[] animatorControllers = new RuntimeAnimatorController[7];
    }
}
