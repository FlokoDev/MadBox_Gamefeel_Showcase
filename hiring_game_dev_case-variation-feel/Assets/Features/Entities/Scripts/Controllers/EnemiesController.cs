using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.GamePlay.Heroes;
using UnityEngine;

namespace Game.GamePlay.Enemies
{
	public class EnemiesController
	{
		private HeroController _heroController;

		// Events
		public event Action<EnemyState> OnEnemySpawned;
		public event Action<int> OnEnemyRemoved;
		public event Action<EnemyState> OnEnemyPositionChanged;
		public event Action<int> OnEnemyDamaged;
		public event Action<int> OnEnemyAttackPerformed;
		public event Action<int> OnEnemyDied;

		// State
		private Dictionary<int, EnemyState> _enemies;
		private CancellationTokenSource _cancellationTokenSource;
		private CancellationTokenSource _deathRemovalCancellationTokenSource;
		private int _nextEnemyId;

		public IReadOnlyDictionary<int, EnemyState> Enemies => _enemies;

		public UniTask<bool> Initialize(HeroController heroController)
		{
			_heroController = heroController;

			_enemies = new Dictionary<int, EnemyState>();
			_nextEnemyId = 0;
			_cancellationTokenSource = new CancellationTokenSource();
			_deathRemovalCancellationTokenSource = new CancellationTokenSource();

			SpawnLoop(_cancellationTokenSource.Token).Forget();
			UpdateLoop(_cancellationTokenSource.Token).Forget();

			return UniTask.FromResult(true);
		}

		public UniTask Reset()
		{
			_cancellationTokenSource?.Cancel();
			_cancellationTokenSource?.Dispose();
			_deathRemovalCancellationTokenSource?.Cancel();
			_deathRemovalCancellationTokenSource?.Dispose();
			_enemies.Clear();

			return UniTask.CompletedTask;
		}

		public void ClearAllEnemies()
		{
			_deathRemovalCancellationTokenSource.Cancel();
			_deathRemovalCancellationTokenSource.Dispose();
			_deathRemovalCancellationTokenSource = new CancellationTokenSource();

			List<int> enemyIds = new List<int>(_enemies.Keys);
			foreach (int enemyId in enemyIds)
			{
				RemoveEnemy(enemyId);
			}
		}

		public void RemoveEnemy(int enemyId)
		{
			if (_enemies.Remove(enemyId))
			{
				OnEnemyRemoved?.Invoke(enemyId);
			}
		}

		public void AttackEnemy(EnemyState enemyState, int damage)
		{
			if (!_enemies.ContainsKey(enemyState.Id)) return;

			int newHealth = enemyState.Health - damage;
			if (damage > 0) OnEnemyDamaged?.Invoke(enemyState.Id);

			Debug.Log($"Attacked enemy id°{enemyState.Id}. Health : {enemyState.Health} -> {newHealth}");

			if (newHealth <= 0)
			{
				Debug.Log($"Enemy id°{enemyState.Id} is dead.");
				KillEnemy(enemyState.Id).Forget();
			}
			else
			{
				EnemyState updatedEnemy = new EnemyState(enemyState.Id, enemyState.Position, newHealth, enemyState.Config);
				_enemies[enemyState.Id] = updatedEnemy;
			}
		}

		

		private async UniTaskVoid SpawnLoop(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				if (!_heroController.CurrentState.IsDead && _enemies.Count < EnemiesConfig.Instance.MaxEnemies)
				{
					SpawnEnemy();
				}

				await UniTask.Delay(TimeSpan.FromSeconds(EnemiesConfig.Instance.SpawnInterval), cancellationToken: cancellationToken);
			}
		}

		private void SpawnEnemy()
		{
			if (EnemiesConfig.Instance.Enemies.Count == 0) return;

			Vector3 playerPosition = _heroController.CurrentState.Position;
			Vector3 spawnPosition = GetRandomPositionAroundPlayer(playerPosition);

			int enemyId = _nextEnemyId++;
			EnemyConfig enemyConfig = EnemiesConfig.Instance.Enemies[0];
			EnemyState newEnemy = new EnemyState(enemyId, spawnPosition, enemyConfig.InitialHealth, enemyConfig);

			_enemies[enemyId] = newEnemy;
			OnEnemySpawned?.Invoke(newEnemy);
		}

		private Vector3 GetRandomPositionAroundPlayer(Vector3 playerPosition)
		{
			float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
			float x = playerPosition.x + EnemiesConfig.Instance.SpawnRadius * Mathf.Cos(angle);
			float z = playerPosition.z + EnemiesConfig.Instance.SpawnRadius * Mathf.Sin(angle);

			return new Vector3(x, playerPosition.y, z);
		}

		private async UniTaskVoid UpdateLoop(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				if (_heroController.CurrentState.IsDead)
				{
					await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
					continue;
				}

				List<int> enemiesToUpdate = new List<int>(_enemies.Keys);
				for (int i = 0; i < enemiesToUpdate.Count; i++)
				{
					int enemyId = enemiesToUpdate[i];
					if (!_enemies.TryGetValue(enemyId, out EnemyState enemy)) continue;

					UpdateEnemy(enemy);
				}

				await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
			}
		}

		private void UpdateEnemy(EnemyState enemy)
		{
			Vector3 heroPosition = _heroController.CurrentState.Position;
			float distanceToHero = Vector3.Distance(enemy.Position, heroPosition);

			if (distanceToHero > enemy.Config.AttackRange)
			{
				Vector3 direction = (heroPosition - enemy.Position).normalized;
				Vector3 newPosition = enemy.Position + direction * (enemy.Config.Speed * Time.deltaTime);

				EnemyState updatedEnemy = new EnemyState(enemy.Id, newPosition, enemy.Health, enemy.Config, enemy.LastAttackTime);
				_enemies[enemy.Id] = updatedEnemy;
				OnEnemyPositionChanged?.Invoke(updatedEnemy);
			}
			else
			{
				if (Time.time - enemy.LastAttackTime >= enemy.Config.AttackCooldown)
				{
					_heroController.TakeHit(enemy.Config.AttackDamage);

					EnemyState updatedEnemy = new EnemyState(enemy.Id, enemy.Position, enemy.Health, enemy.Config, Time.time);
					_enemies[enemy.Id] = updatedEnemy;
					OnEnemyAttackPerformed?.Invoke(enemy.Id);
				}
			}
		}
		
		private async UniTask KillEnemy(int enemyId)
		{
			if (!_enemies.Remove(enemyId)) return;
			
			CancellationToken cancellationToken = _deathRemovalCancellationTokenSource.Token;
			OnEnemyDied?.Invoke(enemyId);
			
			await UniTask.Delay(TimeSpan.FromSeconds(EnemiesConfig.Instance.DeathRemovalDelay),
				cancellationToken: cancellationToken, cancelImmediately: true).SuppressCancellationThrow();

			Debug.Log($"Removing Enemy id°{enemyId}'s corpse.");
			OnEnemyRemoved?.Invoke(enemyId);
		}
	}
}
