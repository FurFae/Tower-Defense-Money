using System;
using UnityEngine;

namespace TowerDefense
{
    public enum RetentionStat { Kills, TowersBuilt, WavesCleared, Wins, FlawlessWins }

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
    }

    // Daily reward, daily missions and achievements. Gameplay code only calls Report();
    // missions and the daily reward pay coins, achievements pay crystals (see MetaProgress).
    public static class RetentionSystem
    {
        public static readonly RetentionGoal[] Missions =
        {
            new RetentionGoal("Уничтожить 20 врагов", RetentionStat.Kills, 20, 25, 0),
            new RetentionGoal("Построить 4 башни", RetentionStat.TowersBuilt, 4, 20, 0),
            new RetentionGoal("Отбить 3 волны", RetentionStat.WavesCleared, 3, 30, 0),
        };

        public static readonly RetentionGoal[] Achievements =
        {
            new RetentionGoal("Первая кровь: убить врага", RetentionStat.Kills, 1, 0, 5),
            new RetentionGoal("Истребитель: 200 врагов", RetentionStat.Kills, 200, 0, 20),
            new RetentionGoal("Архитектор: 25 башен", RetentionStat.TowersBuilt, 25, 0, 15),
            new RetentionGoal("Первая победа", RetentionStat.Wins, 1, 0, 10),
            new RetentionGoal("Без потерь: победа с полными жизнями", RetentionStat.FlawlessWins, 1, 0, 20),
        };

        // Coins for each day of the login streak; loops after the last one,
        // and the last day of the cycle also pays crystals.
        public static readonly int[] DailyRewards = { 30, 40, 50, 60, 80, 100, 150 };
        public const int StreakCrystals = 10;
        // Crystals for claiming every mission of the day.
        public const int AllMissionsCrystals = 5;

        public static event Action OnChanged;
        public static event Action<string> OnGoalCompleted;

        // PlayerPrefs is backed by IndexedDB in WebGL. Swap Load/Flush for the
        // PluginYG2 Storage module once it is installed to get cloud saves.
        private const string SaveKey = "retention_v1";
        private const string DayFormat = "yyyy-MM-dd";
        private static readonly int StatCount = Enum.GetValues(typeof(RetentionStat)).Length;

        private static RetentionData data;
        private static bool dirty;

        private static RetentionData Data
        {
            get
            {
                if (data == null) Load();
                ResetDayIfNeeded();
                return data;
            }
        }

        private static string Today => DateTime.Today.ToString(DayFormat);

        private static void Load()
        {
            data = JsonUtility.FromJson<RetentionData>(PlayerPrefs.GetString(SaveKey, "")) ?? new RetentionData();
            Array.Resize(ref data.daily, StatCount);
            Array.Resize(ref data.total, StatCount);
            Array.Resize(ref data.missionClaimed, Missions.Length);
            Array.Resize(ref data.achievementClaimed, Achievements.Length);

            if (data.bankedGold > 0)
            {
                MetaProgress.Add(data.bankedGold, 0);
                data.bankedGold = 0;
                dirty = true;
            }
        }

        private static void ResetDayIfNeeded()
        {
            if (data.day == Today) return;
            data.day = Today;
            Array.Clear(data.daily, 0, data.daily.Length);
            Array.Clear(data.missionClaimed, 0, data.missionClaimed.Length);
            dirty = true;
        }

        public static void Flush()
        {
            if (!dirty || data == null) return;
            dirty = false;
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        public static void Report(RetentionStat stat, int amount = 1)
        {
            RetentionData d = Data;
            int s = (int)stat;
            int oldDaily = d.daily[s];
            int oldTotal = d.total[s];
            d.daily[s] += amount;
            d.total[s] += amount;
            dirty = true;

            foreach (RetentionGoal goal in Missions)
                if (goal.stat == stat && oldDaily < goal.target && d.daily[s] >= goal.target)
                    OnGoalCompleted?.Invoke(goal.title);
            foreach (RetentionGoal goal in Achievements)
                if (goal.stat == stat && oldTotal < goal.target && d.total[s] >= goal.target)
                    OnGoalCompleted?.Invoke(goal.title);

            // Kills are frequent, so they are written by the periodic flush instead.
            if (stat != RetentionStat.Kills) Flush();
            OnChanged?.Invoke();
        }

        public static int GetProgress(bool mission, int index)
        {
            RetentionGoal goal = mission ? Missions[index] : Achievements[index];
            int value = mission ? Data.daily[(int)goal.stat] : Data.total[(int)goal.stat];
            return Mathf.Min(value, goal.target);
        }

        public static bool IsClaimed(bool mission, int index)
        {
            return mission ? Data.missionClaimed[index] : Data.achievementClaimed[index];
        }

        public static bool CanClaim(bool mission, int index)
        {
            RetentionGoal goal = mission ? Missions[index] : Achievements[index];
            return !IsClaimed(mission, index) && GetProgress(mission, index) >= goal.target;
        }

        public static void Claim(bool mission, int index)
        {
            if (!CanClaim(mission, index)) return;
            RetentionGoal goal = mission ? Missions[index] : Achievements[index];
            int crystals = goal.crystals;

            if (mission)
            {
                data.missionClaimed[index] = true;
                if (Array.TrueForAll(data.missionClaimed, claimed => claimed))
                    crystals += AllMissionsCrystals;
            }
            else
            {
                data.achievementClaimed[index] = true;
            }
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

        private static int DailyCycleIndex => (Mathf.Max(DailyStreakDay, 1) - 1) % DailyRewards.Length;

        public static int DailyCoins => DailyRewards[DailyCycleIndex];

        public static int DailyCrystals => DailyCycleIndex == DailyRewards.Length - 1 ? StreakCrystals : 0;

        public static void ClaimDaily()
        {
            if (!CanClaimDaily) return;
            int coins = DailyCoins;
            int crystals = DailyCrystals;
            data.streak = DailyStreakDay;
            data.lastRewardDay = Today;
            Grant(coins, crystals);
        }

        public static int ClaimableCount()
        {
            int count = CanClaimDaily ? 1 : 0;
            for (int i = 0; i < Missions.Length; i++)
                if (CanClaim(true, i)) count++;
            for (int i = 0; i < Achievements.Length; i++)
                if (CanClaim(false, i)) count++;
            return count;
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
