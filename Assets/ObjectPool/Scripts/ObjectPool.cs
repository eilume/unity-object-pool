using System;
using System.Diagnostics;
using System.Collections;
using UnityEngine;

namespace eilume.ObjectPool
{
    // TODO: add option to set fill node's state (allow active)
    // TODO: add no-gc mode, only needed for `FreeScheduled`

    public class ObjectPool<T> : MonoBehaviour where T : new()
    {
        // Enums for settings in the object pool
        public enum FillTrigger
        {
            Manual,
            OnAwake,
            OnStart
        }

        public enum FillMethod
        {
            Sync,
            Async
        }

        public enum FillAsyncTarget
        {
            PerSecond,
            PerFrame
        }

        public enum FillAsyncTiming
        {
            EveryFrame = 0,
            EveryUpdate = 0,
            EveryFixedUpdate,
            Manual
        }

        // TODO: maybe redesign this enum for more efficient checking state
        //       eg. last bit determines state, other bits are type flags
        public enum NodeActiveState
        {
            Inactive,
            Active,
            FirstInactive,
            FirstActive,
            InitialInactive,
            InitialActive
        }

        // Internal node data structure for objects in the pool
        [Serializable]
        public class Node
        {
            public int Id { get; internal set; }
            internal int currActiveStateCount;
            public T data;
            public NodeActiveState Active { get; internal set; }

            internal Node(int id, int currActiveStateCount, T data, bool activeState)
            {
                Id = id;
                this.currActiveStateCount = currActiveStateCount;
                this.data = data;
                Active = activeState ? NodeActiveState.InitialActive : NodeActiveState.InitialInactive;
            }

            public bool IsActive() => Active == NodeActiveState.Active || Active == NodeActiveState.FirstActive || Active == NodeActiveState.InitialActive;
            public bool IsInactive() => Active == NodeActiveState.Inactive || Active == NodeActiveState.FirstInactive || Active == NodeActiveState.InitialInactive;
        }

        // Useful for when you have multiple object pools present attached
        // to a single gameobject, this can help identify by setting an id
        // you can compare to or just to visual label in the inspector
        public string id = string.Empty;

        // Internal data structures
        // These properties values have to be seperated as `Array.Resize()`
        // requires a direct reference to a variable. Properties aren't
        // considered variables. Read first section here for info:
        // https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/using-properties 
        public Node[] PooledNodes
        {
            get => _pooledNodes;
            protected set => _pooledNodes = value;
        }

        public Node[] ActiveNodes
        {
            get => _activeNodes;
            protected set => _activeNodes = value;
        }

        public Node[] InactiveNodes
        {
            get => _inactiveNodes;
            protected set => _inactiveNodes = value;
        }

        protected Node[] _pooledNodes;
        protected Node[] _activeNodes;
        protected Node[] _inactiveNodes;

        public FillTrigger fillTrigger = FillTrigger.Manual;
        public FillMethod fillMethod = FillMethod.Sync;

        public bool fillActiveState;

        public FillAsyncTiming fillAsyncTiming
        {
            get => _asyncTiming;
            set
            {
                if (value == FillAsyncTiming.Manual)
                {
                    StopFillAsync();
                }

                _asyncTiming = value;
            }
        }

        [SerializeField]
        protected FillAsyncTiming _asyncTiming = FillAsyncTiming.EveryFrame;
        public FillAsyncTarget asyncTarget = FillAsyncTarget.PerSecond;

        public int TargetFill
        {
            get => _targetFill;
            set
            {
                if (value < 1) value = 1;

                _targetFill = value;
            }
        }

        public Optional<int> FirstFrameFill
        {
            get => _firstFrameFill;
            set
            {
                if (value.value < 1) value.value = 1;

                _firstFrameFill = value;
            }
        }

        public Optional<int> MinFill
        {
            get => _minFill;
            set
            {
                if (value.value < 0) value.value = 0;

                _minFill = value;
            }
        }

        [SerializeField]
        [Min(1)]
        protected int _targetFill = 1;

