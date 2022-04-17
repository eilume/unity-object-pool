using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using eilume.ObjectPool;

public class ObjectPoolTests
{
    protected class ObjectPoolTestsInt : ObjectPool<int> { }
    protected class ObjectPoolTestsGameObject : ObjectPool<GameObject> { }

    protected ObjectPoolTestsInt CreateNewIntPool()
    {
        GameObject gm = new GameObject();
        gm.name = "ObjectPoolTestsInt";

        return gm.AddComponent<ObjectPoolTestsInt>();
    }

    protected ObjectPoolTestsGameObject CreateNewGameObjectPool()
    {
        GameObject gm = new GameObject();
        gm.name = "ObjectPoolTestsGameObject";

        return gm.AddComponent<ObjectPoolTestsGameObject>();
    }

    [Test]
    public void Instantiation()
    {
        ObjectPoolTestsInt pool = CreateNewIntPool();

        Assert.NotNull(pool);
        MonoBehaviour.Destroy(pool.gameObject);
    }

    [Test]
    public void SetupWithSizeZero()
    {
        ObjectPoolTestsInt pool = CreateNewIntPool();

        LogAssert.ignoreFailingMessages = true;
        Assert.DoesNotThrow(() => pool.Setup(0));
        LogAssert.ignoreFailingMessages = false;
        MonoBehaviour.Destroy(pool.gameObject);
    }

    [Test]
    public void SetupWithSizeNegative()
    {
        ObjectPoolTestsInt pool = CreateNewIntPool();

        LogAssert.ignoreFailingMessages = true;
        Assert.DoesNotThrow(() => pool.Setup(-25));
        LogAssert.ignoreFailingMessages = false;
        MonoBehaviour.Destroy(pool.gameObject);
    }

    [Test]
    public void FillSingle()
    {
        ObjectPoolTestsInt pool = CreateNewIntPool();

        pool.Setup(5).Fill().Resize(10).FillSingle();

        Assert.True(pool.PoolCapacity == 10 && pool.PooledCount == 6 && !pool.IsFull && !pool.IsEmpty);
        MonoBehaviour.Destroy(pool.gameObject);
    }

    [Test]
    public void FillSync()
    {
        ObjectPoolTestsInt pool = CreateNewIntPool();

        pool.fillMethod = ObjectPoolTestsInt.FillMethod.Sync;
        pool.Setup(10).Fill();

        Assert.True(pool.IsFull);
        MonoBehaviour.Destroy(pool.gameObject);
    }

    [UnityTest]
    public IEnumerator FillAsyncEveryUpdate()
    {
        ObjectPoolTestsInt pool = CreateNewIntPool();

        pool.fillMethod = ObjectPoolTestsInt.FillMethod.Async;
        pool.TargetFill = 1;
        pool.fillAsyncTiming = ObjectPoolTestsInt.FillAsyncTiming.EveryUpdate;
        pool.asyncTarget = ObjectPoolTestsInt.FillAsyncTarget.PerFrame;
        pool.Setup(10).Fill();

        for (int i = 0; i < 9; i++)
        {
            if (pool.IsFull) Assert.Fail();

            yield return null;
        }

        Assert.True(pool.IsFull);
        MonoBehaviour.Destroy(pool.gameObject);
    }

    [UnityTest]
    public IEnumerator FillAsyncEveryFixedUpdate()
    {
        ObjectPoolTestsInt pool = CreateNewIntPool();

        pool.fillMethod = ObjectPoolTestsInt.FillMethod.Async;
        pool.TargetFill = 1;
        pool.fillAsyncTiming = ObjectPoolTestsInt.FillAsyncTiming.EveryFixedUpdate;
        pool.asyncTarget = ObjectPoolTestsInt.FillAsyncTarget.PerFrame;
        pool.Setup(10).Fill();

        for (int i = 0; i < 9; i++)
        {
            if (pool.IsFull) Assert.Fail();

            yield return new WaitForFixedUpdate();
        }

        Assert.True(pool.IsFull);
        MonoBehaviour.Destroy(pool.gameObject);
    }

