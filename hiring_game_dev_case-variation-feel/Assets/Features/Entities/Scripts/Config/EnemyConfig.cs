using UnityEngine;

namespace Game.GamePlay.Enemies
{
	[CreateAssetMenu(fileName = "EnemyX", menuName = "Content/Enemy")]
	public class EnemyConfig : ScriptableObject
	{
		[SerializeField] private string id;
		[SerializeField] private int initialHealth;
		[SerializeField] private float speed;
		[Tooltip("Lateral steering strength while approaching the player. Zero gives a straight pursuit.")]
		[SerializeField, Min(0f)] private float curveStrength = 0.4f;
		[Tooltip("Distance outside attack range over which lateral steering fades to zero.")]
		[SerializeField, Min(0.01f)] private float curveFadeDistance = 4f;
		[SerializeField] private float attackCooldown;
		[SerializeField] private int attackDamage;
		[SerializeField] private float attackRange;
		[SerializeField] private EnemyView prefab;

		public string Id => id;
		public int InitialHealth => initialHealth;
		public float Speed => speed;
		public float CurveStrength => Mathf.Max(0f, curveStrength);
		public float CurveFadeDistance => Mathf.Max(0.01f, curveFadeDistance);
		public float AttackCooldown => attackCooldown;
		public int AttackDamage => attackDamage;
		public float AttackRange => attackRange;
		public EnemyView Prefab => prefab;
	}
}
