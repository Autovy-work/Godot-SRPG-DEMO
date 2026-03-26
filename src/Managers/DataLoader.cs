using Godot;
using System.Collections.Generic;

namespace CSharpTestGame.Managers
{
	[GlobalClass]
	public partial class DataLoader : RefCounted
	{
		private Dictionary<string, Godot.Collections.Dictionary> equipmentData;
		private Dictionary<string, Godot.Collections.Dictionary> unitData;
		private Godot.Collections.Dictionary gameConfigData;

		/// <summary>
		/// 加载所有数据文件
		/// </summary>
		public void LoadData()
		{
			LoadEquipmentData();
			LoadUnitData();
			LoadGameConfigData();
		}

		/// <summary>
		/// 加载装备数据
		/// </summary>
		private void LoadEquipmentData()
		{
			equipmentData = new Dictionary<string, Godot.Collections.Dictionary>();

			try
			{
				var file = FileAccess.Open("res://Data/equipment.json", FileAccess.ModeFlags.Read);
				if (file != null)
				{
					var jsonString = file.GetAsText();
					var json = new Json();
					var error = json.Parse(jsonString);
					if (error == Error.Ok)
					{
						var dataVariant = json.Data;
						if (dataVariant.VariantType == Variant.Type.Dictionary)
						{
							var data = dataVariant.AsGodotDictionary();
							if (data.ContainsKey("equipment"))
							{
								var equipmentListVariant = data["equipment"];
								if (equipmentListVariant.VariantType == Variant.Type.Array)
								{
									var equipmentList = equipmentListVariant.AsGodotArray();
									foreach (var item in equipmentList)
									{
										var itemVariant = (Variant)item;
										if (itemVariant.VariantType == Variant.Type.Dictionary)
										{
											var equipment = itemVariant.AsGodotDictionary();
											if (equipment.ContainsKey("name"))
											{
												var name = equipment["name"].ToString();
												if (!string.IsNullOrEmpty(name))
												{
													equipmentData[name] = equipment;
												}
											}
										}
									}
								}
							}
						}
					}
					file.Close();
				}
			}
			catch (System.Exception e)
			{
				GD.PrintErr($"Error loading equipment data: {e.Message}");
			}
		}

		/// <summary>
		/// 加载单位数据
		/// </summary>
		private void LoadUnitData()
		{
			unitData = new Dictionary<string, Godot.Collections.Dictionary>();

			try
			{
				var file = FileAccess.Open("res://Data/units.json", FileAccess.ModeFlags.Read);
				if (file != null)
				{
					var jsonString = file.GetAsText();
					var json = new Json();
					var error = json.Parse(jsonString);
					if (error == Error.Ok)
					{
						var dataVariant = json.Data;
						if (dataVariant.VariantType == Variant.Type.Dictionary)
						{
							var data = dataVariant.AsGodotDictionary();
							if (data.ContainsKey("units"))
							{
								var unitListVariant = data["units"];
								if (unitListVariant.VariantType == Variant.Type.Array)
								{
									var unitList = unitListVariant.AsGodotArray();
									foreach (var item in unitList)
									{
										var itemVariant = (Variant)item;
										if (itemVariant.VariantType == Variant.Type.Dictionary)
										{
											var unit = itemVariant.AsGodotDictionary();
											if (unit.ContainsKey("class"))
											{
												var unitClass = unit["class"].ToString();
												if (!string.IsNullOrEmpty(unitClass))
												{
													unitData[unitClass] = unit;
												}
											}
										}
									}
								}
							}
						}
					}
					file.Close();
				}
			}
			catch (System.Exception e)
			{
				GD.PrintErr($"Error loading unit data: {e.Message}");
			}
		}

		/// <summary>
		/// 根据名称获取装备数据
		/// </summary>
		/// <param name="equipmentName">装备名称</param>
		/// <returns>装备数据</returns>
		public Godot.Collections.Dictionary GetEquipmentData(string equipmentName)
		{
			if (equipmentData.TryGetValue(equipmentName, out var data))
			{
				return data;
			}
			return null;
		}

