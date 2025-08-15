using System.Collections.Generic;
using UnityEngine;

namespace QuantumConnect
{
    /// <summary>Generic pool for Components created on empty GameObjects.</summary>
    public class ComponentPool<T> : BasePool where T : Component
    {
        #region State
        readonly Queue<T> free = new();
        readonly HashSet<T> active = new();
        Transform root;
        #endregion

        #region Unity
        void Awake()
        {
            root = EnsureRoot(typeof(T).Name);
        }
        #endregion

        #region API
        public T Get()
        {
            T inst;
            if (free.Count > 0)
            {
                inst = free.Dequeue();
                if (!inst) inst = NewInstance();
            }
            else inst = NewInstance();

            if (!inst) return null;
            inst.gameObject.SetActive(true);
            active.Add(inst);
            return inst;
        }

        public void Return(T inst)
        {
            if (!inst) return;
            inst.gameObject.SetActive(false);
            inst.transform.SetParent(root, false);
            active.Remove(inst);
            free.Enqueue(inst);
        }

        public void ReturnAll()
        {
            var list = new List<T>(active);
            for (int i = 0; i < list.Count; i++) Return(list[i]);
        }
        #endregion

        #region Internals
        T NewInstance()
        {
            var go = new GameObject($"{typeof(T).Name}");
            go.transform.SetParent(root, false);
            var comp = go.AddComponent<T>();
            PrepareOnCreate(comp);
            return comp;
        }

        /// <summary>Override to init new instances with defaults.</summary>
        protected virtual void PrepareOnCreate(T inst) { }
        #endregion
    }
}
