using Godot;

namespace CSharpTestGame.AI.Strategies
{
	public class EnemyAIStrategyFactory
	{
		private static UnitManager unitManager;

		public static void SetUnitManager(UnitManager manager)
		{
			unitManager = manager;
		}

		public static IEnemyAIStrategy GetStrategy(Unit.UnitClass unitClass)
		{
			switch (unitClass)
			{
				case Unit.UnitClass.Goblin:
					return new GoblinAIStrategy();
				case Unit.UnitClass.ElfArcher:
					return new ElfArcherAIStrategy();
				case Unit.UnitClass.WarAngel:
					return new WarAngelAIStrategy();
				case Unit.UnitClass.Skeleton:
					return new SkeletonAIStrategy();
				case Unit.UnitClass.Acolyte:
					return new AcolyteAIStrategy(unitManager);
				default:
					return new GoblinAIStrategy(); // 默认使用哥布林策略
			}
		}
	}
}
