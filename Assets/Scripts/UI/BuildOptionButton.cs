using UnityEngine;
using UnityEngine.UI;

namespace TowerDefense
{
    [RequireComponent(typeof(Button))]
    public class BuildOptionButton : MonoBehaviour
    {
        [SerializeField] private TowerData towerData;
        [SerializeField] private GameObject towerPrefab;
        [SerializeField] private Text label;
        [SerializeField] private Image icon;

        private void Start()
        {
            if (towerData == null) return;
            // Towers the player has not collected yet cannot be built.
            if (!MetaProgress.IsUnlocked(towerData.name))
            {
                gameObject.SetActive(false);
                return;
            }
            if (label != null) label.text = $"{towerData.displayName}\n{towerData.cost} маны";
            if (icon != null) icon.sprite = towerData.baseSprite;
            GetComponent<Button>().onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            if (BuildMenuUI.Instance != null)
                BuildMenuUI.Instance.BuildSelected(towerData, towerPrefab);
        }
    }
}
