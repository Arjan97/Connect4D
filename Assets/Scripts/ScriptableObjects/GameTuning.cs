using UnityEngine;

namespace QuantumConnect
{
    [CreateAssetMenu(menuName = "QuantumConnect/Config/Game Tuning", fileName = "GameTuning")]
    public class GameTuning : ScriptableObject
    {
        [Header("Grid")]
        public int sizeX = 4;
        public int sizeY = 4;
        public int sizeZ = 4;
        public Vector3 startPosition = Vector3.zero;
        public Vector3 cellSpacing = new Vector3(1.8f, 1.8f, 1.8f);

        [Header("Cube Rotation")]
        public float rotationDuration = 0.2f;
        public float rotationPause = 0.2f;

        [Header("Drop")]
        public float dropHeight = 1.8f;
        public float dropSpeed = 8f;

        [Header("Win FX")]
        public float blinkInterval = 0.5f;
        public Color winHighlight;

        [Header("Black Holes")]
        [Range(0f, 1f)] public float blackHoleSpawnChance = 0.1f;
        public int minInitialBlackHoles = 2;
        public int maxInitialBlackHoles = 6;
        public float blackHoleWarpInDuration = 0.5f;
        public int minEmptyCellsForSpawn = 10;
        public int maxConcurrentBlackHoles = 6;

        [Header("AI")]
        [Range(0.1f, 5f)] public float aiMoveDelay = 0.7f;
        [Range(1, 8)] public int aiMaxDepth = 4;
        [Range(0f, 1f)] public float aiRotationChance = 0.3f;

        [Header("Input")]
        public float maxRayDistance = 1000f;
    }
}
