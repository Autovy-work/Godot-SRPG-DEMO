using Godot;
using System.Collections.Generic;
using CSharpTestGame.Managers;
using CSharpTestGame.Items;

namespace CSharpTestGame
{
	public class UnitManager
	{
		private List<Unit> units;
		private UnitRenderer unitRenderer;
		private DataLoader dataLoader;
		private EquipmentManager equipmentManager;

		public List<Unit> Units { get { return units; } }

		public UnitManager(Node2D mapLayer, DataLoader dataLoader, EquipmentManager equipmentManager)
		{
			units = new List<Unit>();
			unitRenderer = new UnitRenderer(mapLayer);
			this.dataLoader = dataLoader;
			this.equipmentManager = equipmentManager;
		}

		public void Initialize()
		{
			// 从数据文件加载玩家单位数据
			var playerData = dataLoader.GetUnitData("Warrior");
			if (playerData != null)
			{
				// 读取图像路径
				string imagePath = playerData.ContainsKey("image") ? playerData["image"].ToString() : "";

				// 安全读取属性
				int maxHealth = GetIntValue(playerData, "max_health", 15);
				int attack = GetIntValue(playerData, "attack", 5);
				int attackRange = GetIntValue(playerData, "attack_range", 4);
				int moveRange = GetIntValue(playerData, "move_range", 4);
				int speed = GetIntValue(playerData, "speed", 6);

				// 创建玩家单位（战士类型）
				var playerUnit = Unit.Create(
					maxHealth,
					attack,
					attackRange,
					moveRange,
					speed,
					Unit.UnitClass.Warrior,
					new Vector2(0, 0),
					true,
					imagePath
				);

				// 加载玩家单位的背包物品
				LoadUnitInventory(playerUnit, playerData);

				units.Add(playerUnit);
			}
			else
			{
				// 如果加载失败，使用默认值
				var defaultPlayerStats = dataLoader.GetDefaultPlayerStats();
				int defaultHealth = 15;
				int defaultAttack = 5;
				int defaultAttackRange = 4;
				int defaultMoveRange = 4;
				int defaultSpeed = 6;

				if (defaultPlayerStats != null)
				{
					defaultHealth = GetIntValue(defaultPlayerStats, "max_health", defaultHealth);
					defaultAttack = GetIntValue(defaultPlayerStats, "attack", defaultAttack);
					defaultAttackRange = GetIntValue(defaultPlayerStats, "attack_range", defaultAttackRange);
					defaultMoveRange = GetIntValue(defaultPlayerStats, "move_range", defaultMoveRange);
					defaultSpeed = GetIntValue(defaultPlayerStats, "speed", defaultSpeed);
				}

				var playerUnit = Unit.Create(
					defaultHealth,
					defaultAttack,
					defaultAttackRange,
					defaultMoveRange,
					defaultSpeed,
					Unit.UnitClass.Warrior,
					new Vector2(0, 0),
					true
				);
				units.Add(playerUnit);
				GD.PrintErr("Failed to load player unit data, using default values");
			}
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
		private void LoadUnitInventory(Unit unit, Godot.Collections.Dictionary? unitData)
		{
			if (unitData == null)
			{
				return;
			}

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
							if (itemObj is Equipment equipment)
							{
								// 装备到单位
								unit.Equipment[equipment.Slot] = equipment;
							}
							else if (itemObj is Item consumable)
							{
								unit.Inventory.AddItem(consumable);
							}
						}
					}
				}
			}
		}

		public void DrawUnits()
		{
			// 绘制单位
			for (int i = 0; i < units.Count; i++)
			{
				unitRenderer.DrawUnit(units[i], i);
			}
		}

		public void UpdateHPBar(Node unitNode, Unit unit)
		{
			unitRenderer.UpdateHPBar(unitNode, unit);
		}

		public void GenerateRandomEnemies(int count, Grid grid)
		{
			// 获取玩家单位
			Unit? playerUnit = units.Find(u => u.IsPlayer);
			if (playerUnit == null)
			{
				GD.Print(Constants.NO_PLAYER_UNIT_FOUND);
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
			Unit.UnitClass[] classes = { Unit.UnitClass.Goblin, Unit.UnitClass.ElfArcher, Unit.UnitClass.WarAngel, Unit.UnitClass.Skeleton, Unit.UnitClass.Acolyte };

			for (int i = 0; i < count; i++)
			{
				// 随机选择职业
				Unit.UnitClass enemyClass = classes[GD.Randi() % classes.Length];

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
				while (attempts < dataLoader.GetMaxEnemySpawnAttempts());

				// 如果找不到合适的位置，使用默认位置
				if (attempts >= dataLoader.GetMaxEnemySpawnAttempts())
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
					false,
					imagePath
				);

				// 加载敌人单位的背包物品
				LoadUnitInventory(enemyUnit, unitData);

				units.Add(enemyUnit);
				unitRenderer.DrawUnit(enemyUnit, units.Count - 1);
				GD.Print($"Created enemy unit {i+1}: Class={enemyClass}, Position={enemyPosition}, Health={enemyMaxHealth}, Attack={enemyAttack}");
			}
		}



		public bool IsCellFree(Vector2 pos)
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

		public void RemoveUnit(Unit unit)
		{
			// 移除单位节点
			unitRenderer.RemoveUnitNode(unit);
			// 从列表中移除
			units.Remove(unit);
		}

		public void RefreshUnitNodePosition(Unit unit, Vector2 newPosition)
		{
			unitRenderer.RefreshUnitNodePosition(unit, newPosition);
		}

		public void DrawUnit(Unit unit)
		{
			unitRenderer.DrawUnit(unit, units.Count - 1);
		}

		public string GetUnitClassName(Unit.UnitClass unitClass)
		{
			return dataLoader.GetUnitClassName(unitClass.ToString());
		}
	}
}
