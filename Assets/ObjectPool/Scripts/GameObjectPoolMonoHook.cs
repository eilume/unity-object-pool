using System.Runtime.CompilerServices;
using UnityEngine;

[assembly: InternalsVisibleTo("eilume.ObjectPool")]

namespace eilume.ObjectPool
{
    public class GameObjectPoolMonoHook : MonoBehaviour
    {
        [HideInInspector]
        public GameObjectPool pool;

        [field: SerializeField]
        [field: ReadOnly]
        public GameObjectPool.Node Node { get; private set; }

        private void OnDisable()
        {
            if (pool.monoDisablesNode)
            {
                Free();
            }
        }

        internal void Setup(GameObjectPool.Node node)
        {
            Node = node;
        }

        public void Free() => pool.Free(Node);
    }
}