        [SerializeField]
        [OptionalMin(1)]
        protected Optional<int> _firstFrameFill = new Optional<int>(1, false);

        [SerializeField]
        [OptionalMin(0)]
        protected Optional<int> _minFill = new Optional<int>(1, false);

        // Read-only value to inspect the current fill rate 
        [field: SerializeField]
        [field: ReadOnly]
        public int CurrentFill { get; protected set; }

        public Optional<int> TargetFrameRate
        {
            get => _targetFrameRate;
            set
            {
                if (value.value < 1) value.value = 1;

                _targetFrameRate = value;
            }
        }

        [field: SerializeField]
        [field: OptionalMin(1)]
        protected Optional<int> _targetFrameRate = new Optional<int>(60, false);

        protected const int DEFAULT_FRAME_RATE = 60;

        // Editor only!
        [SerializeField]
        private int initialPoolSize = 1;

        // Whilst this flag is true, nodes that are re-enabled will have
        // their data reinstantiated. This reduces the benefits of using
        // an object pool but it's still implimented here for ease of use
        // and for more rapid prototyping.
        public bool recreateData;

        // Whilst this flag is true, nodes in inherited classes that define
        // custom process/disposal of data will be triggered  
        public bool disposeData;

        [field: SerializeField]
        [field: ReadOnly]
        public int PoolCapacity { get; protected set; }

        [field: SerializeField]
        [field: ReadOnly]
        public int PooledCount { get; protected set; }
        [field: SerializeField]
        [field: ReadOnly]
        public int ActiveCount { get; protected set; }
        [field: SerializeField]
        [field: ReadOnly]
        public int InactiveCount { get; protected set; }

        public bool AllActive { get => ActiveCount == PooledCount; }
        public bool AllInactive { get => InactiveCount == PooledCount; }

        [field: SerializeField]
        [field: ReadOnly]
        public bool IsSetup { get; protected set; } = false;

        [field: SerializeField]
        [field: ReadOnly]
        public bool IsEmpty { get; protected set; } = true;
        [field: SerializeField]
        [field: ReadOnly]
        public bool IsFull { get; protected set; }

        [field: SerializeField]
        [field: ReadOnly]
        public bool IsAsyncCurrentlyFilling { get; protected set; }

        [field: SerializeField]
        [field: ReadOnly]
        public double FillTimeTotal { get; protected set; }

        [field: SerializeField]
        [field: ReadOnly]
        public int AsyncFillTicks { get; protected set; }
        [field: SerializeField]
        [field: ReadOnly]
        public double AsyncFillTimePerTick { get; protected set; }

        public event Action<Node> OnNodeFill;
        public event Action<Node> OnNodeActive;
        public event Action<Node> OnNodeInactive;

        public event Action OnAllActive;
        public event Action OnAllInactive;

        // Triggered when starting filling
        public event Action OnFillStart;
        // Triggered when completed filling (mostly useful for async filling)
        public event Action OnFillComplete;

        private Stopwatch stopwatch = new Stopwatch();

        protected virtual void Awake()
        {
            if (fillTrigger == FillTrigger.OnAwake)
            {
                Setup(initialPoolSize);
                Fill();
            }
        }

        private void Start()
        {
            if (fillTrigger == FillTrigger.OnStart)
            {
                Setup(initialPoolSize);
                Fill();
            }
        }

        private void OnValidate()
        {
            // TODO: whenever inspector size is updated, do something idk
        }

        // TODO: maybe add option to redirect to resize since it takes the same
        //       args and essentially has the same outcome in a way?
        public ObjectPool<T> Setup(int size)
        {
            if (size < 1)
            {
                UnityEngine.Debug.LogError("Can't setup pool with size less than 1!");
            }
            else
            {
                SetupInternal(size);

                PoolCapacity = size;
                IsSetup = true;
            }

            return this;
        }

        protected virtual void SetupInternal(int size)
        {
            _pooledNodes = new Node[size];

            _activeNodes = new Node[size];
            _inactiveNodes = new Node[size];
        }