		/// <summary>
		/// 根据单位类型获取单位数据
		/// </summary>
		/// <param name="unitClass">单位类型</param>
		/// <returns>单位数据</returns>
		public Godot.Collections.Dictionary GetUnitData(string unitClass)
		{
			if (unitData.TryGetValue(unitClass, out var data))
			{
				return data;
			}
			return null;
		}

		/// <summary>
		/// 获取所有装备数据
		/// </summary>
		/// <returns>装备数据字典</returns>
		public Dictionary<string, Godot.Collections.Dictionary> GetAllEquipmentData()
		{
			return equipmentData;
		}

		/// <summary>
		/// 获取所有单位数据
		/// </summary>
		/// <returns>单位数据字典</returns>
		public Dictionary<string, Godot.Collections.Dictionary> GetAllUnitData()
		{
			return unitData;
		}

		/// <summary>
		/// 加载游戏配置数据
		/// </summary>
		private void LoadGameConfigData()
		{
			try
			{
				var file = FileAccess.Open("res://Data/game_config.json", FileAccess.ModeFlags.Read);
				if (file != null)
				{
					var jsonString = file.GetAsText();
					var json = new Json();
					var error = json.Parse(jsonString);
					if (error == Error.Ok)
					{
						var dataVariant = json.Data;
						if (dataVariant.VariantType == Variant.Type.Dictionary)
						{
							var data = dataVariant.AsGodotDictionary();
							if (data.ContainsKey("game"))
							{
								var gameVariant = data["game"];
								if (gameVariant.VariantType == Variant.Type.Dictionary)
								{
									gameConfigData = gameVariant.AsGodotDictionary();
								}
							}
						}
					}
					file.Close();
				}
			}
			catch (System.Exception e)
			{
				GD.PrintErr($"Error loading game config data: {e.Message}");
			}
		}

		/// <summary>
		/// 获取游戏配置数据
		/// </summary>
		/// <returns>游戏配置数据</returns>
		public Godot.Collections.Dictionary GetGameConfigData()
		{
			return gameConfigData;
		}

		/// <summary>
		/// 获取单位类别的显示名称
		/// </summary>
		/// <param name="unitClass">单位类别</param>
		/// <returns>显示名称</returns>
		public string GetUnitClassName(string unitClass)
		{
			if (unitData != null && unitData.ContainsKey(unitClass))
			{
				var unit = unitData[unitClass];
				if (unit != null && unit.ContainsKey("name"))
				{
					return unit["name"].ToString();
				}
			}
			return "未知";
		}

		/// <summary>
		/// 获取敌人生成的最大尝试次数
		/// </summary>
		/// <returns>最大尝试次数</returns>
		public int GetMaxEnemySpawnAttempts()
		{
			if (gameConfigData != null && gameConfigData.ContainsKey("enemy_spawn"))
			{
				var enemySpawn = gameConfigData["enemy_spawn"].AsGodotDictionary();
				if (enemySpawn.ContainsKey("max_attempts"))
				{
					var value = enemySpawn["max_attempts"];
					if (value.VariantType == Variant.Type.Int)
					{
						return value.AsInt32();
					}
					else if (value.VariantType == Variant.Type.Float)
					{
						return (int)value.AsDouble();
					}
				}
			}
			return 100; // 默认值
		}

		/// <summary>
		/// 获取默认的敌人生成数量
		/// </summary>
		/// <returns>敌人生成数量</returns>
		public int GetDefaultEnemyCount()
		{
			if (gameConfigData != null && gameConfigData.ContainsKey("enemy_spawn"))
			{
				var enemySpawn = gameConfigData["enemy_spawn"].AsGodotDictionary();
				if (enemySpawn.ContainsKey("default_count"))
				{
					var value = enemySpawn["default_count"];
					if (value.VariantType == Variant.Type.Int)
					{
						return value.AsInt32();
					}
					else if (value.VariantType == Variant.Type.Float)
					{
						return (int)value.AsDouble();
					}
				}
			}
			return 3; // 默认值
		}

