using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace TowerDefense
{
    // Fixed build spot. Click it to open the build menu and place a tower.
    // Uses the new Input System directly (this project's Active Input Handling
    // is set to "Input System Package (New)" only, so legacy OnMouseDown /
    // UnityEngine.Input would throw at runtime).
    [RequireComponent(typeof(Collider2D))]
    public class TowerSlot : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer indicator;

        public bool IsOccupied { get; private set; }
        private Tower placedTower;

        private void Update()
        {
            if (IsOccupied) return;
            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector2 mouseScreen = Mouse.current.position.ReadValue();
            float distanceToPlane = Mathf.Abs(cam.transform.position.z - transform.position.z);
            Vector3 mouseWorld = cam.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, distanceToPlane));
            Collider2D hit = Physics2D.OverlapPoint(mouseWorld);
            if (hit != null && hit.gameObject == gameObject)
                BuildMenuUI.Instance?.Open(this);
        }

        public bool Build(TowerData data, GameObject towerPrefab)
        {
            if (IsOccupied || data == null || towerPrefab == null) return false;
            if (GameManager.Instance == null || !GameManager.Instance.TrySpend(data.cost)) return false;

            GameObject go = Instantiate(towerPrefab, transform.position, Quaternion.identity, transform);
            placedTower = go.GetComponent<Tower>();
            placedTower.Initialize(data);
            IsOccupied = true;
            if (indicator != null) indicator.enabled = false;
            return true;
        }
    }
}
