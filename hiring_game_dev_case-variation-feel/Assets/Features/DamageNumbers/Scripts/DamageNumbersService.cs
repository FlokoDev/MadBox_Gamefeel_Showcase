using System;
using System.Collections.Generic;
using Core.ObjectPooling;
using Core.ServicesManager;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.DamageNumbers
{
	public class DamageNumbersService : IService
	{
		private sealed class PoolEntry
		{
			public readonly PrefabPool<DamageNumberView> Pool;
			public readonly Action<DamageNumberView> Release;

			public PoolEntry(DamageNumberView prefab, Transform parent, int maxSize)
			{
				Pool = new PrefabPool<DamageNumberView>(prefab, parent, maxSize);
				Release = Pool.Release;
			}
		}

		private GameObject _root;
		private readonly Dictionary<DamageNumberView, PoolEntry> _pools = new Dictionary<DamageNumberView, PoolEntry>();
		private DamageNumbersConfig _config;
		private Camera _camera;

		public Type[] GetDependencies() => null;

		public UniTask<bool> Initialize()
		{
			_config = DamageNumbersConfig.Instance;
			if (_config == null) return UniTask.FromResult(false);

			_root = new GameObject("DamageNumbers");
			Object.DontDestroyOnLoad(_root);
			foreach (DamageNumberView prefab in _config.PrewarmPrefabs)
			{
				if (prefab != null) GetOrCreatePool(prefab).Pool.Prewarm(_config.PrewarmCount);
			}
			return UniTask.FromResult(true);
		}

		public void Show(DamageNumberView prefab, Vector3 position, int damage)
		{
			if (damage <= 0 || _root == null) return;
			if (prefab == null)
			{
				Debug.LogError("A damage number prefab must be supplied to Show.");
				return;
			}
			if (_camera == null) _camera = Camera.main;
			PoolEntry entry = GetOrCreatePool(prefab);
			entry.Pool.Get().Play(position, damage, _camera, entry.Release);
		}

		private PoolEntry GetOrCreatePool(DamageNumberView prefab)
		{
			if (!_pools.TryGetValue(prefab, out PoolEntry entry))
			{
				entry = new PoolEntry(prefab, _root.transform, _config.MaxPoolSize);
				_pools.Add(prefab, entry);
			}
			return entry;
		}

		public void Clear()
		{
			foreach (PoolEntry entry in _pools.Values) entry.Pool.ReleaseAll();
		}

		public UniTask Reset()
		{
			foreach (PoolEntry entry in _pools.Values) entry.Pool.Dispose();
			_pools.Clear();
			_config = null;
			_camera = null;
			if (_root != null) Object.Destroy(_root);
			_root = null;
			return UniTask.CompletedTask;
		}
	}
}