        public ObjectPool<T> Clear()
        {
            if (IsSetup)
            {
                StopFillAsync();

                PooledCount = 0;
                ActiveCount = 0;
                InactiveCount = 0;

                IsEmpty = true;
                IsFull = false;
            }

            return this;
        }

        public void StopFillAsync()
        {
            if (IsAsyncCurrentlyFilling)
            {
                // Ensure `FillAsync` isn't running to prevent unexpected behavior
                StopCoroutine(FillAsync());
                IsAsyncCurrentlyFilling = false;
            }
        }

        public ObjectPool<T> Resize(int size)
        {
            if (size < 1) size = 1;

            if (size == PoolCapacity) return this;

            StopFillAsync();

            if (size == PooledCount)
            {
                // Pool is being resized equal to the pooled amount
                IsFull = true;
            }
            else if (size < PooledCount)
            {
                // Pool is being resized smaller than the pooled amount
                DisposeRange(size, PooledCount - 1);

                RefillInternalArrays(size);

                IsFull = true;
            }
            else
            {
                // Pool is being resized larger than the pooled amount
                IsFull = false;
            }

            ResizeInternalArrays(size);

            PoolCapacity = size;

            return this;
        }

        public ObjectPool<T> Shrink() => Resize(PooledCount);

        protected virtual void ResizeInternalArrays(int size)
        {
            Array.Resize(ref _pooledNodes, size);
            Array.Resize(ref _activeNodes, size);
            Array.Resize(ref _inactiveNodes, size);
        }

        protected virtual void RefillInternalArrays(int size)
        {
            // Check for nodes that will be trimmed as a result of the pool
            // resize and re-arrange the arrays as necessary
            for (int i = 0; i < ActiveCount; i++)
            {
                if (_activeNodes[i].Id >= size)
                {
                    _activeNodes[i] = _activeNodes[ActiveCount - 1];
                    _activeNodes[i].currActiveStateCount = i;
                    ActiveCount--;
                }
            }

            for (int i = 0; i < InactiveCount; i++)
            {
                if (_inactiveNodes[i].Id >= size)
                {
                    _inactiveNodes[i] = _inactiveNodes[InactiveCount - 1];
                    _inactiveNodes[i].currActiveStateCount = i;
                    InactiveCount--;
                }
            }
        }

        public ObjectPool<T> Fill()
        {
            if (!IsSetup)
            {
                UnityEngine.Debug.LogError("You need to setup the pool first!");
            }
            else if (!IsFull)
            {
                if (!FillSetup()) return this;

                IsFull = false;
                IsEmpty = false;

                if (fillMethod == FillMethod.Sync) FillSync();
                else if (fillAsyncTiming != FillAsyncTiming.Manual) StartCoroutine(FillAsync());
            }

            return this;
        }

        protected virtual bool FillSetup() => true;

        protected void FillSync()
        {
            stopwatch.Start();

            for (int i = PooledCount; i < PoolCapacity; i++)
            {
                FillNode(i);
            }

            stopwatch.Stop();

            FillTimeTotal = stopwatch.Elapsed.TotalSeconds;

            IsFull = true;
        }

