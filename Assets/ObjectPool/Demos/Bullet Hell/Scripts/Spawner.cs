using System.Collections;
using UnityEngine;
using eilume.ObjectPool.Demos.Shared;

namespace eilume.ObjectPool.Demos.BulletHell
{
    public class Spawner : MonoBehaviour
    {
        [HideInInspector]
        public SpawnerUpdateMethod updateMethod;

        [Space(12)]
        public GameObject bulletPrefab;

        [Header("Bullet Spawner Settings")]
        [HideInInspector]
        public int bulletAmount = 50000;
        [HideInInspector]
        public int bulletsPerSecond = 1000;

        [Space(12)]
        [HideInInspector]
        public float bulletSpeed = 3f;
        [HideInInspector]
        public int bulletLifespan = 5;

        private Transform bulletParent;
        private int bulletCount;

        private float _spawnTimer;

        private void OnEnable()
        {
            CreateBulletParent();
        }

        private void OnDisable()
        {
            if (bulletParent != null)
            {
                StopAllCoroutines();
                Destroy(bulletParent.gameObject);
            }

            bulletCount = 0;
        }

        private void CreateBulletParent()
        {
            bulletParent = new GameObject().transform;
            bulletParent.gameObject.name = "BulletTransform";
            bulletParent.SetParent(transform);
            bulletParent.localPosition = Vector3.zero;
        }

        private void Update()
        {
            float bulletsInterval = 1.0f / bulletsPerSecond;
            _spawnTimer += Time.deltaTime;

            while (_spawnTimer >= bulletsInterval) {
                _spawnTimer -= bulletsInterval;

                Bullet bullet = Instantiate(bulletPrefab, bulletParent).GetComponent<Bullet>();

                bullet.velocity = (Vector3)UnityEngine.Random.insideUnitCircle.normalized * bulletSpeed;
                bullet.transform.localPosition = Vector3.zero + (Vector3)(bullet.velocity * _spawnTimer);

                bulletCount++;

                StartCoroutine(BulletLifespan(bullet, bulletLifespan));
            }
        }

        private IEnumerator BulletLifespan(Bullet bullet, float delay)
        {
            yield return new WaitForSeconds(delay);

            Destroy(bullet.gameObject);

            bulletCount--;
        }
    }
}