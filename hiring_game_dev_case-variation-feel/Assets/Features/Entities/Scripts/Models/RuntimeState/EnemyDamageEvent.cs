namespace Game.GamePlay.Enemies
{
	public readonly struct EnemyDamageEvent
	{
		public int EnemyId { get; }
		public int Damage { get; }

		public EnemyDamageEvent(int enemyId, int damage)
		{
			EnemyId = enemyId;
			Damage = damage;
		}
	}
}