        protected IEnumerator FillAsync()
        {
            IsAsyncCurrentlyFilling = true;

            AsyncFillTicks = 0;
            FillTimeTotal = 0;

            int fillTarget = PoolCapacity - PooledCount;
            int localFilledCount = 0;
            int currentFrame = 0;
            CurrentFill = _targetFill;

            if (!TargetFrameRate.enabled) UpdateTargetFrameRate();

            int objectsPerTick = GetObjectsPerTick(CurrentFill);

            while (localFilledCount < fillTarget)
            {
                currentFrame++;

                int objectsThisFrame;
                if (_firstFrameFill.enabled && currentFrame == 1)
                {
                    objectsThisFrame = _firstFrameFill.value;
                }
                else
                {
                    objectsThisFrame = objectsPerTick;
                }

                // Clamp objects this tick to not overfill
                if (localFilledCount + objectsThisFrame > fillTarget)
                {
                    objectsThisFrame = fillTarget - localFilledCount;
                }

                stopwatch.Start();

                for (int i = 0; i < objectsThisFrame; i++)
                {
                    FillNode(PooledCount);
                }

                stopwatch.Stop();

                // Skip first frame if since it
                if (!FirstFrameFill.enabled || currentFrame != 1)
                {
                    AsyncFillTicks++;
                    FillTimeTotal += stopwatch.Elapsed.TotalSeconds;
                    AsyncFillTimePerTick = FillTimeTotal / AsyncFillTicks;
                }

                localFilledCount += objectsThisFrame;

                if (localFilledCount == fillTarget) break;

                yield return GetAsyncYield();

                if (_minFill.enabled && AsyncFillTicks > 0)
                {
                    if (!TargetFrameRate.enabled) UpdateTargetFrameRate();

                    // Check if we can reduce the fill amount if we're not hitting
                    // the target frametime
                    if (_minFill.value < CurrentFill && Time.deltaTime != 0 && _targetFrameRate.value > 1f / Time.deltaTime)
                    {
                        // Generate multiplier to scale down `CurrentFill` down
                        // so that the fill impact is reduce to reach the target
                        // frame time
                        float multiplier = (float)((AsyncFillTimePerTick - (Time.deltaTime - (1f / _targetFrameRate.value))) / AsyncFillTimePerTick);

                        // Apply multiplier and ensure `minFill` is respected
                        int newFill = (int)(CurrentFill * multiplier);
                        newFill = Mathf.Max(newFill, _minFill.value);

                        if (CurrentFill != newFill)
                        {
                            CurrentFill = newFill;
                            objectsPerTick = GetObjectsPerTick(CurrentFill);
                        }
                    }
                }
            }

            IsAsyncCurrentlyFilling = false;
            IsFull = true;
        }

        public void FillAsyncManualTick(int amount)
        {
            if (!IsFull && !IsAsyncCurrentlyFilling && fillAsyncTiming == FillAsyncTiming.Manual)
            {
                // Clamp objects this frame to not overfill
                if (PooledCount + amount > PoolCapacity)
                {
                    amount = PoolCapacity - PooledCount;
                }

                for (int i = 0; i < amount; i++)
                {
                    FillNode(PooledCount);
                }

                if (PooledCount == PoolCapacity)
                {
                    IsFull = true;
                }
            }
        }

        protected void UpdateTargetFrameRate()
        {
            // Try to use the monitor's refresh rate if v-sync is activated
            if (QualitySettings.vSyncCount > 0)
            {
                _targetFrameRate.value = Screen.currentResolution.refreshRate;
            }
            // Try to use the target framerate if it is set 
            else if (Application.targetFrameRate != -1)
            {
                _targetFrameRate.value = Application.targetFrameRate;
            }
            // Try to use smooth delta time if it isn't the first frame
            else if (Time.smoothDeltaTime > 0)
            {
                _targetFrameRate.value = Mathf.RoundToInt(1f / Time.smoothDeltaTime);
            }
            // Fallback to a constant (please change it if you're not targetting 60FPS)
            else
            {
                _targetFrameRate.value = DEFAULT_FRAME_RATE;
            }
        }

        protected int GetObjectsPerTick(int currentFill)
        {
            switch (asyncTarget)
            {
                case FillAsyncTarget.PerSecond:
                    float localTargetFrameRate;
                    if (fillAsyncTiming == FillAsyncTiming.EveryFixedUpdate)
                    {
                        localTargetFrameRate = 1 / Time.fixedDeltaTime;
                    }
                    else
                    {
                        localTargetFrameRate = _targetFrameRate.value;
                    }

                    return (int)(currentFill / localTargetFrameRate);

                case FillAsyncTarget.PerFrame:
                    return currentFill;

                default:
                    UnityEngine.Debug.LogError("An unsupported `FillAsyncTarget` value has been set!");
                    return 1;
            }
        }

