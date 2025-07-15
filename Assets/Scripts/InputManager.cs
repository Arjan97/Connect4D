using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.EventSystems;

namespace QuantumConnect
{
    /// <summary>
    /// Routes mouse clicks into the scene to select Cells via physics raycast, using the new Input System.
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }
        [Header("Click Settings")]
        [Tooltip("Which layers are clickable for cells.")]
        public LayerMask clickableLayer;

        [Header("Rotation Settings")]
        [Tooltip("How sensitive drag is (degrees per pixel).")]
        public float dragSensitivity = 0.2f;
        [Tooltip("Angle increment to snap to (usually 90).")]
        public float snapAngle = 90f;
        [Tooltip("Time (in seconds) it takes to finish the snap animation.")]
        public float snapDuration = 0.2f;

        bool _isDragging = false;
        Vector2 _lastMousePos;
        Coroutine _snapCoroutine;

        void Awake()
        {
            if (Instance != null && Instance != this) Destroy(gameObject);
            else Instance = this;
        }

        void Update()
        {
            if (EventSystem.current.IsPointerOverGameObject())
                return;

            HandleLeftClick();
            //HandleRightDrag();
        }
        void HandleLeftClick()
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
                if (Physics.Raycast(ray, out var hit, 100f, clickableLayer))
                {
                    if (hit.collider.TryGetComponent<Cell>(out var cell))
                        GameManager.Instance.HandleCellClick(cell.X, cell.Z);
                }
            }
        }
        void HandleRightDrag()
        {
            var container = GridManager.Instance.TimelineContainer;
            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                _isDragging = true;
                _lastMousePos = Mouse.current.position.ReadValue();

                if (_snapCoroutine != null) StopCoroutine(_snapCoroutine);
            }

            if (_isDragging && Mouse.current.rightButton.isPressed)
            {
                var current = Mouse.current.position.ReadValue();
                var delta = current - _lastMousePos;
                _lastMousePos = current;

                container.Rotate(Vector3.up, -delta.x * dragSensitivity, Space.World);
                container.Rotate(Vector3.right, delta.y * dragSensitivity, Space.World);
            }

            if (_isDragging && Mouse.current.rightButton.wasReleasedThisFrame)
            {
                _isDragging = false;
                _snapCoroutine = StartCoroutine(SnapToNearestAxis(container));
            }
        }

        IEnumerator SnapToNearestAxis(Transform target)
        {
            Quaternion startRot = target.rotation;

            Vector3 e = target.eulerAngles;
            float snappedX = Mathf.Round(e.x / snapAngle) * snapAngle;
            float snappedY = Mathf.Round(e.y / snapAngle) * snapAngle;
            float snappedZ = Mathf.Round(e.z / snapAngle) * snapAngle;
            Quaternion endRot = Quaternion.Euler(snappedX, snappedY, snappedZ);

            float elapsed = 0f;
            while (elapsed < snapDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / snapDuration);
                target.rotation = Quaternion.Slerp(startRot, endRot, t);
                yield return null;
            }
            target.rotation = endRot;
            _snapCoroutine = null;
        }
    }
}