using System;
using System.Runtime.CompilerServices;
using UnityEngine;

[assembly: InternalsVisibleTo("eilume.ObjectPool.Editor")]

namespace eilume.ObjectPool
{
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    internal sealed class OptionalMinAttribute : PropertyAttribute
    {
        public readonly float min;

        public OptionalMinAttribute(float min)
        {
            this.min = min;
        }
    }
}