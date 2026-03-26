using Godot;
using System.Collections.Generic;
using CSharpTestGame.Items;

namespace CSharpTestGame
{
	[GlobalClass]
	public partial class Unit : Resource
	{
		public enum UnitClass
	{
		Goblin,    // 哥布林
		ElfArcher, // 精灵弓手
		WarAngel,  // 战争天使
		Skeleton,  // 骷髅士兵
		Acolyte,   // 生命法师
		Warrior    // 战士
	}

	public int MaxHealth { get; set; }
	public int CurrentHealth { get; set; }
	public int Attack { get; set; }
	public int AttackRange { get; set; }
	public int MoveRange { get; set; }
	public int Luck { get; set; }
	public int Speed { get; set; }
	public UnitClass Class { get; set; }
	public Vector2 Position { get; set; }
	public bool IsPlayer { get; set; }
	public int Level { get; set; }
	public int Experience { get; set; }
	public int ExperienceToNextLevel { get; set; }
	public Dictionary<Equipment.EquipmentSlot, Equipment> Equipment { get; set; }
		public Inventory Inventory { get; set; }
		public string ImagePath { get; set; }

		public Unit() : base()
		{
			Level = 1;
			Experience = 0;
			ExperienceToNextLevel = 100;
			Equipment = new Dictionary<Equipment.EquipmentSlot, Equipment>();
			Inventory = new Inventory();
		}

		public static Unit Create(int maxHealth, int attack, int attackRange, int moveRange, int speed, UnitClass unitClass, Vector2 position, bool isPlayer, string imagePath = "")
		{
			var unit = new Unit();
			unit.MaxHealth = maxHealth;
			unit.CurrentHealth = maxHealth;
			unit.Attack = attack;
			unit.AttackRange = attackRange;
			unit.MoveRange = moveRange;
			unit.Luck = 5;
			unit.Speed = speed;
			unit.Class = unitClass;
			unit.Position = position;
			unit.IsPlayer = isPlayer;
			unit.Level = 1;
			unit.Experience = 0;
			unit.ExperienceToNextLevel = 100;
			unit.Equipment = new Dictionary<Equipment.EquipmentSlot, Equipment>();
			unit.Inventory = new Inventory();
			unit.ImagePath = imagePath;
			return unit;
		}

		public int GetEffectiveAttack()
		{
			int attack = Attack + Level - 1;
			foreach (var equipment in Equipment.Values)
			{
				attack += equipment.AttackBonus;
			}
			return attack;
		}

		public int GetEffectiveMoveRange()
		{
			return MoveRange + (Level - 1) / 3;
		}

		public int GetEffectiveAttackRange()
		{
			return AttackRange + (Level - 1) / 5;
		}

		public int GetEffectiveMaxHealth()
		{
			int maxHealth = MaxHealth + (Level - 1) * 2;
			foreach (var equipment in Equipment.Values)
			{
				maxHealth += equipment.DefenseBonus;
			}
			return maxHealth;
		}

		public int GetEffectiveSpeed()
		{
			int speed = Speed + (Level - 1) / 2;
			foreach (var equipment in Equipment.Values)
			{
				speed += equipment.SpeedBonus;
			}
			return speed;
		}

		public int GetEffectiveLuck()
		{
			int luck = Luck;
			foreach (var equipment in Equipment.Values)
			{
				luck += equipment.LuckBonus;
			}
			return luck;
		}

		public void TakeDamage(int amount)
		{
			CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
		}

		public bool IsAlive()
		{
			return CurrentHealth > 0;
		}

		public void AddExperience(int amount)
		{
			Experience += amount;
			CheckLevelUp();
		}

		private void CheckLevelUp()
		{
			while (Experience >= ExperienceToNextLevel)
			{
				Experience -= ExperienceToNextLevel;
				Level++;
				ExperienceToNextLevel = CalculateExperienceToNextLevel();
				// 升级时保留当前生命值，不恢复
			}
		}

		private int CalculateExperienceToNextLevel()
		{
			return 100 + (Level - 1) * 50 + (Level - 1) * (Level - 1) * 10;
		}

		public int GetExperienceReward()
		{
			return 50 + Level * 10;
		}
	}
}
