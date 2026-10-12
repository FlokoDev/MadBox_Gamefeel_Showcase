using Game.GamePlay.Entities;
using Game.GamePlay.Heroes;
using Core.ServicesManager;
using Game.DamageNumbers;
using UnityEngine;

namespace Game.GamePlay.Enemies
{
	public class EnemyView : MonoBehaviour
	{
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

		private void Update()
		{
			if (IsDead || _heroController == null || _heroController.CurrentState.IsDead) return;

			Vector3 heroPosition = _heroController.CurrentState.Position;
			Vector3 direction = (heroPosition - transform.position).normalized;

			if (direction.sqrMagnitude > 0.01f)
			{
				Quaternion targetRotation = Quaternion.LookRotation(direction);
				transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
			}
		}

		private void LateUpdate()
		{
			if (animator == null) return;
			float speed = 0f;
			if (!IsDead && Time.deltaTime > 0f && _moveSpeed > 0f)
			{
				speed = (transform.position - _previousPosition).magnitude / (Time.deltaTime * _moveSpeed);
			}
			_previousPosition = transform.position;
			animator.SetFloat(SpeedHash, Mathf.Clamp01(speed));
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
