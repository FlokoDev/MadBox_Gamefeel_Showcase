using Core.ScriptableObjectSingleton;
using System.Collections.Generic;
using UnityEngine;

namespace Game.DamageNumbers
{
	[CreateAssetMenu(fileName = "DamageNumbersConfig", menuName = "Game/DamageNumbersConfig")]
	public class DamageNumbersConfig : ScriptableObjectSingleton<DamageNumbersConfig>
	{
		[Tooltip("Optional list of prefabs whose pools are populated during initialization. Other prefabs are pooled on first use.")]
		[SerializeField] private List<DamageNumberView> prewarmPrefabs = new List<DamageNumberView>();
		[SerializeField, Min(0)] private int prewarmCount = 8;
		[Tooltip("Maximum number of inactive instances retained per prefab.")]
		[SerializeField, Min(1)] private int maxPoolSize = 64;

		public IReadOnlyList<DamageNumberView> PrewarmPrefabs => prewarmPrefabs;
		public int PrewarmCount => Mathf.Max(0, prewarmCount);
		public int MaxPoolSize => Mathf.Max(1, maxPoolSize);
	}
}
