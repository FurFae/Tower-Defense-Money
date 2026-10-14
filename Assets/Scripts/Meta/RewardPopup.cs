using UnityEngine;
using UnityEngine.UI;

namespace TowerDefense
{
    // Popup on the meta canvas that lists what the player just received:
    // an opened chest or the rewards for a won level. Added by RetentionUI.
    public class RewardPopup : MonoBehaviour
    {
        private static RewardPopup instance;

        private GameObject panel;
        private Text body;

        private void Awake()
        {
            instance = this;

            Transform w = RuntimeUI.CreateWindow(transform, "Награда", 780f, out panel, out _, out float y);
            body = RuntimeUI.CreateText(RuntimeUI.TopRow(w, ref y, 550f), "", 26, TextAnchor.MiddleCenter);
            Button ok = RuntimeUI.CreateButton(RuntimeUI.TopRow(w, ref y, 70f), "Ok", "Отлично", 28, out _);
            RuntimeUI.SetAnchors((RectTransform)ok.transform, 0.3f, 0.7f);
            ok.onClick.AddListener(() => panel.SetActive(false));

            MetaProgress.OnLevelCompleted += ShowLevelResult;
        }

        private void OnDestroy()
        {
            MetaProgress.OnLevelCompleted -= ShowLevelResult;
        }

        public static void Show(string title, string text)
        {
            if (instance == null) return;
            instance.body.text = $"<b>{title}</b>\n\n{text}";
            // Shown over whatever window is open, so the others are not closed.
            instance.panel.transform.SetAsLastSibling();
            instance.panel.SetActive(true);
        }

        private void ShowLevelResult(LevelResult result)
        {
            string text = $"Звёзды: {result.stars} из 3\n+{RuntimeUI.CoinsText(result.coins)}\n";
            if (result.manaCoins > 0)
                text += $"Остаток маны {result.leftoverMana}: +{RuntimeUI.CoinsText(result.manaCoins)}\n";
            if (result.campaignTower != null)
                text += $"Награда кампании: {TowerCatalog.ColoredTitle(result.campaignTower)}\n";
            text += $"\n{Chests.Victory.title}:\n{result.chest.Describe()}";
            Show("Уровень пройден", text);
        }
    }
}
