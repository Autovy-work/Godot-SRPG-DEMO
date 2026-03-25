using Godot;
using System.Collections.Generic;

namespace CSharpTestGame
{
	public class EnemyAI
{
	private Grid grid;
	private UnitManager unitManager;
	private MovementSystem movementSystem;
	private CombatSystem combatSystem;
	private Node2D mapLayer;
	private TurnManager turnManager;

	private Timer enemyTurnTimer;
	private bool isEnemyTurnExecuting = false;

	public EnemyAI(Grid grid, UnitManager unitManager, MovementSystem movementSystem, CombatSystem combatSystem, Node2D mapLayer, TurnManager turnManager)
	{
		this.grid = grid;
		this.unitManager = unitManager;
		this.movementSystem = movementSystem;
		this.combatSystem = combatSystem;
		this.mapLayer = mapLayer;
		this.turnManager = turnManager;
	}

		public void ScheduleEnemyTurn(Unit enemyUnit)
		{
			// 清理之前的定时器（如果存在）
			if (enemyTurnTimer != null && enemyTurnTimer.IsInsideTree())
			{
				enemyTurnTimer.QueueFree();
				enemyTurnTimer = null;
				GD.Print("Previous enemy turn timer cleaned up");
			}

			// 延迟执行敌人逻辑，让玩家看到回合切换
			enemyTurnTimer = new Timer();
			enemyTurnTimer.WaitTime = 1.0f;
			enemyTurnTimer.OneShot = true;
			mapLayer.AddChild(enemyTurnTimer);
			enemyTurnTimer.Timeout += () => ExecuteEnemyTurn(enemyUnit);
			enemyTurnTimer.Start();
			GD.Print("Enemy turn timer started for unit: " + enemyUnit.Position);
		}

