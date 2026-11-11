using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TowerDefense
{
    // Pause button at the top of the screen during a level (Esc works too) and the pause
    // window: continue, restart, main menu. Added by RetentionUI, which owns the canvas.
    public class PauseUI : MonoBehaviour
    {
        private const string MainMenuScene = "MainMenu";

        private GameObject pauseButton;
        private GameObject panel;
        private Text levelText;

        private void Awake()
        {
            Build();
            pauseButton.SetActive(false);
        }

        private void Update()
        {
            GameManager game = GameManager.Instance;
            bool inBattle = game != null && !game.IsGameOver;
            bool visible = inBattle && !BuildMenuUI.IsOpen;
            if (pauseButton.activeSelf != visible) pauseButton.SetActive(visible);
            if (!inBattle)
            {
                if (panel.activeSelf) panel.SetActive(false);
                return;
            }

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                SetPaused(!game.IsPaused);
            // The window was closed with its X or by clicking outside it.
            else if (game.IsPaused && !panel.activeSelf)
                game.SetPaused(false);
        }

        private void SetPaused(bool paused)
        {
            GameManager game = GameManager.Instance;
            if (game == null) return;
            game.SetPaused(paused);
            if (!paused)
            {
                panel.SetActive(false);
                return;
            }

            LevelDefinition level = game.Level;
            levelText.text = level != null ? $"Уровень {level.number}: {level.title}" : "";
            RuntimeUI.CloseAllWindows();
            panel.transform.SetAsLastSibling();
            panel.SetActive(true);
        }

        private void Build()
        {
            Button open = RuntimeUI.CreateButton(transform, "PauseButton", "Пауза", 26, out _);
            RuntimeUI.StyleMenuButton(open);
            RuntimeUI.Place(open.transform, Vector2.one, Vector2.one, new Vector2(-24f, -24f), new Vector2(140f, 64f));
            open.onClick.AddListener(() => SetPaused(true));
            pauseButton = open.gameObject;

            Transform w = RuntimeUI.CreateWindow(transform, "Пауза", 470f, out panel, out _, out float y);
            levelText = RuntimeUI.CreateText(RuntimeUI.TopRow(w, ref y, 50f), "", 28, TextAnchor.MiddleCenter);
            y += 10f;

            AddButton(w, ref y, "Продолжить", () => SetPaused(false));
            AddButton(w, ref y, "Заново", () => GameManager.Instance?.RestartLevel());
            AddButton(w, ref y, "В меню", () => GameManager.Instance?.LoadMainMenu(MainMenuScene));
        }

        private static void AddButton(Transform window, ref float y, string label, UnityEngine.Events.UnityAction action)
        {
            Button button = RuntimeUI.CreateButton(RuntimeUI.TopRow(window, ref y, 70f), label, label, 30, out _);
            RuntimeUI.SetAnchors((RectTransform)button.transform, 0.25f, 0.75f);
            button.onClick.AddListener(action);
            y += 8f;
        }
    }
}
