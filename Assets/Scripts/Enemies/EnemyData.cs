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
    }
}
