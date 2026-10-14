using System;
using UnityEngine;

namespace TowerDefense
{
    // Appended only: saves store counters by index.
    public enum RetentionStat { Kills, TowersBuilt, WavesCleared, Wins, FlawlessWins, Stars, TowerUpgrades, ChestsOpened }

    public enum GoalList { Daily, Weekly, Achievements }

    public class RetentionGoal
    {
        public readonly string title;
        public readonly RetentionStat stat;
        public readonly int target;
        public readonly int coins;
        public readonly int crystals;

        public RetentionGoal(string title, RetentionStat stat, int target, int coins, int crystals)
        {
            this.title = title;
            this.stat = stat;
            this.target = target;
            this.coins = coins;
            this.crystals = crystals;
        }
    }

    [Serializable]
    public class RetentionData
    {
        public string day = "";
        public int[] daily = new int[0];
        public int[] total = new int[0];
        public bool[] missionClaimed = new bool[0];
        public bool[] achievementClaimed = new bool[0];
        public string lastRewardDay = "";
        public int streak;
        public int bankedGold; // legacy: rewards used to be paid in level gold
        public string week = "";
        public int[] weekly = new int[0];
        public bool[] weeklyClaimed = new bool[0];
        public string eventKey = "";
        public int eventPoints;
        public bool[] eventClaimed = new bool[0];
    }

    // Daily reward, daily and weekly missions, achievements and the live event
    // (RetentionSystem.Event.cs). Gameplay code only calls Report(); missions and the daily
    // reward pay coins, achievements pay crystals (see MetaProgress).
    public static partial class RetentionSystem
    {
        // Every day DailyCount missions with different stats are drawn from the pool,
        // every week (from Monday) WeeklyCount; the draw depends only on the date.
        public const int DailyCount = 3;
        public const int WeeklyCount = 4;

        private static readonly RetentionGoal[] DailyPool =
        {
            new RetentionGoal("Уничтожить 30 врагов", RetentionStat.Kills, 30, 25, 0),
            new RetentionGoal("Построить 5 башен", RetentionStat.TowersBuilt, 5, 20, 0),
            new RetentionGoal("Отбить 5 волн", RetentionStat.WavesCleared, 5, 30, 0),
            new RetentionGoal("Победить в уровне", RetentionStat.Wins, 1, 35, 0),
            new RetentionGoal("Победить без потерь", RetentionStat.FlawlessWins, 1, 50, 0),
            new RetentionGoal("Собрать 3 звезды", RetentionStat.Stars, 3, 40, 0),
            new RetentionGoal("Открыть 2 сундука", RetentionStat.ChestsOpened, 2, 30, 0),
        };

        private static readonly RetentionGoal[] WeeklyPool =
        {
            new RetentionGoal("Уничтожить 300 врагов", RetentionStat.Kills, 300, 150, 5),
            new RetentionGoal("Победить 7 раз", RetentionStat.Wins, 7, 200, 10),
            new RetentionGoal("Отбить 40 волн", RetentionStat.WavesCleared, 40, 150, 0),
            new RetentionGoal("Построить 40 башен", RetentionStat.TowersBuilt, 40, 120, 0),
            new RetentionGoal("3 победы без потерь", RetentionStat.FlawlessWins, 3, 100, 10),
            new RetentionGoal("Собрать 15 звёзд", RetentionStat.Stars, 15, 150, 5),
            new RetentionGoal("Улучшить башню 2 раза", RetentionStat.TowerUpgrades, 2, 100, 10),
            new RetentionGoal("Открыть 10 сундуков", RetentionStat.ChestsOpened, 10, 120, 0),
        };

        // Appended only: claimed flags are stored by index.
        public static readonly RetentionGoal[] Achievements =
        {
            new RetentionGoal("Первая кровь: убить врага", RetentionStat.Kills, 1, 0, 5),
            new RetentionGoal("Истребитель: 200 врагов", RetentionStat.Kills, 200, 0, 20),
            new RetentionGoal("Архитектор: 25 башен", RetentionStat.TowersBuilt, 25, 0, 15),
            new RetentionGoal("Первая победа", RetentionStat.Wins, 1, 0, 10),
            new RetentionGoal("Без потерь: победа с полными жизнями", RetentionStat.FlawlessWins, 1, 0, 20),
            new RetentionGoal("Гроза жуков: 1000 врагов", RetentionStat.Kills, 1000, 0, 50),
            new RetentionGoal("Ветеран: 10 побед", RetentionStat.Wins, 10, 0, 25),
            new RetentionGoal("Звездочёт: 30 звёзд", RetentionStat.Stars, 30, 0, 30),
            new RetentionGoal("Кузнец: 5 улучшений башен", RetentionStat.TowerUpgrades, 5, 0, 25),
            new RetentionGoal("Кладоискатель: 25 сундуков", RetentionStat.ChestsOpened, 25, 0, 25),
        };

