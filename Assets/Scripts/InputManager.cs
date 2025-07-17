using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace QuantumConnect
{
    /// <summary>
    /// Routes mouse clicks into the scene to select Cells via physics raycast, using the new Input System.
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this) Destroy(gameObject);
            else Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }

        void Update()
        {
            if (EventSystem.current.IsPointerOverGameObject())
                return;

            HandleLeftClick();
        }

        void HandleLeftClick()
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, 100f))
                {
                    if (hit.collider.TryGetComponent<Cell>(out var cell))
                    {
                        GameManager.Instance.HandleCellClick(cell.X, cell.Z);
                        return;
                    }
                }
            }
        }

        public void StartGame()
        {
            SceneManager.LoadScene("QuantumConnect");
        }

        public void RestartGame()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.ResetGame();
        }
    }
}