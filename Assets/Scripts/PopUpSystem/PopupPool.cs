using System.Collections.Generic;
using UnityEngine;

namespace PopupSystem
{
    /// <summary>
    /// Minimal object pool for a MonoBehaviour prefab. Used for toasts,
    /// which can appear rapidly and benefit from reuse. Modals are
    /// typically single-instance and don't need pooling.
    /// </summary>
    /// <typeparam name="T">Component on the pooled prefab.</typeparam>
    public class PopupPool<T> where T : Component
    {
        private readonly T prefab;
        private readonly Transform parent;
        private readonly Queue<T> available = new();

        public PopupPool(T prefab, Transform parent, int prewarm = 0)
        {
            this.prefab = prefab;
            this.parent = parent;
            for (int i = 0; i < prewarm; i++)
                available.Enqueue(CreateInstance());
        }

        private T CreateInstance()
        {
            T instance = Object.Instantiate(prefab, parent);
            instance.gameObject.SetActive(false);
            return instance;
        }

        public T Get()
        {
            T instance = available.Count > 0 ? available.Dequeue() : CreateInstance();
            instance.gameObject.SetActive(true);
            return instance;
        }

        public void Return(T instance)
        {
            instance.gameObject.SetActive(false);
            available.Enqueue(instance);
        }
    }
}
