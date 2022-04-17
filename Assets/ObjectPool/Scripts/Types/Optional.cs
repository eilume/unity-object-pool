using System;

namespace eilume.ObjectPool
{
    [Serializable]
    public struct Optional<T>
    {
        public bool enabled;
        public T value;

        public Optional(T value, bool enabled = true)
        {
            this.enabled = enabled;
            this.value = value;
        }

        public override bool Equals(object obj)
        {
            return obj is Optional<T> other && enabled == other.enabled && value.Equals(other.value);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(enabled, value);
        }
    }
}