using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace UnityEssentials
{
    /// <summary>
    /// Provides access to reusable object pools for creating and managing instances of objects.
    /// </summary>
    /// <remarks>The <see cref="ObjectPools"/> class allows efficient reuse of objects by maintaining object
    /// pools for different types. This reduces the overhead of repeatedly creating and destroying objects, particularly
    /// for frequently used or expensive-to-create types.  Object pools are created on demand when a type is first
    /// requested. For value types or types with parameterless constructors, a default factory method is used to create
    /// new instances. For other types, a factory method must be provided.  This class supports both generic and
    /// non-generic access to object pools: <list type="bullet"> <item> <description>Use <see cref="GetPool{T}"/> to
    /// retrieve a strongly-typed object pool for a specific type.</description> </item> <item> <description>Use <see
    /// cref="GetPool(Type)"/> to retrieve a non-generic object pool for a type at runtime.</description> </item>
    /// </list></remarks>
    public static class ObjectPools
    {
        private static readonly Dictionary<Type, object> _pools = new();

        /// <summary>
        /// Retrieves an object pool for the specified type <typeparamref name="T"/>.
        /// </summary>
        /// <remarks>This method ensures that a single object pool is maintained per type <typeparamref
        /// name="T"/>. If a pool does not already exist for the type, a new pool is created using a factory method for
        /// the type. Subsequent calls for the same type will return the existing pool.</remarks>
        /// <typeparam name="T">The type of objects managed by the pool.</typeparam>
        /// <returns>An <see cref="ObjectPool{T}"/> instance for the specified type <typeparamref name="T"/>. If no pool exists
        /// for the type, a new one is created and returned.</returns>
        public static ObjectPool<T> GetPool<T>()
        {
            Type type = typeof(T);
            if (!_pools.TryGetValue(type, out object poolObj))
            {
                // Create a factory method for the type
                Func<T> factoryMethod = CreateFactoryMethod<T>();
                ObjectPool<T> pool = new(factoryMethod);

                _pools[type] = pool;

                return pool;
            }

            return (ObjectPool<T>)poolObj;
        }

        /// <summary>
        /// Retrieves an object pool for the specified type, creating it if it does not already exist.
        /// </summary>
        /// <remarks>This method ensures that only one object pool is created per type. If a pool for the
        /// specified type  already exists, it is returned. Otherwise, a new pool is created using a factory method for
        /// the type.</remarks>
        /// <param name="type">The type of objects managed by the pool. This cannot be <see langword="null"/>.</param>
        /// <returns>An instance of an object pool for the specified type. The returned object is of type <see
        /// cref="ObjectPool{T}"/>,  where <c>T</c> is the specified type.</returns>
        public static object GetPool(Type type)
        {
            if (!_pools.TryGetValue(type, out object poolObj))
            {
                // Create a factory method for the type
                var factoryMethod = CreateFactoryMethod(type);
                var poolType = typeof(ObjectPool<>).MakeGenericType(type);

                poolObj = Activator.CreateInstance(poolType, factoryMethod);

                _pools[type] = poolObj;
            }

            return poolObj;
        }

        /// <summary>
        /// Creates a factory method for instantiating objects of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of object to create.</typeparam>
        /// <returns>A function that, when invoked, creates a new instance of type <typeparamref name="T"/>. For value types, the
        /// function returns a default-initialized instance. For reference types, the function uses the default
        /// constructor to create the instance.</returns>
        private static Func<T> CreateFactoryMethod<T>()
        {
            // For value types, use Activator.CreateInstance without new()
            if (typeof(T).IsValueType || typeof(T) == typeof(string))
            {
                return () => Activator.CreateInstance<T>();
            }

            // For reference types, use the default constructor
            return () => (T)Activator.CreateInstance(typeof(T));
        }

        /// <summary>
        /// Creates a factory method that instantiates an object of the specified type.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> of the object to be created. Must represent a type with a parameterless constructor.</param>
        /// <returns>A <see cref="Func{TResult}"/> delegate that, when invoked, creates a new instance of the specified type.</returns>
        private static Func<object> CreateFactoryMethod(Type type) =>
            () => Activator.CreateInstance(type);
    }

    /// <summary>
    /// Provides a thread-safe pool of reusable objects of type <typeparamref name="T"/>.
    /// </summary>
    /// <remarks>The <see cref="ObjectPool{T}"/> class is designed to manage a collection of reusable objects,
    /// reducing the overhead of creating and destroying objects frequently. Objects are created using  a factory method
    /// provided during initialization. The pool automatically allocates additional  objects in batches when
    /// needed.</remarks>
    /// <typeparam name="T">The type of objects managed by the pool.</typeparam>
    public class ObjectPool<T>
    {
        private readonly ConcurrentQueue<T> _pool;
        private readonly int _batchSize;
        private readonly Func<T> _factoryMethod;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectPool{T}"/> class with a specified factory method and
        /// batch sizes.
        /// </summary>
        /// <param name="factoryMethod">A function that creates new instances of the object type <typeparamref name="T"/>.  This parameter cannot be
        /// <see langword="null"/>.</param>
        /// <param name="initialBatchSize">The number of objects to preallocate and add to the pool upon initialization.  Defaults to 100 if not
        /// specified.</param>
        /// <param name="batchSize">The number of objects to allocate when the pool is empty and additional objects are needed.  Defaults to 100
        /// if not specified.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="factoryMethod"/> is <see langword="null"/>.</exception>
        public ObjectPool(Func<T> factoryMethod, int initialBatchSize = 100, int batchSize = 100)
        {
            _factoryMethod = factoryMethod ?? throw new ArgumentNullException(nameof(factoryMethod));
            _batchSize = batchSize;
            _pool = new();

            AllocateBatch(initialBatchSize);
        }

        /// <summary>
        /// Retrieves an object from the pool.
        /// </summary>
        /// <remarks>If the pool is empty, a new batch of objects is allocated to ensure
        /// availability.</remarks>
        /// <returns>An object of type <typeparamref name="T"/> from the pool.</returns>
        public T Get()
        {
            // Retrieves an object from the pool.
            // If the pool is empty, a new batch is allocated.
            if (!_pool.TryDequeue(out T item))
            {
                AllocateBatch(_batchSize);
                _pool.TryDequeue(out item);
            }

            return item;
        }

        /// <summary>
        /// Returns an item to the pool for reuse.
        /// </summary>
        /// <remarks>This method enqueues the specified item back into the pool, making it available for
        /// future retrieval. Ensure that the item is no longer in use before returning it to avoid unexpected
        /// behavior.</remarks>
        /// <param name="item">The item to return to the pool. Must not be null.</param>
        public void Return(T item) =>
            _pool.Enqueue(item);

        /// <summary>
        /// Allocates a batch of objects and adds them to the pool.
        /// </summary>
        /// <remarks>This method uses the factory method to create the specified number of objects and
        /// enqueues them into the pool. Ensure that the <c>size</c> parameter is appropriate for the pool's capacity to
        /// avoid excessive memory usage.</remarks>
        /// <param name="size">The number of objects to allocate and add to the pool. Must be a non-negative integer.</param>
        private void AllocateBatch(int size)
        {
            for (int i = 0; i < size; i++)
            {
                var item = _factoryMethod();
                _pool.Enqueue(item);
            }
        }
    }
}