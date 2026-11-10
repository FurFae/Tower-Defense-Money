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

        // Read by BuildMenuUI, which rebuilds the panel as an icon grid in code.
        public TowerData TowerData => towerData;
        public GameObject TowerPrefab => towerPrefab;

        private void Start()
        {
            if (towerData == null) return;
            // Only towers the player took into battle can be built.
            if (!MetaProgress.IsInDeck(towerData.name))
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
