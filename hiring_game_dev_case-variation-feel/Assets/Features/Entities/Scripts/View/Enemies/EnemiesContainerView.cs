using System.Collections.Generic;
using Game.GamePlay.Entities;
using Core.ServicesManager;
using UnityEngine;

namespace Game.GamePlay.Enemies
{
	public class EnemiesContainerView : MonoBehaviour
	{
		private EnemiesController _enemiesController;
		private Dictionary<int, EnemyView> _enemyViews;

		private void Start()
		{
			ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;
		}

		private void OnServicesInitialized()
		{
			_enemiesController = ServicesLocator.Instance.GetService<EntitiesService>().EnemiesController;
			_enemyViews = new Dictionary<int, EnemyView>();

			_enemiesController.OnEnemySpawned += OnEnemySpawned;
			_enemiesController.OnEnemyRemoved += OnEnemyRemoved;
			
			_enemiesController.OnEnemyPositionChanged += OnEnemyPositionChanged;
			_enemiesController.OnEnemyAttackPerformed += OnEnemyAttackPerformed;
			
			_enemiesController.OnEnemyDamaged += OnEnemyDamaged;
			_enemiesController.OnEnemyDied += OnEnemyDied;
		}

		private void OnEnemySpawned(EnemyState enemyState)
		{
			EnemyView enemyView = Instantiate(enemyState.Config.Prefab, transform);
			enemyView.transform.position = enemyState.Position;
			enemyView.Initialize(enemyState);
			_enemyViews[enemyState.Id] = enemyView;
		}

		private void OnEnemyRemoved(int enemyId)
		{
			if (_enemyViews.Remove(enemyId, out EnemyView enemyView))
			{
				Destroy(enemyView.gameObject);
			}
		}

		private void OnEnemyAttackPerformed(int enemyId)
		{
			if (_enemyViews.TryGetValue(enemyId, out EnemyView view)) view.PlayAttack();
		}

		private void OnEnemyDied(int enemyId)
		{
			if (_enemyViews.TryGetValue(enemyId, out EnemyView view)) view.PlayDeath();
		}

		private void OnEnemyDamaged(int enemyId)
		{
			if (_enemyViews.TryGetValue(enemyId, out EnemyView enemyView))
			{
				enemyView.PlayOnHitFeedback();
			}
		}

		private void OnEnemyPositionChanged(EnemyState enemyState)
		{
			if (_enemyViews.TryGetValue(enemyState.Id, out EnemyView enemyView))
			{
				enemyView.transform.position = enemyState.Position;
			}
		}

		private void OnDestroy()
		{
			ServicesLocator.Instance.OnAllServicesInitialized -= OnServicesInitialized;
			if (_enemiesController != null)
			{
				_enemiesController.OnEnemySpawned -= OnEnemySpawned;
				_enemiesController.OnEnemyRemoved -= OnEnemyRemoved;
				_enemiesController.OnEnemyPositionChanged -= OnEnemyPositionChanged;
				_enemiesController.OnEnemyDamaged -= OnEnemyDamaged;
				_enemiesController.OnEnemyAttackPerformed -= OnEnemyAttackPerformed;
				_enemiesController.OnEnemyDied -= OnEnemyDied;
			}
		}
	}
}
