using Godot;
using System;

namespace CSharpTestGame.AI.Strategies
{
	public class AcolyteAIStrategy : BaseEnemyAIStrategy
	{
		private const int HEAL_RANGE = 3; // 生命法师的治疗范围
		private UnitManager unitManager;

		public AcolyteAIStrategy(UnitManager unitManager)
		{
			this.unitManager = unitManager;
		}

		public override bool ShouldAttack(Unit enemyUnit, Unit playerUnit)
		{
			// 生命法师优先治疗，只有在没有需要治疗的敌人时才考虑攻击
			return false;
		}

		public override bool ShouldMove(Unit enemyUnit, Unit playerUnit)
		{
			// 当有需要治疗的友方单位时，不移动
			if (FindLowHealthEnemyInRange(enemyUnit, unitManager) != null)
			{
				return false;
			}
			// 否则尝试远离玩家
			return true;
		}

		public override Vector2 CalculateMovePosition(Unit enemyUnit, Unit playerUnit, Grid grid, UnitManager unitManager)
		{
			var moveRange = enemyUnit.GetEffectiveMoveRange();
			var currentX = enemyUnit.Position.X;
			var currentY = enemyUnit.Position.Y;
			var targetX = playerUnit.Position.X;
			var targetY = playerUnit.Position.Y;

			// 搜索周围可移动的位置，找到距离玩家最远的位置
			Vector2 farthestPosition = enemyUnit.Position;
			int maxDistance = CalculateDistance(enemyUnit.Position, playerUnit.Position);

			// 搜索周围可移动的位置
			for (int dx = -moveRange; dx <= moveRange; dx++)
			{
				for (int dy = -moveRange; dy <= moveRange; dy++)
				{
					if (Mathf.Abs(dx) + Mathf.Abs(dy) <= moveRange)
					{
						var newPos = new Vector2(currentX + dx, currentY + dy);
						var newDistance = CalculateDistance(newPos, playerUnit.Position);

						// 检查位置是否有效、可通行
						if (grid.IsValidPosition(newPos) && unitManager.IsCellFree(newPos) && grid.IsPassable(newPos, unitManager.Units))
						{
							// 找到距离玩家最远的位置
							if (newDistance > maxDistance)
							{
								maxDistance = newDistance;
								farthestPosition = newPos;
							}
						}
					}
				}
			}

			return farthestPosition;
		}

		public override void ExecuteAction(Unit enemyUnit, Unit playerUnit, CombatSystem combatSystem, MovementSystem movementSystem, Grid grid, UnitManager unitManager, Node mapLayer, RichTextLabel battleLog, Action onActionComplete)
		{
			// 优先治疗范围内的低血量敌人
			Unit targetToHeal = FindLowHealthEnemyInRange(enemyUnit, unitManager);
			if (targetToHeal != null)
			{
				// 治疗范围内的低血量敌人
				HealEnemy(enemyUnit, targetToHeal, mapLayer, battleLog, onActionComplete);
				return;
			}

			// 如果没有需要治疗的敌人，尝试远离玩家
			if (ShouldMove(enemyUnit, playerUnit))
			{
				// 计算移动目标位置
				var targetPos = CalculateMovePosition(enemyUnit, playerUnit, grid, unitManager);

				// 检查目标位置是否有效且需要移动
				if (targetPos != enemyUnit.Position)
				{
					// 计算路径并使用路径移动系统
					var path = movementSystem.CalculatePath(enemyUnit.Position, targetPos);
					if (path.Count > 1)
					{
						// 显示路径动画
						movementSystem.ShowPathAnimation(path);
						// 开始路径移动
						movementSystem.StartPathMovement(enemyUnit, path);

						// 延迟结束行动
						var moveTimer = new Timer();
						moveTimer.WaitTime = 1.0f * path.Count;
						moveTimer.OneShot = true;
						mapLayer.AddChild(moveTimer);
						moveTimer.Timeout += () => {
							onActionComplete();
							moveTimer.QueueFree();
						};
						moveTimer.Start();
					}
					else
					{
						// 路径无效，直接结束行动
						onActionComplete();
					}
				}
				else
				{
					// 不需要移动，直接结束行动
					onActionComplete();
				}
			}
			else
			{
				// 既不需要治疗也不需要移动，直接结束行动
				onActionComplete();
			}
		}

		// 查找范围内需要治疗的低血量敌人
		private Unit FindLowHealthEnemyInRange(Unit healer, UnitManager unitManager)
		{
			Unit targetToHeal = null;
			float lowestHealthRatio = 1.0f;

			foreach (var unit in unitManager.Units)
			{
				// 只治疗非玩家单位（包括自己）
				if (!unit.IsPlayer)
				{
					// 计算距离
					int distance = CalculateDistance(unit.Position, healer.Position);
					// 检查是否在治疗范围内且血量低于100%
					if (distance <= HEAL_RANGE && unit.CurrentHealth < unit.MaxHealth)
					{
						float healthRatio = CalculateHealthRatio(unit);
						if (healthRatio < lowestHealthRatio)
						{
							lowestHealthRatio = healthRatio;
							targetToHeal = unit;
						}
					}
				}
			}

			return targetToHeal;
		}

		// 治疗敌人
		private void HealEnemy(Unit healer, Unit target, Node mapLayer, RichTextLabel battleLog, Action onActionComplete)
		{
			// 显示治疗动画
			GD.Print($"生命法师治疗 {target.Class}，恢复生命值");

			// 治疗量为生命法师攻击力的2倍
			int healAmount = healer.Attack * 2;
			// 确保不会超过最大生命值
			int actualHeal = Mathf.Min(healAmount, target.MaxHealth - target.CurrentHealth);
			target.CurrentHealth += actualHeal;

			// 添加战斗日志记录
			if (battleLog != null)
			{
				battleLog.AppendText($"生命法师治疗 {target.Class}，恢复 {actualHeal} 点生命值！\n");
			}

			// 延迟结束行动
			var healTimer = new Timer();
			healTimer.WaitTime = 1.0f;
			healTimer.OneShot = true;
			mapLayer.AddChild(healTimer);
			healTimer.Timeout += () => {
				onActionComplete();
				healTimer.QueueFree();
			};
			healTimer.Start();
		}
	}
}