        protected YieldInstruction GetAsyncYield()
        {
            switch (fillAsyncTiming)
            {
                case FillAsyncTiming.EveryFrame:
                    return null;

                case FillAsyncTiming.EveryFixedUpdate:
                    return new WaitForFixedUpdate();

                default:
                    UnityEngine.Debug.LogError("An unsupported `FillAsyncTiming` value has been set!");
                    return null;
            }
        }

        protected void FillNode(int id)
        {
            Node node = new Node(id, InactiveCount, GenerateData(id), fillActiveState);

            if (fillActiveState)
            {
                _activeNodes[ActiveCount] = node;
                ActiveCount++;
            }
            else
            {
                _inactiveNodes[InactiveCount] = node;
                InactiveCount++;
            }

            _pooledNodes[id] = node;
            PooledCount++;

            FillNodeFinalize(node);

            OnNodeFill?.Invoke(node);
        }

        protected virtual void FillNodeFinalize(Node node) { }

        // Adds a single new node (if there is space in the pool)
        public ObjectPool<T> FillSingle()
        {
            if (PooledCount >= PoolCapacity)
            {
                UnityEngine.Debug.LogWarning("There isn't enough space in the pool to fill another node!");
            }
            else
            {
                FillNode(PooledCount);

                IsFull = PooledCount == PoolCapacity;
            }

            return this;
        }

        protected bool IsValidCapacityId(int id) => id >= 0 && id < PoolCapacity;

        protected bool IsValidPooledId(int id) => id >= 0 && id < PooledCount;

        protected bool EnsureIdsInRange(ref int startId, ref int endId)
        {
            if (endId < startId)
            {
                // Wrong order, swap ids around
                int startIdCache = startId;
                startId = endId;
                endId = startIdCache;
            }

            bool startIdValid = IsValidPooledId(startId);
            bool endIdValid = IsValidPooledId(endId);

            if (!startIdValid && !endIdValid)
            {
                UnityEngine.Debug.LogWarning("Can't use range as both given ids are invalid!");
                return false;
            }

            if (startIdValid && !endIdValid)
            {
                endId = PooledCount - 1;
            }
            else if (!startIdValid && endIdValid)
            {
                startId = 0;
            }

            return true;
        }

        protected virtual T GenerateData(int id) => new T();

        protected void RecreateData(Node node) => node.data = GenerateData(node.Id);

        protected void SetNodeActive(Node node)
        {
            if (InactiveCount == 0) return;

            SetNodeActiveInternal(node);

            OnNodeActive?.Invoke(node);
        }

        protected void SetNodeInactive(Node node)
        {
            if (ActiveCount == 0) return;

            SetNodeInactiveInternal(node);

            OnNodeInactive?.Invoke(node);
        }

        protected virtual void SetNodeActiveInternal(Node node)
        {
            if (recreateData && (node.Active == NodeActiveState.Inactive || node.Active == NodeActiveState.InitialInactive))
            {
                RecreateData(node);
            }

            node.Active = node.Active == NodeActiveState.InitialInactive ? NodeActiveState.FirstActive : NodeActiveState.Active;

            if (InactiveCount >= 1 && node.currActiveStateCount != InactiveCount - 1)
            {
                _inactiveNodes[node.currActiveStateCount] = _inactiveNodes[InactiveCount - 1];
                _inactiveNodes[node.currActiveStateCount].currActiveStateCount = node.currActiveStateCount;
            }

            _activeNodes[ActiveCount] = node;
            node.currActiveStateCount = ActiveCount;

            ActiveCount++;
            InactiveCount--;
        }

        protected virtual void SetNodeInactiveInternal(Node node)
        {
            node.Active = node.Active == NodeActiveState.InitialActive ? NodeActiveState.FirstInactive : NodeActiveState.Inactive;

            if (ActiveCount >= 1 && node.currActiveStateCount != ActiveCount - 1)
            {
                _activeNodes[node.currActiveStateCount] = _activeNodes[ActiveCount - 1];
                _activeNodes[node.currActiveStateCount].currActiveStateCount = node.currActiveStateCount;
            }

            _inactiveNodes[InactiveCount] = node;
            node.currActiveStateCount = InactiveCount;

            ActiveCount--;
            InactiveCount++;
        }

