using Godot;
using System.Collections.Generic;

namespace CSharpTestGame
{
	public class UnitManager
	{
		private List<Unit> units;
		private Node2D mapLayer;

		public List<Unit> Units { get { return units; } }

		public UnitManager(Node2D mapLayer)
		{
			this.mapLayer = mapLayer;
			units = new List<Unit>();
		}

		public void Initialize()
		{
			// 创建玩家单位（精英类型，两种攻击方式都支持）
			var playerUnit = Unit.Create(15, 5, 4, 4, 6, Unit.UnitClass.Elite, new Vector2(0, 0), true);
			units.Add(playerUnit);
		}

		public void DrawUnits()
		{
			// 绘制单位
			foreach (var unit in units)
			{
				DrawUnit(unit);
			}
		}

		public void DrawUnit(Unit unit)
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
						textureContainer.StretchMode = TextureRect.StretchModeEnum.Scale;
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
						textureContainer.StretchMode = TextureRect.StretchModeEnum.Scale;
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

			// 添加血条
			var hpBar = new ColorRect();
			hpBar.Size = new Vector2(40, 5);
			// 血条放到上方
			hpBar.Position = new Vector2(0, -10);
			hpBar.Color = new Color(0.8f, 0.2f, 0.2f);
			hpBar.Name = "HPBar";
			container.AddChild(hpBar);
			UpdateHPBar(container, unit);

			mapLayer.AddChild(container);
		}

		public void UpdateHPBar(Node unitNode, Unit unit)
		{
			var hpBar = unitNode.GetNode<ColorRect>("HPBar");
			if (hpBar != null)
			{
				hpBar.Scale = new Vector2((float)unit.CurrentHealth / unit.MaxHealth, 1);
			}
		}

		public void GenerateRandomEnemies(int count, Grid grid)
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
				DrawUnit(enemyUnit);
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
			foreach (var child in mapLayer.GetChildren())
			{
				if (child.HasMeta("unit") && child.GetMeta("unit").As<Unit>() == unit)
				{
					child.QueueFree();
					break;
				}
			}
			// 从列表中移除
			units.Remove(unit);
		}

		public void RefreshUnitNodePosition(Unit unit, Vector2 newPosition)
		{
			foreach (var child in mapLayer.GetChildren())
			{
				if (child.HasMeta("unit"))
				{
					var childUnit = child.GetMeta("unit").As<Unit>();
					if (childUnit != null && childUnit == unit)
					{
						if (child is Control control)
						{
							control.Position = new Vector2(newPosition.X * 56, newPosition.Y * 56);
						}
						break;
					}
				}
			}
		}

		public string GetUnitClassName(Unit.UnitClass unitClass)
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
	}
}
