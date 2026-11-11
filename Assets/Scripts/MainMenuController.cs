using TowerDefense;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "game_scene";
    private void Start()
    {
        var canvasObject = GameObject.Find("Canvas");
        if (canvasObject == null) return;
        Transform canvas = canvasObject.transform;
        foreach (string name in new[] { "MainMenu", "Settings" })
        {
            var old = canvas.Find(name);
            if (old != null) old.gameObject.SetActive(false);
        }
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        if (scaler != null) scaler.matchWidthOrHeight = 0.5f;
        var shade = RuntimeUI.CreateImage(canvas, "MenuShade", new Color(0.025f, 0.055f, 0.045f, 0.18f));
        RuntimeUI.Stretch(shade.rectTransform);
        var left = RuntimeUI.CreateImage(canvas, "MenuSidebar", new Color(0.035f, 0.075f, 0.06f, 0.94f));
        left.rectTransform.anchorMin = Vector2.zero; left.rectTransform.anchorMax = new Vector2(0, 1);
        left.rectTransform.pivot = Vector2.zero; left.rectTransform.sizeDelta = new Vector2(720, 0);
        left.rectTransform.anchoredPosition = Vector2.zero;
        var line = RuntimeUI.CreateImage(left.transform, "GoldRule", new Color(0.68f, 0.57f, 0.32f));
        RuntimeUI.Place(line.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(64, -120), new Vector2(72, 3));
        var kicker = RuntimeUI.CreateText(left.transform, "СТРАТЕГИЯ  /  ЗАЩИТА ЗАМКА", 24, TextAnchor.MiddleLeft);
        kicker.color = new Color(0.75f, 0.67f, 0.44f);
        RuntimeUI.Place(kicker.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(64, -146), new Vector2(590, 40));
        var title = RuntimeUI.CreateText(left.transform, "WARDEN’S\nGATE", 96, TextAnchor.UpperLeft);
        title.fontStyle = FontStyle.Bold; title.lineSpacing = 0.9f;
        RuntimeUI.Place(title.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(58, -210), new Vector2(640, 225));
        var caption = RuntimeUI.CreateText(left.transform, "Ваш замок. Ваша стратегия.\nОстановите нашествие у ворот.", 30, TextAnchor.UpperLeft);
        caption.color = new Color(0.68f, 0.77f, 0.70f);
        RuntimeUI.Place(caption.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(64, -460), new Vector2(580, 100));
        var play = RuntimeUI.CreateButton(left.transform, "Play", "НАЧАТЬ ПОХОД    →", 30, out var playLabel);
        playLabel.fontStyle = FontStyle.Bold;
        play.image.color = new Color(0.62f, 0.45f, 0.20f);
        RuntimeUI.Place(play.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(64, -595), new Vector2(580, 88));
        play.onClick.AddListener(OnPlayClicked);
        var sound = RuntimeUI.CreateButton(left.transform, "Sound", "", 24, out var soundLabel);
        RuntimeUI.StyleMenuButton(sound);
        RuntimeUI.Place(sound.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(64, -710), new Vector2(280, 60));
        AudioListener.volume = PlayerPrefs.GetInt("audio_muted", 0) == 1 ? 0 : 1;
        void RefreshSound() => soundLabel.text = AudioListener.volume > 0 ? "Звук: включён" : "Звук: выключен";
        RefreshSound();
        sound.onClick.AddListener(() => { AudioListener.volume = AudioListener.volume > 0 ? 0 : 1; PlayerPrefs.SetInt("audio_muted", AudioListener.volume == 0 ? 1 : 0); PlayerPrefs.Save(); RefreshSound(); });
        var footer = RuntimeUI.CreateText(left.transform, "Соберите отряд башен. Удержите оборону.", 22, TextAnchor.MiddleLeft);
        footer.color = new Color(0.52f, 0.63f, 0.56f);
        RuntimeUI.Place(footer.transform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(64, 145), new Vector2(590, 40));
    }
    public void OnPlayClicked()
    {
        if (!LevelSelectUI.Open()) SceneManager.LoadScene(gameSceneName);
    }
    public void OnQuitClicked()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}