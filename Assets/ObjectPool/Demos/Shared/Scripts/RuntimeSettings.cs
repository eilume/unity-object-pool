using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace eilume.ObjectPool.Demos.Shared
{
    [DefaultExecutionOrder(-5)]
    public class RuntimeSettings : MonoBehaviour
    {
        private void Awake()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 1000;
        }
    }
}