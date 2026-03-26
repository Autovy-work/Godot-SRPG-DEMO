using Godot;
using System.Collections.Generic;
using CSharpTestGame.Managers;

namespace CSharpTestGame
{
	public class DebugManager
{
	private UnitManager unitManager;
	private OptionButton unitSelect;
	private OptionButton enemyClassSelect;
	private Unit currentEditUnit;
	private Node rootNode;
	private DataLoader dataLoader;
	private EquipmentManager equipmentManager;

	public DebugManager(UnitManager unitManager, Node rootNode, DataLoader dataLoader, EquipmentManager equipmentManager)
	{
		this.unitManager = unitManager;
		this.rootNode = rootNode;
		this.dataLoader = dataLoader;
		this.equipmentManager = equipmentManager;
	}

	public void InitializeDebugMenu()
	{
		// 获取调试菜单面板
		var canvasLayer = rootNode.GetNode<CanvasLayer>("CanvasLayer");
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
			string[] propertiesLeft = { "max_health", "attack", "move_range", "level" };
			string[] propertiesRight = { "current_health", "attack_range", "speed" };

			for (int i = 0; i < propertiesLeft.Length; i++)
			{
				AddPropertyInput(leftCol, propertiesLeft[i]);
				if (i < propertiesRight.Length)
				{
					AddPropertyInput(rightCol, propertiesRight[i]);
				}
			}

			// 为经验值添加只读标签
			AddExperienceLabel(rightCol, "experience");

			// 连接信号
			unitSelect.ItemSelected += (index) => OnUnitSelected(index);

			// 更新单位列表
			UpdateUnitList();

			// 确保初始化时选择默认单位，显示属性默认值
			if (unitManager.Units.Count > 0)
			{
				unitSelect.Select(0);
				OnUnitSelected(0);
			}
			else
			{
				// 如果没有单位，添加一个默认单位用于显示
				var defaultUnit = Unit.Create(100, 10, 3, 3, 5, Unit.UnitClass.WarAngel, new Vector2(0, 0), true);
				unitManager.Units.Add(defaultUnit);
				UpdateUnitList();
				if (unitManager.Units.Count > 0)
				{
					unitSelect.Select(0);
					OnUnitSelected(0);
				}
			}

			// 添加敌人生成器
			AddEnemyGenerator(debugPanel);
			// 添加调试按钮
			AddDebugButtons(debugPanel);
		}

		private void AddPropertyInput(VBoxContainer container, string propertyName)
		{
			// 创建水平容器
			var hbox = new HBoxContainer();
			container.AddChild(hbox);

			// 创建标签
			var label = new Label();
			label.Text = propertyName;
			label.SizeFlagsHorizontal = Godot.Control.SizeFlags.ShrinkEnd;
			hbox.AddChild(label);

			// 创建输入框
			var spinBox = new SpinBox();
			spinBox.SizeFlagsHorizontal = Godot.Control.SizeFlags.ExpandFill;
			spinBox.Name = propertyName;
			spinBox.MinValue = 1;
			spinBox.MaxValue = 100;
			spinBox.Step = 1;
			spinBox.ValueChanged += (value) => OnPropertyChanged(propertyName, value);
			hbox.AddChild(spinBox);
		}

		private void AddEnemyGenerator(VBoxContainer debugPanel)
		{
			// 创建敌人生成器部分
			var hbox = new HBoxContainer();
			debugPanel.AddChild(hbox);

			// 创建标签
			var label = new Label();
			label.Text = "生成敌人";
			label.SizeFlagsHorizontal = Godot.Control.SizeFlags.ShrinkEnd;
			hbox.AddChild(label);

			// 创建职业选择
			enemyClassSelect = new OptionButton();
			enemyClassSelect.SizeFlagsHorizontal = Godot.Control.SizeFlags.ExpandFill;
			
			// 动态从UnitClass枚举中获取所有单位类型，只添加非玩家单位
			foreach (Unit.UnitClass unitClass in System.Enum.GetValues(typeof(Unit.UnitClass)))
			{
				// 获取单位数据
				var unitData = dataLoader.GetUnitData(unitClass.ToString());
				// 检查是否是非玩家单位
				bool isPlayer = false;
				if (unitData != null && unitData.ContainsKey("is_player"))
				{
					isPlayer = unitData["is_player"].AsBool();
				}
				// 只添加非玩家单位
				if (!isPlayer)
				{
					enemyClassSelect.AddItem(unitManager.GetUnitClassName(unitClass));
				}
			}
			
			hbox.AddChild(enemyClassSelect);

			// 创建生成按钮
			var button = new Button();
			button.Text = "生成";
			button.SizeFlagsHorizontal = Godot.Control.SizeFlags.ShrinkEnd;
			button.Pressed += OnGenerateEnemyPressed;
			hbox.AddChild(button);
		}

