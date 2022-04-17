using System;
using UnityEngine;
using eilume.ObjectPool;

namespace eilume.ObjectPool.Demos.Shared
{
    public abstract class APooledVersion : MonoBehaviour
    {
        [HideInInspector]
        public GameObjectPool.FillMethod FillMethod
        {
            get => _fillMethod;
            set
            {
                _fillMethod = value;
                OnFillMethodChange?.Invoke();
            }
        }

        protected GameObjectPool.FillMethod _fillMethod = GameObjectPool.FillMethod.Sync;

        public Action OnFillMethodChange;
    }
}