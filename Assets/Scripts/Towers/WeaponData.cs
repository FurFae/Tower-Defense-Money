using UnityEngine;

namespace TowerDefense
{
    [CreateAssetMenu(menuName = "Tower Defense/Weapon Data", fileName = "NewWeaponData")]
    public class WeaponData : ScriptableObject
    {
        public int pixelsHeight = 50;
        public Sprite sprite;
        public RuntimeAnimatorController animatorController;
    }
}
