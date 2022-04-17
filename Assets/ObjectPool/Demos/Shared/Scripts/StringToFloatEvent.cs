using System;
using UnityEngine;
using UnityEngine.Events;

namespace eilume.ObjectPool.Demos.Shared
{
    public class StringToFloatEvent : MonoBehaviour
    {
        public UnityEvent<float> OnTrigger = new UnityEvent<float>();

        public void Trigger(string value)
        {
            try
            {
                float converted = Convert.ToSingle(value);
                OnTrigger.Invoke(converted);
            }
            catch { }
        }
    }
}