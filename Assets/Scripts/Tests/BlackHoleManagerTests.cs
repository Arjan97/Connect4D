using NUnit.Framework;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace QuantumConnect.Tests
{
    public class BlackHoleManagerTests
    {
        GameObject _root;
        GameObject _cellPrefab;
        GameObject _bhPrefab;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("~BHTestRoot");

            _cellPrefab = new GameObject("CellPrefab");
            _cellPrefab.AddComponent<Cell>();

            _bhPrefab = new GameObject("BH_Prefab");
            _bhPrefab.AddComponent<Cell>(); 
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_bhPrefab);
            Object.DestroyImmediate(_cellPrefab);
            Object.DestroyImmediate(_root);
        }

        [Test]
        public void InitializeBlackHoles_SpawnsAnd_RemoveRestoresCell()
        {
            var cubeGO = new GameObject("CubeManager");
            cubeGO.transform.SetParent(_root.transform);
            var cm = cubeGO.AddComponent<CubeManager>();
            cm.sizeX = 3; cm.sizeY = 3; cm.sizeZ = 3;

#if UNITY_EDITOR
            var cmSO = new SerializedObject(cm);
            cmSO.FindProperty("_cellPrefab").objectReferenceValue = _cellPrefab;
            cmSO.ApplyModifiedPropertiesWithoutUndo();
#endif
            cm.Initialize(null, respawn: true);

            var bhGO = new GameObject("BlackHoleManager");
            bhGO.transform.SetParent(_root.transform);
            var bhm = bhGO.AddComponent<BlackHoleManager>();

            var tuning = ScriptableObject.CreateInstance<GameTuning>();
            tuning.minInitialBlackHoles = 1;
            tuning.maxInitialBlackHoles = 1;

#if UNITY_EDITOR
            var bhSO = new SerializedObject(bhm);
            bhSO.FindProperty("_blackHolePrefab").objectReferenceValue = _bhPrefab;
            bhSO.ApplyModifiedPropertiesWithoutUndo();
#endif

            bhm.Initialize(null, cm, null, tuning);
            var board = new BoardModel(cm.sizeX, cm.sizeY, cm.sizeZ);
            bhm.InitializeBlackHoles(board);

            Assert.GreaterOrEqual(bhm.BlackHoles.Count, 1, "Should spawn at least one black hole.");

            var enumerator = bhm.BlackHoles.GetEnumerator();
            Assert.IsTrue(enumerator.MoveNext());
            var src = enumerator.Current.Key;

            Assert.IsNotNull(cm.Cells[src.x, src.y, src.z], "BH replaces cell with a GO that still has Cell.");

            bhm.RemoveBlackHole(src);

            Assert.IsFalse(bhm.BlackHoles.ContainsKey(src), "Black hole entry should be removed.");
            Assert.IsNotNull(cm.Cells[src.x, src.y, src.z], "A normal Cell should be restored at the source.");
        }
    }
}
