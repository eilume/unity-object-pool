using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace eilume.ObjectPool.Demos.Shared
{
    public abstract class AUsePool : MonoBehaviour
    {
        public bool UsePool
        {
            get => _usePool;
            set
            {
                _usePool = value;

                onUsePoolChange?.Invoke();
            }
        }

        protected bool _usePool = false;

        public Action onUsePoolChange;

        protected void UpdateUsePool()
        {
#if ENABLE_INPUT_SYSTEM
    #if UNITY_STANDALONE || UNITY_WEBGL || UNITY_EDITOR
            if (Keyboard.current[Key.Space].wasPressedThisFrame)
    #elif UNITY_ANDROID || UNITY_IOS
            if (Touchscreen.current.primaryTouch.phase.ReadUnprocessedValue() == UnityEngine.InputSystem.TouchPhase.Began)
    #else
            if (Gamepad.current.buttonSouth.wasPressedThisFrame)
    #endif
#else
            if (Input.GetKeyDown(KeyCode.Space))
#endif
            {
                UsePool = !_usePool;
            }
        }
    }
}