using Godot;
using System;

namespace CSharpTestGame
{
	public class InputManager
	{
		private GameManager gameManager;
		private TurnManager turnManager;
		private UnitManager unitManager;
		private MovementSystem movementSystem;
		private CombatSystem combatSystem;
		private Node2D mapLayer;

		// 状态变量
		private Unit selectedUnit = null;
		private bool hasMoved = false;
		private bool hasAttacked = false;
		private bool isAttackMode = false;

		public InputManager(GameManager gameManager, TurnManager turnManager, UnitManager unitManager, MovementSystem movementSystem, CombatSystem combatSystem, Node2D mapLayer)
		{
			this.gameManager = gameManager;
			this.turnManager = turnManager;
			this.unitManager = unitManager;
			this.movementSystem = movementSystem;
			this.combatSystem = combatSystem;
			this.mapLayer = mapLayer;
		}

		public void HandleInput(InputEvent @event)
		{
			if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
			{
				// 获取鼠标点击位置
				var mousePos = mouseEvent.Position;
				GD.Print("Mouse clicked at: " + mousePos);
				
				// 检查是否点击了UI元素（操作面板或调试面板）
				var canvasLayer = gameManager.GetNode<CanvasLayer>("CanvasLayer");
				if (canvasLayer != null)
				{
					var ui = canvasLayer.GetNode<Control>("UI");
					if (ui != null)
					{
						// 检查是否点击了操作面板
						var actionPanel = ui.GetNode<Control>("ActionPanel");
						if (actionPanel != null)
						{
							// 使用全局坐标进行检测
							var actionPanelGlobalRect = actionPanel.GetGlobalRect();
							if (actionPanelGlobalRect.HasPoint(mousePos))
							{
								GD.Print(Constants.ACTION_PANEL_BUTTON_CLICKED);
								return;
							}
						}
						
						// 检查是否点击了调试面板
						var debugPanel = ui.GetNode<Control>("DebugPanel");
						if (debugPanel != null)
						{
							// 使用全局坐标进行检测
							var debugPanelGlobalRect = debugPanel.GetGlobalRect();
							if (debugPanelGlobalRect.HasPoint(mousePos))
							{
								GD.Print("Debug panel clicked, skipping _input");
								return;
							}
						}
					}
				}
				
				// 检查是否点击了单位
				foreach (var child in mapLayer.GetChildren())
				{
					if (child.HasMeta(Constants.UNIT_META_KEY) && child is Control control)
					{
						// 计算单位的世界坐标
						var unitWorldPos = control.GetGlobalPosition();
						var unitRect = new Rect2(unitWorldPos, control.Size);
						if (unitRect.HasPoint(mousePos))
						{
							GD.Print("Unit clicked: " + child.Name);
							var unit = child.GetMeta(Constants.UNIT_META_KEY).As<Unit>();
							
							// 更新调试菜单中的单位选择
							gameManager.GetDebugManager()?.SelectUnitInDebugMenu(unit);
							
							// 检查是否处于攻击模式，如果是，且点击的是敌人单位，则触发攻击
							if (isAttackMode && selectedUnit != null && !unit.IsPlayer && turnManager.IsPlayerTurn() && !hasAttacked)
							{
								// 计算距离（使用整数位置避免浮点数精度问题）
								var attackerPos = new Vector2(Mathf.FloorToInt(selectedUnit.Position.X), Mathf.FloorToInt(selectedUnit.Position.Y));
								var targetPos = new Vector2(Mathf.FloorToInt(unit.Position.X), Mathf.FloorToInt(unit.Position.Y));
								var distance = Mathf.Abs(attackerPos.X - targetPos.X) + Mathf.Abs(attackerPos.Y - targetPos.Y);
								
								// 确定攻击范围
								int attackRange = selectedUnit.GetEffectiveAttackRange();
								int minDistance = 1;
								
								// 精英单位特殊处理：同时拥有近战和远程攻击能力
								if (selectedUnit.Class == Unit.UnitClass.WarAngel)
								{
									// 距离为1时使用近战攻击
									if (distance == 1)
									{
										attackRange = 1; // 近战攻击范围
										minDistance = 1; // 近战攻击允许距离1
									}
									// 距离大于1时使用远程攻击
									else if (distance > 1)
									{
										attackRange = selectedUnit.GetEffectiveAttackRange(); // 远程攻击范围
										minDistance = 2; // 远程攻击最小距离2
									}
								}
								// 其他单位
								else
								{
									// 对于远程攻击（攻击范围大于1），不包括距离为1的格子
									if (attackRange > 1)
									{
										minDistance = 2;
									}
									// 近战攻击允许距离1
									else if (attackRange == 1 && distance == 1)
									{
										minDistance = 1;
									}
								}
								
								if (distance >= minDistance && distance <= attackRange)
								{
									// 触发攻击
									combatSystem.AttackUnit(selectedUnit, unit);
									combatSystem.ClearHighlights();
									// 标记玩家已攻击
									hasAttacked = true;
									return;
								}
								else
								{
									GD.Print(string.Format(Constants.ENEMY_OUT_OF_ATTACK_RANGE, distance, minDistance, attackRange));
								}
							}
							
							if (turnManager.IsPlayerTurn() && unit.IsPlayer)
							{
								GD.Print("Selecting player unit");
								SelectUnit(unit);
							}
							return;
						}
					}
				}
				
				// 检查是否点击了格子（用于移动）
				if (selectedUnit != null && turnManager.IsPlayerTurn() && !hasMoved)
				{
					// 计算点击的格子坐标（转换为地图局部坐标）
					Vector2 mapLocalPos = mapLayer.GetLocalMousePosition();
					Vector2 gridPos = new Vector2(
						Mathf.FloorToInt(mapLocalPos.X / Constants.TILE_SIZE),
						Mathf.FloorToInt(mapLocalPos.Y / Constants.TILE_SIZE)
					);
					GD.Print("Map local pos: " + mapLocalPos + ", Grid pos: " + gridPos);
					
					// 检查格子是否有效且可移动
					if (gameManager.MapManager.Grid.IsValidPosition(gridPos) && unitManager.IsCellFree(gridPos))
					{
						// 计算路径
						var path = movementSystem.CalculatePath(selectedUnit.Position, gridPos);
						if (path.Count > 1)
						{
							// 检查移动范围
							int moveRange = selectedUnit.GetEffectiveMoveRange();
							if (path.Count - 1 <= moveRange)
							{
								// 执行移动
								movementSystem.MoveUnit(selectedUnit, gridPos);
								combatSystem.ClearHighlights();
								movementSystem.ClearHighlights(); // 清除移动范围动画
								// 标记玩家已移动
								hasMoved = true;
								return;
							}
						}
					}
				}
			}
		}



