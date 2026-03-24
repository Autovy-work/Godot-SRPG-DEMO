using Godot;
using System.Collections.Generic;

namespace CSharpTestGame
{
	public class MovementSystem
	{
		private Grid grid;
		private UnitManager unitManager;
		private Node2D mapLayer;
		private List<ColorRect> highlightCells = new List<ColorRect>();
		private List<Node> pathAnimationNodes = new List<Node>();

		public MovementSystem(Grid grid, UnitManager unitManager, Node2D mapLayer)
		{
			this.grid = grid;
			this.unitManager = unitManager;
			this.mapLayer = mapLayer;
		}

		public void CalculateMovableCells(Unit unit)
		{
			GD.Print("Calculating movable cells for unit at: " + unit.Position);
			var moveRange = unit.GetEffectiveMoveRange();
			GD.Print("Move range: " + moveRange);
			int count = 0;
			for (int y = 0; y < grid.GridSize.Y; y++)
			{
				for (int x = 0; x < grid.GridSize.X; x++)
				{
					var pos = new Vector2(x, y);
					var distance = Mathf.Abs(pos.X - unit.Position.X) + Mathf.Abs(pos.Y - unit.Position.Y); // 曼哈顿距离
					if (distance <= moveRange && unitManager.IsCellFree(pos) && grid.IsPassable(pos, unitManager.Units))
					{
						// 使用A*算法检查实际路径是否可通行
						var path = CalculatePath(unit.Position, pos);
						if (path.Count > 1 && path.Count - 1 <= moveRange)
						{
							GD.Print("Highlighting cell: " + pos);
							HighlightCell(pos);
							count++;
						}
					}
				}
			}
			GD.Print("Total highlighted cells: " + count);
		}

		public List<Vector2> CalculatePath(Vector2 start, Vector2 end)
		{
			// A*算法实现路径查找
			List<Vector2> path = new List<Vector2>();
			
			// 定义四个方向：上、右、下、左
			Vector2[] directions = new Vector2[]
			{
				new Vector2(0, -1), // 上
				new Vector2(1, 0),  // 右
				new Vector2(0, 1),  // 下
				new Vector2(-1, 0)  // 左
			};
			
			// 开放列表和关闭列表
			List<PathNode> openList = new List<PathNode>();
			List<PathNode> closedList = new List<PathNode>();
			
			// 创建起点节点
			PathNode startNode = new PathNode(start, null, 0, Heuristic(start, end));
			openList.Add(startNode);
			
			while (openList.Count > 0)
			{
				// 找到F值最小的节点
				PathNode currentNode = openList[0];
				for (int i = 1; i < openList.Count; i++)
				{
					if (openList[i].F < currentNode.F)
					{
						currentNode = openList[i];
					}
				}
				
				// 从开放列表中移除当前节点，添加到关闭列表
				openList.Remove(currentNode);
				closedList.Add(currentNode);
				
				// 检查是否到达终点
				if (currentNode.Position == end)
				{
					// 回溯路径
					PathNode temp = currentNode;
					while (temp != null)
					{
						path.Add(temp.Position);
						temp = temp.Parent;
					}
					path.Reverse();
					return path;
				}
				
				// 探索四个方向
				foreach (Vector2 dir in directions)
				{
					Vector2 newPos = currentNode.Position + dir;
					
					// 检查位置是否有效且可通行
					if (!grid.IsPassable(newPos, unitManager.Units))
					{
						continue;
					}
					
					// 检查是否在关闭列表中
					bool inClosedList = false;
					foreach (PathNode node in closedList)
					{
						if (node.Position == newPos)
						{
							inClosedList = true;
							break;
						}
					}
					if (inClosedList)
					{
						continue;
					}
					
					// 计算G和H值
					float g = currentNode.G + 1;
					float h = Heuristic(newPos, end);
					
					// 检查是否在开放列表中
					bool inOpenList = false;
					foreach (PathNode node in openList)
					{
						if (node.Position == newPos)
						{
							inOpenList = true;
							if (g < node.G)
							{
								node.G = g;
								node.Parent = currentNode;
							}
							break;
						}
					}
					if (!inOpenList)
					{
						// 创建新节点并添加到开放列表
						PathNode newNode = new PathNode(newPos, currentNode, g, h);
						openList.Add(newNode);
					}
				}
			}
			
			// 如果没有找到路径，返回直接路径
			path.Add(start);
			path.Add(end);
			return path;
		}

		public void ShowMovementRange(Unit unit)
		{
			GD.Print("Showing movement range for unit at: " + unit.Position);
			var moveRange = unit.GetEffectiveMoveRange();
			GD.Print("Move range: " + moveRange);
			int count = 0;
			for (int y = 0; y < grid.GridSize.Y; y++)
			{
				for (int x = 0; x < grid.GridSize.X; x++)
				{
					var pos = new Vector2(x, y);
					var distance = Mathf.Abs(pos.X - unit.Position.X) + Mathf.Abs(pos.Y - unit.Position.Y); // 曼哈顿距离
					if (distance <= moveRange && unitManager.IsCellFree(pos) && grid.IsPassable(pos, unitManager.Units))
					{
						// 使用A*算法检查实际路径是否可通行
						var path = CalculatePath(unit.Position, pos);
						if (path.Count > 1 && path.Count - 1 <= moveRange)
						{
							GD.Print("Highlighting cell: " + pos);
							HighlightCell(pos);
							count++;
						}
					}
				}
			}
			GD.Print("Total highlighted cells: " + count);
		}

		// 启发函数（曼哈顿距离）
		private float Heuristic(Vector2 a, Vector2 b)
		{
			return Mathf.Abs(a.X - b.X) + Mathf.Abs(a.Y - b.Y);
		}