        // Coins for each day of the login streak; loops after the last one,
        // and the last day of the cycle also pays crystals.
        public static readonly int[] DailyRewards = { 30, 40, 50, 60, 80, 100, 150 };
        public const int StreakCrystals = 10;
        // Crystals for claiming every mission of the day / of the week.
        public const int AllMissionsCrystals = 5;
        public const int AllWeeklyCrystals = 25;

        public static event Action OnChanged;
        public static event Action<string> OnGoalCompleted;

        private const string DayFormat = "yyyy-MM-dd";
        private static readonly int StatCount = Enum.GetValues(typeof(RetentionStat)).Length;

        private static RetentionData data;
        private static bool dirty;
        private static string goalsDay;
        private static string goalsWeek;
        private static readonly RetentionGoal[] dailyGoals = new RetentionGoal[DailyCount];
        private static readonly RetentionGoal[] weeklyGoals = new RetentionGoal[WeeklyCount];

        private static RetentionData Data
        {
            get
            {
                if (data == null) Load();
                ResetPeriodsIfNeeded();
                return data;
            }
        }

        private static string Today => DateTime.Today.ToString(DayFormat);

        private static DateTime WeekStart => DateTime.Today.AddDays(-(((int)DateTime.Today.DayOfWeek + 6) % 7));

        private static string ThisWeek => WeekStart.ToString(DayFormat);

        public static TimeSpan UntilDailyReset => DateTime.Today.AddDays(1) - DateTime.Now;

        public static TimeSpan UntilWeeklyReset => WeekStart.AddDays(7) - DateTime.Now;

        private static void Load()
        {
            data = JsonUtility.FromJson<RetentionData>(SaveStorage.Get(SaveStorage.RetentionKey)) ?? new RetentionData();
            Array.Resize(ref data.daily, StatCount);
            Array.Resize(ref data.total, StatCount);
            Array.Resize(ref data.weekly, StatCount);
            Array.Resize(ref data.missionClaimed, DailyCount);
            Array.Resize(ref data.weeklyClaimed, WeeklyCount);
            Array.Resize(ref data.achievementClaimed, Achievements.Length);
            Array.Resize(ref data.eventClaimed, EventMilestones.Length);

            if (data.bankedGold > 0)
            {
                MetaProgress.Add(data.bankedGold, 0);
                data.bankedGold = 0;
                dirty = true;
            }
        }

        // Drops the cached state, e.g. when the cloud save has arrived.
        public static void Reload()
        {
            data = null;
            dirty = false;
            OnChanged?.Invoke();
        }

        private static void ResetPeriodsIfNeeded()
        {
            if (data.day != Today)
            {
                data.day = Today;
                Array.Clear(data.daily, 0, data.daily.Length);
                Array.Clear(data.missionClaimed, 0, data.missionClaimed.Length);
                dirty = true;
            }
            if (data.week != ThisWeek)
            {
                data.week = ThisWeek;
                Array.Clear(data.weekly, 0, data.weekly.Length);
                Array.Clear(data.weeklyClaimed, 0, data.weeklyClaimed.Length);
                dirty = true;
            }
            ResetEventIfNeeded();
        }

        public static void Flush()
        {
            if (!dirty || data == null || !SaveStorage.IsReady) return;
            dirty = false;
            SaveStorage.Set(SaveStorage.RetentionKey, JsonUtility.ToJson(data));
        }

        public static void Report(RetentionStat stat, int amount = 1)
        {
            RetentionData d = Data;
            int s = (int)stat;
            int oldDaily = d.daily[s];
            int oldWeekly = d.weekly[s];
            int oldTotal = d.total[s];
            d.daily[s] += amount;
            d.weekly[s] += amount;
            d.total[s] += amount;
            dirty = true;

            NotifyCompleted(GetGoals(GoalList.Daily), stat, oldDaily, d.daily[s]);
            NotifyCompleted(GetGoals(GoalList.Weekly), stat, oldWeekly, d.weekly[s]);
            NotifyCompleted(Achievements, stat, oldTotal, d.total[s]);
            AddEventPoints(stat, amount);

            // Kills are frequent, so they are written by the periodic flush instead.
            if (stat != RetentionStat.Kills) Flush();
            OnChanged?.Invoke();
        }

        private static void NotifyCompleted(RetentionGoal[] goals, RetentionStat stat, int oldValue, int newValue)
        {
            foreach (RetentionGoal goal in goals)
                if (goal.stat == stat && oldValue < goal.target && newValue >= goal.target)
                    OnGoalCompleted?.Invoke(goal.title);
        }

