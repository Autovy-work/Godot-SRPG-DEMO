using Godot;
using System.Collections.Generic;

namespace CSharpTestGame
{
	public partial class GameManager : Node2D
	{
		// 模块管理器
		private MapManager mapManager;
		private UnitManager unitManager;
		private MovementSystem movementSystem;
		private CombatSystem combatSystem;
		private EnemyAI enemyAI;
		private GameStateManager gameStateManager;
		private DebugManager debugManager;

		// 核心组件
		private TurnManager turnManager;
		private Unit selectedUnit = null;
		private RichTextLabel battleLog;
		private Label turnLabel;
		private bool hasMoved = false;
		private bool hasAttacked = false;
		private Node2D mapLayer;

		public override void _Ready()
		{
			// 创建地图层，添加到MapContainer中
			mapLayer = new Node2D();
			mapLayer.Name = "MapLayer";
			// 调整地图位置，使其在MapContainer内紧贴边缘
			mapLayer.Position = new Vector2(0, 0); // 无偏移，紧贴边缘
			// 获取MapContainer节点
			var canvasLayerForMap = GetNode<CanvasLayer>("CanvasLayer");
			if (canvasLayerForMap != null)
			{
				GD.Print("Found CanvasLayer");
				var ui = canvasLayerForMap.GetNode<Control>("UI");
				if (ui != null)
				{
					GD.Print("Found UI");
					var mapContainer = ui.GetNode<Control>("MapContainer");
					if (mapContainer != null)
					{
						GD.Print("Found MapContainer, adding mapLayer");
						mapContainer.AddChild(mapLayer); // 加到MapContainer
					}
					else
					{
						GD.Print("MapContainer not found");
					}
				}
				else
				{
					GD.Print("UI not found");
				}
			}
			else
			{
				GD.Print("CanvasLayer not found");
			}

			// 显示战斗日志
			battleLog = GetNode<RichTextLabel>("CanvasLayer/UI/BattleLog");
			if (battleLog != null)
			{
				battleLog.ScrollFollowing = true;
				battleLog.AppendText("游戏开始！\n");
			}

			// 初始化模块
			InitializeModules();
			
			// 连接操作按钮信号
			GD.Print("Connecting action buttons...");
			var canvasLayer = GetNode<CanvasLayer>("CanvasLayer");
			if (canvasLayer != null)
			{
				GD.Print("Found CanvasLayer");
				var ui = canvasLayer.GetNode<Control>("UI");
				if (ui != null)
				{
					GD.Print("Found UI");
					var actionPanel = ui.GetNode<Control>("ActionPanel");
					if (actionPanel != null)
					{
						GD.Print("Found ActionPanel");
						var meleeButton = actionPanel.GetNode<Button>("MeleeAttackButton");
						if (meleeButton != null)
						{
							GD.Print("Found MeleeAttackButton");
							meleeButton.Pressed += OnMeleeAttackPressed;
							GD.Print("Melee attack button connected");
						}
						else
						{
							GD.Print("MeleeAttackButton not found");
						}
						
						var rangedButton = actionPanel.GetNode<Button>("RangedAttackButton");
						if (rangedButton != null)
						{
							GD.Print("Found RangedAttackButton");
							rangedButton.Pressed += OnRangedAttackPressed;
							GD.Print("Ranged attack button connected");
						}
						else
						{
							GD.Print("RangedAttackButton not found");
						}
						
						var endTurnButton = actionPanel.GetNode<Button>("EndTurnButton");
				if (endTurnButton != null)
				{
					GD.Print("Found EndTurnButton");
					endTurnButton.Pressed += OnEndTurnPressed;
					GD.Print("End turn button connected");
				}
				else
				{
					GD.Print("EndTurnButton not found");
				}
				
				// 找到回合标签
				turnLabel = ui.GetNode<Label>("TurnLabel");
				if (turnLabel != null)
				{
					GD.Print("Found TurnLabel");
				}
				else
				{
					GD.Print("TurnLabel not found");
				}
			}
			else
			{
				GD.Print("ActionPanel not found");
			}
		}
		else
		{
			GD.Print("UI not found");
		}
	}
	else
	{
		GD.Print("CanvasLayer not found");
	}
			
			// 启用鼠标捕获
			Input.MouseMode = Input.MouseModeEnum.Visible;
			// 开始游戏循环
			StartGameLoop();
		}

