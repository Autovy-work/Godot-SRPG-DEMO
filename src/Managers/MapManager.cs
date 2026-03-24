using Godot;
using System.Collections.Generic;

namespace CSharpTestGame
{
	public class MapManager
	{
		private Grid grid;
		private Node2D mapLayer;

		public Grid Grid { get { return grid; } }

		public MapManager(Node2D mapLayer)
		{
			this.mapLayer = mapLayer;
		}

		public void Initialize(int width, int height)
		{
			grid = new Grid(width, height);
			DrawBoard();
		}

		private void DrawBoard()
		{
			for (int y = 0; y < grid.GridSize.Y; y++)
			{
				for (int x = 0; x < grid.GridSize.X; x++)
				{
					// 先加边框（底层）
					var border = new ColorRect();
					border.Size = new Vector2(56, 56);
					border.Position = new Vector2(x * 56, y * 56);
					border.Color = new Color(0.15f, 0.15f, 0.15f, 1.0f);
					border.Name = string.Format("Border_{0}_{1}", x, y);
					mapLayer.AddChild(border);

					// 再加格子（在边框上面）
					Control cell;
					Texture2D? grassTexture = ResourceLoader.Load<Texture2D>("res://Resources/grass.png");
					if (grassTexture != null)
					{
						var textureCell = new TextureRect();
						textureCell.Size = new Vector2(54, 54);
						textureCell.Position = new Vector2(x * 56 + 1, y * 56 + 1);
						textureCell.Texture = grassTexture;
						textureCell.Set("stretch_mode", 0); // STRETCH_SCALE - 缩放图像以适应容器大小
						textureCell.Name = string.Format("Cell_{0}_{1}", x, y);
						cell = textureCell;
					}
					else
					{
						// 如果图片加载失败，使用默认颜色
						var colorCell = new ColorRect();
						colorCell.Size = new Vector2(54, 54);
						colorCell.Position = new Vector2(x * 56 + 1, y * 56 + 1);
						colorCell.Color = new Color(0.25f, 0.25f, 0.25f, 1.0f);
						colorCell.Name = string.Format("Cell_{0}_{1}", x, y);
						cell = colorCell;
					}
					mapLayer.AddChild(cell);
				}
			}
		}

		public void GenerateRandomObstacles(int count, List<Unit> units)
		{
			for (int i = 0; i < count; i++)
			{
				// 生成随机位置
				Vector2 obstaclePos;
				int attempts = 0;
				const int maxAttempts = 100;

				do
				{
					obstaclePos = new Vector2(
						GD.Randi() % (int)grid.GridSize.X,
						GD.Randi() % (int)grid.GridSize.Y
					);

					// 检查位置是否有效、不是玩家初始位置、没有障碍物且没有单位
					bool isValid = grid.IsValidPosition(obstaclePos) && 
						obstaclePos != new Vector2(0, 0) && 
						grid.IsPassable(obstaclePos, units);

					if (isValid)
					{
						break;
					}

					attempts++;
				}
				while (attempts < maxAttempts);

				// 如果找不到合适的位置，跳过
				if (attempts >= maxAttempts)
				{
					GD.Print("Failed to find valid position for obstacle");
					continue;
				}

				// 设置障碍物
				grid.SetObstacle(obstaclePos, true);
				GD.Print("Generated obstacle at: " + obstaclePos);

				// 创建障碍物视觉效果
				Control obstacle;
				Texture2D? obstacleTexture = ResourceLoader.Load<Texture2D>("res://Resources/rock.png");
				if (obstacleTexture != null)
				{
					// 调整图像大小以适应格子
					Image image = obstacleTexture.GetImage();
					if (image != null)
					{
						// 调整图像大小为 54x54
						image.Resize(54, 54);
						// 创建新的纹理
						ImageTexture resizedTexture = ImageTexture.CreateFromImage(image);

						var textureObstacle = new TextureRect();
						textureObstacle.Size = new Vector2(54, 54); // 比格子小2像素，显示在边框内
						textureObstacle.Position = new Vector2(obstaclePos.X * 56 + 1, obstaclePos.Y * 56 + 1); // 偏移1像素，显示在边框内
						textureObstacle.Texture = resizedTexture;
						textureObstacle.Name = "Obstacle";
						obstacle = textureObstacle;
					}
					else
					{
						// 如果图像获取失败，使用原始纹理
						var textureObstacle = new TextureRect();
						textureObstacle.Size = new Vector2(54, 54); // 比格子小2像素，显示在边框内
						textureObstacle.Position = new Vector2(obstaclePos.X * 56 + 1, obstaclePos.Y * 56 + 1); // 偏移1像素，显示在边框内
						textureObstacle.Texture = obstacleTexture;
						textureObstacle.Name = "Obstacle";
						obstacle = textureObstacle;
					}
				}
				else
				{
					// 如果图片加载失败，使用默认颜色
					var colorObstacle = new ColorRect();
					colorObstacle.Size = new Vector2(54, 54); // 比格子小2像素，显示在边框内
					colorObstacle.Position = new Vector2(obstaclePos.X * 56 + 1, obstaclePos.Y * 56 + 1); // 偏移1像素，显示在边框内
					colorObstacle.Color = new Color(0.55f, 0.45f, 0.2f, 1.0f); // 棕黄色，明显区别于格子
					colorObstacle.Name = "Obstacle";
					obstacle = colorObstacle;
				}
				mapLayer.AddChild(obstacle);

				// 创建障碍物物理碰撞
				var staticBody = new StaticBody2D();
				staticBody.Position = new Vector2(obstaclePos.X * 56, obstaclePos.Y * 56);
				mapLayer.AddChild(staticBody);

				var collisionShape = new CollisionShape2D();
				var rectangleShape = new RectangleShape2D();
				rectangleShape.Size = new Vector2(56, 56);
				collisionShape.Shape = rectangleShape;
				staticBody.AddChild(collisionShape);
			}
		}
	}
}