    [UnityTest]
    public IEnumerator FillAsyncManual()
    {
        ObjectPoolTestsInt pool = CreateNewIntPool();

        pool.fillMethod = ObjectPoolTestsInt.FillMethod.Async;
        pool.TargetFill = 1;
        pool.fillAsyncTiming = ObjectPoolTestsInt.FillAsyncTiming.Manual;
        pool.asyncTarget = ObjectPoolTestsInt.FillAsyncTarget.PerFrame;
        pool.Setup(10).Fill();

        if (pool.PooledCount > 0) Assert.Fail();

        pool.FillAsyncManualTick(5);
        yield return new WaitForSeconds(1f);

        if (pool.IsFull) Assert.Fail();
        pool.FillAsyncManualTick(5);

        Assert.True(pool.IsFull);
        MonoBehaviour.Destroy(pool.gameObject);
    }

    [UnityTest]
    public IEnumerator FillAsyncManualToEveryUpdate()
    {
        ObjectPoolTestsInt pool = CreateNewIntPool();

        pool.fillMethod = ObjectPoolTestsInt.FillMethod.Async;
        pool.TargetFill = 1;
        pool.fillAsyncTiming = ObjectPoolTestsInt.FillAsyncTiming.Manual;
        pool.asyncTarget = ObjectPoolTestsInt.FillAsyncTarget.PerFrame;
        pool.Setup(10).Fill();

        if (pool.IsFull) Assert.Fail();

        pool.FillAsyncManualTick(5);
        yield return new WaitForSeconds(1f);

        pool.fillAsyncTiming = ObjectPoolTestsInt.FillAsyncTiming.EveryFrame;
        pool.FillAsyncManualTick(5);

        if (pool.IsFull) Assert.Fail();

        pool.Fill();

        for (int i = 0; i < 4; i++)
        {
            if (pool.IsFull) Assert.Fail();

            yield return null;
        }

        Assert.True(pool.IsFull);
        MonoBehaviour.Destroy(pool.gameObject);
    }

    [UnityTest]
    public IEnumerator FillAsyncEveryUpdateToManual()
    {
        ObjectPoolTestsInt pool = CreateNewIntPool();

        pool.fillMethod = ObjectPoolTestsInt.FillMethod.Async;
        pool.TargetFill = 1;
        pool.MinFill = new Optional<int>(1);
        pool.fillAsyncTiming = ObjectPoolTestsInt.FillAsyncTiming.EveryFrame;
        pool.asyncTarget = ObjectPoolTestsInt.FillAsyncTarget.PerFrame;
        pool.Setup(10).Fill();

        for (int i = 0; i < 4; i++)
        {
            if (pool.IsFull) Assert.Fail();

            yield return null;
        }

        pool.FillAsyncManualTick(5);

        if (pool.IsFull) Assert.Fail();

        pool.fillAsyncTiming = ObjectPoolTestsInt.FillAsyncTiming.Manual;
        pool.FillAsyncManualTick(5);

        Assert.True(pool.IsFull);
        MonoBehaviour.Destroy(pool.gameObject);
    }

    [UnityTest]
    public IEnumerator FillAsyncFirstFrameFill()
    {
        ObjectPoolTestsInt pool = CreateNewIntPool();

        pool.fillMethod = ObjectPoolTestsInt.FillMethod.Async;
        pool.FirstFrameFill = new Optional<int>(5);
        pool.TargetFill = 1;
        pool.fillAsyncTiming = ObjectPoolTestsInt.FillAsyncTiming.EveryFrame;
        pool.asyncTarget = ObjectPoolTestsInt.FillAsyncTarget.PerFrame;
        pool.Setup(10).Fill();

        for (int i = 0; i < 5; i++)
        {
            if (pool.IsFull) Assert.Fail();

            yield return null;
        }

        Assert.True(pool.IsFull);
        MonoBehaviour.Destroy(pool.gameObject);
    }