		private void InitializeModules()
		{
			// 初始化地图管理器
			mapManager = new MapManager(mapLayer);
			mapManager.Initialize(10, 10);

			// 初始化单位管理器
			unitManager = new UnitManager(mapLayer);
			unitManager.Initialize();
			// 绘制玩家单位
			unitManager.DrawUnits();

			// 初始化移动系统
			movementSystem = new MovementSystem(mapManager.Grid, unitManager, mapLayer);

			// 生成敌人
			unitManager.GenerateRandomEnemies(3, mapManager.Grid);

			// 生成障碍物
			mapManager.GenerateRandomObstacles(5, unitManager.Units);

			// 初始化回合管理器
			turnManager = new TurnManager();
			turnManager.SetUnits(unitManager.Units);
			turnManager.TurnChange += OnTurnChange;

			// 初始化游戏状态管理器
			gameStateManager = new GameStateManager(unitManager, battleLog);

			// 初始化战斗系统
			combatSystem = new CombatSystem(mapManager.Grid, unitManager, mapLayer, battleLog, gameStateManager);

			// 初始化敌人AI
			enemyAI = new EnemyAI(mapManager.Grid, unitManager, movementSystem, combatSystem, mapLayer, turnManager);

			// 初始化调试管理器
		debugManager = new DebugManager(unitManager, this);
		debugManager.InitializeDebugMenu();
	}

	// 公开方法，用于更新回合管理器的单位列表
	public void UpdateTurnManagerUnits()
	{
		if (turnManager != null)
		{
			turnManager.SetUnits(unitManager.Units);
			GD.Print("Turn manager units updated: " + unitManager.Units.Count + " units");
		}
	}

	// 公开属性，用于获取地图管理器
	public MapManager MapManager
	{
		get { return mapManager; }
	}

		public override void _Process(double delta)
		{
			// 检测战斗是否结束
			if (!gameStateManager.IsBattleOver)
			{
				gameStateManager.CheckBattleEnd();
			}
		}

