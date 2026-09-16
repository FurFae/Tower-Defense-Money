using UnityEngine;

namespace TowerDefense
{
    public class BuildMenuUI : MonoBehaviour
    {
        public static BuildMenuUI Instance { get; private set; }

        [SerializeField] private RectTransform panel;
        [SerializeField] private Camera worldCamera;

        private TowerSlot currentSlot;

        private void Awake()
        {
            Instance = this;
            if (worldCamera == null) worldCamera = Camera.main;
            if (panel != null) panel.gameObject.SetActive(false);
        }

        public void Open(TowerSlot slot)
        {
            currentSlot = slot;
            if (panel == null) return;

            Vector3 screenPos = worldCamera.WorldToScreenPoint(slot.transform.position);
            panel.position = screenPos;
            panel.gameObject.SetActive(true);
        }

        public void Close()
        {
            currentSlot = null;
            if (panel != null) panel.gameObject.SetActive(false);
        }

        public void BuildSelected(TowerData data, GameObject prefab)
        {
            if (currentSlot == null) return;
            currentSlot.Build(data, prefab);
            Close();
        }
    }
}