        public Node GetNode()
        {
            if (InactiveCount == 0)
            {
                UnityEngine.Debug.LogWarning("Can't get unused node, as all nodes are already active!");
                return null;
            }

            Node node = _inactiveNodes[InactiveCount - 1];

            SetNodeActive(node);

            return node;
        }

        public T GetData() => GetNode().data;

        protected ObjectPool<T> FreeNode(Node node)
        {
            if (node == null)
            {
                UnityEngine.Debug.LogWarning("Couldn't free node since it's uninitalized/equal to null");
            }
            else if (node.IsActive())
            {
                SetNodeInactive(node);
            }

            return this;
        }

        public ObjectPool<T> Free(int id)
        {
            if (IsEmpty)
            {
                UnityEngine.Debug.LogWarning("Couldn't free node at id: '" + id + "' since the pool is empty");
            }
            else if (!IsValidPooledId(id))
            {
                UnityEngine.Debug.LogWarning("Couldn't free node at id: '" + id + "' since it's an invalid id");
            }
            else
            {
                FreeNode(_pooledNodes[id]);
            }

            return this;
        }

        public ObjectPool<T> Free(Node node) => FreeNode(node);

        // Delay in seconds
        public ObjectPool<T> FreeScheduled(Node node, float delay)
        {
            StartCoroutine(FreeScheduledRoutine(node, delay));

            return this;
        }

        protected IEnumerator FreeScheduledRoutine(Node node, float delay)
        {
            // TODO: add no gc option
            yield return new WaitForSeconds(delay);

            FreeNode(node);
        }

        public ObjectPool<T> FreeRange(int startId, int endId)
        {
            if (EnsureIdsInRange(ref startId, ref endId))
            {
                for (int i = endId; i >= startId; i--)
                {
                    FreeNode(_pooledNodes[i]);
                }
            }

            return this;
        }

        public ObjectPool<T> FreeRangeFrom(int startId, int length) => FreeRange(startId, startId + length - 1);

        protected ObjectPool<T> DisposeNode(Node node)
        {
            if (node == null)
            {
                UnityEngine.Debug.LogWarning("Couldn't dispose node since it's uninitalized/equal to null");
                return this;
            }

            if (disposeData) DisposeData(node);

            if (node.IsActive())
            {
                if (node.currActiveStateCount != ActiveCount - 1)
                {
                    _activeNodes[node.currActiveStateCount] = _activeNodes[ActiveCount - 1];
                    _activeNodes[node.currActiveStateCount].currActiveStateCount = node.currActiveStateCount;
                }

                ActiveCount--;
            }
            else
            {
                if (node.currActiveStateCount != InactiveCount - 1)
                {
                    _inactiveNodes[node.currActiveStateCount] = _inactiveNodes[InactiveCount - 1];
                    _inactiveNodes[node.currActiveStateCount].currActiveStateCount = node.currActiveStateCount;
                }

                InactiveCount--;
            }

            if (node.Id != PooledCount - 1)
            {
                _pooledNodes[node.Id] = _pooledNodes[PooledCount - 1];
                _pooledNodes[node.Id].Id = node.Id;
            }

            PooledCount--;

            IsFull = false;
            IsEmpty = PooledCount == 0;

            return this;
        }

        // Allows for inherited classes to define custom processing/disposal of data
        protected virtual void DisposeData(Node node) { }

        public ObjectPool<T> Dispose(int id)
        {
            if (IsEmpty)
            {
                UnityEngine.Debug.LogWarning("Couldn't dispose node at id: '" + id + "' since the pool is empty");
            }
            else if (!IsValidPooledId(id))
            {
                UnityEngine.Debug.LogWarning("Couldn't dispose node at id: '" + id + "' since it's an invalid id");
            }
            else
            {
                DisposeNode(_pooledNodes[id]);
            }

            return this;
        }

        public ObjectPool<T> Dispose(Node node) => DisposeNode(node);