    [UnityTest]
    public IEnumerator FillAsyncMinFill()
    {
        ObjectPoolTestsGameObject pool = CreateNewGameObjectPool();

        pool.fillMethod = ObjectPoolTestsGameObject.FillMethod.Async;
        pool.TargetFill = 10000;
        pool.MinFill = new Optional<int>(5000);
        pool.fillAsyncTiming = ObjectPoolTestsGameObject.FillAsyncTiming.EveryFrame;
        pool.asyncTarget = ObjectPoolTestsGameObject.FillAsyncTarget.PerFrame;

        // Some stupid value, forcing the pool to reduce the fill time
        pool.TargetFrameRate = new Optional<int>(1000);

        pool.Setup(100000).Fill();

        for (int i = 0; i < 10; i++)
        {
            if (pool.IsFull) Assert.Fail();

            yield return null;
        }

        yield return new WaitForSeconds(1);

        Assert.True(pool.IsFull);
        MonoBehaviour.Destroy(pool.gameObject);
    }

    [Test]
    public void FillAsyncFirstFrameFillTooLarge()
    {
        ObjectPoolTestsGameObject pool = CreateNewGameObjectPool();

        pool.fillMethod = ObjectPoolTestsGameObject.FillMethod.Async;
        pool.FirstFrameFill = new Optional<int>(1000000);
        pool.TargetFill = 10000;
        pool.fillAsyncTiming = ObjectPoolTestsGameObject.FillAsyncTiming.EveryFrame;
        pool.asyncTarget = ObjectPoolTestsGameObject.FillAsyncTarget.PerFrame;

        // Some stupid value, forcing the pool to reduce the fill time
        pool.TargetFrameRate = new Optional<int>(1000);

        pool.Setup(25000).Fill();

        Assert.True(pool.IsFull);
        MonoBehaviour.Destroy(pool.gameObject);
    }

    [UnityTest]
    public IEnumerator FillAsyncTargetFillTooLarge()
    {
        ObjectPoolTestsGameObject pool = CreateNewGameObjectPool();

        pool.fillMethod = ObjectPoolTestsGameObject.FillMethod.Async;
        pool.TargetFill = 10000;
        pool.MinFill = new Optional<int>(1000);
        pool.fillAsyncTiming = ObjectPoolTestsGameObject.FillAsyncTiming.EveryFrame;
        pool.asyncTarget = ObjectPoolTestsGameObject.FillAsyncTarget.PerFrame;

        // Some stupid value, forcing the pool to reduce the fill time
        pool.TargetFrameRate = new Optional<int>(1000);

        pool.Setup(25000).Fill();

        for (int i = 0; i < 10; i++)
        {
            if (pool.IsFull) Assert.Fail();

            yield return null;
        }

        yield return new WaitForSeconds(1);

        Assert.True(pool.IsFull);
        MonoBehaviour.Destroy(pool.gameObject);
    }

    [UnityTest]
    public IEnumerator FillAsyncMinFillTooLarge()
    {
        ObjectPoolTestsGameObject pool = CreateNewGameObjectPool();

        pool.fillMethod = ObjectPoolTestsGameObject.FillMethod.Async;
        pool.TargetFill = 10000;
        pool.MinFill = new Optional<int>(50000);
        pool.fillAsyncTiming = ObjectPoolTestsGameObject.FillAsyncTiming.EveryFrame;
        pool.asyncTarget = ObjectPoolTestsGameObject.FillAsyncTarget.PerFrame;

        // Some stupid value, forcing the pool to reduce the fill time
        pool.TargetFrameRate = new Optional<int>(1000);

        pool.Setup(25000).Fill();

        for (int i = 0; i < 2; i++)
        {
            if (pool.IsFull) Assert.Fail();

            yield return null;
        }

        yield return new WaitForSeconds(1);

        Assert.True(pool.IsFull);
        MonoBehaviour.Destroy(pool.gameObject);
    }

    [Test]
    public void ClearWhenFull()
    {
        ObjectPoolTestsInt pool = CreateNewIntPool();

        pool.Setup(5).Fill().Clear();

        Assert.True(pool.PooledCount == 0 && pool.IsEmpty);
        MonoBehaviour.Destroy(pool.gameObject);
    }

