using Godot;
using System.Collections.Generic;

namespace CSharpTestGame
{
	public class UnitRenderer
	{
		private Node2D mapLayer;

		public UnitRenderer(Node2D mapLayer)
		{
			this.mapLayer = mapLayer;
		}

		public void DrawUnit(Unit unit, int unitIndex)
		{
			// 创建单位节点的容器
			Control container;
			// 只从Unit.ImagePath加载图像
			Texture2D? unitTexture = null;
			if (!string.IsNullOrEmpty(unit.ImagePath))
			{
				unitTexture = ResourceLoader.Load<Texture2D>(unit.ImagePath);
			}

			if (unitTexture != null)
			{
				// 调整图像大小以适应格子
				Image image = unitTexture.GetImage();
				if (image != null)
				{
					// 调整图像大小为 TILE_SIZE x TILE_SIZE
					image.Resize(Constants.TILE_SIZE, Constants.TILE_SIZE);
					// 创建新的纹理
					ImageTexture resizedTexture = ImageTexture.CreateFromImage(image);

					var textureContainer = new TextureRect();
					textureContainer.Size = new Vector2(Constants.TILE_SIZE, Constants.TILE_SIZE);
					textureContainer.Position = new Vector2(unit.Position.X * Constants.TILE_SIZE, unit.Position.Y * Constants.TILE_SIZE);
					textureContainer.Name = string.Format(Constants.UNIT_NODE_PREFIX + "{0}", unitIndex);
					textureContainer.SetMeta(Constants.UNIT_META_KEY, unit);
					textureContainer.Texture = resizedTexture;
					textureContainer.StretchMode = TextureRect.StretchModeEnum.Scale;
					container = textureContainer;
				}
				else
				{
					// 如果图像获取失败，使用原始纹理
					var textureContainer = new TextureRect();
					textureContainer.Size = new Vector2(Constants.TILE_SIZE, Constants.TILE_SIZE);
					textureContainer.Position = new Vector2(unit.Position.X * Constants.TILE_SIZE, unit.Position.Y * Constants.TILE_SIZE);
					textureContainer.Name = string.Format(Constants.UNIT_NODE_PREFIX + "{0}", unitIndex);
					textureContainer.SetMeta(Constants.UNIT_META_KEY, unit);
					textureContainer.Texture = unitTexture;
					textureContainer.StretchMode = TextureRect.StretchModeEnum.Scale;
					container = textureContainer;
				}
			}
			else
			{
				// 如果图片加载失败，使用默认颜色
				var colorContainer = new ColorRect();
				colorContainer.Size = new Vector2(Constants.TILE_SIZE, Constants.TILE_SIZE);
				colorContainer.Position = new Vector2(unit.Position.X * Constants.TILE_SIZE + 4, unit.Position.Y * Constants.TILE_SIZE + 4);
				colorContainer.Name = string.Format(Constants.UNIT_NODE_PREFIX + "{0}", unitIndex);
				colorContainer.SetMeta(Constants.UNIT_META_KEY, unit);
				colorContainer.Color = unit.IsPlayer ? Constants.PLAYER_COLOR : Constants.ENEMY_COLOR;
				container = colorContainer;
			}

			// 添加血条
			var hpBar = new ColorRect();
			hpBar.Size = new Vector2(Constants.HP_BAR_WIDTH, Constants.HP_BAR_HEIGHT);
			// 血条放到上方
			hpBar.Position = new Vector2(0, -10);
			hpBar.Color = Constants.HP_BAR_COLOR;
			hpBar.Name = Constants.HP_BAR_NAME;
			container.AddChild(hpBar);
			UpdateHPBar(container, unit);

			mapLayer.AddChild(container);
		}

		public void UpdateHPBar(Node unitNode, Unit unit)
		{
			var hpBar = unitNode.GetNode<ColorRect>(Constants.HP_BAR_NAME);
			if (hpBar != null)
			{
				hpBar.Scale = new Vector2((float)unit.CurrentHealth / unit.MaxHealth, 1);
			}
		}

		public void RemoveUnitNode(Unit unit)
		{
			// 移除单位节点
			foreach (var child in mapLayer.GetChildren())
			{
				if (child.HasMeta(Constants.UNIT_META_KEY) && child.GetMeta(Constants.UNIT_META_KEY).As<Unit>() == unit)
				{
					child.QueueFree();
					break;
				}
			}
		}

		public void RefreshUnitNodePosition(Unit unit, Vector2 newPosition)
		{
			foreach (var child in mapLayer.GetChildren())
			{
				if (child.HasMeta(Constants.UNIT_META_KEY))
				{
					var childUnit = child.GetMeta(Constants.UNIT_META_KEY).As<Unit>();
					if (childUnit != null && childUnit == unit)
					{
						if (child is Control control)
						{
							control.Position = new Vector2(newPosition.X * Constants.TILE_SIZE, newPosition.Y * Constants.TILE_SIZE);
						}
						break;
					}
				}
			}
		}
	}
}