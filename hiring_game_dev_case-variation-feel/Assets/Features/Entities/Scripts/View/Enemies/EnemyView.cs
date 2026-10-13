using Game.GamePlay.Entities;
using Game.GamePlay.Heroes;
using Core.ServicesManager;
using Game.DamageNumbers;
using Game.Generic.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.GamePlay.Enemies
{
	public class EnemyView : MonoBehaviour
	{
		private const float MinMovementDistance = 0.001f;
		private static readonly int SpeedHash = Animator.StringToHash("Speed");
		private static readonly int AttackHash = Animator.StringToHash("Attack");
		private static readonly int HitHash = Animator.StringToHash("Hit");
		private static readonly int IsDeadHash = Animator.StringToHash("IsDead");

		[SerializeField] private float rotationSpeed = 10f;
		[SerializeField] private RendererFlicker rendererFlicker;
		[SerializeField] private Animator animator;
		[SerializeField] private DamageNumberView damageNumberPrefab;

		[Header("On Hit Punch Scale")]
		[Tooltip("Relative scale change per axis: negative squashes, positive stretches.")]
		[FormerlySerializedAs("punchStrength")]
		[SerializeField] private Vector3 onHitPunchScaleStrength = new Vector3(0.18f, -0.22f, 0.18f);
		[FormerlySerializedAs("punchDuration")]
		[SerializeField, Min(0.01f)] private float onHitPunchScaleDuration = 0.2f;

		private HeroController _heroController;
		private EnemiesController _enemiesController;
		private TweenService _tweenService;
		private int _enemyId;
		private Vector3 _previousPosition;
		private float _moveSpeed;

		public bool IsDead { get; private set; }
		public DamageNumberView DamageNumberPrefab => damageNumberPrefab;

		private void Start()
		{
			ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;
		}

		private void OnServicesInitialized()
		{
			EntitiesService entities = ServicesLocator.Instance.GetService<EntitiesService>();
			_heroController = entities.HeroController;
			_enemiesController = entities.EnemiesController;
			_tweenService = ServicesLocator.Instance.GetService<TweenService>();
		}

		private void LateUpdate()
		{
			Vector3 movement = transform.position - _previousPosition;
			bool isStaggered = _enemiesController != null && _enemiesController.IsEnemyStaggered(_enemyId);
			if (!IsDead && !isStaggered && _heroController != null && !_heroController.CurrentState.IsDead)
			{
				bool hasMoved = movement.sqrMagnitude > MinMovementDistance * MinMovementDistance;
				Vector3 direction = hasMoved ? movement : _heroController.CurrentState.Position - transform.position;
				if (direction.sqrMagnitude > MinMovementDistance * MinMovementDistance)
				{
					Quaternion targetRotation = Quaternion.LookRotation(direction);
					transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
				}
			}
			float speed = 0f;
			if (!IsDead && !isStaggered && Time.deltaTime > 0f && _moveSpeed > 0f)
			{
				speed = movement.magnitude / (Time.deltaTime * _moveSpeed);
			}
			_previousPosition = transform.position;
			if (animator != null) animator.SetFloat(SpeedHash, Mathf.Clamp01(speed));
		}

		private void OnDestroy()
		{
			ServicesLocator.Instance.OnAllServicesInitialized -= OnServicesInitialized;
		}

		private void OnDisable() => _tweenService?.Stop(transform);

		public void Initialize(EnemyState state)
		{
			_enemyId = state.Id;
			_previousPosition = transform.position;
			_moveSpeed = state.Config.Speed;
		}

		public void PlayAttack()
		{
			if (IsDead || animator == null) return;
			animator.SetTrigger(AttackHash);
		}

		public void PlayOnHitFeedback()
		{
			rendererFlicker?.Play();
			_tweenService?.DOPunchScale(transform, onHitPunchScaleStrength, onHitPunchScaleDuration);
			if (IsDead || animator == null) return;

			animator.SetTrigger(HitHash);
		}

		public void PlayDeath()
		{
			IsDead = true;
			if (animator == null) return;

			animator.SetFloat(SpeedHash, 0f);
			animator.SetBool(IsDeadHash, true);
		}

		
	}
}
