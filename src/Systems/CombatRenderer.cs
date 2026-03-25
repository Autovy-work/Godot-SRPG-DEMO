using Godot;
using System.Collections.Generic;

namespace CSharpTestGame
{
	public class CombatRenderer
	{
		private Node2D mapLayer;
		private List<ColorRect> highlightCells = new List<ColorRect>();

		public CombatRenderer(Node2D mapLayer)
		{
			this.mapLayer = mapLayer;
		}

		public void HighlightAttackCell(Vector2 pos, Unit targetUnit)
		{
			var highlight = new ColorRect();
			highlight.Size = new Vector2(Constants.TILE_SIZE, Constants.TILE_SIZE);
			highlight.Position = new Vector2(pos.X * Constants.TILE_SIZE, pos.Y * Constants.TILE_SIZE);
			// 根据是否有目标单位设置不同的颜色
			if (targetUnit != null)
			{
				highlight.Color = new Color(0.8f, 0.2f, 0.2f, 0.5f); // 红色透明高亮表示可以攻击敌人
			}
			else
			{
				highlight.Color = new Color(0.8f, 0.8f, 0.2f, 0.5f); // 黄色高亮表示攻击范围但没有敌人
			}
			highlight.Name = string.Format("AttackHighlight_{0}_{1}", pos.X, pos.Y);
			highlight.SetMeta("position", pos);
			highlight.SetMeta(Constants.UNIT_META_KEY, targetUnit);

			mapLayer.AddChild(highlight);
			highlightCells.Add(highlight);
		}

		public void ClearHighlights()
		{
			foreach (var highlight in highlightCells)
			{
				highlight.QueueFree();
			}
			highlightCells.Clear();
		}

		public void ShowDamageFeedback(Node unitNode, Unit target)
		{
			if (unitNode is ColorRect colorRect)
			{
				colorRect.Color = new Color(1, 0, 0);
				// 使用Timer来延迟恢复颜色
				var timer = new Timer();
				timer.WaitTime = 0.2f;
				timer.OneShot = true;
				mapLayer.AddChild(timer);
				timer.Timeout += () =>
				{
					if (colorRect != null && GodotObject.IsInstanceValid(colorRect))
					{
						colorRect.Color = target.IsPlayer ? Constants.PLAYER_COLOR : Constants.ENEMY_COLOR;
					}
					timer.QueueFree();
				};
				timer.Start();
			}
		}

		public void ShowAttackRangeAnimation(Unit unit, int range, Grid grid)
		{
			// 显示攻击范围动画
			for (int y = 0; y < grid.GridSize.Y; y++)
			{
				for (int x = 0; x < grid.GridSize.X; x++)
				{
					var pos = new Vector2(x, y);
					var distance = Mathf.Abs(pos.X - unit.Position.X) + Mathf.Abs(pos.Y - unit.Position.Y); // 曼哈顿距离

					// 跳过角色所在的中心位置
					if (distance == 0)
					{
						continue;
					}

					// 对于远程攻击（攻击范围大于1），不包括距离为1的格子
					int minDistance = 1;
					if (range > 1 && !(unit.Class == Unit.UnitClass.Elite && distance == 1))
					{
						minDistance = 2;
					}

					if (distance <= range && distance >= minDistance)
					{
						// 创建攻击范围高亮
						var highlight = new ColorRect();
						highlight.Size = new Vector2(Constants.TILE_SIZE, Constants.TILE_SIZE);
						highlight.Position = new Vector2(pos.X * Constants.TILE_SIZE, pos.Y * Constants.TILE_SIZE);
						highlight.Color = new Color(1, 0, 0, 0.3f);
						highlight.Name = string.Format("AttackRangeHighlight_{0}_{1}", x, y);
						mapLayer.AddChild(highlight);

						// 2秒后移除高亮
						var timer = new Timer();
						timer.WaitTime = 2.0f;
						timer.OneShot = true;
						mapLayer.AddChild(timer);
						timer.Timeout += () => {
							highlight.QueueFree();
							timer.QueueFree();
						};
						timer.Start();
					}
				}
			}
		}
	}
}