using System;
using UnityEngine;
using UnityEngine.Events;

namespace eilume.ObjectPool.Demos.Shared
{
    public class StringToIntEvent : MonoBehaviour
    {
        public UnityEvent<int> OnTrigger = new UnityEvent<int>();

        public void Trigger(string value)
        {
            try
            {
                int converted = Convert.ToInt32(value);
                OnTrigger.Invoke(converted);
            }
            catch { }
        }
    }
}