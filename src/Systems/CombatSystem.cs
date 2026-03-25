using Godot;
using System.Collections.Generic;

namespace CSharpTestGame
{
	public class CombatSystem
	{
		private Grid grid;
		private UnitManager unitManager;
		private Node2D mapLayer;
		private RichTextLabel battleLog;
		private GameStateManager gameStateManager;
		private CombatRenderer combatRenderer;

		public CombatSystem(Grid grid, UnitManager unitManager, Node2D mapLayer, RichTextLabel battleLog, GameStateManager gameStateManager)
		{
			this.grid = grid;
			this.unitManager = unitManager;
			this.mapLayer = mapLayer;
			this.battleLog = battleLog;
			this.gameStateManager = gameStateManager;
			this.combatRenderer = new CombatRenderer(mapLayer);
		}

		public void CalculateAttackRange(Unit unit, int attackRange)
		{
			GD.Print("Calculating attack range for unit at: " + unit.Position);
			GD.Print("Attack range: " + attackRange);
			int count = 0;
			for (int y = 0; y < grid.GridSize.Y; y++)
			{
				for (int x = 0; x < grid.GridSize.X; x++)
				{
					var pos = new Vector2(x, y);
					// 计算距离（使用整数位置避免浮点数精度问题）
					var unitPos = new Vector2(Mathf.FloorToInt(unit.Position.X), Mathf.FloorToInt(unit.Position.Y));
					var distance = Mathf.Abs(pos.X - unitPos.X) + Mathf.Abs(pos.Y - unitPos.Y); // 曼哈顿距离

					// 跳过角色所在的中心位置
					if (distance == 0)
					{
						continue;
					}

					// 对于远程攻击（攻击范围大于1），不包括距离为1的格子
				int minDistance = 1;
				if (attackRange > 1)
				{
					minDistance = 2;
				}
				
				// 近战攻击允许距离1（所有单位）
				if (attackRange == 1 && distance == 1)
				{
					minDistance = 1; // 近战攻击允许距离1
				}

					if (distance <= attackRange && distance >= minDistance)
					{
						// 检查是否有敌人在该位置
						Unit targetUnit = null;
						foreach (var u in unitManager.Units)
						{
							// 直接使用整数坐标比较，避免浮点数精度问题
							int uX = (int)u.Position.X;
							int uY = (int)u.Position.Y;
							int pX = (int)pos.X;
							int pY = (int)pos.Y;
							if (uX == pX && uY == pY && u.IsPlayer != unit.IsPlayer && u.IsAlive())
							{
								targetUnit = u;
								GD.Print("Found target unit at: " + u.Position + " in cell: " + pos);
								break;
							}
						}
						// 不管有没有敌人，都显示攻击范围
						GD.Print("Highlighting attack cell: " + pos);
						combatRenderer.HighlightAttackCell(pos, targetUnit);
						count++;
					}
				}
			}
			GD.Print("Total attack cells: " + count);
		}

		public void ClearHighlights()
		{
			combatRenderer.ClearHighlights();
		}

		public void ShowAttackRange(Unit unit, int attackRange)
		{
			GD.Print("Calculating attack range for unit at: " + unit.Position);
			GD.Print("Attack range: " + attackRange);
			// 清除之前的高亮
			ClearHighlights();
			int count = 0;
			for (int y = 0; y < grid.GridSize.Y; y++)
			{
				for (int x = 0; x < grid.GridSize.X; x++)
				{
					var pos = new Vector2(x, y);
					// 计算距离（使用整数位置避免浮点数精度问题）
					var unitPos = new Vector2(Mathf.FloorToInt(unit.Position.X), Mathf.FloorToInt(unit.Position.Y));
					var distance = Mathf.Abs(pos.X - unitPos.X) + Mathf.Abs(pos.Y - unitPos.Y); // 曼哈顿距离
					
					// 跳过角色所在的中心位置
					if (distance == 0)
					{
						continue;
					}
					
					// 对于远程攻击（攻击范围大于1），不包括距离为1的格子
					int minDistance = 1;
					int originalAttackRange = unit.GetEffectiveAttackRange();
					if (originalAttackRange > 1)
					{
						minDistance = 2;
					}
					
					// 精英单位特殊处理：只有在近战攻击时才允许距离1
					if (unit.Class == Unit.UnitClass.Elite && distance == 1 && attackRange == 1)
					{
						minDistance = 1; // 近战攻击允许距离1
					}
					
					if (distance <= attackRange && distance >= minDistance)
					{
						// 检查是否有敌人在该位置
						Unit targetUnit = null;
						foreach (var u in unitManager.Units)
						{
							// 直接使用整数坐标比较，避免浮点数精度问题
							int uX = (int)u.Position.X;
							int uY = (int)u.Position.Y;
							int pX = (int)pos.X;
							int pY = (int)pos.Y;
							if (uX == pX && uY == pY && u.IsPlayer != unit.IsPlayer && u.IsAlive())
							{
								targetUnit = u;
								GD.Print("Found target unit at: " + u.Position + " in cell: " + pos);
								break;
							}
						}
						// 不管有没有敌人，都显示攻击范围
						GD.Print("Highlighting attack cell: " + pos);
						combatRenderer.HighlightAttackCell(pos, targetUnit);
						count++;
					}
				}
			}
			GD.Print("Total attack cells: " + count);
		}

