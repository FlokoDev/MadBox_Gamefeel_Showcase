using Game.GamePlay.Entities;
using Game.GamePlay.Heroes;
using Core.ServicesManager;
using Game.DamageNumbers;
using UnityEngine;

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

		private HeroController _heroController;
		private Vector3 _previousPosition;
		private float _moveSpeed;

		public bool IsDead { get; private set; }
		public DamageNumberView DamageNumberPrefab => damageNumberPrefab;

		public void Initialize(EnemyState state)
		{
			_previousPosition = transform.position;
			_moveSpeed = state.Config.Speed;
		}

		private void Start()
		{
			ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;
		}

		private void OnServicesInitialized()
		{
			_heroController = ServicesLocator.Instance.GetService<EntitiesService>().HeroController;
		}

		private void LateUpdate()
		{
			Vector3 movement = transform.position - _previousPosition;
			if (!IsDead && _heroController != null && !_heroController.CurrentState.IsDead)
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
			if (!IsDead && Time.deltaTime > 0f && _moveSpeed > 0f)
			{
				speed = movement.magnitude / (Time.deltaTime * _moveSpeed);
			}
			_previousPosition = transform.position;
			if (animator != null) animator.SetFloat(SpeedHash, Mathf.Clamp01(speed));
		}

		public void PlayAttack()
		{
			if (IsDead || animator == null) return;
			animator.SetTrigger(AttackHash);
		}

		public void PlayDeath()
		{
			IsDead = true;
			if (animator == null) return;
			
			animator.SetFloat(SpeedHash, 0f);
			animator.SetBool(IsDeadHash, true);
		}

		private void OnDestroy()
		{
			ServicesLocator.Instance.OnAllServicesInitialized -= OnServicesInitialized;
		}
		
		public void PlayOnHitFeedback()
		{
			rendererFlicker?.Play();
			if (IsDead || animator == null) return;
			
			animator.SetTrigger(HitHash);
		}
	}
}
