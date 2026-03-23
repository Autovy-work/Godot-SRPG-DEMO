using Godot;
using System.Collections.Generic;

namespace CSharpTestGame
{
	public partial class GameManager : Node2D
	{
		private Grid grid;
		private List<Unit> units;
		private TurnManager turnManager;
		private Unit selectedUnit = null;
		private List<ColorRect> highlightCells = new List<ColorRect>();
		private List<Node> pathAnimationNodes = new List<Node>();
		private RichTextLabel battleLog;
		private bool hasMoved = false;
		private bool hasAttacked = false;
		private Unit currentEditUnit = null;
		private bool isBattleOver = false;
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

			// 初始化游戏
			InitializeGame();
			
			// 显示战斗日志
			battleLog = GetNode<RichTextLabel>("CanvasLayer/UI/BattleLog");
			if (battleLog != null)
			{
				battleLog.ScrollFollowing = true;
				battleLog.AppendText("游戏开始！\n");
			}
			
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

		public override void _Process(double delta)
		{
			// 检测战斗是否结束
			if (!isBattleOver)
			{
				CheckBattleEnd();
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
							// 只有当没有显示攻击范围时才跳过输入
							if (actionPanelGlobalRect.HasPoint(mousePos) && highlightCells.Count == 0)
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
						
						// 切换调试菜单到当前单位
						if (unitSelect != null)
						{
							int unitIndex = units.IndexOf(unit);
							GD.Print("Unit index: " + unitIndex);
							if (unitIndex >= 0)
							{
								GD.Print("Selecting unit in debug menu: " + unitIndex);
								unitSelect.Select(unitIndex);
								OnUnitSelected(unitIndex);
							}
							else
							{
								GD.Print("Unit not found in units list");
							}
						}
						else
						{
							GD.Print("unitSelect is null");
							// 尝试重新初始化调试菜单
							InitializeDebugMenu();
							// 再次尝试切换到当前单位
							if (unitSelect != null)
							{
								int unitIndex = units.IndexOf(unit);
								if (unitIndex >= 0)
								{
									unitSelect.Select(unitIndex);
									OnUnitSelected(unitIndex);
								}
							}
						}
						
						// 检查是否处于攻击模式，如果是，且点击的是敌人单位，则触发攻击
						if (selectedUnit != null && !unit.IsPlayer && turnManager.IsPlayerTurn())
						{
							// 检查是否在攻击范围内
							var distance = Mathf.Abs(selectedUnit.Position.X - unit.Position.X) + Mathf.Abs(selectedUnit.Position.Y - unit.Position.Y);
							int attackRange = selectedUnit.GetEffectiveAttackRange();
							
							// 精英单位特殊处理：距离为1时使用近战攻击范围
							if (selectedUnit.Class == Unit.UnitClass.Elite && distance == 1)
							{
								attackRange = 1; // 近战攻击范围
							}
							
							// 对于远程攻击（攻击范围大于1），不包括距离为1的格子
							int minDistance = 1;
							if (attackRange > 1 && !(selectedUnit.Class == Unit.UnitClass.Elite && distance == 1))
							{
								minDistance = 2;
							}
							
							if (distance >= minDistance && distance <= attackRange)
							{
								// 触发攻击
								AttackUnit(selectedUnit, unit);
								ClearHighlights();
								return;
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
				
				// 检查是否点击了高亮格子
				foreach (var highlight in highlightCells)
				{
					// 计算高亮格子的世界坐标
					var highlightWorldPos = highlight.GetGlobalPosition();
					var highlightRect = new Rect2(highlightWorldPos, highlight.Size);
					if (highlightRect.HasPoint(mousePos))
				{
					GD.Print("Highlight clicked: " + highlight.Name);
					GD.Print("Highlight color: " + highlight.Color);
						// 检查是否是攻击范围高亮（通过颜色判断）
						// 攻击范围高亮的颜色是红色(0.8, 0.2, 0.2, 0.5)或黄色(0.8, 0.8, 0.2, 0.5)
						if (Mathf.Abs(highlight.Color.R - 0.8f) < 0.01f && (Mathf.Abs(highlight.Color.G - 0.2f) < 0.01f || Mathf.Abs(highlight.Color.G - 0.8f) < 0.01f))
						{
							// 是攻击范围高亮，触发攻击
							OnAttackCellClicked(highlight);
						}
						else
						{
							// 是移动范围高亮，触发移动
							var targetPos = highlight.GetMeta("position").As<Vector2>();
							GD.Print("Moving unit to: " + targetPos);
							MoveUnit(selectedUnit, targetPos);
						}
						return;
					}
				}
			}
		}

		private void InitializeGame()
		{
			// 创建地图
			grid = new Grid(10, 10);
			// 创建单位
			units = new List<Unit>();
			// 创建玩家单位（精英类型，两种攻击方式都支持）
			var playerUnit = Unit.Create(15, 5, 4, 4, 6, Unit.UnitClass.Elite, new Vector2(0, 0), true);
			units.Add(playerUnit);

			// 生成3个随机职业敌人
			GenerateRandomEnemies(3);
			// 绘制棋盘
			DrawBoard();
			// 生成随机障碍物
			GenerateRandomObstacles(5);
			// 绘制单位
			DrawUnits();
			// 初始化回合管理器
			turnManager = new TurnManager();
			turnManager.SetUnits(units);
			turnManager.TurnChange += OnTurnChange;
			// 初始化调试菜单
			InitializeDebugMenu();
		}

		private void CheckBattleEnd()
		{
			// 检查胜利条件：所有敌人都死亡
			bool allEnemiesDead = true;
			foreach (var unit in units)
			{
				if (!unit.IsPlayer && unit.IsAlive())
				{
					allEnemiesDead = false;
					break;
				}
			}

			// 检查失败条件：玩家死亡
			bool playerDead = false;
			foreach (var unit in units)
			{
				if (unit.IsPlayer && !unit.IsAlive())
				{
					playerDead = true;
					break;
				}
			}

			if (allEnemiesDead || playerDead)
			{
				isBattleOver = true;
				ShowSettlementMenu(allEnemiesDead);
			}
		}

		private void ShowSettlementMenu(bool isVictory)
		{
			// 创建一个新的CanvasLayer，确保它在所有其他节点之上
			var canvasLayer = new CanvasLayer();
			canvasLayer.Name = "SettlementCanvasLayer";
			AddChild(canvasLayer);

			// 创建背景遮罩
			var blurBackground = new ColorRect();
			blurBackground.Size = GetViewportRect().Size;
			blurBackground.Position = Vector2.Zero;
			blurBackground.Color = new Color(0, 0, 0, 0.8f); // 增加透明度，确保完全盖住背景
			blurBackground.Name = "BlurBackground";
			blurBackground.MouseFilter = Control.MouseFilterEnum.Stop; // 阻止点击穿透
			canvasLayer.AddChild(blurBackground);
			


			// 创建结算菜单面板
			var menuPanel = new ColorRect();
			menuPanel.Size = new Vector2(300, 200);
			menuPanel.Position = (GetViewportRect().Size - menuPanel.Size) / 2;
			menuPanel.Color = new Color(0.2f, 0.2f, 0.2f);
			menuPanel.Name = "SettlementMenu";
			canvasLayer.AddChild(menuPanel);

			// 添加标题
			var titleLabel = new Label();
			titleLabel.Size = new Vector2(300, 50);
			titleLabel.Position = new Vector2(0, 20);
			titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
			titleLabel.VerticalAlignment = VerticalAlignment.Center;
			titleLabel.AddThemeColorOverride("font_color", new Color(1, 1, 1));
			titleLabel.Text = isVictory ? "胜利！" : "失败！";
			titleLabel.Name = "TitleLabel";
			menuPanel.AddChild(titleLabel);

			// 添加Restart按钮
			var restartButton = new Button();
			restartButton.Size = new Vector2(120, 40);
			restartButton.Position = new Vector2(30, 100);
			restartButton.Text = "重新开始";
			restartButton.Name = "RestartButton";
			restartButton.Pressed += OnRestartButtonPressed;
			menuPanel.AddChild(restartButton);

			// 添加Quit按钮
			var quitButton = new Button();
			quitButton.Size = new Vector2(120, 40);
			quitButton.Position = new Vector2(150, 100);
			quitButton.Text = "退出";
			quitButton.Name = "QuitButton";
			quitButton.Pressed += OnQuitButtonPressed;
			menuPanel.AddChild(quitButton);
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

		// 生成随机障碍物
		private void GenerateRandomObstacles(int count)
		{
			for (int i = 0; i < count; i++)
			{
				// 生成随机位置
				Vector2 obstaclePos;
				int attempts = 0;
				const int maxAttempts = 100;
				
				do
				{
					obstaclePos = new Vector2(
						GD.Randi() % (int)grid.GridSize.X,
						GD.Randi() % (int)grid.GridSize.Y
					);
					
					// 检查位置是否有效、不是玩家初始位置、没有障碍物且没有单位
					bool isValid = grid.IsValidPosition(obstaclePos) && 
						obstaclePos != new Vector2(0, 0) && 
						grid.IsPassable(obstaclePos, units);
					
					if (isValid)
					{
						break;
					}
					
					attempts++;
				}
				while (attempts < maxAttempts);
				
				// 如果找不到合适的位置，跳过
				if (attempts >= maxAttempts)
				{
					GD.Print("Failed to find valid position for obstacle");
					continue;
				}
				
				// 设置障碍物
				grid.SetObstacle(obstaclePos, true);
				GD.Print("Generated obstacle at: " + obstaclePos);
				
				// 创建障碍物视觉效果
			Control obstacle;
			Texture2D? obstacleTexture = ResourceLoader.Load<Texture2D>("res://Resources/rock.png");
			if (obstacleTexture != null)
			{
				// 调整图像大小以适应格子
				Image image = obstacleTexture.GetImage();
				if (image != null)
				{
					// 调整图像大小为 54x54
						image.Resize(54, 54);
					// 创建新的纹理
					ImageTexture resizedTexture = ImageTexture.CreateFromImage(image);
					
					var textureObstacle = new TextureRect();
					textureObstacle.Size = new Vector2(54, 54); // 比格子小2像素，显示在边框内
					textureObstacle.Position = new Vector2(obstaclePos.X * 56 + 1, obstaclePos.Y * 56 + 1); // 偏移1像素，显示在边框内
					textureObstacle.Texture = resizedTexture;
					textureObstacle.Name = "Obstacle";
					obstacle = textureObstacle;
				}
				else
				{
					// 如果图像获取失败，使用原始纹理
					var textureObstacle = new TextureRect();
					textureObstacle.Size = new Vector2(54, 54); // 比格子小2像素，显示在边框内
					textureObstacle.Position = new Vector2(obstaclePos.X * 56 + 1, obstaclePos.Y * 56 + 1); // 偏移1像素，显示在边框内
					textureObstacle.Texture = obstacleTexture;
					textureObstacle.Name = "Obstacle";
					obstacle = textureObstacle;
				}
			}
			else
			{
				// 如果图片加载失败，使用默认颜色
				var colorObstacle = new ColorRect();
				colorObstacle.Size = new Vector2(54, 54); // 比格子小2像素，显示在边框内
				colorObstacle.Position = new Vector2(obstaclePos.X * 56 + 1, obstaclePos.Y * 56 + 1); // 偏移1像素，显示在边框内
				colorObstacle.Color = new Color(0.55f, 0.45f, 0.2f, 1.0f); // 棕黄色，明显区别于格子
				colorObstacle.Name = "Obstacle";
				obstacle = colorObstacle;
			}
			mapLayer.AddChild(obstacle);
				
				// 创建障碍物物理碰撞
				var staticBody = new StaticBody2D();
				staticBody.Position = new Vector2(obstaclePos.X * 56, obstaclePos.Y * 56);
				AddChild(staticBody);
				
				var collisionShape = new CollisionShape2D();
				var rectangleShape = new RectangleShape2D();
				rectangleShape.Size = new Vector2(56, 56);
				collisionShape.Shape = rectangleShape;
				staticBody.AddChild(collisionShape);
			}
		}

		private void DrawBoard()
		{
			for (int y = 0; y < grid.GridSize.Y; y++)
			{
				for (int x = 0; x < grid.GridSize.X; x++)
				{
					// 先加边框（底层）
				var border = new ColorRect();
				border.Size = new Vector2(56, 56);
				border.Position = new Vector2(x * 56, y * 56);
				border.Color = new Color(0.15f, 0.15f, 0.15f, 1.0f);
				border.Name = string.Format("Border_{0}_{1}", x, y);
				mapLayer.AddChild(border);

				// 再加格子（在边框上面）
			Control cell;
			Texture2D? grassTexture = ResourceLoader.Load<Texture2D>("res://Resources/grass.png");
			if (grassTexture != null)
			{
				var textureCell = new TextureRect();
				textureCell.Size = new Vector2(54, 54);
				textureCell.Position = new Vector2(x * 56 + 1, y * 56 + 1);
				textureCell.Texture = grassTexture;
				textureCell.Set("stretch_mode", 0); // STRETCH_SCALE - 缩放图像以适应容器大小
				textureCell.Name = string.Format("Cell_{0}_{1}", x, y);
				cell = textureCell;
			}
			else
			{
				// 如果图片加载失败，使用默认颜色
				var colorCell = new ColorRect();
				colorCell.Size = new Vector2(54, 54);
						colorCell.Position = new Vector2(x * 56 + 1, y * 56 + 1);
				colorCell.Color = new Color(0.25f, 0.25f, 0.25f, 1.0f);
				colorCell.Name = string.Format("Cell_{0}_{1}", x, y);
				cell = colorCell;
			}
			mapLayer.AddChild(cell);
				}
			}
		}

		private void DrawUnits()
		{
			// 绘制单位
			foreach (var unit in units)
			{
				DrawUnit(unit);
			}
		}

		private void DrawUnit(Unit unit)
		{
			// 创建单位节点的容器
			Control container;
			// 根据单位类型选择不同的图片
			Texture2D? unitTexture = null;
			if (unit.IsPlayer)
			{
				unitTexture = ResourceLoader.Load<Texture2D>("res://Resources/warrior.png");
			}
			else
			{
				switch (unit.Class)
{
	case Unit.UnitClass.Melee:
		unitTexture = ResourceLoader.Load<Texture2D>("res://Resources/goblin.png");
		break;
	case Unit.UnitClass.Ranged:
	unitTexture = ResourceLoader.Load<Texture2D>("res://Resources/elfmale_ranger.png");
	break;
	case Unit.UnitClass.Elite:
		unitTexture = ResourceLoader.Load<Texture2D>("res://Resources/skeleton.png");
		break;
}
			}
			
			if (unitTexture != null)
			{
				// 调整图像大小以适应格子
				Image image = unitTexture.GetImage();
				if (image != null)
				{
					// 调整图像大小为 56x56
					image.Resize(56, 56);
					// 创建新的纹理
					ImageTexture resizedTexture = ImageTexture.CreateFromImage(image);
					
					var textureContainer = new TextureRect();
					textureContainer.Size = new Vector2(56, 56);
					textureContainer.Position = new Vector2(unit.Position.X * 56, unit.Position.Y * 56);
					textureContainer.Name = string.Format("Unit_{0}", units.IndexOf(unit));
					textureContainer.SetMeta("unit", unit);
					textureContainer.Texture = resizedTexture;
					container = textureContainer;
				}
				else
				{
					// 如果图像获取失败，使用原始纹理
					var textureContainer = new TextureRect();
					textureContainer.Size = new Vector2(56, 56);
					textureContainer.Position = new Vector2(unit.Position.X * 56, unit.Position.Y * 56);
					textureContainer.Name = string.Format("Unit_{0}", units.IndexOf(unit));
					textureContainer.SetMeta("unit", unit);
					textureContainer.Texture = unitTexture;
					container = textureContainer;
				}
			}
			else
			{
				// 如果图片加载失败，使用默认颜色
				var colorContainer = new ColorRect();
				colorContainer.Size = new Vector2(56, 56);
				colorContainer.Position = new Vector2(unit.Position.X * 56 + 4, unit.Position.Y * 56 + 4);
				colorContainer.Name = string.Format("Unit_{0}", units.IndexOf(unit));
				colorContainer.SetMeta("unit", unit);
				colorContainer.Color = unit.IsPlayer ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.8f, 0.2f, 0.2f);
				container = colorContainer;
			}
			
			// 添加点击检测
			var button = new Button();
			button.Size = new Vector2(56, 56);
			button.Position = new Vector2(0, 0);
			button.Flat = true;
			button.MouseFilter = Control.MouseFilterEnum.Stop;
			// 连接信号
			button.Pressed += () => OnUnitButtonPressed(container);
			container.AddChild(button);
			
			mapLayer.AddChild(container);
			// 添加血条
			var hpBar = new ColorRect();
			hpBar.Size = new Vector2(40, 5);
			// 血条放到上方
			hpBar.Position = new Vector2(0, -10);
			hpBar.Color = new Color(0.8f, 0.2f, 0.2f);
			hpBar.Name = "HPBar";
			container.AddChild(hpBar);
			UpdateHPBar(container, unit);
		}

		private void UpdateHPBar(Node unitNode, Unit unit)
		{
			var hpBar = unitNode.GetNode<ColorRect>("HPBar");
			if (hpBar != null)
			{
				hpBar.Scale = new Vector2((float)unit.CurrentHealth / unit.MaxHealth, 1);
			}
		}

		private void OnUnitButtonPressed(Control unitNode)
		{
			GD.Print("Unit button pressed!");
			var unit = unitNode.GetMeta("unit").As<Unit>();
			GD.Print("Unit: " + unit);
			GD.Print("Is player turn: " + turnManager.IsPlayerTurn());
			GD.Print("Is player: " + unit.IsPlayer);
			
			// 切换调试菜单到当前单位
			if (unitSelect != null)
			{
				int unitIndex = units.IndexOf(unit);
				GD.Print("Unit index: " + unitIndex);
				if (unitIndex >= 0)
				{
					GD.Print("Selecting unit in debug menu: " + unitIndex);
					unitSelect.Select(unitIndex);
					OnUnitSelected(unitIndex);
				}
				else
				{
					GD.Print("Unit not found in units list");
				}
			}
			else
			{
				GD.Print("unitSelect is null");
				// 尝试重新初始化调试菜单
				InitializeDebugMenu();
				// 再次尝试切换到当前单位
				if (unitSelect != null)
				{
					int unitIndex = units.IndexOf(unit);
					if (unitIndex >= 0)
					{
						unitSelect.Select(unitIndex);
						OnUnitSelected(unitIndex);
					}
				}
			}
			
			if (turnManager.IsPlayerTurn() && unit.IsPlayer)
			{
				GD.Print("Selecting unit!");
				SelectUnit(unit);
			}
		}

		private void SelectUnit(Unit unit)
		{
			GD.Print("Selecting unit: " + unit);
			// 清除之前的高亮
			ClearHighlights();
			// 设置当前选中单位
			selectedUnit = unit;
			GD.Print("Selected unit set: " + selectedUnit);
			// 只有在玩家还没移动时才显示可移动范围
			if (!hasMoved)
			{
				// 计算可移动范围并高亮
				CalculateMovableCells(unit);
			}
			else
			{
				GD.Print("Player has already moved this turn, not showing movable cells");
				// 直接返回，不显示任何高亮
				return;
			}
		}

		private void CalculateMovableCells(Unit unit)
		{
			GD.Print("Calculating movable cells for unit at: " + unit.Position);
			var moveRange = unit.GetEffectiveMoveRange();
			GD.Print("Move range: " + moveRange);
			int count = 0;
			for (int y = 0; y < grid.GridSize.Y; y++)
			{
				for (int x = 0; x < grid.GridSize.X; x++)
				{
					var pos = new Vector2(x, y);
					var distance = Mathf.Abs(pos.X - unit.Position.X) + Mathf.Abs(pos.Y - unit.Position.Y); // 曼哈顿距离
					if (distance <= moveRange && IsCellFree(pos) && grid.IsPassable(pos, units))
					{
						// 使用A*算法检查实际路径是否可通行
						var path = CalculatePath(unit.Position, pos);
						if (path.Count > 1 && path.Count - 1 <= moveRange)
						{
							GD.Print("Highlighting cell: " + pos);
							HighlightCell(pos);
							count++;
						}
					}
				}
			}
			GD.Print("Total highlighted cells: " + count);
		}

		private List<Vector2> CalculatePath(Vector2 start, Vector2 end)
		{
			// A*算法实现路径查找
			List<Vector2> path = new List<Vector2>();
			
			// 定义四个方向：上、右、下、左
			Vector2[] directions = new Vector2[]
			{
				new Vector2(0, -1), // 上
				new Vector2(1, 0),  // 右
				new Vector2(0, 1),  // 下
				new Vector2(-1, 0)  // 左
			};
			
			// 开放列表和关闭列表
			List<PathNode> openList = new List<PathNode>();
			List<PathNode> closedList = new List<PathNode>();
			
			// 创建起点节点
			PathNode startNode = new PathNode(start, null, 0, Heuristic(start, end));
			openList.Add(startNode);
			
			while (openList.Count > 0)
			{
				// 找到F值最小的节点
				PathNode currentNode = openList[0];
				for (int i = 1; i < openList.Count; i++)
				{
					if (openList[i].F < currentNode.F)
					{
						currentNode = openList[i];
					}
				}
				
				// 从开放列表中移除当前节点，添加到关闭列表
				openList.Remove(currentNode);
				closedList.Add(currentNode);
				
				// 检查是否到达终点
				if (currentNode.Position == end)
				{
					// 回溯路径
					PathNode temp = currentNode;
					while (temp != null)
					{
						path.Add(temp.Position);
						temp = temp.Parent;
					}
					path.Reverse();
					return path;
				}
				
				// 探索四个方向
				foreach (Vector2 dir in directions)
				{
					Vector2 newPos = currentNode.Position + dir;
					
					// 检查位置是否有效且可通行
					if (!grid.IsPassable(newPos, units))
					{
						continue;
					}
					
					// 检查是否在关闭列表中
					bool inClosedList = false;
					foreach (PathNode node in closedList)
					{
						if (node.Position == newPos)
						{
							inClosedList = true;
							break;
						}
					}
					if (inClosedList)
					{
						continue;
					}
					
					// 计算G和H值
					float g = currentNode.G + 1;
					float h = Heuristic(newPos, end);
					
					// 检查是否在开放列表中
					bool inOpenList = false;
					foreach (PathNode node in openList)
					{
						if (node.Position == newPos)
						{
							inOpenList = true;
							if (g < node.G)
							{
								node.G = g;
								node.Parent = currentNode;
							}
							break;
						}
					}
					if (!inOpenList)
					{
						// 创建新节点并添加到开放列表
						PathNode newNode = new PathNode(newPos, currentNode, g, h);
						openList.Add(newNode);
					}
				}
			}
			
			// 如果没有找到路径，返回直接路径
			path.Add(start);
			path.Add(end);
			return path;
		}

		// 启发函数（曼哈顿距离）
		private float Heuristic(Vector2 a, Vector2 b)
		{
			return Mathf.Abs(a.X - b.X) + Mathf.Abs(a.Y - b.Y);
		}

		// 路径节点类
		private class PathNode
		{
			public Vector2 Position { get; set; }
			public PathNode Parent { get; set; }
			public float G { get; set; } // 从起点到当前节点的代价
			public float H { get; set; } // 从当前节点到终点的估计代价
			public float F { get { return G + H; } } // 总代价

			public PathNode(Vector2 position, PathNode parent, float g, float h)
			{
				Position = position;
				Parent = parent;
				G = g;
				H = h;
			}
		}

		private bool IsCellFree(Vector2 pos)
		{
			foreach (var unit in units)
			{
				// 使用整数坐标比较，避免浮点数精度问题
				int unitX = (int)unit.Position.X;
				int unitY = (int)unit.Position.Y;
				int posX = (int)pos.X;
				int posY = (int)pos.Y;
				if (unitX == posX && unitY == posY)
				{
					return false;
				}
			}
			return true;
		}

		private void HighlightCell(Vector2 pos)
		{
			// 创建高亮格子的容器
		var highlight = new ColorRect();
		highlight.Size = new Vector2(56, 56);
		highlight.Position = new Vector2(pos.X * 56, pos.Y * 56);
		highlight.Color = new Color(0.2f, 0.6f, 0.8f, 0.5f);
		highlight.Name = string.Format("Highlight_{0}_{1}", pos.X, pos.Y);
		highlight.SetMeta("position", pos);
		
		// 添加点击检测
		var button = new Button();
		button.Size = new Vector2(56, 56);
		button.Position = new Vector2(0, 0);
		button.Flat = true;
		button.MouseFilter = Control.MouseFilterEnum.Stop;
			// 连接信号
			button.Pressed += () => OnHighlightButtonPressed(highlight);
			highlight.AddChild(button);
			
			mapLayer.AddChild(highlight);
			highlightCells.Add(highlight);
		}

		private void OnHighlightButtonPressed(ColorRect highlight)
		{
			var targetPos = highlight.GetMeta("position").As<Vector2>();
			if (selectedUnit != null)
			{
				// 计算路径
				var path = CalculatePath(selectedUnit.Position, targetPos);
				// 显示路径动画
				ShowPathAnimation(path);
				// 延迟执行移动
				var timer = new Timer();
				timer.WaitTime = 1.0f; // 增加延迟时间，确保动画有足够时间显示
				timer.OneShot = true;
				AddChild(timer);
				timer.Timeout += () => {
					MoveUnit(selectedUnit, targetPos);
					timer.QueueFree();
				};
				timer.Start();
			}
		}

		private void ShowPathAnimation(List<Vector2> path)
		{
			// 清除之前的路径动画
			ClearPathAnimations();
			
			// 显示路径动画
			for (int i = 0; i < path.Count - 1; i++)
			{
				var start = path[i];
				var end = path[i + 1];
				// 创建箭头精灵
			var arrow = new Label();
			arrow.Text = "→";
			arrow.Size = new Vector2(20, 20);
			arrow.Position = new Vector2(start.X * 56 + 32, start.Y * 56 + 32);
			arrow.HorizontalAlignment = HorizontalAlignment.Center;
			arrow.VerticalAlignment = VerticalAlignment.Center;
			arrow.AddThemeColorOverride("font_color", new Color(1, 1, 0));
				
				// 计算旋转角度
				if (end.X > start.X)
				{
					arrow.Rotation = 0;
				}
				else if (end.X < start.X)
				{
					arrow.Rotation = Mathf.Pi;
				}
				else if (end.Y > start.Y)
				{
					arrow.Rotation = Mathf.Pi / 2;
				}
				else if (end.Y < start.Y)
				{
					arrow.Rotation = -Mathf.Pi / 2;
				}
				
				mapLayer.AddChild(arrow);
				pathAnimationNodes.Add(arrow);
				
				// 2秒后移除箭头
				var timer = new Timer();
				timer.WaitTime = 2.0f;
				timer.OneShot = true;
				AddChild(timer);
				timer.Timeout += () => {
					arrow.QueueFree();
					pathAnimationNodes.Remove(arrow);
					timer.QueueFree();
				};
				timer.Start();
				pathAnimationNodes.Add(timer);
			}
		}

		private void ClearPathAnimations()
		{
			// 清除所有路径动画节点
			foreach (var node in pathAnimationNodes)
			{
				if (IsInstanceValid(node))
				{
					node.QueueFree();
				}
			}
			pathAnimationNodes.Clear();
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
				ClearHighlights();
				CalculateAttackRange(selectedUnit, 1); // 近战攻击范围为1
			}
			else
			{
				if (hasAttacked)
				{
					GD.Print("Melee attack: player has already attacked this turn");
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
				ClearHighlights();
				CalculateAttackRange(selectedUnit, selectedUnit.GetEffectiveAttackRange()); // 远程攻击范围为单位的攻击范围
			}
			else
			{
				if (hasAttacked)
				{
					GD.Print("Ranged attack: player has already attacked this turn");
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
				ClearHighlights();
				turnManager.NextTurn();
				// 不再直接调用OnTurnChange，因为NextTurn会触发TurnChange事件
			}
			else
			{
				GD.Print("Not player turn, cannot end turn");
			}
		}

		private void CalculateAttackRange(Unit unit, int attackRange)
		{
			GD.Print("Calculating attack range for unit at: " + unit.Position);
			GD.Print("Attack range: " + attackRange);
			int count = 0;
			for (int y = 0; y < grid.GridSize.Y; y++)
			{
				for (int x = 0; x < grid.GridSize.X; x++)
				{
					var pos = new Vector2(x, y);
					// 计算距离（使用整数位置避免浮点数精度问题）
					var unitPos = new Vector2(Mathf.FloorToInt(unit.Position.X), Mathf.FloorToInt(unit.Position.Y));
					var distance = Mathf.Abs(pos.X - unitPos.X) + Mathf.Abs(pos.Y - unitPos.Y); // 曼哈顿距离
					
					// 跳过角色所在的中心位置
					if (distance == 0)
					{
						continue;
					}
					
					// 对于远程攻击（攻击范围大于1），不包括距离为1的格子
					int minDistance = 1;
					if (attackRange > 1 && !(unit.Class == Unit.UnitClass.Elite && distance == 1))
					{
						minDistance = 2;
					}
					if (distance <= attackRange && distance >= minDistance)
					{
						// 检查是否有敌人在该位置
						Unit targetUnit = null;
						foreach (var u in units)
						{
							// 直接使用整数坐标比较，避免浮点数精度问题
							int uX = (int)u.Position.X;
							int uY = (int)u.Position.Y;
							int pX = (int)pos.X;
							int pY = (int)pos.Y;
							if (uX == pX && uY == pY && u.IsPlayer != unit.IsPlayer && u.IsAlive())
							{
								targetUnit = u;
								GD.Print("Found target unit at: " + u.Position + " in cell: " + pos);
								break;
							}
						}
						// 不管有没有敌人，都显示攻击范围
						GD.Print("Highlighting attack cell: " + pos);
						HighlightAttackCell(pos, targetUnit);
						count++;
					}
				}
			}
			GD.Print("Total attack cells: " + count);
		}

		private void HighlightAttackCell(Vector2 pos, Unit? targetUnit)
		{
			var highlight = new ColorRect();
		highlight.Size = new Vector2(56, 56);
		highlight.Position = new Vector2(pos.X * 56, pos.Y * 56);
		// 根据是否有目标单位设置不同的颜色
		if (targetUnit != null)
		{
			highlight.Color = new Color(0.8f, 0.2f, 0.2f, 0.5f); // 红色高亮表示可以攻击敌人
		}
		else
		{
			highlight.Color = new Color(0.8f, 0.8f, 0.2f, 0.5f); // 黄色高亮表示攻击范围但没有敌人
		}
		highlight.Name = string.Format("AttackHighlight_{0}_{1}", pos.X, pos.Y);
		highlight.SetMeta("position", pos);
		highlight.SetMeta("target_unit", targetUnit);
		
		// 添加点击检测
		var button = new Button();
		button.Size = new Vector2(56, 56);
		button.Position = new Vector2(0, 0);
		button.Flat = true;
		button.MouseFilter = Control.MouseFilterEnum.Stop;
			// 连接信号
			button.Pressed += () => OnAttackCellClicked(highlight);
			highlight.AddChild(button);
			mapLayer.AddChild(highlight);
			highlightCells.Add(highlight);
		}

		private void OnAttackCellClicked(ColorRect highlight)
		{
			var targetPos = highlight.GetMeta("position").As<Vector2>();
			Unit targetUnit = null;
			if (highlight.HasMeta("target_unit"))
			{
				targetUnit = highlight.GetMeta("target_unit").As<Unit>();
			}
			GD.Print("Attack cell clicked: " + highlight.Name);
			GD.Print("Target position: " + targetPos);
			GD.Print("Target unit: " + (targetUnit != null ? targetUnit.Position : "null"));
			if (selectedUnit != null)
			{
				if (targetUnit != null)
				{
					// 有目标单位，触发攻击
					GD.Print("Attacking target unit at: " + targetUnit.Position);
					AttackUnit(selectedUnit, targetUnit);
				}
				else
				{
					// 没有目标单位，提示对空气造成0点伤害
					var attackerType = selectedUnit.IsPlayer ? "玩家" : "敌人 " + GetUnitClassName(selectedUnit.Class);
					battleLog.AppendText($"{attackerType} 攻击空气造成 0 点伤害！\n");
					// 标记玩家已攻击
					if (selectedUnit.IsPlayer)
					{
						hasAttacked = true;
						GD.Print("Player has attacked, hasAttacked=true");
					}
				}
				ClearHighlights();
			}
		}

		private void MoveUnit(Unit unit, Vector2 targetPos)
		{
			if (unit.IsPlayer && hasMoved)
			{
				GD.Print("Player has already moved this turn");
				return;
			}
			// 检查目标位置是否空闲
			if (!IsCellFree(targetPos))
			{
				GD.Print("Target cell is not free, cannot move");
				return;
			}
			
			// 计算移动路径
			var path = CalculatePath(unit.Position, targetPos);
			
			// 检查路径是否有效
			if (path.Count <= 1)
			{
				GD.Print("No valid path to target, cannot move");
				return;
			}
			
			// 开始路径移动
			StartPathMovement(unit, path);
		}

		private void StartPathMovement(Unit unit, List<Vector2> path)
		{
			if (path.Count <= 1)
			{
				// 路径为空或只有一个点，直接完成移动
				CompleteMovement(unit, path[0]);
				return;
			}
			
			// 逐步移动单位
			int currentStep = 0;
			
			var moveTimer = new Timer();
			moveTimer.WaitTime = 0.2f; // 每步移动的时间间隔
			moveTimer.OneShot = false;
			AddChild(moveTimer);
			
			moveTimer.Timeout += () => {
				// 检查是否已经到达路径终点
				if (currentStep >= path.Count - 1)
				{
					// 移动完成
					moveTimer.Stop();
					moveTimer.QueueFree();
					CompleteMovement(unit, path[^1]);
					return;
				}
				
				// 检查路径是否仍然有效（可能有其他单位移动导致路径阻塞）
				var currentPos = unit.Position;
				var nextPos = path[currentStep + 1];
				
				// 检查下一步是否可通行
				if (!IsCellFree(nextPos) || !grid.IsPassable(nextPos, units))
				{
					// 路径被阻塞，重新计算路径
					GD.Print("Path blocked, recalculating...");
					var newPath = CalculatePath(currentPos, path[^1]);
					if (newPath.Count > 1)
					{
						// 使用新路径
						path = newPath;
						currentStep = 0;
						nextPos = path[currentStep + 1];
					}
					else
					{
						// 无法找到新路径，停止移动
						moveTimer.Stop();
						moveTimer.QueueFree();
						CompleteMovement(unit, currentPos);
						return;
					}
				}
				
				currentStep++;
				// 更新单位位置
					unit.Position = path[currentStep];
					// 更新单位节点位置
						foreach (var child in mapLayer.GetChildren())
						{
							if (child.HasMeta("unit"))
							{
								var childUnit = child.GetMeta("unit").As<Unit>();
								if (childUnit != null)
								{
									// 比较单位的引用，确保找到正确的单位节点
									if (childUnit == unit)
									{
										if (child is Control control)
										{
											control.Position = new Vector2(path[currentStep].X * 56, path[currentStep].Y * 56);
										}
									}
								}
							}
						}
			};
			
			moveTimer.Start();
		}

		private void CompleteMovement(Unit unit, Vector2 finalPos)
		{
			// 标记玩家已移动
			if (unit.IsPlayer)
			{
				hasMoved = true;
				GD.Print("Player has moved, hasMoved=true");
			}
			// 清除高亮
			ClearHighlights();
			GD.Print("Movement completed to: " + finalPos);
		}

		private void ClearHighlights()
		{
			foreach (var highlight in highlightCells)
			{
				highlight.QueueFree();
			}
			highlightCells.Clear();
		}

		private void StartGameLoop()
		{
			// 开始游戏循环
			OnTurnChange(turnManager.GetCurrentUnit(), turnManager.GetRoundCount());
		}

		private void GenerateRandomEnemies(int count)
		{
			// 获取玩家单位
			Unit playerUnit = units.Find(u => u.IsPlayer);
			if (playerUnit == null)
			{
				GD.Print("No player unit found");
				return;
			}
			
			// 玩家属性
			int playerMaxHealth = playerUnit.MaxHealth;
			int playerAttack = playerUnit.Attack;
			int playerAttackRange = playerUnit.AttackRange;
			int playerMoveRange = playerUnit.MoveRange;
			int playerSpeed = playerUnit.Speed;
			
			// 最大移动距离（确保敌人与玩家保持这个距离以上）
			int maxMoveDistance = playerMoveRange + 1;
			
			// 职业类型数组
			Unit.UnitClass[] classes = { Unit.UnitClass.Melee, Unit.UnitClass.Ranged, Unit.UnitClass.Elite };
			
			for (int i = 0; i < count; i++)
			{
				// 随机选择职业
				Unit.UnitClass enemyClass = classes[GD.Randi() % classes.Length];
				
				// 根据职业生成属性
				int enemyMaxHealth, enemyAttack, enemyAttackRange, enemyMoveRange, enemySpeed;
				
				if (enemyClass == Unit.UnitClass.Elite)
				{
					// 精英单位属性比玩家低10%-20%
					float eliteFactor = 0.8f + (float)GD.Randf() * 0.1f; // 0.8-0.9
					enemyMaxHealth = (int)(playerMaxHealth * eliteFactor);
					enemyAttack = (int)(playerAttack * eliteFactor);
					enemyAttackRange = 4; // 精英单位有远程攻击能力
					enemyMoveRange = 3; // 增加移动距离
					enemySpeed = (int)(playerSpeed * eliteFactor);
				}
				else
				{
					// 普通单位属性比玩家低40%-60%
					float normalFactor = 0.4f + (float)GD.Randf() * 0.2f; // 0.4-0.6
					enemyMaxHealth = (int)(playerMaxHealth * normalFactor);
					enemyAttack = (int)(playerAttack * normalFactor);
					
					if (enemyClass == Unit.UnitClass.Melee)
					{
						enemyAttackRange = 1; // 近战单位只能近战
						enemyMoveRange = 3; // 增加移动距离
					}
					else // Ranged
					{
						enemyAttackRange = 4; // 远程单位只能远程
						enemyMoveRange = 3; // 增加移动距离
					}
					
					enemySpeed = (int)(playerSpeed * normalFactor);
				}
				
				// 生成随机位置，确保与玩家保持距离
				Vector2 enemyPosition;
				int attempts = 0;
				const int maxAttempts = 100;
				
				do
				{
					// 生成随机位置
				enemyPosition = new Vector2(
					GD.Randi() % (int)grid.GridSize.X,
					GD.Randi() % (int)grid.GridSize.Y
				);
					
					// 计算与玩家的距离
					int distance = Mathf.Abs((int)(enemyPosition.X - playerUnit.Position.X)) + Mathf.Abs((int)(enemyPosition.Y - playerUnit.Position.Y));
					
					// 检查位置是否有效且与玩家保持距离
					bool isValid = grid.IsValidPosition(enemyPosition) && 
						grid.IsPassable(enemyPosition) && 
						distance > maxMoveDistance &&
						IsCellFree(enemyPosition);
					
					if (isValid)
					{
						break;
					}
					
					attempts++;
				}
				while (attempts < maxAttempts);
				
				// 如果找不到合适的位置，使用默认位置
				if (attempts >= maxAttempts)
				{
					enemyPosition = new Vector2(4 + i, 4 + i);
					GD.Print("Could not find valid position, using default");
				}
				
				// 创建敌人单位
				var enemyUnit = Unit.Create(
					enemyMaxHealth,
					enemyAttack,
					enemyAttackRange,
					enemyMoveRange,
					enemySpeed,
					enemyClass,
					enemyPosition,
					false
				);
				
				units.Add(enemyUnit);
				GD.Print($"Created enemy unit {i+1}: Class={enemyClass}, Position={enemyPosition}, Health={enemyMaxHealth}, Attack={enemyAttack}");
			}
		}

		private void OnTurnChange(Unit? currentUnit, int round)
		{
			GD.Print("OnTurnChange called");
			// 更新回合显示
			var turnLabel = GetNode<Label>("CanvasLayer/UI/TurnLabel");
			if (turnLabel != null)
			{
				if (currentUnit != null)
				{
					if (currentUnit.IsPlayer)
					{
						turnLabel.Text = "等待玩家执行动作";
					}
					else
					{
						turnLabel.Text = "敌人执行动作";
					}
				}
				else
				{
					turnLabel.Text = "等待玩家执行动作";
				}
				GD.Print("Turn label updated to: " + turnLabel.Text);
			}
			else
			{
				GD.Print("Turn label not found");
			}
			// 处理敌人回合
			if (currentUnit != null && !currentUnit.IsPlayer)
			{
				GD.Print("Starting enemy turn for unit: " + currentUnit.Position);
				ScheduleEnemyTurn(currentUnit);
			}
			else if (currentUnit != null && currentUnit.IsPlayer)
			{
				GD.Print("Starting player turn");
				// 重置玩家行动次数
				hasMoved = false;
				hasAttacked = false;
				GD.Print("Reset player actions: hasMoved=false, hasAttacked=false");
			}
		}

		private Timer enemyTurnTimer;

		private void ScheduleEnemyTurn(Unit enemyUnit)
		{
			// 防止多次创建定时器
			if (enemyTurnTimer != null && IsInstanceValid(enemyTurnTimer))
			{
				GD.Print("Enemy turn timer already exists, skipping");
				return;
			}

			// 延迟执行敌人逻辑，让玩家看到回合切换
			enemyTurnTimer = new Timer();
			enemyTurnTimer.WaitTime = 1.0f;
			enemyTurnTimer.OneShot = true;
			AddChild(enemyTurnTimer);
			enemyTurnTimer.Timeout += () => ExecuteEnemyTurn(enemyUnit);
			enemyTurnTimer.Start();
			GD.Print("Enemy turn timer started for unit: " + enemyUnit.Position);
		}

		// 添加一个标志来防止敌人回合被多次执行
		private bool isEnemyTurnExecuting = false;

		private void ExecuteEnemyTurn(Unit enemyUnit)
		{
			// 防止多次执行
			if (isEnemyTurnExecuting)
			{
				GD.Print("Enemy turn already executing, skipping");
				return;
			}
			
			isEnemyTurnExecuting = true;
			GD.Print("Executing enemy turn for unit: " + enemyUnit.Position);
			
			// 检查敌人是否存活
			if (!enemyUnit.IsAlive())
			{
				GD.Print("Enemy unit is dead, skipping");
				isEnemyTurnExecuting = false;
				turnManager.NextTurn();
				return;
			}
			
			// 寻找玩家单位
			Unit playerUnit = null;
			foreach (var u in units)
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
			if (enemyUnit.Class == Unit.UnitClass.Elite)
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
									if (grid.IsValidPosition(newPos) && IsCellFree(newPos) && grid.IsPassable(newPos, units) && newDistance >= 2 && newDistance <= attackRange)
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
							// 移动到撤退位置
							enemyUnit.Position = retreatPosition;
							// 立即更新单位节点位置
							RefreshUnitNodePosition(enemyUnit, retreatPosition);
							// 然后进行远程攻击
							shouldAttack = true;
							shouldMove = false;
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
					}
					// 血量中等时根据距离决定
					else if (healthRatio > 0.3f)
					{
						// 距离越近，攻击欲望越高
						shouldAttack = distance <= 2;
					}
					// 血量低时攻击欲望低，除非距离在有效攻击范围内
					else
					{
						shouldAttack = true;
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
						shouldAttack = distance <= 2;
					}
					// 血量低时攻击欲望低，除非距离在有效攻击范围内
					else
					{
						shouldAttack = true;
					}
				}
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
					if (enemyUnit.Class == Unit.UnitClass.Elite && distance == 1)
					{
						actualAttackRange = 1; // 近战攻击范围
					}
					
					// 显示攻击范围动画
					ShowAttackRangeAnimation(enemyUnit, actualAttackRange);
					
					// 延迟执行攻击
					var attackTimer = new Timer();
					attackTimer.WaitTime = 1.0f;
					attackTimer.OneShot = true;
					AddChild(attackTimer);
					attackTimer.Timeout += () => {
						// 可以攻击，执行攻击
						GD.Print("Enemy attacking player");
						AttackUnit(enemyUnit, playerUnit);
						GD.Print("Enemy attack completed");
						
						// 延迟结束回合
						var endTurnTimer = new Timer();
						endTurnTimer.WaitTime = 1.0f;
						endTurnTimer.OneShot = true;
						AddChild(endTurnTimer);
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
					
					// 计算路径
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
					if (!grid.IsValidPosition(targetPos) || !IsCellFree(targetPos) || !grid.IsPassable(targetPos, units))
					{
						// 目标位置无效，直接结束回合
						GD.Print("Enemy target position is invalid, ending turn");
						EndEnemyTurn(enemyUnit);
						return;
					}
					
					// 精英单位特殊处理：如果距离大于1，尝试移动到远程攻击范围
					if (enemyUnit.Class == Unit.UnitClass.Elite && distance > 1)
					{
						// 尝试找到一个距离为2的位置，以便使用远程攻击
						bool foundPosition = false;
						for (int dx = -2; dx <= 2; dx++)
						{
							for (int dy = -2; dy <= 2; dy++)
							{
								if (Mathf.Abs(dx) + Mathf.Abs(dy) == 2) // 距离为2
								{
									var newPos = new Vector2(enemyUnit.Position.X + dx, enemyUnit.Position.Y + dy);
									if (grid.IsValidPosition(newPos) && IsCellFree(newPos) && grid.IsPassable(newPos, units))
									{
										targetPos = newPos;
										foundPosition = true;
										break;
									}
								}
								if (foundPosition)
									break;
							}
						}
					}
					
					var path = CalculatePath(enemyUnit.Position, targetPos);
					
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
				AddChild(moveTimer);
				moveTimer.Timeout += () => {
					MoveEnemyTowardsPlayer(enemyUnit, playerUnit);
					GD.Print("Enemy move completed");
						
						// 延迟检查是否可以攻击
						var checkAttackTimer = new Timer();
						checkAttackTimer.WaitTime = 1.0f;
						checkAttackTimer.OneShot = true;
						AddChild(checkAttackTimer);
						checkAttackTimer.Timeout += () => {
							// 移动后再次检查是否可以攻击
							var newDistance = Mathf.Abs(enemyUnit.Position.X - playerUnit.Position.X) + Mathf.Abs(enemyUnit.Position.Y - playerUnit.Position.Y);
							GD.Print("Distance after move: " + newDistance);
							
							// 重新计算攻击欲望
					bool shouldAttackAfterMove = false;
					
					// 精英单位特殊处理：同时拥有近战和远程攻击能力
					if (enemyUnit.Class == Unit.UnitClass.Elite)
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
							// 血量中等时根据距离决定
							else if (healthRatio > 0.3f)
							{
								// 距离越近，攻击欲望越高
								shouldAttackAfterMove = newDistance <= 2;
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
							// 血量中等时根据距离决定
							else if (healthRatio > 0.3f)
							{
								// 距离越近，攻击欲望越高
								shouldAttackAfterMove = newDistance <= 2;
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
								if (enemyUnit.Class == Unit.UnitClass.Elite && newDistance == 1)
								{
									actualAttackRange = 1; // 近战攻击范围
								}
								
								// 显示攻击范围动画
								ShowAttackRangeAnimation(enemyUnit, actualAttackRange);
								
								// 延迟执行攻击
								var attackAfterMoveTimer = new Timer();
								attackAfterMoveTimer.WaitTime = 1.0f;
								attackAfterMoveTimer.OneShot = true;
								AddChild(attackAfterMoveTimer);
								attackAfterMoveTimer.Timeout += () => {
									// 移动后可以攻击，执行攻击
									GD.Print("Enemy attacking player after move");
									AttackUnit(enemyUnit, playerUnit);
									GD.Print("Enemy attack after move completed");
									
									// 延迟结束回合
									var endTurnAfterAttackTimer = new Timer();
									endTurnAfterAttackTimer.WaitTime = 1.0f;
									endTurnAfterAttackTimer.OneShot = true;
									AddChild(endTurnAfterAttackTimer);
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
			if (enemyTurnTimer != null && IsInstanceValid(enemyTurnTimer))
			{
				enemyTurnTimer.QueueFree();
				GD.Print("Enemy turn timer cleaned up");
			}
			enemyTurnTimer = null;
			// 调用NextTurn来结束敌人回合
			turnManager.NextTurn();
			// 不再直接调用OnTurnChange，因为NextTurn会触发TurnChange事件
		}

		private Vector2 CalculateEnemyMovePosition(Unit enemyUnit, Unit playerUnit)
		{
			// 计算敌人移动目标位置
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
			
			return new Vector2(newX, newY);
		}

		private void ShowAttackRangeAnimation(Unit unit, int range)
		{
			// 显示攻击范围动画
			for (int y = 0; y < grid.GridSize.Y; y++)
			{
				for (int x = 0; x < grid.GridSize.X; x++)
				{
					var pos = new Vector2(x, y);
					var distance = Mathf.Abs(pos.X - unit.Position.X) + Mathf.Abs(pos.Y - unit.Position.Y); // 曼哈顿距离
					
					// 跳过角色所在的中心位置
					if (distance == 0)
					{
						continue;
					}
					
					// 对于远程攻击（攻击范围大于1），不包括距离为1的格子
					int minDistance = 1;
					if (range > 1 && !(unit.Class == Unit.UnitClass.Elite && distance == 1))
					{
						minDistance = 2;
					}
					
					if (distance <= range && distance >= minDistance)
					{
						// 创建攻击范围高亮
				var highlight = new ColorRect();
				highlight.Size = new Vector2(56, 56);
				highlight.Position = new Vector2(pos.X * 56, pos.Y * 56);
				highlight.Color = new Color(1, 0, 0, 0.3f);
				highlight.Name = string.Format("AttackRangeHighlight_{0}_{1}", x, y);
				mapLayer.AddChild(highlight);
						
						// 2秒后移除高亮
						var timer = new Timer();
						timer.WaitTime = 2.0f;
						timer.OneShot = true;
						AddChild(timer);
						timer.Timeout += () => {
							highlight.QueueFree();
							timer.QueueFree();
						};
						timer.Start();
					}
				}
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
			if (grid.IsValidPosition(newPos) && IsCellFree(newPos) && grid.IsPassable(newPos, units))
			{
				// 使用新的路径移动系统
				var path = CalculatePath(enemyUnit.Position, newPos);
				// 显示路径动画
				ShowPathAnimation(path);
				StartPathMovement(enemyUnit, path);
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
							if (grid.IsValidPosition(testPos) && grid.IsPassable(testPos, units))
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
					var path = CalculatePath(enemyUnit.Position, closestPos);
					ShowPathAnimation(path);
					StartPathMovement(enemyUnit, path);
				}
			}
		}

		private void AttackUnit(Unit attacker, Unit target)
		{
			// 计算距离（使用整数位置避免浮点数精度问题）
			var attackerPos = new Vector2(Mathf.FloorToInt(attacker.Position.X), Mathf.FloorToInt(attacker.Position.Y));
			var targetPos = new Vector2(Mathf.FloorToInt(target.Position.X), Mathf.FloorToInt(target.Position.Y));
			var distance = Mathf.Abs(attackerPos.X - targetPos.X) + Mathf.Abs(attackerPos.Y - targetPos.Y);
			
			// 确定攻击范围
			int attackRange = attacker.GetEffectiveAttackRange();
			// 精英单位特殊处理：距离为1时使用近战攻击范围
			if (attacker.Class == Unit.UnitClass.Elite && distance == 1)
			{
				attackRange = 1; // 近战攻击范围
			}
			
			// 检查是否在攻击范围内
			// 对于远程攻击（攻击范围大于1），不包括距离为1的格子
			int minDistance = 1;
			if (attackRange > 1)
			{
				minDistance = 2;
			}
			
			if (distance < minDistance || distance > attackRange)
			{
				GD.Print("Attack out of range! Distance: " + distance + ", Min: " + minDistance + ", Max: " + attackRange);
				return;
			}
			
			// 计算伤害
			var damage = attacker.GetEffectiveAttack();
			// 计算暴击
			bool isCritical = false;
			var criticalChance = (float)attacker.Luck / 20.0f; // 幸运值越高，暴击率越高
			if (GD.Randf() < criticalChance)
			{
				isCritical = true;
				damage *= 2; // 暴击伤害翻倍
			}
			// 应用伤害
			target.TakeDamage(damage);
			// 如果当前编辑的单位是目标单位，更新调试菜单
			if (currentEditUnit == target)
			{
				UpdateUnitProperties(target);
			}
			// 打印战斗日志
			var attackerType = attacker.IsPlayer ? "玩家" : "敌人 " + GetUnitClassName(attacker.Class);
			var targetType = target.IsPlayer ? "玩家" : "敌人 " + GetUnitClassName(target.Class);
			if (isCritical)
			{
				// 红色字体显示暴击
				battleLog.AppendText($"{attackerType} 攻击 {targetType} 造成 [color=#ff0000]{damage} 点暴击伤害！[/color]\n");
			}
			else
			{
				battleLog.AppendText($"{attackerType} 攻击 {targetType} 造成 {damage} 点伤害！\n");
			}
			// 标记玩家已攻击
			if (attacker.IsPlayer)
			{
				hasAttacked = true;
				GD.Print("Player has attacked, hasAttacked=true");
			}
			// 更新血条
			foreach (var child in mapLayer.GetChildren())
			{
				if (child.HasMeta("unit") && child.GetMeta("unit").As<Unit>() == target)
				{
					UpdateHPBar(child, target);
					// 视觉反馈：闪红
					if (child is ColorRect colorRect)
					{
						colorRect.Color = new Color(1, 0, 0);
						// 使用Timer来延迟恢复颜色
						var timer = new Timer();
						timer.WaitTime = 0.2f;
						timer.OneShot = true;
						AddChild(timer);
						timer.Timeout += () =>
						{
							if (colorRect != null && GodotObject.IsInstanceValid(colorRect))
							{
								colorRect.Color = target.IsPlayer ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.8f, 0.2f, 0.2f);
							}
							timer.QueueFree();
						};
						timer.Start();
					}
					break;
				}
			}
			// 检查单位是否死亡
			if (!target.IsAlive())
			{
				battleLog.AppendText($"{targetType} 被击败了！\n");
				// 移除死亡单位
			foreach (var child in mapLayer.GetChildren())
			{
				if (child.HasMeta("unit") && child.GetMeta("unit").As<Unit>() == target)
				{
					child.QueueFree();
					break;
				}
			}
				units.Remove(target);
				// 检查游戏结束
				CheckGameOver();
			}
		}

		private void CheckGameOver()
		{
			// 检查是否所有玩家单位都死亡
			bool playerAlive = false;
			foreach (var unit in units)
			{
				if (unit.IsPlayer && unit.IsAlive())
				{
					playerAlive = true;
					break;
				}
			}
			if (!playerAlive)
			{
				battleLog.AppendText("游戏结束！敌人获胜！\n");
				// 显示游戏结束菜单
				ShowGameOverMenu(false);
				return;
			}
			// 检查是否所有敌人单位都死亡
			bool enemyAlive = false;
			foreach (var unit in units)
			{
				if (!unit.IsPlayer && unit.IsAlive())
				{
					enemyAlive = true;
					break;
				}
			}
			if (!enemyAlive)
			{
				battleLog.AppendText("胜利！玩家获胜！\n");
				// 显示游戏结束菜单
				ShowGameOverMenu(true);
			}
		}

		private OptionButton unitSelect;
		private OptionButton enemyClassSelect;

		private void InitializeDebugMenu()
		{
			// 获取调试菜单面板
			var canvasLayer = GetNode<CanvasLayer>("CanvasLayer");
			if (canvasLayer == null)
			{
				GD.Print("CanvasLayer not found");
				return;
			}

			var ui = canvasLayer.GetNode<Control>("UI");
			if (ui == null)
			{
				GD.Print("UI not found");
				return;
			}

			// 获取DebugPanel
			var debugPanel = ui.GetNode<VBoxContainer>("DebugPanel");
			if (debugPanel == null)
			{
				GD.Print("DebugPanel not found");
				return;
			}

			// 添加样式
			var stylebox = new StyleBoxFlat();
			stylebox.BgColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
			stylebox.BorderColor = new Color(0.5f, 0.5f, 0.5f);
			stylebox.BorderWidthTop = 1;
			stylebox.BorderWidthBottom = 1;
			stylebox.BorderWidthLeft = 1;
			stylebox.BorderWidthRight = 1;
			debugPanel.AddThemeStyleboxOverride("panel", stylebox);
			
			// 获取单位选择下拉框
			unitSelect = debugPanel.GetNode<OptionButton>("UnitSelect");
			if (unitSelect == null)
			{
				GD.Print("UnitSelect not found");
				return;
			}
			
			// 获取属性容器
			var propertyContainer = debugPanel.GetNode<HBoxContainer>("PropertyContainer");
			if (propertyContainer == null)
			{
				GD.Print("PropertyContainer not found");
				return;
			}
			
			// 获取左右列
			var leftCol = propertyContainer.GetNode<VBoxContainer>("LeftCol");
			var rightCol = propertyContainer.GetNode<VBoxContainer>("RightCol");
			if (leftCol == null || rightCol == null)
			{
				GD.Print("LeftCol or RightCol not found");
				return;
			}
			
			// 添加属性标签和输入框（两列布局）
			string[] propertiesLeft = { "max_health", "attack", "move_range" };
			string[] propertiesRight = { "current_health", "attack_range", "speed" };
			
			for (int i = 0; i < propertiesLeft.Length; i++)
			{
				AddPropertyInput(leftCol, propertiesLeft[i]);
				AddPropertyInput(rightCol, propertiesRight[i]);
			}
			
			// 连接信号
			unitSelect.ItemSelected += (index) => OnUnitSelected(index);

			// 更新单位列表
			UpdateUnitList();

			// 确保初始化时选择默认单位，显示属性默认值
			if (units.Count > 0)
			{
				unitSelect.Select(0);
				OnUnitSelected(0);
			}
			else
			{
				// 如果没有单位，添加一个默认单位用于显示
				var defaultUnit = Unit.Create(100, 10, 3, 3, 5, Unit.UnitClass.Elite, new Vector2(0, 0), true);
				units.Add(defaultUnit);
				UpdateUnitList();
				if (units.Count > 0)
				{
					unitSelect.Select(0);
					OnUnitSelected(0);
				}
			}
			
			// 添加敌人生成器
			AddEnemyGenerator(debugPanel);
		}

		private string GetUnitClassName(Unit.UnitClass unitClass)
		{
			switch (unitClass)
			{
				case Unit.UnitClass.Melee:
					return "近战";
				case Unit.UnitClass.Ranged:
					return "远程";
				case Unit.UnitClass.Elite:
					return "精英";
				default:
					return "未知";
			}
		}

		private void UpdateUnitList()
		{
			// 更新单位选择下拉框
			if (unitSelect != null)
			{
				unitSelect.Clear();
				for (int i = 0; i < units.Count; i++)
				{
					var unit = units[i];
					var unitName = unit.IsPlayer ? 
						"玩家 " + i : 
						"敌人 " + GetUnitClassName(unit.Class) + " " + i;
					unitSelect.AddItem(unitName, i);
				}
			}
		}

		private void OnUnitSelected(long index)
		{
			if (index >= 0 && index < units.Count)
			{
				var unit = units[(int)index];
				UpdateUnitProperties(unit);
			}
		}

		private void AddEnemyGenerator(VBoxContainer debugPanel)
		{
			// 创建敌人生成器区域
			var hbox = new HBoxContainer();
			hbox.Name = "EnemyGenerator";
			debugPanel.AddChild(hbox);
			
			// 添加标签
			var label = new Label();
			label.Text = "敌人生成器:";
			label.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
			hbox.AddChild(label);
			
			// 添加职业选择下拉框
			enemyClassSelect = new OptionButton();
			enemyClassSelect.Name = "EnemyClassSelect";
			enemyClassSelect.AddItem("近战", (int)Unit.UnitClass.Melee);
			enemyClassSelect.AddItem("远程", (int)Unit.UnitClass.Ranged);
			enemyClassSelect.AddItem("精英", (int)Unit.UnitClass.Elite);
			enemyClassSelect.Select(0);
			hbox.AddChild(enemyClassSelect);
			
			// 添加生成按钮
			var generateButton = new Button();
			generateButton.Text = "生成敌人";
			generateButton.Pressed += OnGenerateEnemyPressed;
			hbox.AddChild(generateButton);
		}

		private void OnGenerateEnemyPressed()
		{
			// 获取选中的职业
			int selectedIndex = enemyClassSelect.Selected;
			Unit.UnitClass selectedClass = (Unit.UnitClass)enemyClassSelect.GetItemId(selectedIndex);
			
			// 生成随机位置
			Vector2 enemyPosition;
			int attempts = 0;
			const int maxAttempts = 100;
			
			do
			{
				enemyPosition = new Vector2(
					GD.Randi() % (int)grid.GridSize.X,
					GD.Randi() % (int)grid.GridSize.Y
				);
				
				// 检查位置是否有效且空闲
				bool isValid = grid.IsValidPosition(enemyPosition) && 
					grid.IsPassable(enemyPosition, units) &&
					IsCellFree(enemyPosition);
				
				if (isValid)
				{
					break;
				}
				
				attempts++;
			}
			while (attempts < maxAttempts);
			
			// 如果找不到合适的位置，使用默认位置
			if (attempts >= maxAttempts)
			{
				enemyPosition = new Vector2(4, 4);
				GD.Print("Could not find valid position, using default");
			}
			
			// 获取玩家单位属性作为参考
			Unit playerUnit = units.Find(u => u.IsPlayer);
			if (playerUnit == null)
			{
				GD.Print("No player unit found");
				return;
			}
			
			// 根据职业生成属性
			int enemyMaxHealth, enemyAttack, enemyAttackRange, enemyMoveRange, enemySpeed;
			
			if (selectedClass == Unit.UnitClass.Elite)
			{
				// 精英单位属性比玩家低10%-20%
				float eliteFactor = 0.8f + (float)GD.Randf() * 0.1f; // 0.8-0.9
				enemyMaxHealth = (int)(playerUnit.MaxHealth * eliteFactor);
				enemyAttack = (int)(playerUnit.Attack * eliteFactor);
				enemyAttackRange = 4; // 精英单位有远程攻击能力
				enemyMoveRange = 3; // 增加移动距离
				enemySpeed = (int)(playerUnit.Speed * eliteFactor);
			}
			else
			{
				// 普通单位属性比玩家低40%-60%
				float normalFactor = 0.4f + (float)GD.Randf() * 0.2f; // 0.4-0.6
				enemyMaxHealth = (int)(playerUnit.MaxHealth * normalFactor);
				enemyAttack = (int)(playerUnit.Attack * normalFactor);
				
				if (selectedClass == Unit.UnitClass.Melee)
				{
					enemyAttackRange = 1; // 近战单位只能近战
					enemyMoveRange = 3; // 增加移动距离
				}
				else // Ranged
				{
					enemyAttackRange = 4; // 远程单位只能远程
					enemyMoveRange = 3; // 增加移动距离
				}
				
				enemySpeed = (int)(playerUnit.Speed * normalFactor);
			}
			
			// 创建敌人单位
			var enemyUnit = Unit.Create(
				enemyMaxHealth,
				enemyAttack,
				enemyAttackRange,
				enemyMoveRange,
				enemySpeed,
				selectedClass,
				enemyPosition,
				false
			);
			
			units.Add(enemyUnit);
			GD.Print($"Created enemy unit: Class={selectedClass}, Position={enemyPosition}, Health={enemyMaxHealth}, Attack={enemyAttack}");
			
			// 绘制新生成的敌人单位
			DrawUnit(enemyUnit);
			
			// 更新单位列表
			UpdateUnitList();
			
			// 更新回合管理器的单位列表，确保新生成的敌人能参与到游戏循环中
			if (turnManager != null)
			{
				turnManager.SetUnits(units);
				GD.Print("Updated turn manager units list");
			}
		}

		private void UpdateUnitProperties(Unit unit)
		{
			currentEditUnit = unit;
			var canvasLayer = GetNode<CanvasLayer>("CanvasLayer");
			if (canvasLayer != null)
			{
				var ui = canvasLayer.GetNode<Control>("UI");
				if (ui != null)
				{
					var debugPanel = ui.GetNode<VBoxContainer>("DebugPanel");
					if (debugPanel != null)
					{
						var propertyContainer = debugPanel.GetNode<HBoxContainer>("PropertyContainer");
						if (propertyContainer != null)
						{
							foreach (var col in propertyContainer.GetChildren())
							{
								if (col is VBoxContainer vbox)
								{
									foreach (var row in vbox.GetChildren())
									{
										if (row is HBoxContainer hbox)
										{
											foreach (var inputField in hbox.GetChildren())
											{
												if (inputField is LineEdit lineEdit)
												{
													var propName = lineEdit.Name.ToString().Replace("Input_", "");
													var value = GetUnitProperty(unit, propName);
													lineEdit.Text = value.ToString();
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
		}

		private void ShowGameOverMenu(bool isVictory)
		{
			// 获取UI节点
			var canvasLayer = GetNode<CanvasLayer>("CanvasLayer");
			if (canvasLayer == null)
			{
				GD.Print("CanvasLayer not found");
				return;
			}

			var ui = canvasLayer.GetNode<Control>("UI");
			if (ui == null)
			{
				GD.Print("UI not found");
				return;
			}

			// 创建游戏结束菜单
			var gameOverMenu = new VBoxContainer();
			gameOverMenu.Name = "GameOverMenu";
			gameOverMenu.AnchorRight = 1.0f;
			gameOverMenu.AnchorBottom = 1.0f;
			gameOverMenu.Alignment = BoxContainer.AlignmentMode.Center;
			gameOverMenu.AddThemeConstantOverride("separation", 20);
			
			// 添加标题
			var titleLabel = new Label();
			titleLabel.Text = isVictory ? "胜利！" : "失败！";
			titleLabel.AddThemeColorOverride("font_color", new Color(1, 1, 1));
			titleLabel.AddThemeFontSizeOverride("font_size", 32);
			titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
			gameOverMenu.AddChild(titleLabel);
			
			// 添加重新游戏按钮
			var restartButton = new Button();
			restartButton.Text = "重新游戏";
			restartButton.AddThemeStyleboxOverride("normal", new StyleBoxFlat() { BgColor = new Color(0.2f, 0.6f, 0.2f) });
			restartButton.AddThemeStyleboxOverride("hover", new StyleBoxFlat() { BgColor = new Color(0.3f, 0.7f, 0.3f) });
			restartButton.AddThemeStyleboxOverride("pressed", new StyleBoxFlat() { BgColor = new Color(0.1f, 0.5f, 0.1f) });
			restartButton.AddThemeColorOverride("font_color", new Color(1, 1, 1));
			restartButton.CustomMinimumSize = new Vector2(200, 50);
			restartButton.Pressed += OnRestartGame;
			gameOverMenu.AddChild(restartButton);
			
			// 添加结束游戏按钮
			var quitButton = new Button();
			quitButton.Text = "结束游戏";
			quitButton.AddThemeStyleboxOverride("normal", new StyleBoxFlat() { BgColor = new Color(0.6f, 0.2f, 0.2f) });
			quitButton.AddThemeStyleboxOverride("hover", new StyleBoxFlat() { BgColor = new Color(0.7f, 0.3f, 0.3f) });
			quitButton.AddThemeStyleboxOverride("pressed", new StyleBoxFlat() { BgColor = new Color(0.5f, 0.1f, 0.1f) });
			quitButton.AddThemeColorOverride("font_color", new Color(1, 1, 1));
			quitButton.CustomMinimumSize = new Vector2(200, 50);
			quitButton.Pressed += OnQuitGame;
			gameOverMenu.AddChild(quitButton);
			
			ui.AddChild(gameOverMenu);
		}

		private void OnRestartGame()
		{
			// 重新游戏
			GetTree().ReloadCurrentScene();
		}

		private void OnQuitGame()
		{
			// 结束游戏
			GetTree().Quit();
		}

		private void AddPropertyInput(VBoxContainer parent, string prop)
		{
			var hbox = new HBoxContainer();
			var propLabel = new Label();
			propLabel.Text = prop + ":";
			propLabel.AddThemeColorOverride("font_color", new Color(1, 1, 1));
			propLabel.CustomMinimumSize = new Vector2(100, 0);
			hbox.AddChild(propLabel);
			var propInput = new LineEdit();
			propInput.Name = "Input_" + prop;
			var inputStyle = new StyleBoxFlat();
			inputStyle.BgColor = new Color(0.2f, 0.2f, 0.2f);
			inputStyle.BorderColor = new Color(0.5f, 0.5f, 0.5f);
			inputStyle.BorderWidthTop = 1;
			inputStyle.BorderWidthBottom = 1;
			inputStyle.BorderWidthLeft = 1;
			inputStyle.BorderWidthRight = 1;
			propInput.AddThemeStyleboxOverride("focused", inputStyle);
			propInput.AddThemeStyleboxOverride("normal", inputStyle);
			propInput.AddThemeColorOverride("font_color", new Color(1, 1, 1));
			propInput.CustomMinimumSize = new Vector2(60, 0);
			propInput.Text = "0";
			propInput.TextSubmitted += (text) => OnPropertyTextSubmitted(text, prop, propInput);
			hbox.AddChild(propInput);
			parent.AddChild(hbox);
		}

		private void OnPropertyTextSubmitted(string text, string prop, LineEdit inputField)
		{
			if (currentEditUnit == null)
			{
				return;
			}
			int value;
			if (!int.TryParse(text, out value))
			{
				value = GetUnitProperty(currentEditUnit, prop);
			}
			SetUnitProperty(currentEditUnit, prop, value);
			inputField.Text = GetUnitProperty(currentEditUnit, prop).ToString();
			
			// 更新游戏内显示
			// 1. 更新血条
			foreach (var child in mapLayer.GetChildren())
			{
				if (child.HasMeta("unit") && child.GetMeta("unit").As<Unit>() == currentEditUnit)
				{
					UpdateHPBar(child, currentEditUnit);
					break;
				}
			}
			
			// 2. 如果修改了攻击范围，清除现有的高亮
			if (prop == "attack_range" || prop == "move_range")
			{
				ClearHighlights();
			}
		}

		private int GetUnitProperty(Unit unit, string prop)
		{
			switch (prop)
			{
				case "max_health": return unit.MaxHealth;
				case "current_health": return unit.CurrentHealth;
				case "attack": return unit.Attack;
				case "attack_range": return unit.AttackRange;
				case "move_range": return unit.MoveRange;
				case "speed": return unit.Speed;
				default: return 0;
			}
		}

		private void SetUnitProperty(Unit unit, string prop, int value)
		{
			switch (prop)
			{
				case "max_health": unit.MaxHealth = value; break;
				case "current_health": unit.CurrentHealth = value; break;
				case "attack": unit.Attack = value; break;
				case "attack_range": unit.AttackRange = value; break;
				case "move_range": unit.MoveRange = value; break;
				case "speed": unit.Speed = value; break;
			}
		}

		private void RefreshUnitNodePosition(Unit unit, Vector2 newPos)
		{
			foreach (var child in mapLayer.GetChildren())
			{
				if (child.HasMeta("unit") && child.GetMeta("unit").As<Unit>() == unit)
				{
					if (child is Control control)
				{
					control.Position = new Vector2(newPos.X * 56, newPos.Y * 56);
				}
					break;
				}
			}
		}
	}
}
