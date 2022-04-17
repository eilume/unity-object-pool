using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using eilume.ObjectPool;

// TODO: add more tests

public class GameObjectPoolTests
{
    internal GameObject objectToPool;

    protected GameObjectPool CreateNewPool()
    {
        if (objectToPool == null)
        {
            objectToPool = new GameObject();
            objectToPool.name = "ObjectToPool";
        }

        GameObject gm = new GameObject();
        gm.name = "GameObjectPool";

        GameObjectPool pool = gm.AddComponent<GameObjectPool>();
        pool.ObjectToPool = objectToPool;

        return pool;
    }

    [Test]
    public void Instantiation()
    {
        GameObjectPool pool = CreateNewPool();

        Assert.NotNull(pool);
        MonoBehaviour.Destroy(pool.gameObject);
    }
}
