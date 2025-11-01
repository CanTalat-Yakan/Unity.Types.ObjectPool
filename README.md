# Unity Essentials

This module is part of the Unity Essentials ecosystem and follows the same lightweight, editor-first approach.
Unity Essentials is a lightweight, modular set of editor utilities and helpers that streamline Unity development. It focuses on clean, dependency-free tools that work well together.

All utilities are under the `UnityEssentials` namespace.

```csharp
using UnityEssentials;
```

## Installation

Install the Unity Essentials entry package via Unity's Package Manager, then install modules from the Tools menu.

- Add the entry package (via Git URL)
    - Window → Package Manager
    - "+" → "Add package from git URL…"
    - Paste: `https://github.com/CanTalat-Yakan/UnityEssentials.git`

- Install or update Unity Essentials packages
    - Tools → Install & Update UnityEssentials
    - Install all or select individual modules; run again anytime to update

---

# Object Pool

> Quick overview: Thread‑safe generic pool with per‑type singletons. Grab items with `Get()`, return with `Return()`. Pools prewarm and refill in batches, and you can access them via `ObjectPools.GetPool<T>()` or `ObjectPools.GetPool(typeof(T))`.

A tiny, fast object pooling utility. For value types and classes with a public parameterless constructor, per‑type pools are created on demand. For special types, construct your own `ObjectPool<T>` with a custom factory. Under the hood it uses a `ConcurrentQueue<T>` and batch allocation for low contention.

![screenshot](Documentation/Screenshot.png)

## Features
- Per‑type global pools
  - `ObjectPools.GetPool<T>()` returns a singleton pool for T
  - `ObjectPools.GetPool(Type)` for non‑generic access and reflection scenarios
- Thread‑safe implementation
  - Internally uses `ConcurrentQueue<T>` for lock‑free enqueue/dequeue
- Batch allocation and prewarm
  - Pools preallocate an initial batch; when empty they allocate another batch
  - Tunable `initialBatchSize` and `batchSize` in the `ObjectPool<T>` constructor
- Zero‑config for common cases
  - Value types and classes with a public parameterless constructor work out of the box
- Custom factories for special types
  - Pass a `Func<T>` to `ObjectPool<T>` when you need non‑default construction

## Requirements
- Unity 6000.0+
- No external dependencies
- Using `ObjectPools.GetPool<T>()` requires `T` to be a value type or a reference type with a public parameterless constructor; otherwise use a custom `ObjectPool<T>` with a factory

## Usage

### 1) Get a per‑type pool and use it
```csharp
using UnityEssentials;

// For types with a parameterless ctor (or value types)
var pool = ObjectPools.GetPool<Bullet>();

var bullet = pool.Get();
// ... initialize/use bullet ...

pool.Return(bullet); // make it available again
```

### 2) Non‑generic access
```csharp
var poolObj = ObjectPools.GetPool(typeof(Bullet));
var pool = (ObjectPool<Bullet>) poolObj;

var bullet = pool.Get();
pool.Return(bullet);
```

### 3) Custom factory for types without a default constructor
```csharp
// Suppose Enemy has no public parameterless ctor or needs args
var enemyPool = new ObjectPool<Enemy>(() => new Enemy(spawnPoint, difficulty), initialBatchSize: 32, batchSize: 32);

var enemy = enemyPool.Get();
// ...
enemyPool.Return(enemy);
```

### 4) Tuning batch sizes
```csharp
// Prewarm 500 items; refill in batches of 100 when empty
var fxPool = new ObjectPool<Explosion>(() => new Explosion(), initialBatchSize: 500, batchSize: 100);
```

## API at a glance
- `static class ObjectPools`
  - `ObjectPool<T> GetPool<T>()`
  - `object GetPool(Type type)` // returns an ObjectPool<T> boxed as object
- `class ObjectPool<T>`
  - Ctor: `ObjectPool(Func<T> factory, int initialBatchSize = 100, int batchSize = 100)`
  - `T Get()` – returns an item (allocates a new batch if empty)
  - `void Return(T item)` – puts the item back into the pool

## How It Works
- A dictionary caches a single pool per `Type`
- For `GetPool<T>()`, a factory is created using `Activator.CreateInstance` (value types and default‑constructible classes)
- `ObjectPool<T>` stores items in a `ConcurrentQueue<T>`
  - If `Get()` finds the queue empty, `AllocateBatch(batchSize)` creates more items via the factory and enqueues them
  - `Return(item)` simply enqueues the item

## Notes and Limitations
- Reset responsibility: The pool doesn’t reset objects; clear/initialize state when you `Get()` or before `Return()`
- Double‑return: There’s no guard against returning the same instance twice - track ownership in your code
- Unbounded growth: Pools can grow as needed; there’s no built‑in cap or shrink
- Construction limits: `ObjectPools.GetPool<T>()` will fail for types without a public parameterless ctor - use a custom factory in that case
- Threading: `ObjectPool<T>` is thread‑safe, but your pooled object’s own state may not be; synchronize your use as needed

## Files in This Package
- `Runtime/ObjectPool.cs` – `ObjectPools` registry and `ObjectPool<T>` implementation
- `Runtime/UnityEssentials.ObjectPool.asmdef` – Runtime assembly definition

## Tags
unity, pooling, object pool, performance, memory, concurrent, thread‑safe, batch, runtime
