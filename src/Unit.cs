using Godot;
using System.Collections.Generic;

namespace CSharpTestGame
{
	[GlobalClass]
	public partial class Unit : Resource
	{
		public enum UnitClass
	{
			Melee,    // 近战
			Ranged,   // 远程
			Elite     // 精英
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

		public Unit() : base()
		{
			Level = 1;
			Experience = 0;
			ExperienceToNextLevel = 100;
		}

		public static Unit Create(int maxHealth, int attack, int attackRange, int moveRange, int speed, UnitClass unitClass, Vector2 position, bool isPlayer)
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
			return unit;
		}

		public int GetEffectiveAttack()
		{
			return Attack + Level - 1;
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
			return MaxHealth + (Level - 1) * 2;
		}

		public int GetEffectiveSpeed()
		{
			return Speed + (Level - 1) / 2;
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
