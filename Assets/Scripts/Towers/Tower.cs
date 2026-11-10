using UnityEngine;

namespace TowerDefense
{
    public enum TargetingMode
    {
        ClosestToBase = 0, // дальше всех прошёл по маршруту
        ClosestToTower = 1,
        Tankiest = 2,
        Fastest = 3,
    }

    public enum PoisonMode
    {
        NewTarget = 0,   // всегда неотравленная цель (по умолчанию)
        StrongestPoison = 1, // добивает самого отравленного... сильнейший яд держит
        Tankiest = 2,
    }

    public class Tower : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer baseRenderer;
        [SerializeField] private SpriteRenderer weaponRenderer;
        [SerializeField] private Animator animator;
        [SerializeField] private Animator weaponAnimator;
        [SerializeField] private Transform weaponPivot;

        // In-battle upgrades bought with mana; they last until the end of the level.
        public const int MaxBattleTier = 2;
        private const float SellRefund = 0.7f; // доля вложенной маны при продаже
        private static readonly float[] TierCostMultipliers = { 1.5f, 3f };
        private const float DamagePerTier = 0.4f;
        private const float RangePerTier = 0.4f;

        private TowerData data;
        private WeaponData weapon;
        private float baseDamage;
        private float cooldown;
        private TextMesh tierBadge;
        private int invested; // мана, вложенная в постройку и улучшения (для продажи)

        public TowerData Data => data;
        public int BattleTier { get; private set; }
        public TargetingMode Targeting { get; set; }
        public PoisonMode PoisonTargeting { get; set; }
        public bool IsBattleMaxed => BattleTier >= MaxBattleTier;
        public int UpgradeCost => IsBattleMaxed ? 0 : Mathf.RoundToInt(data.cost * TierCostMultipliers[BattleTier]);
        public int SellPrice => Mathf.RoundToInt(invested * SellRefund);
        public float Damage => DamageAt(BattleTier);
        public float Range => RangeAt(BattleTier);

        public float DamageAt(int tier) => baseDamage * (1f + DamagePerTier * tier);

        public float RangeAt(int tier) => data.range + RangePerTier * tier;

        public bool TryUpgrade()
        {
            GameManager game = GameManager.Instance;
            if (IsBattleMaxed || game == null || game.IsPaused || !game.TrySpend(UpgradeCost)) return false;
            invested += UpgradeCost;
            BattleTier++;
            GameSfx.Play(GameSfx.Event.Upgrade, transform.position);
            ShowTierBadge();
            return true;
        }

        // Returns invested mana and marks the slot free; the slot destroys the tower.
        public bool TrySell()
        {
            GameManager game = GameManager.Instance;
            if (game == null || game.IsPaused || game.IsGameOver) return false;
            game.AddCurrency(SellPrice);
            GameSfx.Play(GameSfx.Event.Sell, transform.position);
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
            BattleTier = 0;
            Targeting = TargetingMode.ClosestToBase;
            PoisonTargeting = PoisonMode.NewTarget;
            invested = towerData.cost;
            // Art exists for fewer levels than a tower can reach: higher levels reuse the last one.
            int level = MetaProgress.GetTowerLevel(data.name);
            Sprite levelSprite = ForLevel(data.levelSprites, level, out int baseArtLevel);
            weapon = ForLevel(data.weapons, level, out _);
            baseDamage = data.damage * MetaProgress.DamageMultiplier(data.name);
            if (baseRenderer != null)
                baseRenderer.sprite = levelSprite != null ? levelSprite : data.baseSprite;
            if (weaponRenderer != null)
                weaponRenderer.sprite = weapon != null ? weapon.sprite : data.weaponSprite;
            if (animator != null)
            {
                animator.runtimeAnimatorController = ForLevel(data.animatorControllers, level, out int artLevel);
                ShowFinalStage(Mathf.Max(artLevel, baseArtLevel));
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
            if (GameManager.Instance != null && (GameManager.Instance.IsGameOver || GameManager.Instance.IsPaused)) return;

            Enemy target = FindTarget();
            if (target == null)
            {
                RecoverRecoil();
                return;
            }

            AimAt(target.transform.position);

            cooldown -= Time.deltaTime;
            if (cooldown <= 0f)
            {
                Fire(target);
                cooldown = 1f / Mathf.Max(0.01f, data.fireRate);
            }
            RecoverRecoil();
        }

        private Enemy FindTarget()
        {
            Vector3 origin = transform.position;
            if (data.preferUnpoisonedTarget && PoisonTargeting == PoisonMode.NewTarget)
            {
                Enemy unpoisoned = FindTarget(origin, true);
                if (unpoisoned != null) return unpoisoned;
            }
            return FindTarget(origin, false);
        }

        private Enemy FindTarget(Vector3 origin, bool unpoisonedOnly)
        {
            Enemy best = null;
            float bestScore = float.MinValue;

            foreach (Enemy enemy in Enemy.Active)
            {
                if (enemy == null) continue;
                if (unpoisonedOnly && enemy.IsPoisoned) continue;
                if (enemy.IsFlying && !data.canHitFlying) continue;
                float dist = Vector3.Distance(origin, enemy.transform.position);
                if (dist > Range) continue;

                float score = ScoreTarget(enemy, origin, dist);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = enemy;
                }
            }
            return best;
        }

