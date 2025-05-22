namespace UnityEssentials
{
    public class ObjectPool<T> where T : struct
    {
        private T[] _pool;
        private int _freeIndex;
        private int _capacity;

        public ObjectPool(int initialCapacity, System.Func<int, T> createItem)
        {
            _capacity = initialCapacity;
            _pool = new T[_capacity];
            _freeIndex = 0;

            for (int i = 0; i < _capacity; i++)
                _pool[i] = createItem(i);
        }

        public T Get()
        {
            if (_freeIndex >= _capacity)
                ExpandPool(_capacity * 2);

            T item = _pool[_freeIndex];
            _freeIndex++;
            return item;
        }

        public void Release(ref T item, int index)
        {
            if (index >= 0 && index < _capacity)
            {
                _pool[index] = item;
                _freeIndex--;
            }
        }

        private void ExpandPool(int newCapacity)
        {
            T[] newPool = new T[newCapacity];
            System.Array.Copy(_pool, newPool, _capacity);
            _pool = newPool;
            _capacity = newCapacity;
        }
    }
}