		public void OnMeleeAttackPressed()
		{
			GD.Print("Melee attack button pressed");
			if (turnManager.IsPlayerTurn() && selectedUnit != null && !hasAttacked)
			{
				// 检查单位类型是否支持近战攻击
				if (selectedUnit.Class == Unit.UnitClass.ElfArcher)
				{
					GD.Print(Constants.MELEE_ATTACK_RANGED_UNITS_CANNOT_USE);
					return;
				}
				
				GD.Print("Melee attack: player turn and unit selected");
				combatSystem.ClearHighlights();
				movementSystem.ClearHighlights(); // 清除移动范围动画
				combatSystem.ShowAttackRange(selectedUnit, 1); // 近战攻击范围为1
				isAttackMode = true; // 进入攻击模式
			}
			else
			{
				if (hasAttacked)
				{
					GD.Print(Constants.MELEE_ATTACK_PLAYER_HAS_ALREADY_ATTACKED);
					// 显示攻击提示
					var battleLog = gameManager.GetNode<RichTextLabel>("CanvasLayer/UI/BattleLog");
					if (battleLog != null)
					{
						battleLog.AppendText(Constants.ALREADY_ATTACKED_MESSAGE);
					}
				}
				else
				{
					GD.Print(Constants.MELEE_ATTACK_NO_UNIT_SELECTED_OR_NOT_PLAYER_TURN);
				}
			}
		}

		public void OnRangedAttackPressed()
		{
			GD.Print("Ranged attack button pressed");
			if (turnManager.IsPlayerTurn() && selectedUnit != null && !hasAttacked)
			{
				// 检查单位类型是否支持远程攻击
				if (selectedUnit.Class == Unit.UnitClass.Goblin)
				{
					GD.Print(Constants.RANGED_ATTACK_MELEE_UNITS_CANNOT_USE);
					return;
				}
				
				GD.Print("Ranged attack: player turn and unit selected");
				combatSystem.ClearHighlights();
				movementSystem.ClearHighlights(); // 清除移动范围动画
				combatSystem.ShowAttackRange(selectedUnit, selectedUnit.GetEffectiveAttackRange()); // 远程攻击范围为单位的攻击范围
				isAttackMode = true; // 进入攻击模式
			}
			else
			{
				if (hasAttacked)
				{
					GD.Print(Constants.RANGED_ATTACK_PLAYER_HAS_ALREADY_ATTACKED);
					// 显示攻击提示
					var battleLog = gameManager.GetNode<RichTextLabel>("CanvasLayer/UI/BattleLog");
					if (battleLog != null)
					{
						battleLog.AppendText(Constants.ALREADY_ATTACKED_MESSAGE);
					}
				}
				else
				{
					GD.Print(Constants.RANGED_ATTACK_NO_UNIT_SELECTED_OR_NOT_PLAYER_TURN);
				}
			}
		}

		public void OnEndTurnPressed()
		{
			GD.Print("End turn button pressed");
			if (turnManager.IsPlayerTurn())
			{
				GD.Print("Ending player turn");
				combatSystem.ClearHighlights();
				movementSystem.ClearHighlights(); // 清除移动范围高亮
				turnManager.NextTurn();
			}
			else
			{
				GD.Print(Constants.END_TURN_NOT_PLAYER_TURN);
			}
		}

		public void ResetTurnState()
		{
			hasMoved = false;
			hasAttacked = false;
			selectedUnit = null;
			isAttackMode = false; // 重置攻击模式状态
		}

		public void SelectUnit(Unit unit)
		{
			GD.Print("Selecting unit: " + unit);
			// 清除之前的高亮
			combatSystem.ClearHighlights();
			movementSystem.ClearHighlights();
			// 设置当前选中单位
			selectedUnit = unit;
			GD.Print("Selected unit set: " + selectedUnit);
			// 重置攻击模式状态
			isAttackMode = false;
			// 只有在玩家还没移动时才显示可移动范围
			if (!hasMoved)
			{
				// 计算可移动范围并高亮
				movementSystem.ShowMovementRange(unit);
			}
			else
			{
				GD.Print(Constants.PLAYER_HAS_ALREADY_MOVED);
				// 直接返回，不显示任何高亮
				return;
			}
		}
	}
}