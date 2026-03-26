using Godot;
using System;

namespace CSharpTestGame.AI.Strategies
{
	public class GoblinAIStrategy : BaseEnemyAIStrategy
	{
		public override bool ShouldAttack(Unit enemyUnit, Unit playerUnit)
		{
			int distance = CalculateDistance(enemyUnit.Position, playerUnit.Position);
			return distance == 1; // 哥布林只能近战攻击
		}

		public override bool ShouldMove(Unit enemyUnit, Unit playerUnit)
		{
			int distance = CalculateDistance(enemyUnit.Position, playerUnit.Position);
			return distance > 1; // 距离大于1时需要移动
		}

		public override Vector2 CalculateMovePosition(Unit enemyUnit, Unit playerUnit, Grid grid, UnitManager unitManager)
		{
			var moveRange = enemyUnit.GetEffectiveMoveRange();
			var targetX = playerUnit.Position.X;
			var targetY = playerUnit.Position.Y;
			var currentX = enemyUnit.Position.X;
			var currentY = enemyUnit.Position.Y;

			// 计算当前距离
			int currentDistance = CalculateDistance(enemyUnit.Position, playerUnit.Position);

			// 如果已经在攻击范围内，不需要移动
			if (currentDistance == 1)
			{
				return enemyUnit.Position;
			}

			// 计算x方向移动
			int moveDirectionX = targetX > currentX ? 1 : targetX < currentX ? -1 : 0;
			// 计算y方向移动
			int moveDirectionY = targetY > currentY ? 1 : targetY < currentY ? -1 : 0;

			// 优先向x方向移动
			var newX = currentX + moveDirectionX * Mathf.Min(moveRange, Mathf.Abs(targetX - currentX));
			var remainingMove = moveRange - Mathf.Abs(newX - currentX);
			var newY = currentY + moveDirectionY * Mathf.Min(remainingMove, Mathf.Abs(targetY - currentY));

			// 如果计算结果与当前位置相同（如对角情况），尝试只移动x或y方向
			if (newX == currentX && newY == currentY)
			{
				// 尝试只移动x方向
				if (moveDirectionX != 0)
				{
					newX = currentX + moveDirectionX;
				}
				// 尝试只移动y方向
				else if (moveDirectionY != 0)
				{
					newY = currentY + moveDirectionY;
				}
			}

			// 确保新位置是有效的
			if (!grid.IsValidPosition(new Vector2(newX, newY)) || !unitManager.IsCellFree(new Vector2(newX, newY)) || !grid.IsPassable(new Vector2(newX, newY), unitManager.Units))
			{
				// 如果新位置无效，尝试只移动y方向
				newX = currentX;
				newY = currentY + moveDirectionY * Mathf.Min(moveRange, Mathf.Abs(targetY - currentY));

				// 如果y方向也无效，尝试只移动x方向
				if (!grid.IsValidPosition(new Vector2(newX, newY)) || !unitManager.IsCellFree(new Vector2(newX, newY)) || !grid.IsPassable(new Vector2(newX, newY), unitManager.Units))
				{
					newX = currentX + moveDirectionX * Mathf.Min(moveRange, Mathf.Abs(targetX - currentX));
					newY = currentY;
				}
			}

			return new Vector2(newX, newY);
		}

		public override void ExecuteAction(Unit enemyUnit, Unit playerUnit, CombatSystem combatSystem, MovementSystem movementSystem, Grid grid, UnitManager unitManager, Node mapLayer, RichTextLabel battleLog, Action onActionComplete)
		{
			if (ShouldAttack(enemyUnit, playerUnit))
			{
				// 显示攻击范围动画
				combatSystem.ShowAttackRangeAnimation(enemyUnit, 1);

				// 延迟执行攻击
				var attackTimer = new Timer();
				attackTimer.WaitTime = 1.0f;
				attackTimer.OneShot = true;
				mapLayer.AddChild(attackTimer);
				attackTimer.Timeout += () => {
					// 执行攻击
					combatSystem.AttackUnit(enemyUnit, playerUnit);

					// 延迟结束行动
					var endActionTimer = new Timer();
					endActionTimer.WaitTime = 1.0f;
					endActionTimer.OneShot = true;
					mapLayer.AddChild(endActionTimer);
					endActionTimer.Timeout += () => {
						onActionComplete();
						attackTimer.QueueFree();
						endActionTimer.QueueFree();
					};
					endActionTimer.Start();
				};
				attackTimer.Start();
			}
			else if (ShouldMove(enemyUnit, playerUnit))
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
				// 既不需要攻击也不需要移动，直接结束行动
				onActionComplete();
			}
		}
	}
}