    [Test]
    public void ClearWhenPartiallyFull()
    {
        ObjectPoolTestsInt pool = CreateNewIntPool();

        pool.Setup(5).Fill().Dispose(pool.GetNode()).Dispose(pool.GetNode()).Clear();

        Assert.True(pool.PooledCount == 0 && pool.IsEmpty);
        MonoBehaviour.Destroy(pool.gameObject);
    }

    [Test]
    public void ClearWhenEmpty()
    {
        ObjectPoolTestsInt pool = CreateNewIntPool();

        pool.Clear();

        Assert.True(pool.PooledCount == 0 && pool.IsEmpty);
        MonoBehaviour.Destroy(pool.gameObject);
    }

    [Test]
    public void ClearThenFill()
    {
        ObjectPoolTestsInt pool = CreateNewIntPool();

        pool.Setup(8).Fill().Clear().Resize(5).Fill();

        Assert.True(pool.PooledCount == 5 && pool.IsFull);
        MonoBehaviour.Destroy(pool.gameObject);
    }

    [Test]
    public void FillWhenFull()
    {
        ObjectPoolTestsInt pool = CreateNewIntPool();

        pool.Setup(8).Fill().Free(pool.GetNode()).Fill();

        Assert.True(pool.PooledCount == 8 && pool.IsFull);
        MonoBehaviour.Destroy(pool.gameObject);
    }

    [Test]
    public void FillWhenPartiallyFull()
    {
        ObjectPoolTestsInt pool = CreateNewIntPool();

        pool.Setup(5).Fill().Dispose(pool.GetNode()).Dispose(pool.GetNode()).Resize(5).Fill();

        Assert.True(pool.PooledCount == 5 && pool.IsFull);
        MonoBehaviour.Destroy(pool.gameObject);
    }

    [Test]
    public void Resize()
    {
        ObjectPoolTestsInt pool = CreateNewIntPool();

        pool.Setup(5).Fill().Resize(10);

        Assert.True(pool.PoolCapacity == 10 && pool.PooledCount == 5 && !pool.IsFull && !pool.IsEmpty);
        MonoBehaviour.Destroy(pool.gameObject);
    }

    [Test]
    public void ResizeRemoveExcess()
    {
        ObjectPoolTestsInt pool = CreateNewIntPool();

        pool.Setup(5).Fill().Resize(10).FillSingle().Shrink();

        Assert.True(pool.PoolCapacity == 6 && pool.PooledCount == 6 && pool.IsFull && !pool.IsEmpty);
        MonoBehaviour.Destroy(pool.gameObject);
    }

    [Test]
    public void ResizeThenClear()
    {
        ObjectPoolTestsInt pool = CreateNewIntPool();

        pool.Setup(5).Fill().Resize(10).Clear();

        Assert.True(pool.PoolCapacity == 10 && pool.IsEmpty);
        MonoBehaviour.Destroy(pool.gameObject);
    }

    [Test]
    public void ResizeWithoutFill()
    {
        ObjectPoolTestsInt pool = CreateNewIntPool();

        pool.Setup(15).Resize(10);

        Assert.True(pool.PoolCapacity == 10 && !pool.IsFull);
        MonoBehaviour.Destroy(pool.gameObject);
    }

    [Test]
    public void ClearThenResize()
    {
        ObjectPoolTestsInt pool = CreateNewIntPool();

        pool.Setup(5).Fill().Clear().Resize(5);

        if (pool.PoolCapacity != 5 || !pool.IsEmpty || pool.IsFull) Assert.Fail();

        pool.Resize(15);

        Assert.True(pool.PoolCapacity == 15 && pool.PooledCount == 0 && !pool.IsFull && pool.IsEmpty);
        MonoBehaviour.Destroy(pool.gameObject);
    }

    [Test]
    public void ClearThenSetupThenFill()
    {
        ObjectPoolTestsInt pool = CreateNewIntPool();

        pool.Clear().Setup(10).Fill();

        Assert.True(pool.PoolCapacity == 10 && pool.PooledCount == 10 && pool.IsFull);
        MonoBehaviour.Destroy(pool.gameObject);
    }