		private void AddDebugButtons(VBoxContainer debugPanel)
		{
			// 创建调试按钮部分
			var hbox = new HBoxContainer();
			debugPanel.AddChild(hbox);

			// 创建标签
			var label = new Label();
			label.Text = "调试功能";
			label.SizeFlagsHorizontal = Godot.Control.SizeFlags.ShrinkEnd;
			hbox.AddChild(label);

			// 创建触发游戏失败按钮
			var failButton = new Button();
			failButton.Text = "触发失败";
			failButton.SizeFlagsHorizontal = Godot.Control.SizeFlags.ExpandFill;
			failButton.Pressed += OnTriggerGameOverPressed;
			hbox.AddChild(failButton);
		}

		private void OnGenerateEnemyPressed()
		{
			// 获取选择的职业（只考虑非玩家单位）
			List<Unit.UnitClass> nonPlayerClasses = new List<Unit.UnitClass>();
			foreach (Unit.UnitClass unitClass in System.Enum.GetValues(typeof(Unit.UnitClass)))
			{
				// 获取单位数据
				var tempUnitData = dataLoader.GetUnitData(unitClass.ToString());
				// 检查是否是非玩家单位
				bool isPlayer = false;
				if (tempUnitData != null && tempUnitData.ContainsKey("is_player"))
				{
					isPlayer = tempUnitData["is_player"].AsBool();
				}
				// 只添加非玩家单位
				if (!isPlayer)
				{
					nonPlayerClasses.Add(unitClass);
				}
			}
			
			// 确保有非玩家单位
			if (nonPlayerClasses.Count == 0)
			{
				GD.PrintErr("No non-player unit classes found");
				return;
			}
			
			// 获取选择的职业
			int selectedIndex = Mathf.Min(enemyClassSelect.Selected, nonPlayerClasses.Count - 1);
			Unit.UnitClass enemyClass = nonPlayerClasses[selectedIndex];

				// 从JSON数据中获取敌人属性
			var unitData = dataLoader.GetUnitData(enemyClass.ToString());
			var defaultEnemyStats = dataLoader.GetDefaultEnemyStats();
			int enemyMaxHealth = 20;
			int enemyAttack = 5;
			int enemyAttackRange = 1;
			int enemyMoveRange = 2;
			int enemySpeed = 4;
			string imagePath = "";

			// 从默认配置中获取敌人属性
			if (defaultEnemyStats != null)
			{
				enemyMaxHealth = GetIntValue(defaultEnemyStats, "max_health", enemyMaxHealth);
				enemyAttack = GetIntValue(defaultEnemyStats, "attack", enemyAttack);
				enemyAttackRange = GetIntValue(defaultEnemyStats, "attack_range", enemyAttackRange);
				enemyMoveRange = GetIntValue(defaultEnemyStats, "move_range", enemyMoveRange);
				enemySpeed = GetIntValue(defaultEnemyStats, "speed", enemySpeed);
			}

			// 从JSON数据中读取属性（覆盖默认值）
			if (unitData != null)
			{
				enemyMaxHealth = GetIntValue(unitData, "max_health", enemyMaxHealth);
				enemyAttack = GetIntValue(unitData, "attack", enemyAttack);
				enemyAttackRange = GetIntValue(unitData, "attack_range", enemyAttackRange);
				enemyMoveRange = GetIntValue(unitData, "move_range", enemyMoveRange);
				enemySpeed = GetIntValue(unitData, "speed", enemySpeed);
				
				// 读取图像路径
				if (unitData.ContainsKey("image"))
				{
					imagePath = unitData["image"].ToString();
				}
			}

			// 生成随机位置，确保与玩家保持距离
			Vector2 enemyPosition;
			int attempts = 0;
			const int maxAttempts = 100;

			// 获取游戏管理器的地图管理器
			var gameManager = rootNode as GameManager;
			var mapManager = gameManager?.MapManager;
			var grid = mapManager?.Grid;

			if (grid == null)
			{
				GD.Print("Grid not found, using default position");
				enemyPosition = new Vector2(5, 5);
			}
			else
			{
				do
				{
					// 生成随机位置
					enemyPosition = new Vector2(
						GD.Randi() % (int)grid.GridSize.X,
						GD.Randi() % (int)grid.GridSize.Y
					);

					// 检查位置是否有效
					bool isValid = grid.IsValidPosition(enemyPosition) && 
						grid.IsPassable(enemyPosition) && 
						unitManager.IsCellFree(enemyPosition);

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
					enemyPosition = new Vector2(5, 5);
					GD.Print("Could not find valid position, using default");
				}
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
				false,
				imagePath
			);

			// 加载敌人单位的背包物品
			if (unitData != null)
			{
				LoadUnitInventory(enemyUnit, unitData);
			}

			// 添加到单位管理器
			unitManager.Units.Add(enemyUnit);
			unitManager.DrawUnit(enemyUnit);
			UpdateUnitList();

			// 通知游戏管理器更新回合管理器
			if (gameManager != null)
			{
				gameManager.UpdateTurnManagerUnits();
			}
		}

