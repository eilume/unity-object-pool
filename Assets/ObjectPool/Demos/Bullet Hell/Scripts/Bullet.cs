using UnityEngine;
using eilume.ObjectPool;

namespace eilume.ObjectPool.Demos.BulletHell
{
    public class Bullet : MonoBehaviour
    {
        public Vector3 velocity;

        private void Update()
        {
            transform.position += velocity * Time.deltaTime;
        }
    }
}