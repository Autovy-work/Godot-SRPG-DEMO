using Godot;
using System.Collections.Generic;
using CSharpTestGame.Items;
using CSharpTestGame.Managers;

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
		private InputManager inputManager;
		private UIManager uiManager;
		private ResourceManager resourceManager;
		private EquipmentManager equipmentManager;
		private DataLoader dataLoader;

		// 核心组件
		private TurnManager turnManager;
		private RichTextLabel battleLog;
		private Label turnLabel;
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
			ConnectActionButtons();
			
			// 启用鼠标捕获
			Input.MouseMode = Input.MouseModeEnum.Visible;
			// 开始游戏循环
			StartGameLoop();
		}

		private void InitializeModules()
		{
			// 初始化数据加载器
			dataLoader = new DataLoader();
			dataLoader.LoadData();

			// 初始化地图管理器
			mapManager = new MapManager(mapLayer);
			mapManager.Initialize(dataLoader.GetDefaultMapWidth(), dataLoader.GetDefaultMapHeight());

			// 初始化资源管理器
			resourceManager = new ResourceManager();

			// 初始化装备管理器
			equipmentManager = new EquipmentManager(resourceManager, dataLoader);

			// 初始化单位管理器
			unitManager = new UnitManager(mapLayer, dataLoader, equipmentManager);
			unitManager.Initialize();
			// 绘制玩家单位
			unitManager.DrawUnits();

			// 装备和物品已从JSON文件加载，不需要手动添加
			// foreach (var unit in unitManager.Units)
			// {
			// 	equipmentManager.AddInitialEquipmentAndItems(unit);
			// }

			// 初始化移动系统
			movementSystem = new MovementSystem(mapManager.Grid, unitManager, mapLayer);

			// 生成敌人
			unitManager.GenerateRandomEnemies(dataLoader.GetDefaultEnemyCount(), mapManager.Grid);

			// 生成障碍物
			mapManager.GenerateRandomObstacles(dataLoader.GetDefaultObstacleCount(), unitManager.Units);

			// 初始化回合管理器
			turnManager = new TurnManager();
			turnManager.SetUnits(unitManager.Units);
			turnManager.TurnChange += OnTurnChange;

			// 初始化游戏状态管理器
			gameStateManager = new GameStateManager(unitManager, battleLog);

			// 初始化战斗系统
			combatSystem = new CombatSystem(mapManager.Grid, unitManager, mapLayer, battleLog, gameStateManager);

			// 初始化敌人AI
			enemyAI = new EnemyAI(mapManager.Grid, unitManager, movementSystem, combatSystem, mapLayer, turnManager, battleLog);

			// 初始化调试管理器
			debugManager = new DebugManager(unitManager, this, dataLoader, equipmentManager);
			debugManager.InitializeDebugMenu();

			// 初始化UI管理器
			uiManager = new UIManager();

			// 初始化输入管理器
			inputManager = new InputManager(this, turnManager, unitManager, movementSystem, combatSystem, mapLayer, dataLoader);
		}

		private void ConnectActionButtons()
		{
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
							meleeButton.Pressed += inputManager.OnMeleeAttackPressed;
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
							rangedButton.Pressed += inputManager.OnRangedAttackPressed;
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
							endTurnButton.Pressed += inputManager.OnEndTurnPressed;
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

		// 公开方法，用于获取调试管理器
		public DebugManager GetDebugManager()
		{
			return debugManager;
		}

		// 公开属性，用于获取UI管理器
		public UIManager UIManager
		{
			get { return uiManager; }
		}

		public override void _Process(double delta)
	{
		// 检测战斗是否结束
		if (gameStateManager != null && !gameStateManager.IsBattleOver)
		{
			gameStateManager.CheckBattleEnd();
		}
	}

		public override void _Input(InputEvent @event)
	{
		if (inputManager != null)
		{
			inputManager.HandleInput(@event);
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
			// 重置输入管理器的状态
			inputManager.ResetTurnState();
			
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


	}
}
