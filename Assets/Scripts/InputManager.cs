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
        public bool playAgainstAI = false;

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
            if (playAgainstAI && GameManager.Instance != null && GameManager.Instance.IsAITurn)
                return;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
                return;
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (!Physics.Raycast(ray, out var hit))
                return;

            if (!hit.collider.TryGetComponent<Cell>(out var cell))
                return;

            var coord = new Vector3Int(cell.X, cell.Y, cell.Z);
            bool isWarp = GridManager.Instance.BlackHoles.ContainsKey(coord);

            if (!isWarp && !IsCellOnFrontFace(cell))
                return;

            GameManager.Instance.HandleCellClick(cell.X, cell.Z);
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

        /// <summary>
        /// Call from main menu UI to choose AI opponent.
        /// </summary>
        public void SetPlayAgainstAI(bool useAI)
        {
            playAgainstAI = useAI;
        }

        /// <summary>
        /// Determines which face of the grid is active (face normal) relative to the camera.
        /// </summary>
        public Vector3Int GetActiveFaceNormal()
        {
            var gm = GridManager.Instance;
            Vector3 toCam = Camera.main.transform.position - gm.TimelineContainer.position;
            Vector3 local = Quaternion.Inverse(gm.TimelineContainer.rotation) * toCam;
            local.y = 0; 
            local.Normalize();
            if (Mathf.Abs(local.z) >= Mathf.Abs(local.x))
                return new Vector3Int(0, 0, local.z > 0 ? 1 : -1);
            else
                return new Vector3Int(local.x > 0 ? 1 : -1, 0, 0);
        }
    }
}