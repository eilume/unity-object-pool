using System;
using System.Collections;
using UnityEngine;

namespace eilume.ObjectPool
{
    // TODO: rename some functions/variables to make more sense

    // TODO: add generic version for including additional data with gameobjects (eg. demo bullet hell bullet data)
    public class GameObjectPool : ObjectPool<GameObject>
    {
        public GameObject ObjectToPool
        {
            get => _objectToPool;
            set
            {
                if (_objectToPool != value)
                {
                    // New object to pool

                    // TODO:
                    _objectToPool = value;
                }
            }
        }

        [SerializeField]
        protected GameObject _objectToPool;

        [field: SerializeField]
        [field: ReadOnly]
        public GameObject CurrentlyPooledObject { get; protected set; }

        // If left blank, this component's transform will be used
        public Transform objectsParentTransform;

        public GameObjectPoolMonoHook[] MonoHooks
        {
            get => _monoHooks;
            protected set => _monoHooks = value;
        }

        private GameObjectPoolMonoHook[] _monoHooks;

        public bool objectInitialActiveState = false;

        public bool nodeEnablesGameObject = true;
        public bool nodeDisablesGameObject = true;

        public bool useMonoHook = true;
        // Require `useMonoHook` to be true
        public bool monoDisablesNode = true;

        // Allows for custom processing to be applied to the pooled object
        public Action OnObjectToPoolSetup;

        private bool objectToPoolChanged;

        private string indexPaddingFormatString;

        // protected override string GetDataTypeKey() => objectToPool.name;

        private static Transform runtimeInstanceTransform = null;

        protected override void Awake()
        {
            if (objectsParentTransform == null)
            {
                objectsParentTransform = transform;
            }

            if (runtimeInstanceTransform == null)
            {
                GameObject runtimeInstanceGameObject = new GameObject();
                runtimeInstanceGameObject.name = "GameObject Pool Runtime Instances";
                runtimeInstanceGameObject.SetActive(false);
                DontDestroyOnLoad(runtimeInstanceGameObject);
                runtimeInstanceTransform = runtimeInstanceGameObject.transform;
            }

            base.Awake();
        }

        private void OnDestroy()
        {
            Destroy(CurrentlyPooledObject);
        }

        protected override void SetupInternal(int size)
        {
            base.SetupInternal(size);

            MonoHooks = new GameObjectPoolMonoHook[size];
        }

        protected override void ResizeInternalArrays(int size)
        {
            base.ResizeInternalArrays(size);

            Array.Resize(ref _monoHooks, size);
        }

        protected override bool FillSetup()
        {
            if (_objectToPool == null) return false;

#if UNITY_EDITOR
            indexPaddingFormatString = "D" + PoolCapacity.ToString().Length;

#endif
            CurrentlyPooledObject = Instantiate(_objectToPool, runtimeInstanceTransform);
            CurrentlyPooledObject.AddComponent<GameObjectPoolMonoHook>().pool = this;

            // Override active state
            CurrentlyPooledObject.SetActive(objectInitialActiveState);

            // TODO: add customizable callback for the user to configure the gameobject before being instantiated
            OnObjectToPoolSetup?.Invoke();

            return true;
        }

        protected override GameObject GenerateData(int id)
        {
            GameObject generatedObject = Instantiate(CurrentlyPooledObject, objectsParentTransform);

#if UNITY_EDITOR
            generatedObject.name += id.ToString(indexPaddingFormatString);

#endif
            MonoHooks[id] = generatedObject.GetComponent<GameObjectPoolMonoHook>();

            return generatedObject;
        }

        protected override void FillNodeFinalize(Node node)
        {
            MonoHooks[node.Id].Setup(node);
        }

        protected override void SetNodeActiveInternal(Node node)
        {
            if (nodeEnablesGameObject && !node.data.activeSelf)
            {
                node.data.SetActive(true);
            }

            base.SetNodeActiveInternal(node);
        }

        protected override void SetNodeInactiveInternal(Node node)
        {
            if (nodeDisablesGameObject && node.data.activeSelf)
            {
                node.data.SetActive(false);
            }

            base.SetNodeInactiveInternal(node);
        }

        protected override void DisposeData(Node node)
        {
            Destroy(node.data);
        }
    }
}