    [Test]
    public void FillThenClearThenResizeThenFill()
    {
        ObjectPoolTestsInt pool = CreateNewIntPool();

        pool.Setup(5).Fill().Clear().Resize(10).Fill();

        Assert.True(pool.PoolCapacity == 10 && pool.PooledCount == 10 && pool.IsFull);
        MonoBehaviour.Destroy(pool.gameObject);
    }

    [Test]
    public void ResizeWithSizeZero()
    {
        ObjectPoolTestsInt pool = CreateNewIntPool();

        pool.Setup(5).Fill().Resize(0);

        Assert.True(pool.PoolCapacity == 1 && pool.IsFull);
        MonoBehaviour.Destroy(pool.gameObject);
    }

    [Test]
    public void ResizeWithSizeNegative()
    {
        ObjectPoolTestsInt pool = CreateNewIntPool();

        pool.Setup(5).Fill().Resize(-1);

        Assert.True(pool.PoolCapacity == 1 && pool.IsFull);
        MonoBehaviour.Destroy(pool.gameObject);
    }

    [Test]
    public void ResizeSmallerWhenEmpty()
    {
        ObjectPoolTestsInt pool = CreateNewIntPool();

        pool.Setup(10).Fill().Resize(5);

        Assert.True(pool.PoolCapacity == 5 && pool.IsFull);
        MonoBehaviour.Destroy(pool.gameObject);
    }

    [Test]
    public void ResizeSmallerWhenPartiallyFull()
    {
        ObjectPoolTestsInt pool = CreateNewIntPool();

        pool.Setup(10).Fill().Dispose(pool.GetNode()).Dispose(pool.GetNode()).Resize(9);

        Assert.True(pool.PoolCapacity == 9 && !pool.IsFull);
        MonoBehaviour.Destroy(pool.gameObject);
    }

    [Test]
    public void ResizeLargerWhenFull()
    {
        ObjectPoolTestsInt pool = CreateNewIntPool();

        pool.Setup(10).Fill().Resize(15);

        Assert.True(pool.PoolCapacity == 15 && !pool.IsFull);
        MonoBehaviour.Destroy(pool.gameObject);
    }

    [Test]
    public void ResizeLargerWhenEmpty()
    {
        ObjectPoolTestsInt pool = CreateNewIntPool();

        pool.Setup(5).Fill().Clear().Resize(10);

        Assert.True(pool.PoolCapacity == 10 && pool.IsEmpty);
        MonoBehaviour.Destroy(pool.gameObject);
    }

    [Test]
    public void ResizeLargerWhenPartiallyFull()
    {
        ObjectPoolTestsInt pool = CreateNewIntPool();

        pool.Setup(10).Fill().Dispose(pool.GetNode()).Dispose(pool.GetNode()).Resize(15);

        Assert.True(pool.PoolCapacity == 15 && !pool.IsFull);
        MonoBehaviour.Destroy(pool.gameObject);
    }

    [Test]
    public void GetNode()
    {
        ObjectPoolTestsInt pool = CreateNewIntPool();

        pool.Setup(5).Fill();
        ObjectPoolTestsInt.Node node = pool.GetNode();

        Assert.True(node != null && pool.ActiveCount == 1 && !pool.AllInactive);
        MonoBehaviour.Destroy(pool.gameObject);
    }

    [Test]
    public void GetTooManyNodes()
    {
        ObjectPoolTestsInt pool = CreateNewIntPool();

        pool.Setup(5).Fill();

        for (int i = 0; i < 6; i++)
        {
            pool.GetNode();
        }

        Assert.True(pool.ActiveCount == 5 && pool.AllActive && pool.PooledCount == 5);
        MonoBehaviour.Destroy(pool.gameObject);
    }

    [Test]
    public void GetNodeEmpty()
    {
        ObjectPoolTestsInt pool = CreateNewIntPool();

        Assert.DoesNotThrow(() => pool.GetNode());
        MonoBehaviour.Destroy(pool.gameObject);
    }