		public void AttackUnit(Unit attacker, Unit target)
		{
			// 计算距离（使用整数位置避免浮点数精度问题）
			var attackerPos = new Vector2(Mathf.FloorToInt(attacker.Position.X), Mathf.FloorToInt(attacker.Position.Y));
			var targetPos = new Vector2(Mathf.FloorToInt(target.Position.X), Mathf.FloorToInt(target.Position.Y));
			var distance = Mathf.Abs(attackerPos.X - targetPos.X) + Mathf.Abs(attackerPos.Y - targetPos.Y);

			// 确定攻击范围
			int attackRange = attacker.GetEffectiveAttackRange();
			int minDistance = 1;
			
			// 精英单位特殊处理：同时拥有近战和远程攻击能力
			if (attacker.Class == Unit.UnitClass.Elite)
			{
				// 距离为1时使用近战攻击
				if (distance == 1)
				{
					attackRange = 1; // 近战攻击范围
					minDistance = 1; // 近战攻击允许距离1
				}
				// 距离大于1时使用远程攻击
				else if (distance > 1)
				{
					attackRange = attacker.GetEffectiveAttackRange(); // 远程攻击范围
					minDistance = 2; // 远程攻击最小距离2
				}
			}
			// 其他单位
			else
			{
				// 对于远程攻击（攻击范围大于1），不包括距离为1的格子
				if (attackRange > 1)
				{
					minDistance = 2;
				}
				// 近战攻击允许距离1
				else if (attackRange == 1 && distance == 1)
				{
					minDistance = 1;
				}
			}

			if (distance < minDistance || distance > attackRange)
			{
				GD.Print(string.Format(Constants.ENEMY_OUT_OF_ATTACK_RANGE, distance, minDistance, attackRange));
				return;
			}

			// 计算伤害
			var damage = attacker.GetEffectiveAttack();
			// 计算暴击
			bool isCritical = false;
			var criticalChance = (float)attacker.Luck / 20.0f; // 幸运值越高，暴击率越高
			if (GD.Randf() < criticalChance)
			{
				isCritical = true;
				damage *= 2; // 暴击伤害翻倍
			}
			// 应用伤害
			target.TakeDamage(damage);
			// 打印战斗日志
			var attackerType = attacker.IsPlayer ? "玩家" : "敌人 " + unitManager.GetUnitClassName(attacker.Class);
			var targetType = target.IsPlayer ? "玩家" : "敌人 " + unitManager.GetUnitClassName(target.Class);
			if (isCritical)
			{
				// 红色字体显示暴击
				battleLog.AppendText($"{attackerType} 攻击 {targetType} 造成 [color=#ff0000]{damage} 点暴击伤害！[/color]\n");
			}
			else
			{
				battleLog.AppendText($"{attackerType} 攻击 {targetType} 造成 {damage} 点伤害！\n");
			}
			// 更新血条
			foreach (var child in mapLayer.GetChildren())
			{
				if (child.HasMeta(Constants.UNIT_META_KEY) && child.GetMeta(Constants.UNIT_META_KEY).As<Unit>() == target)
				{
					unitManager.UpdateHPBar(child, target);
					// 视觉反馈：闪红
					combatRenderer.ShowDamageFeedback(child, target);
					break;
				}
			}
			// 检查单位是否死亡
			if (!target.IsAlive())
			{
				battleLog.AppendText($"{targetType} 被击败了！\n");
				// 移除死亡单位
				unitManager.RemoveUnit(target);
				// 检查游戏是否结束
				if (gameStateManager != null)
				{
					gameStateManager.CheckGameOver();
				}
			}
			// 每次攻击后都检查游戏是否结束
			if (gameStateManager != null)
			{
				gameStateManager.CheckGameOver();
			}
		}

		public void ShowAttackRangeAnimation(Unit unit, int range)
		{
			combatRenderer.ShowAttackRangeAnimation(unit, range, grid);
		}
	}
}
