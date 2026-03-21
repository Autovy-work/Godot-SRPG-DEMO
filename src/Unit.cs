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

		public Unit() : base()
		{
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
			return unit;
		}

		public int GetEffectiveAttack()
		{
			return Attack;
		}

		public int GetEffectiveMoveRange()
		{
			return MoveRange;
		}

		public int GetEffectiveAttackRange()
		{
			return AttackRange;
		}

		public void TakeDamage(int amount)
		{
			CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
		}

		public bool IsAlive()
		{
			return CurrentHealth > 0;
		}
	}
}
