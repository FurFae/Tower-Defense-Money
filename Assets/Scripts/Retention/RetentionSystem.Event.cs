using System;

namespace TowerDefense
{
    public class EventDefinition
    {
        public readonly string id;
        public readonly string title;
        public readonly string description;
        // Event points per unit of each stat.
        public readonly RetentionStat[] stats;
        public readonly int[] points;
        // Tower whose blueprints the event track pays.
        public readonly string featuredTowerId;

        public EventDefinition(string id, string title, string description, RetentionStat[] stats, int[] points,
            string featuredTowerId)
        {
            this.id = id;
            this.title = title;
            this.description = description;
            this.stats = stats;
            this.points = points;
            this.featuredTowerId = featuredTowerId;
        }

        public int PointsFor(RetentionStat stat)
        {
            int index = Array.IndexOf(stats, stat);
            return index < 0 ? 0 : points[index];
        }
    }

    public class EventMilestone
    {
        public readonly int points;
        public readonly int coins;
        public readonly int crystals;
        public readonly int blueprints;
        public readonly ChestDefinition chest;

        public EventMilestone(int points, int coins = 0, int crystals = 0, int blueprints = 0, ChestDefinition chest = null)
        {
            this.points = points;
            this.coins = coins;
            this.crystals = crystals;
            this.blueprints = blueprints;
            this.chest = chest;
        }
    }

    // The live event: themed two-week events run back to back without a server. Points come
    // from the stats of the current theme; the milestone track resets with every new event.
    public static partial class RetentionSystem
    {
        public static readonly EventDefinition[] Events =
        {
            new EventDefinition("bugs", "Нашествие жуков", "Очки за каждого врага и за победы",
                new[] { RetentionStat.Kills, RetentionStat.Wins }, new[] { 1, 15 }, "Tower_CannonTower"),
            new EventDefinition("fortress", "Несокрушимая крепость", "Очки за волны и победы, особенно без потерь",
                new[] { RetentionStat.WavesCleared, RetentionStat.Wins, RetentionStat.FlawlessWins }, new[] { 5, 15, 40 },
                TowerCatalog.StarterId),
            new EventDefinition("builders", "Великая стройка", "Очки за постройку и улучшение башен и за победы",
                new[] { RetentionStat.TowersBuilt, RetentionStat.TowerUpgrades, RetentionStat.Wins }, new[] { 4, 40, 15 },
                "Tower_CannonTower"),
        };

        public static readonly EventMilestone[] EventMilestones =
        {
            new EventMilestone(30, coins: 50),
            new EventMilestone(80, crystals: 5),
            new EventMilestone(150, blueprints: 3),
            new EventMilestone(250, coins: 100),
            new EventMilestone(400, chest: Chests.ShopChests[0]),
            new EventMilestone(600, crystals: 15),
            new EventMilestone(850, blueprints: 5),
            new EventMilestone(1200, chest: Chests.ShopChests[1]),
        };

        // A Monday; events start every EventDays from it.
        private static readonly DateTime EventEpoch = new DateTime(2026, 10, 5);
        public const int EventDays = 14;

        private static int EventPeriod => (int)Math.Floor((DateTime.Today - EventEpoch).TotalDays / EventDays);

        public static EventDefinition CurrentEvent
        {
            get
            {
                int count = Events.Length;
                return Events[(EventPeriod % count + count) % count];
            }
        }

        public static TimeSpan UntilEventEnd => EventEpoch.AddDays((EventPeriod + 1) * EventDays) - DateTime.Now;

        public static int EventPoints => Data.eventPoints;

        private static string CurrentEventKey => $"{CurrentEvent.id}#{EventPeriod}";

        private static void ResetEventIfNeeded()
        {
            if (data.eventKey == CurrentEventKey) return;
            data.eventKey = CurrentEventKey;
            data.eventPoints = 0;
            Array.Clear(data.eventClaimed, 0, data.eventClaimed.Length);
            dirty = true;
        }

        private static void AddEventPoints(RetentionStat stat, int amount)
        {
            int gained = CurrentEvent.PointsFor(stat) * amount;
            if (gained <= 0) return;

            int old = data.eventPoints;
            data.eventPoints += gained;
            foreach (EventMilestone milestone in EventMilestones)
                if (old < milestone.points && data.eventPoints >= milestone.points)
                    OnGoalCompleted?.Invoke($"{CurrentEvent.title}: {milestone.points} очков");
        }

        public static bool IsEventClaimed(int index) => Data.eventClaimed[index];

        public static bool CanClaimEvent(int index)
        {
            return !IsEventClaimed(index) && Data.eventPoints >= EventMilestones[index].points;
        }

        public static int ClaimableEventCount()
        {
            int count = 0;
            for (int i = 0; i < EventMilestones.Length; i++)
                if (CanClaimEvent(i)) count++;
            return count;
        }

        public static string DescribeEventReward(int index)
        {
            EventMilestone milestone = EventMilestones[index];
            if (milestone.chest != null) return milestone.chest.title;
            if (milestone.blueprints > 0)
                return $"{TowerCatalog.ColoredTitle(TowerCatalog.Find(CurrentEvent.featuredTowerId))} ×{milestone.blueprints}";
            return RuntimeUI.RewardText(milestone.coins, milestone.crystals);
        }

        // Returns what was received, for the reward popup, or null if nothing was claimed.
        public static string ClaimEvent(int index)
        {
            if (!CanClaimEvent(index)) return null;
            EventMilestone milestone = EventMilestones[index];
            data.eventClaimed[index] = true;
            dirty = true;
            Flush();

            string text = DescribeEventReward(index);
            if (milestone.chest != null)
            {
                text = Chests.Open(milestone.chest, milestone.chest.guaranteeRare, false).Describe();
            }
            else if (milestone.blueprints > 0)
            {
                TowerDefinition tower = TowerCatalog.Find(CurrentEvent.featuredTowerId);
                if (MetaProgress.AddBlueprints(tower.id, milestone.blueprints, false))
                    text += $"\nНовая башня: {TowerCatalog.ColoredTitle(tower)}";
            }

            MetaProgress.Add(milestone.coins, milestone.crystals);
            OnChanged?.Invoke();
            return text;
        }
    }
}