    [Test]
    public void FreeNode()
    {
        ObjectPoolTestsInt pool = CreateNewIntPool();

        pool.Setup(5).Fill();
        ObjectPoolTestsInt.Node node = pool.GetNode();

        pool.Free(node);

        Assert.True(pool.ActiveCount == 0 && pool.AllInactive && pool.PooledCount == 5);
        MonoBehaviour.Destroy(pool.gameObject);
    }

    [Test]
    public void FreeTooManyNodes()
    {
        ObjectPoolTestsInt pool = CreateNewIntPool();

        pool.Setup(5).Fill();

        for (int i = 0; i < 5; i++)
        {
            pool.GetNode();
        }

        for (int i = 0; i < 5; i++)
        {
            pool.Free(i);
        }

        Assert.DoesNotThrow(() => pool.Free(6));
        MonoBehaviour.Destroy(pool.gameObject);
    }

    [Test]
    public void FreeNodeEmpty()
    {
        ObjectPoolTestsInt pool = CreateNewIntPool();

        LogAssert.ignoreFailingMessages = true;
        Assert.DoesNotThrow(() => pool.Free(2));
        LogAssert.ignoreFailingMessages = false;
        MonoBehaviour.Destroy(pool.gameObject);
    }

    [Test]
    public void FreeNodeTwice()
    {
        ObjectPoolTestsInt pool = CreateNewIntPool();

        pool.Setup(5).Fill().Free(2);

        Assert.DoesNotThrow(() => pool.Free(2));
        MonoBehaviour.Destroy(pool.gameObject);
    }

    [Test]
    public void FreeRange()
    {
        ObjectPoolTestsInt pool = CreateNewIntPool();

        pool.Setup(5).Fill();

        for (int i = 0; i < 5; i++)
        {
            pool.GetNode();
        }

        pool.FreeRange(1, 3);

        Assert.True(pool.ActiveCount == 2 && pool.PooledNodes[1].Active == ObjectPoolTestsInt.NodeActiveState.Inactive);
        MonoBehaviour.Destroy(pool.gameObject);
    }

    [Test]
    public void FreeRangeInvalidOne()
    {
        ObjectPoolTestsInt pool = CreateNewIntPool();

        pool.Setup(5).Fill();

        for (int i = 0; i < 5; i++)
        {
            pool.GetNode();
        }

        pool.FreeRange(1, 7);

        Assert.True(pool.PooledCount == 5 && pool.ActiveCount == 1 && !pool.AllActive && !pool.AllInactive && pool.IsFull);
        MonoBehaviour.Destroy(pool.gameObject);
    }

    [Test]
    public void FreeRangeInvalidTwo()
    {
        ObjectPoolTestsInt pool = CreateNewIntPool();

        pool.Setup(5).Fill();

        for (int i = 0; i < 5; i++)
        {
            pool.GetNode();
        }

        pool.FreeRange(-1, 6);

        Assert.True(pool.PooledCount == 5 && pool.ActiveCount == 5 && pool.AllActive && pool.IsFull);
        MonoBehaviour.Destroy(pool.gameObject);
    }

    [Test]
    public void FreeRangeInvalidThree()
    {
        ObjectPoolTestsInt pool = CreateNewIntPool();

        pool.Setup(5).Fill();

        for (int i = 0; i < 5; i++)
        {
            pool.GetNode();
        }

        pool.FreeRange(3, 2);

        Assert.True(pool.PooledCount == 5 && pool.ActiveCount == 3 && !pool.AllActive && pool.IsFull);
        MonoBehaviour.Destroy(pool.gameObject);
    }

    [Test]
    public void SetAllActive()
    {
        ObjectPoolTestsInt pool = CreateNewIntPool();

        pool.Setup(5).Fill();
        pool.GetNode();
        pool.GetNode();
        pool.SetAllActive();

        Assert.True(pool.PooledCount == 5 && pool.AllActive && pool.IsFull);
        MonoBehaviour.Destroy(pool.gameObject);
    }

