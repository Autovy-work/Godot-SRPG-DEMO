using Godot;
using System.Collections.Generic;

namespace CSharpTestGame
{
	public class UnitManager
	{
		private List<Unit> units;
		private UnitRenderer unitRenderer;

		public List<Unit> Units { get { return units; } }

		public UnitManager(Node2D mapLayer)
		{
			units = new List<Unit>();
			unitRenderer = new UnitRenderer(mapLayer);
		}

		public void Initialize()
		{
			// 创建玩家单位（战士类型，两种攻击方式都支持）
		var playerUnit = Unit.Create(
			Constants.DEFAULT_PLAYER_HEALTH,
			Constants.DEFAULT_PLAYER_ATTACK,
			Constants.DEFAULT_PLAYER_ATTACK_RANGE,
			Constants.DEFAULT_PLAYER_MOVE_RANGE,
			Constants.DEFAULT_PLAYER_SPEED,
			Unit.UnitClass.Warrior,
			new Vector2(0, 0),
			true
		);
			units.Add(playerUnit);
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
			Unit playerUnit = units.Find(u => u.IsPlayer);
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

				// 根据职业生成属性
				int enemyMaxHealth, enemyAttack, enemyAttackRange, enemyMoveRange, enemySpeed;

				if (enemyClass == Unit.UnitClass.WarAngel)
				{
					// 战争天使单位属性比玩家低10%-20%
					float eliteFactor = Constants.ELITE_FACTOR_MIN + (float)GD.Randf() * (Constants.ELITE_FACTOR_MAX - Constants.ELITE_FACTOR_MIN);
					enemyMaxHealth = (int)(playerMaxHealth * eliteFactor);
					enemyAttack = (int)(playerAttack * eliteFactor);
					enemyAttackRange = 4; // 战争天使单位有远程攻击能力
					enemyMoveRange = 3; // 增加移动距离
					enemySpeed = (int)(playerSpeed * eliteFactor);
				}
				else
				{
					// 普通单位属性比玩家低40%-60%
					float normalFactor = Constants.NORMAL_FACTOR_MIN + (float)GD.Randf() * (Constants.NORMAL_FACTOR_MAX - Constants.NORMAL_FACTOR_MIN);
					enemyMaxHealth = (int)(playerMaxHealth * normalFactor);
					enemyAttack = (int)(playerAttack * normalFactor);

					if (enemyClass == Unit.UnitClass.Goblin)
					{
						enemyAttackRange = 1; // 哥布林只能近战
						enemyMoveRange = 3; // 增加移动距离
					}
					else if (enemyClass == Unit.UnitClass.Skeleton)
				{
					// 骷髅士兵只能近战，但属性比哥布林稍强
					enemyAttackRange = 1; // 骷髅士兵只能近战
					enemyMoveRange = 3; // 增加移动距离
					// 骷髅士兵属性比普通单位高20%
					enemyMaxHealth = (int)(enemyMaxHealth * 1.2f);
					enemyAttack = (int)(enemyAttack * 1.2f);
				}
				// 初始化速度
					enemySpeed = (int)(playerSpeed * normalFactor);

					if (enemyClass == Unit.UnitClass.Goblin)
					{
						// 哥布林只能近战
						enemyAttackRange = 1; // 哥布林只能近战
						enemyMoveRange = 3; // 增加移动距离
					}
					else if (enemyClass == Unit.UnitClass.Skeleton)
					{
						// 骷髅士兵只能近战，但属性比哥布林稍强
						enemyAttackRange = 1; // 骷髅士兵只能近战
						enemyMoveRange = 3; // 增加移动距离
						// 骷髅士兵属性比普通单位高20%
						enemyMaxHealth = (int)(enemyMaxHealth * 1.2f);
						enemyAttack = (int)(enemyAttack * 1.2f);
					}
					else if (enemyClass == Unit.UnitClass.Acolyte)
					{
						// 生命法师，弱近战攻击，移动较慢
						enemyAttackRange = 1; // 生命法师只能近战
						enemyMoveRange = 2; // 移动较慢
						// 生命法师攻击力较弱，但生命值较高
						enemyMaxHealth = (int)(enemyMaxHealth * 1.3f); // 生命值较高
						enemyAttack = (int)(enemyAttack * 0.7f); // 攻击力较弱
						enemySpeed = (int)(playerSpeed * normalFactor * 0.8f); // 速度较慢
					}
					else // ElfArcher
					{
						// 精灵弓手只能远程
						enemyAttackRange = 4; // 精灵弓手只能远程
						enemyMoveRange = 3; // 增加移动距离
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
				while (attempts < Constants.MAX_ENEMY_SPAWN_ATTEMPTS);

				// 如果找不到合适的位置，使用默认位置
				if (attempts >= Constants.MAX_ENEMY_SPAWN_ATTEMPTS)
				{
					enemyPosition = new Vector2(4 + i, 4 + i);
					GD.Print(Constants.COULD_NOT_FIND_VALID_POSITION);
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
			switch (unitClass)
			{
				case Unit.UnitClass.Goblin:
					return "哥布林";
				case Unit.UnitClass.ElfArcher:
					return "精灵弓手";
				case Unit.UnitClass.WarAngel:
					return "战争天使";
				case Unit.UnitClass.Skeleton:
					return "骷髅士兵";
				case Unit.UnitClass.Acolyte:
				return "生命法师";
			case Unit.UnitClass.Warrior:
				return "战士";
			default:
				return "未知";
			}
		}
	}
}
