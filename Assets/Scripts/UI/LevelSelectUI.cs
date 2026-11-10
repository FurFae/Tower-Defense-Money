using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TowerDefense
{
    // Level select window opened by the main menu's Play button: every campaign level with
    // its best stars; a level opens once the previous one is won. Added by RetentionUI.
    public class LevelSelectUI : MonoBehaviour
    {
        private class Row
        {
            public LevelDefinition level;
            public Text title;
            public Button button;
            public Text buttonLabel;
        }

        private static LevelSelectUI instance;

        private GameObject panel;
        private Text wallet;
        private readonly List<Row> rows = new List<Row>();
        private readonly List<Button> challengeButtons = new List<Button>();

        private void Awake()
        {
            instance = this;
            Build();
            MetaProgress.OnChanged += Refresh;
        }

        private void OnDestroy()
        {
            MetaProgress.OnChanged -= Refresh;
        }

        // Returns false if the meta canvas is not there, so the caller can load a level itself.
        public static bool Open()
        {
            if (instance == null) return false;
            RuntimeUI.CloseAllWindows();
            instance.Refresh();
            instance.panel.SetActive(true);
            return true;
        }

        private void Refresh()
        {
            wallet.text = RuntimeUI.WalletText();
            foreach (Row row in rows)
            {
                LevelDefinition level = row.level;
                bool unlocked = Levels.IsUnlocked(level);
                int stars = MetaProgress.GetLevelStars(level.number);

                string details = !unlocked ? $"Откроется после уровня {level.number - 1}"
                    : stars > 0 ? $"Лучший результат: {stars} из 3 звёзд" : "Ещё не пройден";
                row.title.text = $"<b>{level.number}. {level.title}</b>\n<size=22>{details}</size>";
                row.buttonLabel.text = unlocked ? "Играть" : "Закрыт";
                RuntimeUI.SetButtonState(row.button, unlocked);
            }

            for (int i = 0; i < challengeButtons.Count; i++)
                RuntimeUI.SetTabState(challengeButtons[i], (int)Levels.ActiveChallenge == i);
        }

        private void Build()
        {
            float height = 220f + Levels.All.Length * 108f;
            Transform w = RuntimeUI.CreateWindow(transform, "Выбор уровня", height, out panel, out wallet, out float y);

            // Режим боя: обычный или челлендж с повышенной наградой.
            Transform challengeRow = RuntimeUI.TopRow(w, ref y, 56f);
            string[] names = { "Обычный", "Без ремонта ×1.5", "Только редкие ×2" };
            for (int i = 0; i < names.Length; i++)
            {
                int index = i;
                Button challenge = RuntimeUI.CreateButton(challengeRow, "Challenge", names[i], 22, out _);
                RuntimeUI.SetAnchors((RectTransform)challenge.transform, i * 0.334f + 0.003f, (i + 1) * 0.334f - 0.003f);
                challenge.onClick.AddListener(() =>
                {
                    Levels.ActiveChallenge = (Challenge)index;
                    Refresh();
                });
                challengeButtons.Add(challenge);
            }

            foreach (LevelDefinition level in Levels.All)
            {
                Transform background = RuntimeUI.CreateRowBackground(RuntimeUI.TopRow(w, ref y, 100f));
                Text title = RuntimeUI.CreateText(background, "", 28, TextAnchor.MiddleLeft);
                RuntimeUI.SetAnchors(title.rectTransform, 0.03f, 0.72f);
                Button button = RuntimeUI.CreateButton(background, "Play", "", 28, out Text buttonLabel);
                RuntimeUI.SetAnchors((RectTransform)button.transform, 0.75f, 0.97f, 14f);
                button.onClick.AddListener(() => Levels.Load(level));
                rows.Add(new Row { level = level, title = title, button = button, buttonLabel = buttonLabel });
            }
        }
    }
}
