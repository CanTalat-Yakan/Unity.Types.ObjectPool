namespace UnityEssentials
{
    public class ObjectPool<T> where T : struct
    {
        private T[] _pool;
        private int _count;
        private int _capacity;

        public ObjectPool(int initialCapacity = 256)
        {
            _capacity = initialCapacity;
            _pool = new T[_capacity];
            _count = _capacity;
        }

        public ref T Get()
        {
            if (_count == 0)
                ExpandPool(_capacity + (_capacity >> 1));

            _count--;
            return ref _pool[_count];
        }

        public void Release(ref T item)
        {
            _pool[_count] = item;
            _count++;
        }

        private void ExpandPool(int newCapacity)
        {
            T[] newPool = new T[newCapacity];
            System.Array.Copy(_pool, 0, newPool, 0, _capacity);
            _pool = newPool;
            _count += newCapacity - _capacity;
            _capacity = newCapacity;
        }
    }
}