        public static RetentionGoal[] GetGoals(GoalList list)
        {
            switch (list)
            {
                case GoalList.Daily:
                    if (goalsDay != Today)
                    {
                        goalsDay = Today;
                        Draw(DailyPool, dailyGoals, (int)(DateTime.Today - DateTime.MinValue).TotalDays);
                    }
                    return dailyGoals;
                case GoalList.Weekly:
                    if (goalsWeek != ThisWeek)
                    {
                        goalsWeek = ThisWeek;
                        Draw(WeeklyPool, weeklyGoals, (int)(WeekStart - DateTime.MinValue).TotalDays / 7);
                    }
                    return weeklyGoals;
                default:
                    return Achievements;
            }
        }

        // Seeded shuffle of the pool; System.Random is deterministic for a seed on every platform.
        private static void Draw(RetentionGoal[] pool, RetentionGoal[] result, int seed)
        {
            RetentionGoal[] shuffled = (RetentionGoal[])pool.Clone();
            System.Random random = new System.Random(seed);
            for (int i = shuffled.Length - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }
            Array.Copy(shuffled, result, result.Length);
        }

        private static int[] Counters(GoalList list)
        {
            RetentionData d = Data;
            return list == GoalList.Daily ? d.daily : list == GoalList.Weekly ? d.weekly : d.total;
        }

        private static bool[] Claimed(GoalList list)
        {
            RetentionData d = Data;
            return list == GoalList.Daily ? d.missionClaimed : list == GoalList.Weekly ? d.weeklyClaimed : d.achievementClaimed;
        }

        public static int GetProgress(GoalList list, int index)
        {
            RetentionGoal goal = GetGoals(list)[index];
            return Mathf.Min(Counters(list)[(int)goal.stat], goal.target);
        }

        public static bool IsClaimed(GoalList list, int index) => Claimed(list)[index];

        public static bool CanClaim(GoalList list, int index)
        {
            return !IsClaimed(list, index) && GetProgress(list, index) >= GetGoals(list)[index].target;
        }

        public static int AllClaimedBonus(GoalList list)
        {
            return list == GoalList.Daily ? AllMissionsCrystals : list == GoalList.Weekly ? AllWeeklyCrystals : 0;
        }

        public static void Claim(GoalList list, int index)
        {
            if (!CanClaim(list, index)) return;
            RetentionGoal goal = GetGoals(list)[index];
            bool[] claimed = Claimed(list);
            claimed[index] = true;

            int crystals = goal.crystals;
            if (list != GoalList.Achievements && Array.TrueForAll(claimed, c => c))
                crystals += AllClaimedBonus(list);
            Grant(goal.coins, crystals);
        }

        public static bool CanClaimDaily => Data.lastRewardDay != Today;

        // Day of the streak the next (or, if already claimed, today's) reward belongs to.
        public static int DailyStreakDay
        {
            get
            {
                RetentionData d = Data;
                if (d.lastRewardDay == Today) return d.streak;
                bool continues = d.lastRewardDay == DateTime.Today.AddDays(-1).ToString(DayFormat);
                return continues ? d.streak + 1 : 1;
            }
        }

        // Position of the next (or today's) reward in the 7-day cycle, from 0.
        public static int DailyCycleIndex => (Mathf.Max(DailyStreakDay, 1) - 1) % DailyRewards.Length;

        public static int DailyCoins => DailyRewards[DailyCycleIndex];

        public static int DailyCrystals => CycleCrystals(DailyCycleIndex);

        public static int CycleCrystals(int cycleIndex) => cycleIndex == DailyRewards.Length - 1 ? StreakCrystals : 0;

        public static void ClaimDaily()
        {
            if (!CanClaimDaily) return;
            int coins = DailyCoins;
            int crystals = DailyCrystals;
            data.streak = DailyStreakDay;
            data.lastRewardDay = Today;
            Grant(coins, crystals);
        }

        public static int ClaimableCount(GoalList list)
        {
            int count = 0;
            for (int i = 0; i < GetGoals(list).Length; i++)
                if (CanClaim(list, i)) count++;
            return count;
        }

        public static int ClaimableCount()
        {
            return (CanClaimDaily ? 1 : 0) + ClaimableCount(GoalList.Daily) + ClaimableCount(GoalList.Weekly)
                + ClaimableCount(GoalList.Achievements) + ClaimableEventCount();
        }

        private static void Grant(int coins, int crystals)
        {
            dirty = true;
            Flush();
            MetaProgress.Add(coins, crystals);
            OnChanged?.Invoke();
        }
    }
}