		public void ExecuteEnemyTurn(Unit enemyUnit)
		{
			// 重置执行标志，确保每个敌人都能执行回合
			isEnemyTurnExecuting = true;
			GD.Print("Executing enemy turn for unit: " + enemyUnit.Position);

			// 检查敌人是否存活
			if (!enemyUnit.IsAlive())
			{
				GD.Print("Enemy unit is dead, skipping");
				isEnemyTurnExecuting = false;
				return;
			}

			// 寻找玩家单位
			Unit playerUnit = null;
			foreach (var u in unitManager.Units)
			{
				if (u.IsPlayer && u.IsAlive())
				{
					playerUnit = u;
					GD.Print("Found player unit at: " + playerUnit.Position);
					break;
				}
			}

			if (playerUnit != null)
			{
				// 计算距离
				var distance = Mathf.Abs(enemyUnit.Position.X - playerUnit.Position.X) + Mathf.Abs(enemyUnit.Position.Y - playerUnit.Position.Y);
				GD.Print("Distance to player: " + distance);
				var attackRange = enemyUnit.GetEffectiveAttackRange();
				GD.Print("Enemy attack range: " + attackRange);
				// 计算血量比例，用于调整攻击欲望
				float healthRatio = (float)enemyUnit.CurrentHealth / enemyUnit.MaxHealth;
				GD.Print("Enemy health ratio: " + healthRatio);

				// 根据单位类型和距离决定攻击方式
				bool shouldAttack = false;
				bool shouldMove = false;

				// 精英单位特殊处理：同时拥有近战和远程攻击能力
				if (enemyUnit.Class == Unit.UnitClass.WarAngel)
				{
					// 距离为1时，根据血量决定攻击方式
					if (distance == 1)
					{
						// 血量高于50%倾向于近战攻击
						if (healthRatio >= 0.5f)
						{
							shouldAttack = true;
							shouldMove = false;
						}
						// 血量低于50%尝试撤退到远程位置再攻击
						else
						{
							// 尝试找到一个距离为2~attackRange的位置
							bool foundRetreatPosition = false;
							Vector2 retreatPosition = enemyUnit.Position;

							// 搜索周围可移动的位置
							for (int dx = -enemyUnit.MoveRange; dx <= enemyUnit.MoveRange; dx++)
							{
								for (int dy = -enemyUnit.MoveRange; dy <= enemyUnit.MoveRange; dy++)
								{
									if (Mathf.Abs(dx) + Mathf.Abs(dy) <= enemyUnit.MoveRange)
									{
										var newPos = new Vector2(enemyUnit.Position.X + dx, enemyUnit.Position.Y + dy);
										var newDistance = Mathf.Abs(newPos.X - playerUnit.Position.X) + Mathf.Abs(newPos.Y - playerUnit.Position.Y);

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
								// 计算路径并使用路径移动系统
								var path = movementSystem.CalculatePath(enemyUnit.Position, retreatPosition);
								if (path.Count > 1)
								{
									// 显示路径动画
									movementSystem.ShowPathAnimation(path);
									// 开始路径移动
									movementSystem.StartPathMovement(enemyUnit, path);
									// 延迟执行攻击
									var attackTimer = new Timer();
									attackTimer.WaitTime = 1.0f;
									attackTimer.OneShot = true;
									mapLayer.AddChild(attackTimer);
									attackTimer.Timeout += () => {
										// 执行远程攻击
										combatSystem.AttackUnit(enemyUnit, playerUnit);
										// 结束敌人回合
										EndEnemyTurn(enemyUnit);
										attackTimer.QueueFree();
									};
									attackTimer.Start();
									return;
								}
								else
								{
									// 路径无效，直接攻击
									shouldAttack = true;
									shouldMove = false;
								}
							}
							else
							{
								// 找不到合适的撤退位置，退而求其次使用近战攻击
								shouldAttack = true;
								shouldMove = false;
							}
						}
					}
					// 距离大于1时，检查是否在远程攻击范围内
				else if (distance <= attackRange && distance >= 2)
				{
					// 血量高时攻击欲望高
					if (healthRatio > 0.7f)
					{
						shouldAttack = true;
						shouldMove = false;
					}
					// 血量中等时根据距离决定
					else if (healthRatio > 0.3f)
					{
						// 距离越近，攻击欲望越高
						if (distance <= 2)
						{
							shouldAttack = true;
							shouldMove = false;
						}
						else
						{
							// 距离较远，移动到更近的位置
							shouldAttack = false;
							shouldMove = true;
						}
					}
					// 血量低时攻击欲望低，除非距离在有效攻击范围内
					else
					{
						shouldAttack = true;
						shouldMove = false;
					}
				}
					// 距离超出攻击范围，需要移动
					else
					{
						shouldMove = true;
					}
				}
				// 其他单位（远程或近战）
				else
				{
					// 对于近战攻击（攻击范围为1），只能在距离为1时攻击
				// 对于远程攻击（攻击范围大于1），不包括距离为1的格子
				int minDistance = 1;
				if (attackRange > 1)
				{
					minDistance = 2;
				}

				if (distance <= attackRange && distance >= minDistance)
				{
					// 血量高时攻击欲望高
					if (healthRatio > 0.7f)
					{
						shouldAttack = true;
					}
					// 血量中等时根据距离决定
					else if (healthRatio > 0.3f)
					{
						// 距离越近，攻击欲望越高
						shouldAttack = true;
					}
					// 血量低时攻击欲望低，除非距离在有效攻击范围内
					else
					{
						shouldAttack = true;
					}
				}
				// 其他情况都需要移动
				else
				{
					shouldMove = true;
				}
				}

				// 执行攻击
				if (shouldAttack)
				{
					// 确定攻击范围：精英单位距离为1时使用近战攻击范围
					int actualAttackRange = attackRange;
					if (enemyUnit.Class == Unit.UnitClass.WarAngel && distance == 1)
					{
						actualAttackRange = 1; // 近战攻击范围
					}

					// 显示攻击范围动画
					combatSystem.ShowAttackRangeAnimation(enemyUnit, actualAttackRange);

					// 延迟执行攻击
					var attackTimer = new Timer();
					attackTimer.WaitTime = 1.0f;
					attackTimer.OneShot = true;
					mapLayer.AddChild(attackTimer);
					attackTimer.Timeout += () => {
						// 可以攻击，执行攻击
						GD.Print("Enemy attacking player");
						combatSystem.AttackUnit(enemyUnit, playerUnit);
						GD.Print("Enemy attack completed");

						// 延迟结束回合
						var endTurnTimer = new Timer();
						endTurnTimer.WaitTime = 1.0f;
						endTurnTimer.OneShot = true;
						mapLayer.AddChild(endTurnTimer);
						endTurnTimer.Timeout += () => {
							// 结束敌人回合
							EndEnemyTurn(enemyUnit);
							attackTimer.QueueFree();
							endTurnTimer.QueueFree();
						};
						endTurnTimer.Start();
					};
					attackTimer.Start();
				}

				// 移动（如果选择移动）
				if (shouldMove)
				{
					// 需要移动
					GD.Print("Enemy moving towards player");

					// 计算移动目标位置
					var targetPos = CalculateEnemyMovePosition(enemyUnit, playerUnit);

					// 检查目标位置是否有效且需要移动
					if (targetPos == enemyUnit.Position)
					{
						// 不需要移动，直接结束回合
						GD.Print("Enemy does not need to move, ending turn");
						EndEnemyTurn(enemyUnit);
						return;
					}

					// 检查目标位置是否有效
					if (!grid.IsValidPosition(targetPos) || !unitManager.IsCellFree(targetPos) || !grid.IsPassable(targetPos, unitManager.Units))
					{
						// 目标位置无效，直接结束回合
						GD.Print("Enemy target position is invalid, ending turn");
						EndEnemyTurn(enemyUnit);
						return;
					}

					var path = movementSystem.CalculatePath(enemyUnit.Position, targetPos);

					// 检查路径是否有效
					if (path.Count <= 1)
					{
						// 路径无效或不需要移动，直接结束回合
						GD.Print("Enemy path is invalid, ending turn");
						EndEnemyTurn(enemyUnit);
						return;
					}

					// 延迟执行移动
					var moveTimer = new Timer();
					moveTimer.WaitTime = 1.0f;
					moveTimer.OneShot = true;
					mapLayer.AddChild(moveTimer);
					moveTimer.Timeout += () => {
						// 使用新的路径移动系统
						movementSystem.ShowPathAnimation(path);
						movementSystem.StartPathMovement(enemyUnit, path);
						GD.Print("Enemy move completed");

						// 延迟检查是否可以攻击
						var checkAttackTimer = new Timer();
						checkAttackTimer.WaitTime = 1.0f;
						checkAttackTimer.OneShot = true;
						mapLayer.AddChild(checkAttackTimer);
						checkAttackTimer.Timeout += () => {
							// 移动后再次检查是否可以攻击
							var newDistance = Mathf.Abs(enemyUnit.Position.X - playerUnit.Position.X) + Mathf.Abs(enemyUnit.Position.Y - playerUnit.Position.Y);
							GD.Print("Distance after move: " + newDistance);

							// 重新计算攻击欲望
							bool shouldAttackAfterMove = false;

							// 精英单位特殊处理：同时拥有近战和远程攻击能力
							if (enemyUnit.Class == Unit.UnitClass.WarAngel)
							{
								// 距离为1时使用近战攻击
								if (newDistance == 1)
								{
									shouldAttackAfterMove = true;
								}
								// 距离大于1时使用远程攻击
								else if (newDistance <= attackRange && newDistance >= 2)
								{
									// 血量高时攻击欲望高
									if (healthRatio > 0.7f)
									{
										shouldAttackAfterMove = true;
									}
									// 血量中等时，只要在攻击范围内就攻击
									else if (healthRatio > 0.3f)
									{
										shouldAttackAfterMove = true;
									}
									// 血量低时攻击欲望低，除非距离在有效攻击范围内
									else
									{
										shouldAttackAfterMove = true;
									}
								}
							}
							// 其他单位（远程或近战）
							else
							{
								// 对于远程攻击（攻击范围大于1），不包括距离为1的格子
								int minDistance = 1;
								if (attackRange > 1)
								{
									minDistance = 2;
								}

								if (newDistance <= attackRange && newDistance >= minDistance)
								{
									// 血量高时攻击欲望高
									if (healthRatio > 0.7f)
									{
										shouldAttackAfterMove = true;
									}
									// 血量中等时，只要在攻击范围内就攻击
									else if (healthRatio > 0.3f)
									{
										shouldAttackAfterMove = true;
									}
									// 血量低时攻击欲望低，除非距离在有效攻击范围内
									else
									{
										shouldAttackAfterMove = true;
									}
								}
							}

							if (shouldAttackAfterMove)
							{
								// 确定攻击范围：精英单位距离为1时使用近战攻击范围
								int actualAttackRange = attackRange;
								if (enemyUnit.Class == Unit.UnitClass.WarAngel && newDistance == 1)
								{
									actualAttackRange = 1; // 近战攻击范围
								}

								// 显示攻击范围动画
								combatSystem.ShowAttackRangeAnimation(enemyUnit, actualAttackRange);

								// 延迟执行攻击
								var attackAfterMoveTimer = new Timer();
								attackAfterMoveTimer.WaitTime = 1.0f;
								attackAfterMoveTimer.OneShot = true;
								mapLayer.AddChild(attackAfterMoveTimer);
								attackAfterMoveTimer.Timeout += () => {
									// 移动后可以攻击，执行攻击
									GD.Print("Enemy attacking player after move");
									combatSystem.AttackUnit(enemyUnit, playerUnit);
									GD.Print("Enemy attack after move completed");

									// 延迟结束回合
									var endTurnAfterAttackTimer = new Timer();
									endTurnAfterAttackTimer.WaitTime = 1.0f;
									endTurnAfterAttackTimer.OneShot = true;
									mapLayer.AddChild(endTurnAfterAttackTimer);
									endTurnAfterAttackTimer.Timeout += () => {
										// 结束敌人回合
										EndEnemyTurn(enemyUnit);
										moveTimer.QueueFree();
										checkAttackTimer.QueueFree();
										attackAfterMoveTimer.QueueFree();
										endTurnAfterAttackTimer.QueueFree();
									};
									endTurnAfterAttackTimer.Start();
								};
								attackAfterMoveTimer.Start();
							}
							else
							{
								// 结束敌人回合
								EndEnemyTurn(enemyUnit);
								moveTimer.QueueFree();
								checkAttackTimer.QueueFree();
							}
						};
						checkAttackTimer.Start();
					};
					moveTimer.Start();
				}
				// 如果既不攻击也不移动，直接结束回合
				else if (!shouldAttack)
				{
					EndEnemyTurn(enemyUnit);
				}
			}
			else
			{
				GD.Print("No player unit found");
				// 结束敌人回合
				EndEnemyTurn(enemyUnit);
			}
		}

		private void EndEnemyTurn(Unit enemyUnit)
		{
			// 结束敌人回合
			GD.Print("Ending enemy turn for unit: " + enemyUnit.Position);
			// 重置标志
			isEnemyTurnExecuting = false;
			// 清理定时器
		if (enemyTurnTimer != null && enemyTurnTimer.IsInsideTree())
		{
			enemyTurnTimer.QueueFree();
			GD.Print("Enemy turn timer cleaned up");
		}
		enemyTurnTimer = null;
		
		// 切换到下一个单位的回合
		turnManager.NextTurn();
		GD.Print("Enemy turn ended, switching to next unit");
	}

		private Vector2 CalculateEnemyMovePosition(Unit enemyUnit, Unit playerUnit)
		{
			// 计算敌人移动目标位置
			var moveRange = enemyUnit.GetEffectiveMoveRange();
			var attackRange = enemyUnit.GetEffectiveAttackRange();
			var targetX = playerUnit.Position.X;
			var targetY = playerUnit.Position.Y;
			var currentX = enemyUnit.Position.X;
			var currentY = enemyUnit.Position.Y;

			// 计算当前距离
			var currentDistance = Mathf.Abs(currentX - targetX) + Mathf.Abs(currentY - targetY);
			
			// 对于近战敌人（攻击范围为1），移动到距离玩家为1的位置
			if (attackRange == 1)
			{
				// 计算x方向移动（最多移动到距离玩家1格）
				int moveDirectionX = targetX > currentX ? 1 : targetX < currentX ? -1 : 0;
				var newX = currentX + moveDirectionX * Mathf.Min(moveRange, Mathf.Abs(targetX - currentX) - 1);

				// 计算y方向移动（最多移动到距离玩家1格）
				int moveDirectionY = targetY > currentY ? 1 : targetY < currentY ? -1 : 0;
				var remainingMove = moveRange - Mathf.Abs(newX - currentX);
				var newY = currentY + moveDirectionY * Mathf.Min(remainingMove, Mathf.Abs(targetY - currentY) - 1);

				// 如果计算结果与当前位置相同，尝试只移动x或y方向
				if (newX == currentX && newY == currentY)
				{
					// 尝试只移动x方向
					if (Mathf.Abs(targetX - currentX) > 1)
					{
						newX = currentX + moveDirectionX;
					}
					// 尝试只移动y方向
					else if (Mathf.Abs(targetY - currentY) > 1)
					{
						newY = currentY + moveDirectionY;
					}
				}

				return new Vector2(newX, newY);
			}
			// 对于远程敌人（攻击范围大于1）
			else
			{
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
								var newDistance = Mathf.Abs(newPos.X - targetX) + Mathf.Abs(newPos.Y - targetY);

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
		}

		private void MoveEnemyTowardsPlayer(Unit enemyUnit, Unit playerUnit)
		{
			// 简单的移动逻辑：向玩家方向移动
			var moveRange = enemyUnit.GetEffectiveMoveRange();
			var targetX = playerUnit.Position.X;
			var targetY = playerUnit.Position.Y;
			var currentX = enemyUnit.Position.X;
			var currentY = enemyUnit.Position.Y;

			// 计算x方向移动
			int moveDirectionX = targetX > currentX ? 1 : targetX < currentX ? -1 : 0;
			var newX = currentX + moveDirectionX * Mathf.Min(moveRange, Mathf.Abs(targetX - currentX));

			// 计算y方向移动
			int moveDirectionY = targetY > currentY ? 1 : targetY < currentY ? -1 : 0;
			var remainingMove = moveRange - Mathf.Abs(newX - currentX);
			var newY = currentY + moveDirectionY * Mathf.Min(remainingMove, Mathf.Abs(targetY - currentY));

			// 检查新位置是否有效
			var newPos = new Vector2(newX, newY);
			if (grid.IsValidPosition(newPos) && unitManager.IsCellFree(newPos) && grid.IsPassable(newPos, unitManager.Units))
			{
				// 使用新的路径移动系统
				var path = movementSystem.CalculatePath(enemyUnit.Position, newPos);
				// 显示路径动画
				movementSystem.ShowPathAnimation(path);
				movementSystem.StartPathMovement(enemyUnit, path);
			}
			else
			{
				// 如果直接移动到目标位置不可行，尝试找到最近的可通行位置
				Vector2 closestPos = enemyUnit.Position;
				float closestDistance = float.MaxValue;

				// 搜索移动范围内的所有位置
				for (int dx = -moveRange; dx <= moveRange; dx++)
				{
					for (int dy = -moveRange; dy <= moveRange; dy++)
					{
						if (Mathf.Abs(dx) + Mathf.Abs(dy) <= moveRange)
						{
							Vector2 testPos = new Vector2(currentX + dx, currentY + dy);
							if (grid.IsValidPosition(testPos) && grid.IsPassable(testPos, unitManager.Units))
							{
								float distance = Mathf.Abs(testPos.X - targetX) + Mathf.Abs(testPos.Y - targetY);
								if (distance < closestDistance)
								{
									closestDistance = distance;
									closestPos = testPos;
								}
							}
						}
					}
				}

				// 如果找到可通行位置，移动到那里
				if (closestPos != enemyUnit.Position)
				{
					var path = movementSystem.CalculatePath(enemyUnit.Position, closestPos);
					movementSystem.ShowPathAnimation(path);
					movementSystem.StartPathMovement(enemyUnit, path);
				}
			}
		}
	}
}
