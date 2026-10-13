using Game.GamePlay.Entities;
using Game.GamePlay.Enemies;
using Game.JoystickInput;
using Game.Weapons;
using Core.ServicesManager;
using Game.DamageNumbers;
using Game.Generic.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.GamePlay.Heroes
{
	public class HeroView : MonoBehaviour
	{
		private static readonly int SpeedHash = Animator.StringToHash("Speed");
		private static readonly int AttackHash = Animator.StringToHash("Attack");
		private static readonly int HitHash = Animator.StringToHash("Hit");
		private static readonly int IsDeadHash = Animator.StringToHash("IsDead");

		[SerializeField] private Animator animator;
		[SerializeField] private float rotationSpeed = 10f;
		[SerializeField] private Transform weaponSlot;
		[SerializeField] private RendererFlicker rendererFlicker;
		[SerializeField] private DamageNumberView damageNumberPrefab;

		[Header("On Hit Punch Scale")]
		[Tooltip("Relative scale change per axis: negative squashes, positive stretches.")]
		[FormerlySerializedAs("punchStrength")]
		[SerializeField] private Vector3 onHitPunchScaleStrength = new Vector3(0.18f, -0.22f, 0.18f);
		[FormerlySerializedAs("punchDuration")]
		[SerializeField, Min(0.01f)] private float onHitPunchScaleDuration = 0.2f;

		private JoystickInputService _joystickInputService;
		private HeroController _heroController;
		private WeaponsService _weaponsService;
		private DamageNumbersService _damageNumbersService;
		private TweenService _tweenService;
		private Vector2 _currentMovementInput;
		private WeaponView _currentWeaponView;
		private bool _isDead;

		private void Start()
		{
			ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;
		}

		private void OnServicesInitialized()
		{
			_joystickInputService = ServicesLocator.Instance.GetService<JoystickInputService>();
			_heroController = ServicesLocator.Instance.GetService<EntitiesService>().HeroController;
			_weaponsService = ServicesLocator.Instance.GetService<WeaponsService>();
			_damageNumbersService = ServicesLocator.Instance.GetService<DamageNumbersService>();
			_tweenService = ServicesLocator.Instance.GetService<TweenService>();

			_joystickInputService.OnStateChanged += OnJoystickStateChanged;
			_heroController.OnStateChanged += OnHeroStateChanged;
			_heroController.OnDamaged += OnDamaged;
			_heroController.OnAttackPerformed += OnAttackPerformed;
			_weaponsService.OnWeaponChanged += OnWeaponChanged;

			OnJoystickStateChanged(_joystickInputService.CurrentState);
			OnHeroStateChanged(_heroController.CurrentState);
			SpawnCurrentWeapon();
		}

		private void OnDestroy()
		{
			ServicesLocator.Instance.OnAllServicesInitialized -= OnServicesInitialized;
			if (_joystickInputService != null)
			{
				_joystickInputService.OnStateChanged -= OnJoystickStateChanged;
			}
			if (_heroController != null)
			{
				_heroController.OnStateChanged -= OnHeroStateChanged;
				_heroController.OnDamaged -= OnDamaged;
				_heroController.OnAttackPerformed -= OnAttackPerformed;
			}
			if (_weaponsService != null)
			{
				_weaponsService.OnWeaponChanged -= OnWeaponChanged;
			}
			if (_currentWeaponView != null)
			{
				Destroy(_currentWeaponView.gameObject);
			}
		}

		private void OnDisable() => _tweenService?.Stop(transform);

		private void OnJoystickStateChanged(JoystickState state)
		{
			_currentMovementInput = state.IsActive ? state.MovementVector : Vector2.zero;
			UpdateAnimator();
		}

		private void OnHeroStateChanged(HeroState heroState)
		{
			transform.position = heroState.Position;
			if (heroState.Health == HeroConfig.Instance.InitialHealth) rendererFlicker?.Stop();
			if (_isDead != heroState.IsDead)
			{
				_isDead = heroState.IsDead;
				_tweenService?.Stop(transform);
				if (!_isDead) _damageNumbersService.Clear();
				if (animator != null)
				{
					if (!_isDead)
					{
						// Restart restores the initial pose and clears pending attack/hit triggers.
						animator.Rebind();
						animator.Update(0f);
					}
					animator.SetBool(IsDeadHash, _isDead);
				}
			}
			UpdateAnimator();
		}

		private void OnDamaged(int damage)
		{
			rendererFlicker?.Play();
			if (!_isDead) _tweenService?.DOPunchScale(transform, onHitPunchScaleStrength, onHitPunchScaleDuration);
			_damageNumbersService.Show(damageNumberPrefab, transform.position, damage);
			if (_isDead || animator == null) return;
			animator.SetTrigger(HitHash);
		}

		private void OnAttackPerformed(EnemyState target)
		{
			if (_isDead) return;
			Vector3 direction = target.Position - transform.position;
			direction.y = 0f;
			// Alice's model has a 180-degree local rotation, matching the movement orientation.
			if (direction.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(-direction);
			if (animator == null) return;
			animator.SetTrigger(AttackHash);
		}

		private void Update()
		{
			if (_heroController == null || _heroController.CurrentState.IsDead) return;
			if (_currentMovementInput.sqrMagnitude <= 0.01f) return;

			Vector3 movement = new Vector3(-_currentMovementInput.x, 0f, -_currentMovementInput.y);
			Quaternion targetRotation = Quaternion.LookRotation(-movement);
			transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
		}

		private void UpdateAnimator()
		{
			if (animator == null) return;
			if (_heroController is { CurrentState: { IsDead: true } })
			{
				animator.SetFloat(SpeedHash, 0f);
				return;
			}

			float speed = _currentMovementInput.magnitude;
			animator.SetFloat(SpeedHash, speed);
		}

		private void OnWeaponChanged(WeaponConfig newWeapon)
		{
			if (_currentWeaponView != null)
			{
				Destroy(_currentWeaponView.gameObject);
				_currentWeaponView = null;
			}

			SpawnCurrentWeapon();
		}

		private void SpawnCurrentWeapon()
		{
			if (_weaponsService.CurrentWeapon == null) return;

			Transform parent = weaponSlot != null ? weaponSlot : transform;
			_currentWeaponView = Instantiate(_weaponsService.CurrentWeapon.Prefab, parent);
			_currentWeaponView.transform.localPosition = Vector3.zero;
			_currentWeaponView.transform.localRotation = Quaternion.identity;
		}

	}
}