		private void HighlightCell(Vector2 pos)
		{
			// 创建高亮格子的容器
			var highlight = new ColorRect();
			highlight.Size = new Vector2(56, 56);
			highlight.Position = new Vector2(pos.X * 56, pos.Y * 56);
			highlight.Color = new Color(0.2f, 0.6f, 0.8f, 0.5f);
			highlight.Name = string.Format("Highlight_{0}_{1}", pos.X, pos.Y);
			highlight.SetMeta("position", pos);

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

		public void MoveUnit(Unit unit, Vector2 targetPos)
		{
			// 检查目标位置是否空闲
			if (!unitManager.IsCellFree(targetPos))
			{
				GD.Print("Target cell is not free, cannot move");
				return;
			}

			// 计算移动路径
			var path = CalculatePath(unit.Position, targetPos);

			// 检查路径是否有效
			if (path.Count <= 1)
			{
				GD.Print("No valid path to target, cannot move");
				return;
			}

			// 开始路径移动
			StartPathMovement(unit, path);
		}

		public void StartPathMovement(Unit unit, List<Vector2> path)
		{
			if (path.Count <= 1)
			{
				// 路径为空或只有一个点，直接完成移动
				CompleteMovement(unit, path[0]);
				return;
			}

			// 逐步移动单位
			int currentStep = 0;

			var moveTimer = new Timer();
			moveTimer.WaitTime = 0.2f; // 每步移动的时间间隔
			moveTimer.OneShot = false;
			mapLayer.AddChild(moveTimer);

			moveTimer.Timeout += () => {
				// 检查是否已经到达路径终点
				if (currentStep >= path.Count - 1)
				{
					// 移动完成
					moveTimer.Stop();
					moveTimer.QueueFree();
					CompleteMovement(unit, path[^1]);
					return;
				}

				// 检查路径是否仍然有效（可能有其他单位移动导致路径阻塞）
				var currentPos = unit.Position;
				var nextPos = path[currentStep + 1];

				// 检查下一步是否可通行
				if (!unitManager.IsCellFree(nextPos) || !grid.IsPassable(nextPos, unitManager.Units))
				{
					// 路径被阻塞，重新计算路径
					GD.Print("Path blocked, recalculating...");
					var newPath = CalculatePath(currentPos, path[^1]);
					if (newPath.Count > 1)
					{
						// 使用新路径
						path = newPath;
						currentStep = 0;
						nextPos = path[currentStep + 1];
					}
					else
					{
						// 无法找到新路径，停止移动
						moveTimer.Stop();
						moveTimer.QueueFree();
						CompleteMovement(unit, currentPos);
						return;
					}
				}

				currentStep++;
				// 更新单位位置
				unit.Position = path[currentStep];
				// 更新单位节点位置
				unitManager.RefreshUnitNodePosition(unit, path[currentStep]);
			};

			moveTimer.Start();
		}

		private void CompleteMovement(Unit unit, Vector2 finalPos)
		{
			// 清除高亮
			ClearHighlights();
			GD.Print("Movement completed to: " + finalPos);
		}

		public void ShowPathAnimation(List<Vector2> path)
		{
			// 清除之前的路径动画
			ClearPathAnimations();

			// 显示路径动画
			for (int i = 0; i < path.Count - 1; i++)
			{
				var start = path[i];
				var end = path[i + 1];
				// 创建箭头精灵
				var arrow = new Label();
				arrow.Text = "→";
				arrow.Size = new Vector2(20, 20);
				arrow.Position = new Vector2(start.X * 56 + 32, start.Y * 56 + 32);
				arrow.HorizontalAlignment = HorizontalAlignment.Center;
				arrow.VerticalAlignment = VerticalAlignment.Center;
				arrow.AddThemeColorOverride("font_color", new Color(1, 1, 0));

				// 计算旋转角度
				if (end.X > start.X)
				{
					arrow.Rotation = 0;
				}
				else if (end.X < start.X)
				{
					arrow.Rotation = Mathf.Pi;
				}
				else if (end.Y > start.Y)
				{
					arrow.Rotation = Mathf.Pi / 2;
				}
				else if (end.Y < start.Y)
				{
					arrow.Rotation = -Mathf.Pi / 2;
				}

				mapLayer.AddChild(arrow);
				pathAnimationNodes.Add(arrow);

				// 2秒后移除箭头
				var timer = new Timer();
				timer.WaitTime = 2.0f;
				timer.OneShot = true;
				mapLayer.AddChild(timer);
				timer.Timeout += () => {
					if (arrow != null && Godot.GodotObject.IsInstanceValid(arrow))
					{
						arrow.QueueFree();
						pathAnimationNodes.Remove(arrow);
					}
					if (timer != null && Godot.GodotObject.IsInstanceValid(timer))
					{
						timer.QueueFree();
					}
				};
				timer.Start();
				// 不要将Timer添加到pathAnimationNodes列表中，避免重复释放
			}
		}

		private void ClearPathAnimations()
		{
			// 清除所有路径动画节点
		foreach (var node in pathAnimationNodes)
		{
			if (node != null && Godot.GodotObject.IsInstanceValid(node))
			{
				node.QueueFree();
			}
		}
		pathAnimationNodes.Clear();
		}

		// 路径节点类
		private class PathNode
		{
			public Vector2 Position { get; set; }
			public PathNode Parent { get; set; }
			public float G { get; set; } // 从起点到当前节点的代价
			public float H { get; set; } // 从当前节点到终点的估计代价
			public float F { get { return G + H; } } // 总代价

			public PathNode(Vector2 position, PathNode parent, float g, float h)
			{
				Position = position;
				Parent = parent;
				G = g;
				H = h;
			}
		}
	}
}
