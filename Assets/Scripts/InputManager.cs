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
                        if (IsCellOnFrontFace(cell))
                        {
                            GameManager.Instance.HandleCellClick(cell.X, cell.Z);
                        }
                        else
                        {
                            Debug.Log("Clicked cell is not on the front-facing plane. Ignored.");
                        }
                    }
                }
            }
        }
        bool IsCellOnFrontFace(Cell cell)
        {
            Transform container = GridManager.Instance.TimelineContainer;
            Vector3 toCamera = Camera.main.transform.position - container.position;

            Vector3 localCamDir = container.InverseTransformDirection(toCamera).normalized;

            Vector3Int faceNormal = Vector3Int.zero;
            if (Mathf.Abs(localCamDir.x) > Mathf.Abs(localCamDir.y) && Mathf.Abs(localCamDir.x) > Mathf.Abs(localCamDir.z))
                faceNormal.x = (localCamDir.x > 0) ? 1 : -1;
            else if (Mathf.Abs(localCamDir.y) > Mathf.Abs(localCamDir.z))
                faceNormal.y = (localCamDir.y > 0) ? 1 : -1;
            else
                faceNormal.z = (localCamDir.z > 0) ? 1 : -1;

            Vector3Int cellPos = new Vector3Int(cell.X, cell.Y, cell.Z);
            Vector3Int size = new Vector3Int(GridManager.Instance.sizeX, GridManager.Instance.sizeY, GridManager.Instance.sizeZ);

            if (faceNormal.x == -1 && cell.X == 0) return true;                      // Left face
            if (faceNormal.x == 1 && cell.X == size.x - 1) return true;               // Right face
            if (faceNormal.y == -1 && cell.Y == 0) return true;                      // Bottom face
            if (faceNormal.y == 1 && cell.Y == size.y - 1) return true;               // Top face
            if (faceNormal.z == -1 && cell.Z == 0) return true;                      // Back face
            if (faceNormal.z == 1 && cell.Z == size.z - 1) return true;               // Front face

            return false;
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