        private float ScoreTarget(Enemy enemy, Vector3 origin, float dist)
        {
            switch (Targeting)
            {
                case TargetingMode.ClosestToTower: return -dist;
                case TargetingMode.Tankiest:
                    return enemy.MaxHealth + (PoisonTargeting == PoisonMode.Tankiest && data.preferUnpoisonedTarget ? enemy.CurrentHealth : 0f);
                case TargetingMode.Fastest: return enemy.MoveSpeed;
                default: return enemy.DistanceTraveled; // ClosestToBase
            }
        }

        // Простая «живая» анимация без новых ассетов: отдача оружия назад и возврат.
        private Vector3 pivotBasePosition;
        private Vector3 recoilOffset;
        private float recoil;

        private void RecoverRecoil()
        {
            if (weaponPivot == null || recoil <= 0f) return;
            recoil = Mathf.MoveTowards(recoil, 0f, Time.deltaTime * 6f);
            weaponPivot.position = pivotBasePosition + recoilOffset * recoil;
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
            GameObject go = SimplePool.Get(data.projectilePrefab, spawnPos, Quaternion.identity);
            Projectile projectile = go.GetComponent<Projectile>();
            if (projectile != null)
                projectile.Initialize(target, Damage, data.projectileSpeed,
                    data.splashRadius, data.splashDamageMultiplier,
                    data.damageOverTimePerSecond, data.damageOverTimeDuration, data.impactPrefab,
                    data.pierceCount, data.chainCount, data.chainRadius, data.chainDamageMultiplier,
                    data.chainArcPrefab, data.slowDuration, data.slowMultiplier,
                    data.burnDamagePerSecond, data.burnDuration, data.projectileIsArrow);

            PlayShotSound();

            // Отдача: оружие дёргается назад и плавно возвращается (RecoverRecoil).
            if (weaponPivot != null)
            {
                if (recoil <= 0f) pivotBasePosition = weaponPivot.position;
                recoil = 1f;
                recoilOffset = -(target.transform.position - spawnPos).normalized * 0.08f;
            }

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

        private void PlayShotSound()
        {
            if (data == null) return;
            if (data.splashRadius > 0f) GameSfx.Play(GameSfx.Event.CannonShot, transform.position);
            else if (data.chainCount > 0) GameSfx.Play(GameSfx.Event.LightningShot, transform.position);
            else if (data.burnDamagePerSecond > 0f) GameSfx.Play(GameSfx.Event.BurnShot, transform.position);
            else if (data.slowDuration > 0f) GameSfx.Play(GameSfx.Event.IceShot, transform.position);
            else if (data.damageOverTimePerSecond > 0f) GameSfx.Play(GameSfx.Event.PoisonShot, transform.position);
            else if (data.projectileIsArrow) GameSfx.Play(data.pierceCount > 0 ? GameSfx.Event.BallistaShot : GameSfx.Event.ArrowShot, transform.position);
        }

        private void OnDrawGizmosSelected()
        {
            if (data == null) return;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, Range);
        }
    }
}
