using Godot;
using System;

namespace CSharpTestGame.AI.Strategies
{
	public interface IEnemyAIStrategy
	{
		bool ShouldAttack(Unit enemyUnit, Unit playerUnit);
		bool ShouldMove(Unit enemyUnit, Unit playerUnit);
		Vector2 CalculateMovePosition(Unit enemyUnit, Unit playerUnit, Grid grid, UnitManager unitManager);
		void ExecuteAction(Unit enemyUnit, Unit playerUnit, CombatSystem combatSystem, MovementSystem movementSystem, Grid grid, UnitManager unitManager, Node mapLayer, RichTextLabel battleLog, Action onActionComplete);
	}
}