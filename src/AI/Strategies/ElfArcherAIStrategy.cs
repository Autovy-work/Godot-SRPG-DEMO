using Godot;
using System;

namespace CSharpTestGame.AI.Strategies
{
	public class ElfArcherAIStrategy : BaseEnemyAIStrategy
	{
		public override bool ShouldAttack(Unit enemyUnit, Unit playerUnit)
		{
			int distance = CalculateDistance(enemyUnit.Position, playerUnit.Position);
			int attackRange = enemyUnit.GetEffectiveAttackRange();
			return distance >= 2 && distance <= attackRange; // 精灵弓手只能远程攻击
		}

		public override bool ShouldMove(Unit enemyUnit, Unit playerUnit)
		{
			int distance = CalculateDistance(enemyUnit.Position, playerUnit.Position);
			int attackRange = enemyUnit.GetEffectiveAttackRange();
			return distance < 2 || distance > attackRange; // 距离过近或过远时需要移动
		}

		public override Vector2 CalculateMovePosition(Unit enemyUnit, Unit playerUnit, Grid grid, UnitManager unitManager)
		{
			var moveRange = enemyUnit.GetEffectiveMoveRange();
			var attackRange = enemyUnit.GetEffectiveAttackRange();
			var targetX = playerUnit.Position.X;
			var targetY = playerUnit.Position.Y;
			var currentX = enemyUnit.Position.X;
			var currentY = enemyUnit.Position.Y;

			// 计算当前距离
			var currentDistance = CalculateDistance(enemyUnit.Position, playerUnit.Position);

			// 如果距离玩家为1，需要拉开距离到2-attackRange
			if (currentDistance == 1)
			{
				// 尝试找到一个距离为2~attackRange的位置
				bool foundRetreatPosition = false;
				Vector2 retreatPosition = enemyUnit.Position;

				// 搜索周围可移动的位置
				for (int dx = -moveRange; dx <= moveRange; dx++)
				{
					for (int dy = -moveRange; dy <= moveRange; dy++)
					{
						if (Mathf.Abs(dx) + Mathf.Abs(dy) <= moveRange)
						{
							var newPos = new Vector2(currentX + dx, currentY + dy);
							var newDistance = CalculateDistance(newPos, playerUnit.Position);

							// 检查位置是否有效、可通行且在远程攻击范围内
							if (grid.IsValidPosition(newPos) && unitManager.IsCellFree(newPos) && grid.IsPassable(newPos, unitManager.Units) && newDistance >= 2 && newDistance <= attackRange)
							{
								retreatPosition = newPos;
								foundRetreatPosition = true;
								break;
							}
						}
						if (foundRetreatPosition)
							break;
					}
					if (foundRetreatPosition)
						break;
				}

				if (foundRetreatPosition)
				{
					return retreatPosition;
				}
			}

			// 其他情况：向玩家方向移动到攻击范围内
			// 计算x方向移动
			int moveDirectionX = targetX > currentX ? 1 : targetX < currentX ? -1 : 0;
			var newX = currentX + moveDirectionX * Mathf.Min(moveRange, Mathf.Abs(targetX - currentX));

			// 计算y方向移动
			int moveDirectionY = targetY > currentY ? 1 : targetY < currentY ? -1 : 0;
			var remainingMove = moveRange - Mathf.Abs(newX - currentX);
			var newY = currentY + moveDirectionY * Mathf.Min(remainingMove, Mathf.Abs(targetY - currentY));

			return new Vector2(newX, newY);
		}

		public override void ExecuteAction(Unit enemyUnit, Unit playerUnit, CombatSystem combatSystem, MovementSystem movementSystem, Grid grid, UnitManager unitManager, Node mapLayer, RichTextLabel battleLog, Action onActionComplete)
		{
			if (ShouldAttack(enemyUnit, playerUnit))
			{
				// 显示攻击范围动画
				combatSystem.ShowAttackRangeAnimation(enemyUnit, enemyUnit.GetEffectiveAttackRange());

				// 延迟执行攻击
				var attackTimer = new Timer();
				attackTimer.WaitTime = 1.0f;
				attackTimer.OneShot = true;
				mapLayer.AddChild(attackTimer);
				attackTimer.Timeout += () => {
					// 执行攻击
					combatSystem.AttackUnit(enemyUnit, playerUnit, enemyUnit.GetEffectiveAttackRange());

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