		private void OnUnitSelected(long index)
		{
			if (index >= 0 && index < unitManager.Units.Count)
			{
				var unit = unitManager.Units[(int)index];
				currentEditUnit = unit;
				UpdateUnitProperties(unit);
			}
		}

		private void UpdateUnitProperties(Unit unit)
		{
			// 获取DebugPanel
	var canvasLayer = rootNode.GetNode<CanvasLayer>("CanvasLayer");
	if (canvasLayer == null) return;

	var ui = canvasLayer.GetNode<Control>("UI");
	if (ui == null) return;

	var debugPanel = ui.GetNode<VBoxContainer>("DebugPanel");
	if (debugPanel == null) return;

	var propertyContainer = debugPanel.GetNode<HBoxContainer>("PropertyContainer");
	if (propertyContainer == null) return;

			// 更新属性值
					UpdatePropertyInput(propertyContainer, "max_health", unit.MaxHealth);
					UpdatePropertyInput(propertyContainer, "current_health", unit.CurrentHealth);
					UpdatePropertyInput(propertyContainer, "attack", unit.Attack);
					UpdatePropertyInput(propertyContainer, "attack_range", unit.AttackRange);
					UpdatePropertyInput(propertyContainer, "move_range", unit.MoveRange);
					UpdatePropertyInput(propertyContainer, "speed", unit.Speed);
					UpdatePropertyInput(propertyContainer, "level", unit.Level);
				
				// 根据单位类型更新经验值显示
				if (unit.IsPlayer)
				{
					// 玩家显示当前经验值/下一级升级所需经验值
					UpdateExperienceLabel(propertyContainer, $"{unit.Experience}/{unit.ExperienceToNextLevel}");
				}
				else
				{
					// 敌人显示打倒能获取到的经验值
					int experienceReward = unit.GetExperienceReward();
					UpdateExperienceLabel(propertyContainer, $"{experienceReward}");
				}
		}

		private void AddExperienceLabel(VBoxContainer container, string labelText)
		{
			// 创建水平容器
			var hbox = new HBoxContainer();
			container.AddChild(hbox);

			// 创建标签
			var label = new Label();
			label.Text = labelText;
			label.SizeFlagsHorizontal = Godot.Control.SizeFlags.ShrinkEnd;
			hbox.AddChild(label);

			// 创建显示值的标签
			var valueLabel = new Label();
			valueLabel.Name = "experience_value";
			valueLabel.SizeFlagsHorizontal = Godot.Control.SizeFlags.ExpandFill;
			hbox.AddChild(valueLabel);
		}

