using Godot;
using System.Collections.Generic;
using System.Linq;

namespace CSharpTestGame
{
	public class TurnManager
	{
		private List<Unit> units;
		private int currentUnitIndex;
		private int roundCount;
		public delegate void TurnChangeHandler(Unit? currentUnit, int round);
		public event TurnChangeHandler TurnChange = delegate { };

		public TurnManager()
		{
			units = new List<Unit>();
			currentUnitIndex = 0;
			roundCount = 1;
		}

		public void SetUnits(List<Unit> newUnits)
		{
			units = newUnits.OrderByDescending(u => u.Speed).ToList();
			currentUnitIndex = 0;
		}

		public Unit? GetCurrentUnit()
		{
			if (units.Count == 0 || currentUnitIndex >= units.Count)
			{
				return null;
			}
			return units[currentUnitIndex];
		}

		public void NextTurn()
		{
			currentUnitIndex++;
			
			// 检查是否所有单位都行动完毕
			if (currentUnitIndex >= units.Count)
			{
				// 进入下一轮
				roundCount++;
				currentUnitIndex = 0;
				// 重新按速度排序
				units = units.OrderByDescending(u => u.Speed).ToList();
			}

			// 移除死亡单位
			units = units.Where(u => u.IsAlive()).ToList();
			if (currentUnitIndex >= units.Count)
			{
				currentUnitIndex = 0;
			}

			var currentUnit = GetCurrentUnit();
			GD.Print($"Turn changed to unit: {currentUnit?.Position}, Round: {roundCount}");
			TurnChange?.Invoke(currentUnit, roundCount);
			GD.Print("Signal emitted");
		}

		public int GetRoundCount()
		{
			return roundCount;
		}

		public bool IsPlayerTurn()
		{
			var currentUnit = GetCurrentUnit();
			return currentUnit != null && currentUnit.IsPlayer;
		}
	}
}
