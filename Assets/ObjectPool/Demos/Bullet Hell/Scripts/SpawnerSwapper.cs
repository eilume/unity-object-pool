using System.Collections;
using System.Collections.Generic;
using eilume.ObjectPool.Demos.Shared;
using UnityEngine;

namespace eilume.ObjectPool.Demos.BulletHell
{
    [DefaultExecutionOrder(-1)]
    public class SpawnerSwapper : AUsePool
    {
        public SpawnerUpdateMethod updateMethod;

        public int BulletAmount
        {
            get => _bulletAmount;
            set
            {
                _bulletAmount = value;
                spawner.bulletAmount = poolSpawner.bulletAmount = value;
            }
        }

        [SerializeField]
        private int _bulletAmount = 11000;

        public int BulletsPerSecond
        {
            get => _bulletsPerSecond;
            set
            {
                _bulletsPerSecond = value;
                spawner.bulletsPerSecond = poolSpawner.bulletsPerSecond = value;
            }
        }

        [SerializeField]
        private int _bulletsPerSecond = 1000;

        public float BulletSpeed
        {
            get => _bulletSpeed;
            set
            {
                _bulletSpeed = value;
                spawner.bulletSpeed = poolSpawner.bulletSpeed = value;
            }
        }

        [SerializeField]
        private float _bulletSpeed = 3f;

        public int BulletLifespan
        {
            get => _bulletLifespan;
            set
            {
                _bulletLifespan = value;
                spawner.bulletLifespan = poolSpawner.bulletLifespan = value;
            }
        }

        [SerializeField]
        private int _bulletLifespan = 5;

        [Space(12)]
        public PoolSpawner poolSpawner;
        public Spawner spawner;

        private void Awake()
        {
            spawner.gameObject.SetActive(false);
            poolSpawner.gameObject.SetActive(false);

            BulletAmount = BulletAmount;
            BulletsPerSecond = BulletsPerSecond;
            BulletSpeed = BulletSpeed;
            BulletLifespan = BulletLifespan;

            spawner.updateMethod = poolSpawner.updateMethod = updateMethod;

            spawner.bulletAmount = poolSpawner.bulletAmount = BulletAmount;
            spawner.bulletsPerSecond = poolSpawner.bulletsPerSecond = BulletsPerSecond;

            spawner.bulletSpeed = poolSpawner.bulletSpeed = BulletSpeed;
            spawner.bulletLifespan = poolSpawner.bulletLifespan = BulletLifespan;

            StartCoroutine(DelayStart());
        }

        private void OnEnable()
        {
            onUsePoolChange += OnPoolChange;
        }

        private void OnDisable()
        {
            onUsePoolChange -= OnPoolChange;
        }

        private void OnPoolChange()
        {
            spawner.gameObject.SetActive(!_usePool);
            poolSpawner.gameObject.SetActive(_usePool);
        }

        private IEnumerator DelayStart()
        {
            yield return new WaitForSeconds(0.5f);
            UsePool = UsePool;
        }

        private void Update()
        {
            UpdateUsePool();
        }

        public void SetBulletAmount(int value)
        {
            spawner.gameObject.SetActive(false);
            poolSpawner.gameObject.SetActive(false);

            BulletAmount = value;

            UsePool = UsePool;
        }

        public void SetBulletPerSecond(int value) => BulletsPerSecond = value;
        public void SetBulletSpeed(float value) => BulletSpeed = value;
        public void SetBulletLifespan(int value) => BulletLifespan = value;
    }
}