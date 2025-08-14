using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace QuantumConnect
{
    /// <summary>
    /// Routes mouse/touch clicks to Cells via physics raycast using the new Input System.
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        #region Constants
        const int NoPointerId = -1;
        #endregion

        #region Fields
        [Header("Raycast")]
        [SerializeField] float _maxRayDistance = 1000f;

        #endregion

        #region Unity
        void Update()
        {
            if (!TryGetPressPosition(out var screenPos, out var pointerId))
                return;

            var es = EventSystem.current;
            if (es != null && es.IsPointerOverGameObject(pointerId))
                return;

            var cam = Camera.main;
            if (cam == null) return;

            var ray = cam.ScreenPointToRay(screenPos);
            if (!Physics.Raycast(ray, out var hit, _maxRayDistance)) return;

            if (hit.collider != null && hit.collider.TryGetComponent(out Cell cell))
                cell.HandleClick();
        }
        #endregion

        #region Helpers
        /// <summary>
        /// Gets the current press position from mouse or primary touch (pointerId works with EventSystem).
        /// </summary>
        bool TryGetPressPosition(out Vector2 pos, out int pointerId)
        {
            pos = default;
            pointerId = NoPointerId;

            var mouse = Mouse.current;
            if (mouse?.leftButton.wasPressedThisFrame == true)
            {
                pos = mouse.position.ReadValue();
                pointerId = PointerInputModule.kMouseLeftId;
                return true;
            }

            var touch = Touchscreen.current;
            if (touch?.primaryTouch.press.wasPressedThisFrame == true)
            {
                pos = touch.primaryTouch.position.ReadValue();
                pointerId = touch.primaryTouch.touchId.ReadValue();
                return true;
            }

            return false;
        }
        #endregion
    }
}
