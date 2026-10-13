using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace Core.ObjectPooling
{
	public sealed class PrefabPool<T> : IDisposable where T : Component
	{
		private readonly ObjectPool<T> _pool;
		private readonly HashSet<T> _active = new HashSet<T>();
		private readonly int _maxSize;
		private bool _disposed;

		public int CountActive => _active.Count;
		public int CountInactive => _pool.CountInactive;

		public PrefabPool(T prefab, Transform parent, int maxSize)
		{
			if (prefab == null) throw new ArgumentNullException(nameof(prefab));
			if (maxSize < 1) throw new ArgumentOutOfRangeException(nameof(maxSize));
			_maxSize = maxSize;
			_pool = new ObjectPool<T>(
				() => Object.Instantiate(prefab, parent),
				actionOnRelease: instance => instance.gameObject.SetActive(false),
				actionOnDestroy: instance =>
				{
					// Unity can destroy pooled objects before the owning service is reset on shutdown.
					if (instance != null) Object.Destroy(instance.gameObject);
				},
				collectionCheck: true, defaultCapacity: Mathf.Min(16, maxSize), maxSize: maxSize);
		}

		public T Get()
		{
			ThrowIfDisposed();
			T instance = _pool.Get();
			_active.Add(instance);
			instance.gameObject.SetActive(true);
			return instance;
		}

		public void Release(T instance)
		{
			ThrowIfDisposed();
			if (instance == null) throw new ArgumentNullException(nameof(instance));
			if (!_active.Remove(instance))
				throw new InvalidOperationException("Instance is not active in this pool or was already returned.");
			_pool.Release(instance);
		}

		public void Prewarm(int count)
		{
			ThrowIfDisposed();
			if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
			List<T> instances = new List<T>(Mathf.Min(count, _maxSize));
			for (int i = 0; i < Mathf.Min(count, _maxSize); i++) instances.Add(Get());
			foreach (T instance in instances) Release(instance);
		}

		public void ReleaseAll()
		{
			ThrowIfDisposed();
			while (_active.Count > 0)
			{
				T instance;
				using (HashSet<T>.Enumerator iterator = _active.GetEnumerator())
				{
					iterator.MoveNext();
					instance = iterator.Current;
				}
				Release(instance);
			}
		}

		public void Clear()
		{
			ThrowIfDisposed();
			foreach (T instance in _active)
			{
				if (instance == null) continue;
				instance.gameObject.SetActive(false);
				Object.Destroy(instance.gameObject);
			}
			_active.Clear();
			_pool.Clear();
		}

		public void Dispose()
		{
			if (_disposed) return;
			Clear();
			_pool.Dispose();
			_disposed = true;
		}

		private void ThrowIfDisposed()
		{
			if (_disposed) throw new ObjectDisposedException(nameof(PrefabPool<T>));
		}
	}
}
