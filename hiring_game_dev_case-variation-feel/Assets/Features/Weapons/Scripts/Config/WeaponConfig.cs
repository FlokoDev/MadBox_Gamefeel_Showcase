using UnityEngine;

namespace Game.Weapons
{
	[CreateAssetMenu(fileName = "WeaponX", menuName = "Content/Weapon")]
	public class WeaponConfig : ScriptableObject
	{
		[SerializeField] private string id;
		[SerializeField] private int damage;
		[SerializeField] private float range;
		[Tooltip("Splash radius around the targeted enemy. Zero keeps attacks single-target.")]
		[SerializeField, Min(0f)] private float aoeRadius;
		[SerializeField, Min(0f)] private float knockbackDistance = 1f;
		[SerializeField, Min(0.01f)] private float knockbackDuration = 0.15f;
		[SerializeField] private float cooldown;
		[SerializeField] private WeaponView prefab;

		public string Id => id;
		public int Damage => damage;
		public float Range => range;
		public float AoeRadius => Mathf.Max(0f, aoeRadius);
		public float KnockbackDistance => Mathf.Max(0f, knockbackDistance);
		public float KnockbackDuration => Mathf.Max(0.01f, knockbackDuration);
		public float Cooldown => cooldown;
		public WeaponView Prefab => prefab;
	}
}