        public ObjectPool<T> DisposeRange(int startId, int endId)
        {
            if (EnsureIdsInRange(ref startId, ref endId))
            {
                for (int i = endId; i >= startId; i--)
                {
                    DisposeNode(_pooledNodes[i]);
                }
            }

            return this;
        }

        public void DisposeRangeFrom(int startId, int length) => DisposeRange(startId, startId + length - 1);

        public Node[] GetActiveNodes()
        {
            Node[] activeNodes = new Node[ActiveCount];

            Array.Copy(_activeNodes, activeNodes, ActiveCount);

            return activeNodes;
        }

        public void GetActiveNodesNonAlloc(ref Node[] activeNodes)
        {
            if (activeNodes.Length < ActiveCount)
            {
                UnityEngine.Debug.LogError("Array given is too small to store all the data!");
                return;
            }

            Array.Copy(_activeNodes, activeNodes, ActiveCount);
        }

        public T[] GetActiveData()
        {
            T[] activeData = new T[ActiveCount];

            for (int i = 0; i < ActiveCount; i++)
            {
                activeData[i] = _activeNodes[i].data;
            }

            return activeData;
        }

        public void GetActiveDataNonAlloc(ref T[] activeData)
        {
            if (activeData.Length < ActiveCount)
            {
                UnityEngine.Debug.LogError("Array given is too small to store all the data!");
                return;
            }

            for (int i = 0; i < ActiveCount; i++)
            {
                activeData[i] = _activeNodes[i].data;
            }
        }

        public Node[] GetInactiveNodes()
        {
            Node[] inactiveNodes = new Node[InactiveCount];

            Array.Copy(_inactiveNodes, inactiveNodes, InactiveCount);

            return inactiveNodes;
        }

        public void GetInactiveNodesNonAlloc(Node[] inactiveNodes)
        {
            if (inactiveNodes.Length < InactiveCount)
            {
                UnityEngine.Debug.LogError("Array given is too small to store all the data!");
                return;
            }

            Array.Copy(_inactiveNodes, inactiveNodes, InactiveCount);
        }

        public T[] GetInactiveData()
        {
            T[] inactiveData = new T[InactiveCount];

            for (int i = 0; i < InactiveCount; i++)
            {
                inactiveData[i] = _inactiveNodes[i].data;
            }

            return inactiveData;
        }

        public void GetInactiveDataNonAlloc(T[] inactiveData)
        {
            if (inactiveData.Length < InactiveCount)
            {
                UnityEngine.Debug.LogError("Array given is too small to store all the data!");
                return;
            }

            for (int i = 0; i < InactiveCount; i++)
            {
                inactiveData[i] = _inactiveNodes[i].data;
            }
        }

        /// <summary>
        /// Sets all inactive nodes in the pool to active.
        /// </summary>
        public ObjectPool<T> SetAllActive()
        {
            for (int i = InactiveCount - 1; i >= 0; i--)
            {
                SetNodeActive(_inactiveNodes[i]);
            }

            return this;
        }

        /// <summary>
        /// Sets all active nodes in the pool to inactive.
        /// </summary>
        public ObjectPool<T> SetAllInactive()
        {
            for (int i = ActiveCount - 1; i >= 0; i--)
            {
                SetNodeInactive(_activeNodes[i]);
            }

            return this;
        }

        public ObjectPool<T> DisposeActive()
        {
            for (int i = ActiveCount - 1; i >= 0; i--)
            {
                DisposeNode(_activeNodes[i]);
            }

            return this;
        }

        public ObjectPool<T> DisposeInactive()
        {
            for (int i = InactiveCount - 1; i >= 0; i--)
            {
                DisposeNode(_inactiveNodes[i]);
            }

            return this;
        }

        public ObjectPool<T> DisposeAll()
        {
            for (int i = ActiveCount - 1; i >= 0; i--)
            {
                DisposeNode(_activeNodes[i]);
            }

            for (int i = InactiveCount - 1; i >= 0; i--)
            {
                DisposeNode(_inactiveNodes[i]);
            }

            return this;
        }
    }
}