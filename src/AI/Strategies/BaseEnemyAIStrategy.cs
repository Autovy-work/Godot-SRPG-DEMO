using Godot;
using System;

namespace CSharpTestGame.AI.Strategies
{
	public abstract class BaseEnemyAIStrategy : IEnemyAIStrategy
	{
		protected int CalculateDistance(Vector2 pos1, Vector2 pos2)
		{
			return Mathf.Abs((int)(pos1.X - pos2.X)) + Mathf.Abs((int)(pos1.Y - pos2.Y));
		}

		protected float CalculateHealthRatio(Unit unit)
		{
			return (float)unit.CurrentHealth / unit.MaxHealth;
		}

		public abstract bool ShouldAttack(Unit enemyUnit, Unit playerUnit);
		public abstract bool ShouldMove(Unit enemyUnit, Unit playerUnit);
		public abstract Vector2 CalculateMovePosition(Unit enemyUnit, Unit playerUnit, Grid grid, UnitManager unitManager);
		public abstract void ExecuteAction(Unit enemyUnit, Unit playerUnit, CombatSystem combatSystem, MovementSystem movementSystem, Grid grid, UnitManager unitManager, Node mapLayer, RichTextLabel battleLog, Action onActionComplete);
	}
}