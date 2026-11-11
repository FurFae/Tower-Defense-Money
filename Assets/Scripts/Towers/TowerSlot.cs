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

        public Tower PlacedTower => placedTower;

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.IsPaused || GameManager.Instance.IsGameOver) return;
            Vector2? pointerScreen = ReadPointerPress();
            if (pointerScreen == null) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector2 mouseScreen = pointerScreen.Value;
            float distanceToPlane = Mathf.Abs(cam.transform.position.z - transform.position.z);
            Vector3 mouseWorld = cam.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, distanceToPlane));
            Collider2D hit = Physics2D.OverlapPoint(mouseWorld);
            if (hit == null || hit.gameObject != gameObject) return;
            // A built tower is upgraded with mana; an empty slot opens the build menu.
            if (IsOccupied) BattleUI.OpenTower(placedTower);
            else BuildMenuUI.Instance?.Open(this);
        }

        // Works with mouse and touch: the new Input System exposes both uniformly.
        private static Vector2? ReadPointerPress()
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                return Mouse.current.position.ReadValue();
            if (Touchscreen.current != null
                && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                return Touchscreen.current.primaryTouch.position.ReadValue();
            return null;
        }

        // Sell flow: the UI asks the tower for the refund, then frees the slot.
        public void ClearTower()
        {
            if (placedTower != null) Destroy(placedTower.gameObject);
            placedTower = null;
            IsOccupied = false;
            if (indicator != null) indicator.enabled = true;
        }

        public string BuildRestriction(TowerData data, GameObject prefab)
        {
            if (data == null || prefab == null || !prefab.TryGetComponent<Tower>(out _)) return "Нет башни";
            if (IsOccupied || !TryGetComponent<Collider2D>(out var c) || !c.enabled) return "Место занято";
            var definition = TowerCatalog.Find(data.name);
            if ((definition != null && !definition.playable) || !MetaProgress.IsUnlocked(data.name)) return "Не открыта";
            if (!MetaProgress.IsInDeck(data.name)) return "Вне колоды";
            if (Levels.ActiveChallenge == Challenge.RareOnly && definition != null && definition.rarity < Rarity.Rare)
                return "Только редкие";
            var game = GameManager.Instance;
            if (game == null || game.IsGameOver || game.IsPaused) return "Бой остановлен";
            return game.Currency < data.cost ? "Мало маны" : null;
        }

        public bool Build(TowerData data, GameObject towerPrefab)
        {
            if (BuildRestriction(data, towerPrefab) != null || !GameManager.Instance.TrySpend(data.cost)) return false;
            var center = indicator != null ? indicator.bounds.center : transform.position;
            GameObject go = Instantiate(towerPrefab, center, Quaternion.identity);
            go.transform.SetParent(transform, true);
            placedTower = go.GetComponent<Tower>();
            placedTower.Initialize(data);
            if (indicator != null) placedTower.AlignFootprint(indicator.bounds);
            IsOccupied = true;
            if (indicator != null) indicator.enabled = false;
            RetentionSystem.Report(RetentionStat.TowersBuilt);
            return true;
        }
    }
}