    [Test]
    public void SetAllInactive()
    {
        ObjectPoolTestsInt pool = CreateNewIntPool();

        pool.Setup(5).Fill();
        pool.GetNode();
        pool.GetNode();
        pool.SetAllInactive();

        Assert.True(pool.PooledCount == 5 && pool.AllInactive && pool.IsFull);
        MonoBehaviour.Destroy(pool.gameObject);
    }

    [Test]
    public void DisposeNode()
    {
        ObjectPoolTestsInt pool = CreateNewIntPool();

        pool.Setup(5).Fill().Dispose(0);

        Assert.True(pool.PoolCapacity == 5 && pool.PooledCount == 4 && pool.AllInactive && !pool.IsFull && !pool.IsEmpty);
        MonoBehaviour.Destroy(pool.gameObject);
    }

    [Test]
    public void DisposeTooManyNodes()
    {
        ObjectPoolTestsInt pool = CreateNewIntPool();

        pool.Setup(5).Fill();

        for (int i = 5; i >= 0; i--)
        {
            pool.Dispose(i);
        }

        Assert.DoesNotThrow(() => pool.Dispose(0));
        MonoBehaviour.Destroy(pool.gameObject);
    }

    [Test]
    public void DisposeNodeEmpty()
    {
        ObjectPoolTestsInt pool = CreateNewIntPool();

        Assert.DoesNotThrow(() => pool.Dispose(2));
        MonoBehaviour.Destroy(pool.gameObject);
    }

    [Test]
    public void DisposeNodeTwice()
    {
        ObjectPoolTestsInt pool = CreateNewIntPool();

        pool.Setup(5).Fill().Dispose(2);

        Assert.DoesNotThrow(() => pool.Dispose(2));
        MonoBehaviour.Destroy(pool.gameObject);
    }

    [Test]
    public void DisposeRange()
    {
        ObjectPoolTestsInt pool = CreateNewIntPool();

        pool.Setup(5).Fill();

        for (int i = 0; i < 5; i++)
        {
            pool.GetNode();
        }

        pool.DisposeRange(1, 3);

        Assert.True(pool.ActiveCount == 2 && pool.PooledCount == 2 && !pool.IsFull && !pool.IsEmpty);
        MonoBehaviour.Destroy(pool.gameObject);
    }

    [Test]
    public void DisposeRangeInvalid()
    {
        ObjectPoolTestsInt pool = CreateNewIntPool();

        pool.Setup(5).Fill().DisposeRange(1, 7);

        Assert.True(pool.PooledCount == 1 && pool.AllInactive && !pool.IsFull && !pool.IsEmpty);
        MonoBehaviour.Destroy(pool.gameObject);
    }

    [Test]
    public void DisposeActive()
    {
        ObjectPoolTestsInt pool = CreateNewIntPool();

        pool.Setup(5).Fill();
        pool.GetNode();
        pool.DisposeActive();

        Assert.True(pool.PooledCount == 4 && pool.PoolCapacity == 5 && !pool.IsFull && !pool.IsEmpty && pool.AllInactive && !pool.AllActive);
        MonoBehaviour.Destroy(pool.gameObject);
    }

    [Test]
    public void DisposeInactive()
    {
        ObjectPoolTestsInt pool = CreateNewIntPool();

        pool.Setup(5).Fill();
        pool.GetNode();
        pool.DisposeInactive();

        Assert.True(pool.PooledCount == 1 && pool.PoolCapacity == 5 && !pool.IsFull && !pool.IsEmpty && pool.AllActive && !pool.AllInactive);
        MonoBehaviour.Destroy(pool.gameObject);
    }


    [Test]
    public void DisposeAll()
    {
        ObjectPoolTestsInt pool = CreateNewIntPool();

        pool.Setup(5).Fill().DisposeAll();

        Assert.True(pool.PooledCount == 0 && pool.PoolCapacity == 5 && !pool.IsFull && pool.IsEmpty && pool.AllActive && pool.AllInactive);
        MonoBehaviour.Destroy(pool.gameObject);
    }
}
