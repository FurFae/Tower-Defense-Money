using UnityEngine;

namespace TowerDefense
{
    public class Tower : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer baseRenderer;
        [SerializeField] private SpriteRenderer weaponRenderer;
        [SerializeField] private Animator animator;
        [SerializeField] private Animator weaponAnimator;
        [SerializeField] private Transform weaponPivot;

        // In-battle upgrades bought with mana; they last until the end of the level.
        public const int MaxBattleTier = 2;
        private static readonly float[] TierCostMultipliers = { 1.5f, 3f };
        private const float DamagePerTier = 0.4f;
        private const float RangePerTier = 0.4f;

        private TowerData data;
        private WeaponData weapon;
        private float baseDamage;
        private float cooldown;
        private TextMesh tierBadge;

        public TowerData Data => data;
        public int BattleTier { get; private set; }
        public bool IsBattleMaxed => BattleTier >= MaxBattleTier;
        public int UpgradeCost => IsBattleMaxed ? 0 : Mathf.RoundToInt(data.cost * TierCostMultipliers[BattleTier]);
        public float Damage => DamageAt(BattleTier);
        public float Range => RangeAt(BattleTier);

        public float DamageAt(int tier) => baseDamage * (1f + DamagePerTier * tier);

        public float RangeAt(int tier) => data.range + RangePerTier * tier;

        public bool TryUpgrade()
        {
            GameManager game = GameManager.Instance;
            if (IsBattleMaxed || game == null || game.IsPaused || !game.TrySpend(UpgradeCost)) return false;
            BattleTier++;
            ShowTierBadge();
            return true;
        }

        // "+1" / "+2" next to the tower, in the UI font.
        private void ShowTierBadge()
        {
            if (tierBadge == null)
            {
                GameObject badge = new GameObject("TierBadge");
                badge.transform.position = transform.position + new Vector3(0.9f, -0.7f, 0f);
                tierBadge = badge.AddComponent<TextMesh>();
                Font font = UISkin.Current != null && UISkin.Current.font != null
                    ? UISkin.Current.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                tierBadge.font = font;
                MeshRenderer badgeRenderer = badge.GetComponent<MeshRenderer>();
                badgeRenderer.sharedMaterial = font.material;
                badgeRenderer.sortingOrder = 50;
                tierBadge.fontSize = 64;
                tierBadge.characterSize = 0.08f;
                tierBadge.anchor = TextAnchor.MiddleCenter;
                tierBadge.color = RuntimeUI.CoinColor;
                // Keeps its world size whatever the slot and tower scales are.
                badge.transform.SetParent(transform, true);
            }
            tierBadge.text = $"+{BattleTier}";
        }

        public void Initialize(TowerData towerData)
        {
            data = towerData;
            // Art exists for fewer levels than a tower can reach: higher levels reuse the last one.
            int level = MetaProgress.GetTowerLevel(data.name);
            weapon = ForLevel(data.weapons, level, out _);
            baseDamage = data.damage * MetaProgress.DamageMultiplier(data.name);
            if (baseRenderer != null) baseRenderer.sprite = data.baseSprite;
            if (weaponRenderer != null)
                weaponRenderer.sprite = weapon != null ? weapon.sprite : data.weaponSprite;
            if (animator != null)
            {
                animator.runtimeAnimatorController = ForLevel(data.animatorControllers, level, out int artLevel);
                ShowFinalStage(artLevel);
            }
            if (weaponAnimator != null)
                weaponAnimator.runtimeAnimatorController = weapon != null ? weapon.animatorController : null;
            if (weapon != null && weaponPivot != null && data.baseSprite != null)
                weaponPivot.position += new Vector3(0, 0, (weapon.pixelsHeight - data.baseSprite.rect.height / 2) * 0.01f);
            cooldown = 0f;
        }

        private static T ForLevel<T>(T[] perLevel, int level, out int artLevel) where T : Object
        {
            for (int i = Mathf.Min(level, perLevel?.Length ?? 0) - 1; i >= 0; i--)
            {
                if (perLevel[i] == null) continue;
                artLevel = i + 1;
                return perLevel[i];
            }
            artLevel = 0;
            return null;
        }

        // The level-N controller grows the tower through stages 1..N and then idles in "N 0".
        // A tower placed in battle already has its level, so it starts in that idle state.
        // State names come from the designer's controllers in Skins_tower/archer_tower.
        private void ShowFinalStage(int artLevel)
        {
            if (animator.runtimeAnimatorController == null) return;
            foreach (string state in new[] { $"{artLevel} 0", artLevel.ToString() })
            {
                if (!animator.HasState(0, Animator.StringToHash(state))) continue;
                animator.Play(state, 0, 0f);
                animator.Update(0f);
                return;
            }
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
            Vector3 origin = transform.position;

            foreach (Enemy enemy in Enemy.Active)
            {
                if (enemy == null) continue;
                float dist = Vector3.Distance(origin, enemy.transform.position);
                if (dist <= Range && enemy.DistanceTraveled > bestProgress)
                {
                    bestProgress = enemy.DistanceTraveled;
                    best = enemy;
                }
            }
            return best;
        }

        private void AimAt(Vector3 worldPos)
        {
            return;
            // float angle = Mathf.Round(Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg / 45f) * 45f;
        }

        private void Fire(Enemy target)
        {
            if (data.projectilePrefab == null) return;
            Vector3 spawnPos = weaponPivot != null ? weaponPivot.position : transform.position;
            GameObject go = Instantiate(data.projectilePrefab, spawnPos, Quaternion.identity);
            Projectile projectile = go.GetComponent<Projectile>();
            if (projectile != null)
                projectile.Initialize(target, Damage, data.projectileSpeed);

            if (weaponAnimator == null || weaponAnimator.runtimeAnimatorController == null) return;
            Vector3 dir = target.transform.position - spawnPos;
            if (dir.y > Mathf.Abs(dir.x))
                weaponAnimator.Play("U_Attack");
            else if (-dir.y > Mathf.Abs(dir.x))
                weaponAnimator.Play("D_Attack");
            else
            {
                weaponAnimator.Play("S_Attack");
                weaponRenderer.flipX = dir.x > 0;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (data == null) return;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, Range);
        }
    }
}