		private void UpdateExperienceLabel(HBoxContainer container, string text)
		{
			foreach (var child in container.GetChildren())
			{
				if (child is VBoxContainer vbox)
				{
					foreach (var hbox in vbox.GetChildren())
					{
						if (hbox is HBoxContainer hboxContainer)
						{
							foreach (var widget in hboxContainer.GetChildren())
							{
								if (widget is Label label && label.Name == "experience_value")
								{
									label.Text = text;
									return;
								}
							}
						}
					}
				}
			}
		}

		private void UpdatePropertyInput(HBoxContainer container, string propertyName, int value)
		{
			foreach (var child in container.GetChildren())
			{
				if (child is VBoxContainer vbox)
				{
					foreach (var hbox in vbox.GetChildren())
					{
						if (hbox is HBoxContainer hboxContainer)
						{
							foreach (var widget in hboxContainer.GetChildren())
							{
								if (widget is SpinBox spinBox && spinBox.Name == propertyName)
								{
									spinBox.Value = value;
									return;
								}
							}
						}
					}
				}
			}
		}

		private void OnPropertyChanged(string propertyName, double value)
		{
			if (currentEditUnit == null) return;

			int intValue = (int)value;
			switch (propertyName)
			{
				case "max_health":
					currentEditUnit.MaxHealth = intValue;
					if (currentEditUnit.CurrentHealth > currentEditUnit.MaxHealth)
					{
						currentEditUnit.CurrentHealth = currentEditUnit.MaxHealth;
					}
					break;
				case "current_health":
					currentEditUnit.CurrentHealth = intValue;
					break;
				case "attack":
					currentEditUnit.Attack = intValue;
					break;
				case "attack_range":
					currentEditUnit.AttackRange = intValue;
					break;
				case "move_range":
					currentEditUnit.MoveRange = intValue;
					break;
				case "speed":
					currentEditUnit.Speed = intValue;
					break;
				case "level":
				currentEditUnit.Level = intValue;
				// 重新计算经验值需求
				currentEditUnit.ExperienceToNextLevel = 100 + (currentEditUnit.Level - 1) * 50 + (currentEditUnit.Level - 1) * (currentEditUnit.Level - 1) * 10;
				// 确保经验值不超过升级所需
				if (currentEditUnit.Experience >= currentEditUnit.ExperienceToNextLevel)
				{
					currentEditUnit.Experience = currentEditUnit.ExperienceToNextLevel - 1;
				}
				// 升级时保留当前生命值，不恢复
				break;
	
			}

			// 更新血条显示
			if (propertyName == "max_health" || propertyName == "current_health" || propertyName == "level")
			{
				// 查找单位对应的节点
				var unitNode = FindUnitNode(currentEditUnit);
				if (unitNode != null)
				{
					unitManager.UpdateHPBar(unitNode, currentEditUnit);
				}
				
				// 如果修改的是等级，更新经验值标签
				if (propertyName == "level")
				{
					// 获取DebugPanel
					var canvasLayer = rootNode.GetNode<CanvasLayer>("CanvasLayer");
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
									// 根据单位类型更新经验值标签
									if (currentEditUnit.IsPlayer)
									{
										UpdateExperienceLabel(propertyContainer, $"{currentEditUnit.Experience}/{currentEditUnit.ExperienceToNextLevel}");
									}
									else
									{
										int experienceReward = currentEditUnit.GetExperienceReward();
										UpdateExperienceLabel(propertyContainer, $"{experienceReward}");
									}
								}
							}
						}
					}
				}
			}
		}

		// 查找单位对应的节点
		private Node FindUnitNode(Unit unit)
		{
			var canvasLayer = rootNode.GetNode<CanvasLayer>("CanvasLayer");
			if (canvasLayer == null) return null;

			var ui = canvasLayer.GetNode<Control>("UI");
			if (ui == null) return null;

			var mapContainer = ui.GetNode<Control>("MapContainer");
			if (mapContainer == null) return null;

			var mapLayer = mapContainer.GetNode<Node2D>("MapLayer");
			if (mapLayer == null) return null;

			// 遍历所有子节点，查找单位节点
			foreach (var child in mapLayer.GetChildren())
			{
				if (child.HasMeta(Constants.UNIT_META_KEY))
				{
					try
					{
						// 使用与UnitRenderer相同的方式获取单位对象
						var childUnit = child.GetMeta(Constants.UNIT_META_KEY).As<Unit>();
						if (childUnit != null && childUnit == unit)
						{
							return child;
						}
					}
					catch {}
				}
			}

			return null;
		}

		/// <summary>
		/// 安全获取整数值
		/// </summary>
		/// <param name="data">数据字典</param>
		/// <param name="key">键</param>
		/// <param name="defaultValue">默认值</param>
		/// <returns>整数值</returns>
		private int GetIntValue(Godot.Collections.Dictionary data, string key, int defaultValue)
		{
			if (data.ContainsKey(key))
			{
				var value = data[key];
				if (value.VariantType == Variant.Type.Int)
				{
					return value.AsInt32();
				}
				else if (value.VariantType == Variant.Type.Float)
				{
					return (int)value.AsDouble();
				}
			}
			return defaultValue;
		}

		/// <summary>
		/// 加载单位的背包物品
		/// </summary>
		/// <param name="unit">单位</param>
		/// <param name="unitData">单位数据</param>
		private void LoadUnitInventory(Unit unit, Godot.Collections.Dictionary unitData)
		{
			if (unitData.ContainsKey("inventory"))
			{
				var inventoryVariant = unitData["inventory"];
				if (inventoryVariant.VariantType == Variant.Type.Array)
				{
					var inventoryArray = inventoryVariant.AsGodotArray();
					foreach (var item in inventoryArray)
					{
						var itemName = item.ToString();
						var itemObj = equipmentManager.CreateEquipmentItem(itemName);
						if (itemObj != null)
						{
							if (itemObj is Items.Equipment equipment)
							{
								// 装备到单位
								unit.Equipment[equipment.Slot] = equipment;
							}
							else if (itemObj is Items.Item consumable)
							{
								unit.Inventory.AddItem(consumable);
							}
						}
					}
				}
			}
		}

		public void UpdateUnitList()
		{
			// 更新单位选择下拉框
			if (unitSelect != null)
			{
				unitSelect.Clear();
				for (int i = 0; i < unitManager.Units.Count; i++)
				{
					var unit = unitManager.Units[i];
					var unitName = unit.IsPlayer ? 
						"玩家 " + i : 
						"敌人 " + unitManager.GetUnitClassName(unit.Class) + " " + i;
					unitSelect.AddItem(unitName, i);
				}
			}
		}

		// 辅助方法，获取节点
		private T GetNode<T>(string path) where T : Node
		{
			var root = Engine.GetMainLoop() as SceneTree;
			if (root == null) return null;

			var currentScene = root.CurrentScene;
			if (currentScene == null) return null;

			return currentScene.GetNode<T>(path);
		}

		// 公开方法，用于在点击单位时更新调试菜单选择
		public void SelectUnitInDebugMenu(Unit unit)
		{
			// 查找单位在列表中的索引
			int unitIndex = unitManager.Units.IndexOf(unit);
			if (unitIndex >= 0 && unitSelect != null)
			{
				// 选择对应索引的单位
				unitSelect.Select(unitIndex);
				// 触发单位选择事件
				OnUnitSelected(unitIndex);
			}
		}

		private void OnTriggerGameOverPressed()
		{
			// 触发游戏失败
			var gameManager = rootNode as GameManager;
			if (gameManager != null)
			{
				// 调用UIManager的ShowGameOverMenu方法，传入false表示失败
				var uiManager = gameManager.UIManager;
				if (uiManager != null)
				{
					uiManager.ShowGameOverMenu(false);
				}
			}
		}
	}
}
