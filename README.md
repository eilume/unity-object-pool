# Unity Object Pool Asset

<p align="center">
    <img alt="Logo" src="docs/logo.png">
    <br>
    <br>
    <img alt="Latest Version" src="https://img.shields.io/github/v/tag/eilume/unity-object-pool?style=flat-square&color=ff594d">
    <img alt="License: MIT" src="https://img.shields.io/github/license/eilume/unity-object-pool?style=flat-square&color=ff594d">
</p>

> A high-performance Object Pool system specifically designed for use with Unity. It utilizes a [sparse set](https://research.swtch.com/sparse), allowing for fast operations on the pooled data, even with very large pool sizes.

# Installation

## Requirements

- Unity 2020.1 or newer (Only tested with 2021.3)

That's it! Only the demos and tests require some official Unity packages...

### Demos Additional Dependencies

- Official Unity Packages:
  - Unity.TextMeshPro
  - Unity.Burst (Optional)
  - Unity.InputSystem (Optional)

### Tests Additional Dependencies

- Official Unity Packages:
  - Unity.TestRunner

## Setup

1. Download latest package release from here: [Releases](https://github.com/eilume/unity-object-pool/releases/latest)
2. Either:
   - Drag the package into the Unity Editor
   - Select from the toolbar: `Assets` -> `Import Package` -> `Custom Package`
3. Select what files you want from the package (eg. you can exclude the `Demos` and `tests` folders)
4. If all goes well, the editor should recompile assemblies without errors!

# Design

The main objectives for this asset was for an efficient way to pre-allocate + store high quantities of objects and easily take unused/disabled objects from the pool for (re)use, rather than (re)instantiating and destroying the same object type over and over, which causes additional GC allocations + CPU time (due to C#'s GC and Unity's C# <-> C++ interop).

The object pool uses a [sparse set](https://research.swtch.com/sparse) as an efficient data structure for determining what objects are enabled or disabled. It's designed to be adaptable to various needs via events and custom callbacks. It provides an extended class specifically for gameobjects, with optional syncing for the pool's node and gameobject's enabled state.

# Examples

## Bullet Spawner

This example shows how you can achieve a bullet spawner via two different approaches, the more traditional + easy to setup [GameObject Pool Approach](#gameobject-pool-approach) and the more efficient, data-oriented [Generic Object Pool Approach](#generic-object-pool-approach). The demo has bullets that update their position based on their velocity and there is a pool of these bullets from which they're spawned by the player.

### GameObject Pool Approach

This approach is more typical of what developers expect an object pool to do in the context of Unity and for good reason; `GameObject`'s benefit a great deal by being pooled due to their high `Instantiate()` and `Destroy()` costs, both in terms of CPU time and GC allocations.

Here we pool a bullet prefab and spawn one whenever the player presses the "spacebar".

```cs
// Bullet.cs
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public Vector3 vel;

    private void OnEnable()
    {
        vel = Random.insideUnitSphere.normalized;
    }

    private void FixedUpdate()
    {
        transform.position += vel * Time.fixedDeltaTime;
        // ...
        // Other Logic, eg. despawning after certain amount of time
        // This can be achieved by calling `gameObject.SetActive(false)`
        // if the `GameObjectPool` has both `useMonoHook` and `monoDisablesNode`
        // set to true (they both are by default)
    }
}

// BulletSpawner.cs
using UnityEngine;
using eilume.ObjectPool;

public class BulletSpawner : MonoBehaviour
{
    public GameObject bulletPrefab;

    private GameObjectPool _pool;

    private void Awake()
    {
        // Add pool to this gameobject
        _pool = gameObject.AddComponent<GameObjectPool>();
        _pool.ObjectToPool = bulletPrefab;

        // Set the initial size of the pool to 1000
        _pool.Setup(1000);
        // Fill the pool with the set amount of nodes (1000)
        _pool.Fill();
    }

    // We get and free pooled objects in `FixedUpdate()` for consistent
    // results independent of the render framerate (`Update()`)
    private void FixedUpdate()
    {
        // Enable and setup bullet when the 'spacebar' is pressed and
        // not all the nodes in the pool are active/already in use
        if (Input.GetKeyDown(KeyCode.Space) && !_pool.AllActive)
        {
            // Get a currently unused node's data from the pool and
            // set it to used 
            GameObject bulletGameObj = _pool.GetData();
        }
    }
}
```

### Generic Object Pool Approach

This approach is more inline with the data-oriented paradigm, as we don't actually spawn any new gameobjects but instead have all the bullet's data purely in memory. This requires more code, but has drastically more significant performance gain opportunities when having large amounts of nodes active at once. We can do more optimized physics queries and optimized rendering via instancing. We can also utilize the Unity Job System (with Burst!) this way, and even potentially move large amounts of processing onto the GPU via a compute shader.

<!-- This approach uses the normal `ObjectPool<T>` class and stores all the relevant data in a sub-class. -->

```cs
using UnityEngine;
using eilume.ObjectPool;

public class BulletSpawner : MonoBehaviour
{
    // In-memory bullet data
    private class Bullet
    {
        public Vector3 pos;
        public Vector3 vel;
        // ...
        // Other Custom Data

        public void ApplyVelocity(float delta)
        {
            pos += vel * delta;
        }
    }

    // Define class inheriting from `ObjectPool` with custom Bullet
    // type, as Unity doesn't support generic `MonoBeaviour`s
    private class ObjectPoolBullet : ObjectPool<Bullet> { }

    private ObjectPoolBullet _pool;

    private void Awake()
    {
        // Add pool to this gameobject
        _pool = gameObject.AddComponent<ObjectPoolBullet>();

        // Set the initial size of the pool to 1000
        _pool.Setup(1000);
        // Fill the pool with the set amount of nodes (1000)
        _pool.Fill();
    }

    // We get and free pooled objects in `FixedUpdate()` for consistent
    // results independent of the render framerate (`Update()`)
    private void FixedUpdate()
    {
        // Enable and setup bullet when the 'spacebar' is pressed and
        // not all the nodes in the pool are active/already in use
        if (Input.GetKeyDown(KeyCode.Space) && !_pool.AllActive)
        {
            // Get a currently unused node's data from the pool and
            // set it to used 
            Bullet bullet = _pool.GetData();
            
            // Reset bullet to initial values
            bullet.vel = Random.insideUnitSphere.normalized;
        }

        // Get all active bullets from the pool and iterate on them 
        ObjectPoolBullet.Node[] activeBullets = _pool.GetActiveNodes();
        for (int i = 0; i < activeBullets.Length; i++)
        {
            ObjectPoolBullet.Node node = activeBullets[i];

            // Apply velocity to each active bullet
            node.data.ApplyVelocity(Time.fixedDeltaTime);

            // ...
            // Custom Logic, eg. physics checks

            bool shouldFree = false;
            if (shouldFree)
            {
                // Free the node from the pool, allowing it to be re-used
                _pool.Free(node);
            }
        }

        // ...
        // Custom Rendering Code, eg. `Graphics.DrawMeshInstancedIndirect()`
    }
}
```

## Fluid API

Allows for various operations to be chained in a row.

```cs
// Create a pool, set the initial size to 5, fill it and then resize pool
// to 10, for future filling and then store the pool in `pool`
var pool = gameObject.AddComponent<ObjectPoolInt>().Setup(5).Fill().Resize(10);
```

## Synchronous or Asynchronous Fill Modes

### Synchronous

```cs
var pool = gameObject.AddComponent<ObjectPoolInt>();

// Synchronous is the default, so setting `fillMethod` is optional
pool.fillMethod = GameObjectPool.FillMethod.Sync;

pool.Setup(100).Fill();
```

### Asynchronous

```cs
var pool = gameObject.AddComponent<ObjectPoolInt>();

pool.fillMethod = GameObjectPool.FillMethod.Async;
// Trigger fill to trigger every render update (`Update()`)
pool.fillAsyncTiming = GameObjectPool.FillAsyncTiming.EveryUpdate;
// We set the target fill amount to be the target for each frame
pool.asyncTarget = GameObjectPool.FillAsyncTarget.PerFrame;
// Set target fill amount per tick (in this case, per frame due to above)
pool.TargetFill = 1;

// This will take 100 `Update()`s to fill the pool fully
pool.Setup(100).Fill();
```

<!-- TODO: add more examples! -->