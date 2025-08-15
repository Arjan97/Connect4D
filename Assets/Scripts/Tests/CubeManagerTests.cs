using NUnit.Framework;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace QuantumConnect.Tests
{
    public class CubeManagerTests
    {
        GameObject _tmpRoot;
        GameObject _cellPrefab;

        [SetUp]
        public void SetUp()
        {
            _tmpRoot = new GameObject("~CubeTestRoot");
            _cellPrefab = new GameObject("CellPrefab");
            _cellPrefab.AddComponent<Cell>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_cellPrefab);
            Object.DestroyImmediate(_tmpRoot);
        }

        [Test]
        public void SpawnGrid_GeneratesOuterCells_Only()
        {
            // Arrange
            var go = new GameObject("CubeManager");
            go.transform.SetParent(_tmpRoot.transform);
            var cm = go.AddComponent<CubeManager>();

            // Configure a small 3x3x3 grid to keep assertions simple.
            cm.sizeX = 3; cm.sizeY = 3; cm.sizeZ = 3;

#if UNITY_EDITOR
            // Set private [SerializeField] _cellPrefab
            var so = new SerializedObject(cm);
            so.FindProperty("_cellPrefab").objectReferenceValue = _cellPrefab;
            so.ApplyModifiedPropertiesWithoutUndo();
#endif

            // Act
            cm.Initialize(null, respawn: true);

            // Assert
            Assert.IsNotNull(cm.CubeContainer, "CubeContainer should exist after Initialize().");
            Assert.IsNotNull(cm.Cells, "Cells array should be allocated.");

            int nonNull = 0;
            int xLen = cm.sizeX, yLen = cm.sizeY, zLen = cm.sizeZ;
            for (int x = 0; x < xLen; x++)
                for (int y = 0; y < yLen; y++)
                    for (int z = 0; z < zLen; z++)
                        if (cm.Cells[x, y, z] != null) nonNull++;

            int n = 3; // side length
            int expectedOuterUnique = 6 * (n * n) - 12 * n + 8; 
            Assert.AreEqual(expectedOuterUnique, nonNull, "Should spawn only the outer shell.");

            // Spot-check: center should be empty; a corner should exist.
            Assert.IsNull(cm.Cells[1, 1, 1], "Center (1,1,1) must be empty for 3x3x3 shell.");
            Assert.IsNotNull(cm.Cells[0, 0, 0], "Corner (0,0,0) should exist.");

            Object.DestroyImmediate(go);
        }
    }
}
