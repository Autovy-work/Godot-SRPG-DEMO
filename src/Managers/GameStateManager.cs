using Godot;
using System.Collections.Generic;

namespace CSharpTestGame
{
	public class GameStateManager
	{
		private UnitManager unitManager;
		private RichTextLabel battleLog;
		private UIManager uiManager;
		private bool isBattleOver = false;

		public bool IsBattleOver { get { return isBattleOver; } }

		public GameStateManager(UnitManager unitManager, RichTextLabel battleLog)
		{
			this.unitManager = unitManager;
			this.battleLog = battleLog;
			this.uiManager = new UIManager();
		}

		public void CheckBattleEnd()
		{
			// 检查胜利条件：所有敌人都死亡
			bool allEnemiesDead = true;
			foreach (var unit in unitManager.Units)
			{
				if (!unit.IsPlayer && unit.IsAlive())
				{
					allEnemiesDead = false;
					break;
				}
			}

			// 检查失败条件：玩家死亡
			bool playerDead = false;
			foreach (var unit in unitManager.Units)
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
				uiManager.ShowSettlementMenu(allEnemiesDead);
			}
		}

		public void CheckGameOver()
		{
			// 检查是否所有玩家单位都死亡
			bool playerAlive = false;
			foreach (var unit in unitManager.Units)
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
				uiManager.ShowGameOverMenu(false);
				return;
			}
			// 检查是否所有敌人单位都死亡
			bool enemyAlive = false;
			foreach (var unit in unitManager.Units)
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
				uiManager.ShowGameOverMenu(true);
			}
		}
	}
}
