using Godot;
using System.Collections.Generic;
using CSharpTestGame.AI.Strategies;

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
		private RichTextLabel battleLog;

		private Timer enemyTurnTimer;
		private bool isEnemyTurnExecuting = false;
		private bool isMoving = false;

		public EnemyAI(Grid grid, UnitManager unitManager, MovementSystem movementSystem, CombatSystem combatSystem, Node2D mapLayer, TurnManager turnManager, RichTextLabel battleLog)
		{
			this.grid = grid;
			this.unitManager = unitManager;
			this.movementSystem = movementSystem;
			this.combatSystem = combatSystem;
			this.mapLayer = mapLayer;
			this.turnManager = turnManager;
			this.battleLog = battleLog;
			// 初始化策略工厂的unitManager
			EnemyAIStrategyFactory.SetUnitManager(unitManager);
			
			// 订阅移动完成事件
			movementSystem.OnMovementCompleted += (unit) => {
				isMoving = false;
				GD.Print("Enemy movement completed, isMoving set to false");
			};
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
			// 检查是否正在移动
			if (isMoving)
			{
				GD.Print("Enemy is moving, skipping turn");
				return;
			}

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
				// 获取对应单位类型的AI策略
				var strategy = EnemyAIStrategyFactory.GetStrategy(enemyUnit.Class);

				// 检查是否应该攻击
				bool shouldAttack = strategy.ShouldAttack(enemyUnit, playerUnit);
				// 检查是否应该移动
				bool shouldMove = strategy.ShouldMove(enemyUnit, playerUnit);

				// 执行攻击
				if (shouldAttack)
				{
					strategy.ExecuteAction(enemyUnit, playerUnit, combatSystem, movementSystem, grid, unitManager, mapLayer, battleLog, () => EndEnemyTurn(enemyUnit));
				}
				// 移动（如果选择移动）
				else if (shouldMove)
				{
					// 计算移动目标位置
					var targetPos = strategy.CalculateMovePosition(enemyUnit, playerUnit, grid, unitManager);

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
						// 设置移动中标志
						isMoving = true;
						// 使用新的路径移动系统
						movementSystem.ShowPathAnimation(path);
						movementSystem.StartPathMovement(enemyUnit, path);
						GD.Print("Enemy move started");

						// 移动后直接结束回合，不检查攻击
						var endTurnTimer = new Timer();
						endTurnTimer.WaitTime = 2.0f; // 等待移动动画完成
						endTurnTimer.OneShot = true;
						mapLayer.AddChild(endTurnTimer);
						endTurnTimer.Timeout += () => {
							EndEnemyTurn(enemyUnit);
							moveTimer.QueueFree();
							endTurnTimer.QueueFree();
						};
						endTurnTimer.Start();
					};
					moveTimer.Start();
				}
				// 如果既不攻击也不移动，调用ExecuteAction方法（例如生命法师的治疗）
				else
				{
					strategy.ExecuteAction(enemyUnit, playerUnit, combatSystem, movementSystem, grid, unitManager, mapLayer, battleLog, () => EndEnemyTurn(enemyUnit));
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
	}
}
