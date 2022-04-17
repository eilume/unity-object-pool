using UnityEngine;
using UnityEngine.Jobs;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using eilume.ObjectPool;
using eilume.ObjectPool.Demos.Shared;

namespace eilume.ObjectPool.Demos.BulletHell
{
    public class PoolSpawner : APooledVersion
    {
        [HideInInspector]
        public SpawnerUpdateMethod updateMethod;

        [Space(12)]
        public GameObject bulletPrefab;

        [HideInInspector]
        public int bulletAmount = 50000;
        [HideInInspector]
        public int bulletsPerSecond = 1000;

        [Space(12)]
        [HideInInspector]
        public float bulletSpeed = 3f;
        [HideInInspector]
        public int bulletLifespan = 5;

        private float _spawnTimer;

        private GameObjectPool pool;

        private TransformAccessArray transformAccess;

        private SpriteRenderer[] sprites;

        private NativeArray<Vector2> nativeBulletHellData;
        private NativeArray<int> expiredBulletIndexes;

        private UpdateBulletJobSystemJob updateBulletJobSystemJob;
        private UpdateBulletJobSystemBurstJob updateBulletJobSystemBurstJob;

        private void Awake()
        {
            PoolSetup();
        }

        private void PoolSetup()
        {
            if (pool == null)
            {
                pool = gameObject.AddComponent<GameObjectPool>();
                pool.ObjectToPool = bulletPrefab;
                pool.useMonoHook = false;
                pool.nodeEnablesGameObject = true;
                pool.nodeDisablesGameObject = false;

                pool.fillMethod = _fillMethod;
            }

            transformAccess = new TransformAccessArray(bulletAmount);

            sprites = new SpriteRenderer[bulletAmount];

            if (_fillMethod == GameObjectPool.FillMethod.Async)
            {
                pool.fillTrigger = GameObjectPool.FillTrigger.Manual;
                pool.fillAsyncTiming = GameObjectPool.FillAsyncTiming.EveryFrame;

                pool.TargetFill = Mathf.RoundToInt(bulletsPerSecond * 1.25f);
                pool.MinFill = new Optional<int>(bulletsPerSecond);

                // Fill transform access to set the size of the array and then
                // begin overriding the values on each fill. This is because
                // Unity only allows you to set the capacity of the transform
                // access but can only insert at a specific position once the
                // length is higher than the index you specify via `[]`.
                Transform blankTransform = new GameObject().transform;
                blankTransform.SetParent(transform);
                blankTransform.gameObject.name = "BlankTransform";

                for (int i = 0; i < bulletAmount; i++)
                {
                    transformAccess.Add(blankTransform);
                }

                pool.OnNodeFill += OnNodeFill;
            }

            pool.OnNodeInactive += OnNodeInactive;

            if (!pool.IsSetup)
            {
                pool.Setup(bulletAmount).Fill();
            }
            else
            {
                pool.Resize(bulletAmount).Fill();
            }

            if (_fillMethod == GameObjectPool.FillMethod.Sync)
            {
                for (int i = 0; i < bulletAmount; i++)
                {
                    transformAccess.Add(pool.PooledNodes[i].data.transform);
                }
            }

            _spawnTimer = 0.0f;

            nativeBulletHellData = new NativeArray<Vector2>(bulletAmount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            expiredBulletIndexes = new NativeArray<int>(bulletAmount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        }

        private void OnEnable()
        {
            if (pool != null)
            {
                DisposeAll();
                Destroy(pool);
                pool = null;
                PoolSetup();
            }
        }

        private void OnDisable()
        {
            if (pool != null)
            {
                pool.SetAllInactive();
            }
        }

        private void OnDestroy()
        {
            pool.OnNodeFill -= OnNodeFill;
            pool.OnNodeInactive -= OnNodeInactive;

            DisposeAll();
        }

        private void DisposeAll()
        {
            transformAccess.Dispose();
            nativeBulletHellData.Dispose();
            expiredBulletIndexes.Dispose();
        }

        private void Update()
        {
            SpawnBullets();
            UpdateBullets();
        }

        private void OnNodeFill(GameObjectPool.Node node) => transformAccess[node.Id] = node.data.transform;

        private void OnNodeInactive(GameObjectPool.Node node)
        {
            nativeBulletHellData[node.Id] = Vector2.zero;

            if (node.Active != ObjectPool<GameObject>.NodeActiveState.InitialInactive)
            {
                node.data.transform.localPosition = Vector3.up * 5000;
                sprites[node.Id].enabled = false;
            }
        }

        private void SpawnBullets()
        {
            float bulletsInterval = 1.0f / bulletsPerSecond;
            _spawnTimer += Time.deltaTime;

            while (_spawnTimer >= bulletsInterval) {
                _spawnTimer -= bulletsInterval;

                GameObjectPool.Node node = pool.GetNode();

                UnityEngine.Vector2 velocity = UnityEngine.Random.insideUnitCircle.normalized * bulletSpeed;
                nativeBulletHellData[node.Id] = velocity;

                node.data.transform.localPosition = Vector3.zero + (Vector3)(velocity * _spawnTimer);

                if (node.Active == GameObjectPool.NodeActiveState.FirstActive)
                {
                    sprites[node.Id] = node.data.GetComponent<SpriteRenderer>();
                }
                else
                {
                    sprites[node.Id].enabled = true;
                }

                pool.FreeScheduled(node, bulletLifespan);
            }
        }

        private void UpdateBullets()
        {
            switch (updateMethod)
            {
                case SpawnerUpdateMethod.CSharp:
                    UpdateBulletsCSharp();
                    return;

                case SpawnerUpdateMethod.JobSystem:
                    UpdateBulletsJobSystem();
                    return;

                case SpawnerUpdateMethod.JobSystemBurst:
                    UpdateBulletsJobSystemBurst();
                    return;

                default:
                    Debug.LogError("An unknown update method has been set!");
                    return;
            }
        }

        private void UpdateBulletsCSharp()
        {
            for (int i = pool.ActiveCount - 1; i >= 0; i--)
            {
                GameObjectPool.Node node = pool.ActiveNodes[i];

                if (node.Active == GameObjectPool.NodeActiveState.Active || node.Active == GameObjectPool.NodeActiveState.FirstActive)
                {
                    Vector2 bullet = nativeBulletHellData[node.Id];

                    Vector3 posOffset = new Vector3(bullet.x, bullet.y, 0) * Time.deltaTime;
                    node.data.transform.position += posOffset;
                }
            }
        }

        private struct UpdateBulletJobSystemJob : IJobParallelForTransform
        {
            [Unity.Collections.ReadOnly]
            public float deltaTime;

            public NativeArray<Vector2> bulletsData;

            public void Execute(int index, TransformAccess transform)
            {
                Vector2 bulletData = bulletsData[index];

                if (bulletData.x != 0 || bulletData.y != 0)
                {
                    transform.position += new Vector3(bulletData.x, bulletData.y, 0) * deltaTime;
                }
            }
        }

        private void UpdateBulletsJobSystem()
        {
            updateBulletJobSystemJob = new UpdateBulletJobSystemJob()
            {
                deltaTime = Time.deltaTime,
                bulletsData = nativeBulletHellData,
            };

            JobHandle updateBulletJobSystemHandle = updateBulletJobSystemJob.Schedule(transformAccess);
            updateBulletJobSystemHandle.Complete();
        }

        [BurstCompile]
        private struct UpdateBulletJobSystemBurstJob : IJobParallelForTransform
        {
            [Unity.Collections.ReadOnly]
            public float deltaTime;

            public NativeArray<Vector2> bulletsData;

            public void Execute(int index, TransformAccess transform)
            {
                Vector2 bulletData = bulletsData[index];

                if (bulletData.x != 0 || bulletData.y != 0)
                {
                    transform.position += new Vector3(bulletData.x, bulletData.y, 0) * deltaTime;
                }
            }
        }

        private void UpdateBulletsJobSystemBurst()
        {
#if UNITY_BURST
            updateBulletJobSystemBurstJob = new UpdateBulletJobSystemBurstJob()
            {
                deltaTime = Time.deltaTime,
                bulletsData = nativeBulletHellData,
            };

            JobHandle updateBulletJobSystemBurstHandle = updateBulletJobSystemBurstJob.Schedule(transformAccess);
            updateBulletJobSystemBurstHandle.Complete();
#else
            Debug.LogError("Burst package isn't installed! Falling back to standard job system...");
            updateMethod = SpawnerUpdateMethod.JobSystem;
            UpdateBulletsJobSystem();
#endif
        }
    }
}