		public override void _Input(InputEvent @event)
		{
			if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
			{
				// 获取鼠标点击位置
				var mousePos = mouseEvent.Position;
				GD.Print("Mouse clicked at: " + mousePos);
				
				// 检查是否点击了操作面板按钮
				var canvasLayer = GetNode<CanvasLayer>("CanvasLayer");
				if (canvasLayer != null)
				{
					var ui = canvasLayer.GetNode<Control>("UI");
					if (ui != null)
					{
						var actionPanel = ui.GetNode<Control>("ActionPanel");
						if (actionPanel != null)
						{
							// 使用全局坐标进行检测
							var actionPanelGlobalRect = actionPanel.GetGlobalRect();
							if (actionPanelGlobalRect.HasPoint(mousePos))
							{
								GD.Print("Action panel button clicked, skipping _input");
								return;
							}
						}
					}
				}
				
				// 检查是否点击了单位
				foreach (var child in mapLayer.GetChildren())
				{
					if (child.HasMeta("unit") && child is Control control)
					{
						// 计算单位的世界坐标
						var unitWorldPos = control.GetGlobalPosition();
						var unitRect = new Rect2(unitWorldPos, control.Size);
						if (unitRect.HasPoint(mousePos))
						{
							GD.Print("Unit clicked: " + child.Name);
							var unit = child.GetMeta("unit").As<Unit>();
							
							// 无论点击的是玩家还是敌人，都更新调试菜单选择
						debugManager.SelectUnitInDebugMenu(unit);
						
						// 检查是否处于攻击模式，如果是，且点击的是敌人单位，则触发攻击
						if (selectedUnit != null && !unit.IsPlayer && turnManager.IsPlayerTurn() && !hasAttacked)
						{
							// 计算距离（使用整数位置避免浮点数精度问题）
							var attackerPos = new Vector2(Mathf.FloorToInt(selectedUnit.Position.X), Mathf.FloorToInt(selectedUnit.Position.Y));
							var targetPos = new Vector2(Mathf.FloorToInt(unit.Position.X), Mathf.FloorToInt(unit.Position.Y));
							var distance = Mathf.Abs(attackerPos.X - targetPos.X) + Mathf.Abs(attackerPos.Y - targetPos.Y);
							
							// 确定攻击范围
						int attackRange = selectedUnit.GetEffectiveAttackRange();
						int minDistance = 1;
						
						// 精英单位特殊处理：同时拥有近战和远程攻击能力
						if (selectedUnit.Class == Unit.UnitClass.Elite)
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
								GD.Print("Enemy out of attack range! Distance: " + distance + ", Min: " + minDistance + ", Max: " + attackRange);
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
						Mathf.FloorToInt(mapLocalPos.X / 56),
						Mathf.FloorToInt(mapLocalPos.Y / 56)
					);
					GD.Print("Map local pos: " + mapLocalPos + ", Grid pos: " + gridPos);
					
					// 检查格子是否有效且可移动
					if (mapManager.Grid.IsValidPosition(gridPos) && unitManager.IsCellFree(gridPos))
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

		private void StartGameLoop()
		{
			// 开始游戏循环
			OnTurnChange(turnManager.GetCurrentUnit(), turnManager.GetRoundCount());
		}

		private void OnTurnChange(Unit currentUnit, int roundCount)
		{
			GD.Print("Turn changed to: " + currentUnit + ", Round: " + roundCount);
			// 清除高亮
			combatSystem.ClearHighlights();
			// 重置移动和攻击状态
			hasMoved = false;
			hasAttacked = false;
			// 清除选中单位
			selectedUnit = null;
			
			// 显示回合信息
			if (battleLog != null)
			{
				if (currentUnit.IsPlayer)
				{
					battleLog.AppendText($"回合 {roundCount}: 玩家回合\n");
				}
				else
				{
					battleLog.AppendText($"回合 {roundCount}: 敌人 {unitManager.GetUnitClassName(currentUnit.Class)} 回合\n");
				}
			}
			
			// 更新回合标签
			if (turnLabel != null)
			{
				if (currentUnit.IsPlayer)
				{
					turnLabel.Text = "待玩家行动";
				}
				else
				{
					turnLabel.Text = "敌人行动中";
				}
				GD.Print("Turn label updated to: " + turnLabel.Text);
			}
			
			// 如果是敌人回合，执行敌人AI
			if (!currentUnit.IsPlayer)
			{
				enemyAI.ScheduleEnemyTurn(currentUnit);
			}
		}

		private void SelectUnit(Unit unit)
		{
			GD.Print("Selecting unit: " + unit);
			// 清除之前的高亮
			combatSystem.ClearHighlights();
			movementSystem.ClearHighlights();
			// 设置当前选中单位
			selectedUnit = unit;
			GD.Print("Selected unit set: " + selectedUnit);
			// 只有在玩家还没移动时才显示可移动范围
			if (!hasMoved)
			{
				// 计算可移动范围并高亮
				movementSystem.ShowMovementRange(unit);
			}
			else
			{
				GD.Print("Player has already moved this turn, not showing movable cells");
				// 直接返回，不显示任何高亮
				return;
			}
		}

		private void OnMeleeAttackPressed()
		{
			GD.Print("Melee attack button pressed");
			if (turnManager.IsPlayerTurn() && selectedUnit != null && !hasAttacked)
			{
				// 检查单位类型是否支持近战攻击
				if (selectedUnit.Class == Unit.UnitClass.Ranged)
				{
					GD.Print("Melee attack: Ranged units cannot use melee attacks");
					return;
				}
				
				GD.Print("Melee attack: player turn and unit selected");
				combatSystem.ClearHighlights();
				movementSystem.ClearHighlights(); // 清除移动范围动画
				combatSystem.ShowAttackRange(selectedUnit, 1); // 近战攻击范围为1
			}
			else
			{
				if (hasAttacked)
				{
					GD.Print("Melee attack: player has already attacked this turn");
					// 显示攻击提示
					if (battleLog != null)
					{
						battleLog.AppendText("该回合已攻击过！\n");
					}
				}
				else
				{
					GD.Print("Melee attack: no unit selected or not player turn");
				}
			}
		}

		private void OnRangedAttackPressed()
		{
			GD.Print("Ranged attack button pressed");
			if (turnManager.IsPlayerTurn() && selectedUnit != null && !hasAttacked)
			{
				// 检查单位类型是否支持远程攻击
				if (selectedUnit.Class == Unit.UnitClass.Melee)
				{
					GD.Print("Ranged attack: Melee units cannot use ranged attacks");
					return;
				}
				
				GD.Print("Ranged attack: player turn and unit selected");
				combatSystem.ClearHighlights();
				movementSystem.ClearHighlights(); // 清除移动范围动画
				combatSystem.ShowAttackRange(selectedUnit, selectedUnit.GetEffectiveAttackRange()); // 远程攻击范围为单位的攻击范围
			}
			else
			{
				if (hasAttacked)
				{
					GD.Print("Ranged attack: player has already attacked this turn");
					// 显示攻击提示
					if (battleLog != null)
					{
						battleLog.AppendText("该回合已攻击过！\n");
					}
				}
				else
				{
					GD.Print("Ranged attack: no unit selected or not player turn");
				}
			}
		}

		private void OnEndTurnPressed()
		{
			GD.Print("End turn button pressed");
			if (turnManager.IsPlayerTurn())
			{
				GD.Print("Ending player turn");
				combatSystem.ClearHighlights();
				turnManager.NextTurn();
				// 不再直接调用OnTurnChange，因为NextTurn会触发TurnChange事件
			}
			else
			{
				GD.Print("Not player turn, cannot end turn");
			}
		}

		private void OnRestartButtonPressed()
		{
			// 重新加载当前场景
			GetTree().ReloadCurrentScene();
		}

		private void OnQuitButtonPressed()
		{
			// 退出程序
			GetTree().Quit();
		}
	}
}