		/// <summary>
		/// 获取默认的障碍物生成数量
		/// </summary>
		/// <returns>障碍物生成数量</returns>
		public int GetDefaultObstacleCount()
		{
			if (gameConfigData != null && gameConfigData.ContainsKey("obstacle_spawn"))
			{
				var obstacleSpawn = gameConfigData["obstacle_spawn"].AsGodotDictionary();
				if (obstacleSpawn.ContainsKey("default_count"))
				{
					var value = obstacleSpawn["default_count"];
					if (value.VariantType == Variant.Type.Int)
					{
						return value.AsInt32();
					}
					else if (value.VariantType == Variant.Type.Float)
					{
						return (int)value.AsDouble();
					}
				}
			}
			return 5; // 默认值
		}

		/// <summary>
		/// 获取默认的地图宽度
		/// </summary>
		/// <returns>地图宽度</returns>
		public int GetDefaultMapWidth()
		{
			if (gameConfigData != null && gameConfigData.ContainsKey("map"))
			{
				var map = gameConfigData["map"].AsGodotDictionary();
				if (map.ContainsKey("default_width"))
				{
					var value = map["default_width"];
					if (value.VariantType == Variant.Type.Int)
					{
						return value.AsInt32();
					}
					else if (value.VariantType == Variant.Type.Float)
					{
						return (int)value.AsDouble();
					}
				}
			}
			return 10; // 默认值
		}

		/// <summary>
		/// 获取默认的地图高度
		/// </summary>
		/// <returns>地图高度</returns>
		public int GetDefaultMapHeight()
		{
			if (gameConfigData != null && gameConfigData.ContainsKey("map"))
			{
				var map = gameConfigData["map"].AsGodotDictionary();
				if (map.ContainsKey("default_height"))
				{
					var value = map["default_height"];
					if (value.VariantType == Variant.Type.Int)
					{
						return value.AsInt32();
					}
					else if (value.VariantType == Variant.Type.Float)
					{
						return (int)value.AsDouble();
					}
				}
			}
			return 10; // 默认值
		}

		/// <summary>
		/// 获取玩家单位的默认属性
		/// </summary>
		/// <returns>玩家单位默认属性</returns>
		public Godot.Collections.Dictionary GetDefaultPlayerStats()
		{
			if (gameConfigData != null && gameConfigData.ContainsKey("default_unit_stats"))
			{
				var defaultStats = gameConfigData["default_unit_stats"].AsGodotDictionary();
				if (defaultStats.ContainsKey("player"))
				{
					return defaultStats["player"].AsGodotDictionary();
				}
			}
			return null;
		}

		/// <summary>
		/// 获取敌人单位的默认属性
		/// </summary>
		/// <returns>敌人单位默认属性</returns>
		public Godot.Collections.Dictionary GetDefaultEnemyStats()
		{
			if (gameConfigData != null && gameConfigData.ContainsKey("default_unit_stats"))
			{
				var defaultStats = gameConfigData["default_unit_stats"].AsGodotDictionary();
				if (defaultStats.ContainsKey("enemy"))
				{
					return defaultStats["enemy"].AsGodotDictionary();
				}
			}
			return null;
		}

		/// <summary>
		/// 获取装备槽位的显示名称
		/// </summary>
		/// <param name="slot">装备槽位</param>
		/// <returns>显示名称</returns>
		public string GetEquipmentSlotName(string slot)
		{
			if (gameConfigData != null && gameConfigData.ContainsKey("equipment_slot_names"))
			{
				var slotNames = gameConfigData["equipment_slot_names"].AsGodotDictionary();
				if (slotNames.ContainsKey(slot))
				{
					return slotNames[slot].ToString();
				}
			}
			return "未知";
		}
	}
}