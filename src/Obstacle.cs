using Godot;

namespace CSharpTestGame
{
	[GlobalClass]
	public partial class Obstacle : StaticBody2D
	{
		public Vector2 GridPosition { get; set; }

		public override void _Ready()
		{
			// 添加到Obstacles组
			AddToGroup("Obstacles");
		}

		public static Obstacle Create(Vector2 gridPosition)
		{
			var obstacle = new Obstacle();
		obstacle.GridPosition = gridPosition;
		obstacle.Position = new Vector2(gridPosition.X * 64, gridPosition.Y * 64);

		// 添加碰撞形状
		var collisionShape = new CollisionShape2D();
		var rectangleShape = new RectangleShape2D();
		rectangleShape.Size = new Vector2(64, 64);
		collisionShape.Shape = rectangleShape;
		obstacle.AddChild(collisionShape);

		// 添加可见的ColorRect
		var colorRect = new ColorRect();
		colorRect.Size = new Vector2(64, 64);
		colorRect.Position = new Vector2(0, 0);
		colorRect.Color = new Color(0.5f, 0.5f, 0.5f);
		colorRect.Name = "ObstacleVisual";
		obstacle.AddChild(colorRect);

			return obstacle;
		}
	}
}