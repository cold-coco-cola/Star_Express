using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 放置客舱模式：点击飞船升级容量。仅在 PlacingCarriage 状态下激活。
/// PRD §4.6、§6.4。
/// </summary>
public class CarriagePlacementInput : MonoBehaviour
{
    private void Update()
    {
        if (GameplayUIController.Instance == null) return;
        if (GameplayUIController.Instance.CurrentState != GameplayUIState.PlacingCarriage) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            GameplayUIController.Instance.TryTransition(GameplayUIState.Idle);
            UIManager.Hide<CarriagePlacementPanel>();
            return;
        }

        if (!Input.GetMouseButtonDown(0)) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        var ship = GetShipUnderMouse();
        if (ship == null) return;

        var lm = GameManager.Instance != null ? GameManager.Instance.GetLineManagerComponent() : null;
        if (lm == null) return;
        if (!lm.TryUseCarriage(ship)) return;

        ship.PlayUpgradeAnimation();
        ship.ApplyVisual();
        GameplayUIController.Instance.TryTransition(GameplayUIState.Idle);
        UIManager.Hide<CarriagePlacementPanel>();
    }

    private static ShipBehaviour GetShipUnderMouse()
    {
        var cam = Camera.main;
        if (cam == null) return null;
        Vector2 world2D = GetMouseWorld2D(cam);
        var hits = Physics2D.OverlapCircleAll(world2D, 0.8f);
        if (hits == null) return null;
        foreach (var c in hits)
        {
            if (c == null) continue;
            var ship = c.GetComponentInParent<ShipBehaviour>();
            if (ship == null) ship = c.GetComponent<ShipBehaviour>();
            if (ship != null) return ship;
        }
        return null;
    }

    private static Vector2 GetMouseWorld2D(Camera cam)
    {
        var ray = cam.ScreenPointToRay(Input.mousePosition);
        float z = 0f;
        if (Mathf.Abs(ray.direction.z) < 0.0001f) return new Vector2(ray.origin.x, ray.origin.y);
        float t = (z - ray.origin.z) / ray.direction.z;
        var p = ray.origin + ray.direction * t;
        return new Vector2(p.x, p.y);
    }
}
