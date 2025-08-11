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
        public static InputManager Instance { get; private set; }

        [Header("Mode (for boot/menu)")]
        [SerializeField] public GameMode selectedMode = GameMode.PvAI; 

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

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
            if (!Physics.Raycast(ray, out var hit)) return;

            if (hit.collider != null && hit.collider.TryGetComponent(out Cell cell))
            {
                cell.HandleClick();
            }
        }

        #region Private Helpers
        bool TryGetPressPosition(out Vector2 pos, out int pointerId)
        {
            pos = default;
            pointerId = -1;

            // Mouse
            var mouse = Mouse.current;
            if (mouse?.leftButton.wasPressedThisFrame == true)
            {
                pos = mouse.position.ReadValue();
                pointerId = PointerInputModule.kMouseLeftId; 
                return true;
            }

            // Touch (primary)
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

        /// <summary>
        /// Returns the face normal (in container-local axes) that is closest to the camera view.
        /// Useful if you gate interactions to the currently visible/front face.
        /// </summary>
        public Vector3Int GetActiveFaceNormal()
        {
            var gm = GridManager.Instance;
            var container = gm != null ? gm.TimelineContainer : null;
            var cam = Camera.main;
            if (container == null || cam == null) return Vector3Int.forward;

            Vector3 toCam = cam.transform.position - container.position;

            // convert to container local, flatten Y, normalize
            Vector3 local = Quaternion.Inverse(container.rotation) * toCam;
            local.y = 0f;
            if (local.sqrMagnitude > 0f) local.Normalize();

            // choose the dominant axis (x or z)
            if (Mathf.Abs(local.z) >= Mathf.Abs(local.x))
                return new Vector3Int(0, 0, local.z >= 0f ? 1 : -1);
            else
                return new Vector3Int(local.x >= 0f ? 1 : -1, 0, 0);
        }
    }
}
