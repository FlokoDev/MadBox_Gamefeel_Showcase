using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.GamePlay.Heroes;
using Game.Generic.Tweening;
using UnityEngine;

namespace Game.GamePlay.Enemies
{
	public class EnemiesController
	{
		private HeroController _heroController;
		private TweenService _tweenService;

		// Events
		public event Action<EnemyState> OnEnemySpawned;
		public event Action<int> OnEnemyRemoved;
		public event Action<EnemyState> OnEnemyPositionChanged;
		public event Action<EnemyDamageEvent> OnEnemyDamaged;
		public event Action<int> OnEnemyAttackPerformed;
		public event Action<int> OnEnemyDied;

		// State
		private Dictionary<int, EnemyState> _enemies;
		private readonly Dictionary<int, TweenHandle> _appliedForces = new Dictionary<int, TweenHandle>();
		private CancellationTokenSource _cancellationTokenSource;
		private CancellationTokenSource _deathRemovalCancellationTokenSource;
		private int _nextEnemyId;

		public IReadOnlyDictionary<int, EnemyState> Enemies => _enemies;
		public UniTask<bool> Initialize(HeroController heroController, TweenService tweenService)
		{
			_heroController = heroController;
			_tweenService = tweenService;

			_enemies = new Dictionary<int, EnemyState>();
			StopAppliedForces();
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
			StopAppliedForces();

			return UniTask.CompletedTask;
		}

		public bool IsEnemyStaggered(int enemyId) => _appliedForces.TryGetValue(enemyId, out TweenHandle force) && force != null && force.IsActive;

		public void ClearAllEnemies()
		{
			StopAppliedForces();
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
			StopAppliedForce(enemyId);
			if (_enemies.Remove(enemyId))
			{
				OnEnemyRemoved?.Invoke(enemyId);
			}
		}

		public void AttackEnemy(EnemyState enemyState, int damage, Vector3 sourcePosition,
			float knockbackDistance, float knockbackDuration)
		{
			if (damage <= 0 || !_enemies.TryGetValue(enemyState.Id, out enemyState)) return;

			int newHealth = enemyState.Health - damage;
			Debug.Log($"Attacked enemy id°{enemyState.Id}. Health : {enemyState.Health} -> {newHealth}");
			OnEnemyDamaged?.Invoke(new EnemyDamageEvent(enemyState.Id, damage));

			if (newHealth <= 0)
			{
				KillEnemy(enemyState.Id).Forget();
				return;
			}

			_enemies[enemyState.Id] = new EnemyState(enemyState.Id, enemyState.Position, newHealth,
				enemyState.Config, curveSide: enemyState.CurveSide);
			ApplyForce(enemyState, sourcePosition, knockbackDistance, knockbackDuration);
		}

		public void AOEAttack(Vector3 center, float radius, int damage, Vector3 sourcePosition,
			float knockbackDistance, float knockbackDuration)
		{
			List<EnemyState> targets = new List<EnemyState>();
			float radiusSquared = radius * radius;

			foreach (EnemyState enemy in _enemies.Values)
			{
				Vector3 offset = enemy.Position - center;
				offset.y = 0f;
				if (offset.sqrMagnitude <= radiusSquared) targets.Add(enemy);
			}
			foreach (EnemyState enemy in targets) AttackEnemy(enemy, damage, sourcePosition, knockbackDistance, knockbackDuration);
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
			float curveSide = UnityEngine.Random.value < 0.5f ? -1f : 1f;
			EnemyState newEnemy = new EnemyState(enemyId, spawnPosition, enemyConfig.InitialHealth,
				enemyConfig, curveSide: curveSide);

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
			if (IsEnemyStaggered(enemy.Id)) return;
			
			Vector3 heroPosition = _heroController.CurrentState.Position;
			float distanceToHero = Vector3.Distance(enemy.Position, heroPosition);

			if (distanceToHero > enemy.Config.AttackRange)
			{
				Vector3 direction = GetMovementDirection(enemy, heroPosition);
				Vector3 newPosition = enemy.Position + direction * (enemy.Config.Speed * Time.deltaTime);
				SetEnemyPosition(enemy.Id, newPosition);
				return;
			}
			AttackHero(enemy);
		}

		private void AttackHero(EnemyState enemy)
		{
			if (Time.time - enemy.LastAttackTime < enemy.Config.AttackCooldown) return;
			
			_heroController.TakeHit(enemy.Config.AttackDamage);
			EnemyState updatedEnemy = new EnemyState(enemy.Id, enemy.Position, enemy.Health, enemy.Config, Time.time, enemy.CurveSide);
			_enemies[enemy.Id] = updatedEnemy;
			OnEnemyAttackPerformed?.Invoke(enemy.Id);
		}

		private Vector3 GetMovementDirection(EnemyState enemy, Vector3 heroPosition)
		{
			Vector3 toHero = heroPosition - enemy.Position;
			Vector3 forward = toHero.normalized;
			Vector3 sideways = Vector3.Cross(Vector3.up, forward);
			float influence = Mathf.Clamp01((toHero.magnitude - enemy.Config.AttackRange)
				/ enemy.Config.CurveFadeDistance);
			return (forward + sideways * (enemy.Config.CurveStrength * influence * enemy.CurveSide)).normalized;
		}

		private void SetEnemyPosition(int enemyId, Vector3 position)
		{
			if (_heroController.CurrentState.IsDead || !_enemies.TryGetValue(enemyId, out EnemyState enemy)) return;
			
			EnemyState updatedEnemy = new EnemyState(enemy.Id, position, enemy.Health, enemy.Config, enemy.LastAttackTime, enemy.CurveSide);
			_enemies[enemyId] = updatedEnemy;
			OnEnemyPositionChanged?.Invoke(updatedEnemy);
		}

		private void ApplyForce(EnemyState enemy, Vector3 sourcePosition, float distance, float duration)
		{
			if (distance <= 0f) return;
			Vector3 direction = enemy.Position - sourcePosition;
			direction.y = 0f;
			direction.Normalize();
			if (direction == Vector3.zero) return;

			StopAppliedForce(enemy.Id);
			_appliedForces[enemy.Id] = _tweenService.DOMove(enemy.Position,
				enemy.Position + direction * distance, duration,
				position => SetEnemyPosition(enemy.Id, position),
				() => _appliedForces.Remove(enemy.Id));
		}

		private void StopAppliedForce(int enemyId)
		{
			if (_appliedForces.Remove(enemyId, out TweenHandle force)) force?.Stop();
		}

		private void StopAppliedForces()
		{
			foreach (TweenHandle force in _appliedForces.Values) force?.Stop();
			_appliedForces.Clear();
		}

		private async UniTask KillEnemy(int enemyId)
		{
			if (!_enemies.Remove(enemyId)) return;
			StopAppliedForce(enemyId);
			
			CancellationToken cancellationToken = _deathRemovalCancellationTokenSource.Token;
			OnEnemyDied?.Invoke(enemyId);
			
			await UniTask.Delay(TimeSpan.FromSeconds(EnemiesConfig.Instance.DeathRemovalDelay),
				cancellationToken: cancellationToken, cancelImmediately: true).SuppressCancellationThrow();

			Debug.Log($"Removing Enemy id°{enemyId}'s corpse.");
			OnEnemyRemoved?.Invoke(enemyId);
		}

	}
}
