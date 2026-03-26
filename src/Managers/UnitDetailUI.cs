using Godot;
using System;
using CSharpTestGame.Items;

namespace CSharpTestGame.Managers
{
	public class UnitDetailUI
	{
		private CanvasLayer canvasLayer = null;
		private Unit currentUnit = null;

		public void ShowUnitDetail(Unit unit)
		{
			currentUnit = unit;

			// 创建一个新的CanvasLayer，确保它在所有其他节点之上
			canvasLayer = new CanvasLayer();
			canvasLayer.Name = "UnitDetailCanvasLayer";

			// 获取根节点并添加
			var root = GetTree().Root;
			root.AddChild(canvasLayer);

			// 创建背景遮罩
			var blurBackground = new ColorRect();
			blurBackground.Size = root.Size;
			blurBackground.Position = Vector2.Zero;
			blurBackground.Color = new Color(0, 0, 0, 0.8f); // 半透明黑色背景
			blurBackground.Name = "BlurBackground";
			blurBackground.MouseFilter = Control.MouseFilterEnum.Stop; // 阻止点击穿透
			// 创建一个不可见的按钮来处理点击事件
			var backgroundButton = new Button();
			backgroundButton.Size = root.Size;
			backgroundButton.Position = Vector2.Zero;
			backgroundButton.Modulate = new Color(1, 1, 1, 0); // 完全透明
			backgroundButton.MouseFilter = Control.MouseFilterEnum.Stop;
			backgroundButton.Pressed += OnBackgroundPressed;
			canvasLayer.AddChild(backgroundButton);
			canvasLayer.AddChild(blurBackground);

			// 创建详情面板
			var detailPanel = new ColorRect();
			detailPanel.Size = new Vector2(900, 650);
			detailPanel.Position = (root.Size - detailPanel.Size) / 2;
			detailPanel.Color = new Color(0.1f, 0.1f, 0.1f, 0.95f); // 深色背景
			detailPanel.Name = "UnitDetailPanel";
			// 添加边框和阴影效果
			var panelStyle = new StyleBoxFlat();
			panelStyle.BgColor = new Color(0.1f, 0.1f, 0.1f);
			panelStyle.BorderColor = new Color(0.3f, 0.3f, 0.3f);
			panelStyle.BorderWidthTop = 2;
			panelStyle.BorderWidthBottom = 2;
			panelStyle.BorderWidthLeft = 2;
			panelStyle.BorderWidthRight = 2;
			panelStyle.ShadowColor = new Color(0, 0, 0, 0.5f);
			panelStyle.ShadowOffset = new Vector2(4, 4);
			detailPanel.AddThemeStyleboxOverride("panel", panelStyle);
			canvasLayer.AddChild(detailPanel);

			// 添加面板背景渐变效果
			var gradientBackground = new ColorRect();
			gradientBackground.Size = new Vector2(896, 646);
			gradientBackground.Position = new Vector2(2, 2);
			gradientBackground.Color = new Color(0.15f, 0.15f, 0.15f, 0.8f);
			gradientBackground.Name = "GradientBackground";
			gradientBackground.ZIndex = -1;
			detailPanel.AddChild(gradientBackground);

			// 添加标题
			var titleLabel = new Label();
			titleLabel.Size = new Vector2(900, 60);
			titleLabel.Position = new Vector2(0, 10);
			titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
			titleLabel.VerticalAlignment = VerticalAlignment.Center;
			titleLabel.AddThemeColorOverride("font_color", new Color(1, 0.8f, 0.4f)); // 金色标题
			titleLabel.AddThemeFontSizeOverride("font_size", 28);
			titleLabel.Text = GetUnitClassName(unit.Class) + (unit.IsPlayer ? " (玩家)" : " (敌人)");
			titleLabel.Name = "TitleLabel";
			detailPanel.AddChild(titleLabel);

			// 添加等级信息
			var levelLabel = new Label();
			levelLabel.Size = new Vector2(100, 30);
			levelLabel.Position = new Vector2(800, 15);
			levelLabel.HorizontalAlignment = HorizontalAlignment.Right;
			levelLabel.AddThemeColorOverride("font_color", new Color(0.8f, 1, 0.8f));
			levelLabel.AddThemeFontSizeOverride("font_size", 16);
			levelLabel.Text = "Lv. " + unit.Level;
			levelLabel.Name = "LevelLabel";
			detailPanel.AddChild(levelLabel);

				// 添加单位形象
			var unitSprite = new Sprite2D();
			unitSprite.Scale = new Vector2(1.0f, 1.0f); // 调整缩放比例，避免超出UI范围
			unitSprite.Position = new Vector2(60, 90);
			unitSprite.Texture = GetUnitTexture(unit);
			unitSprite.Name = "UnitSprite";
			detailPanel.AddChild(unitSprite);

			// 创建标签页容器
			var tabContainer = new TabContainer();
			tabContainer.Size = new Vector2(880, 520);
			tabContainer.Position = new Vector2(10, 160);
			tabContainer.Name = "TabContainer";
			// 设置标签页样式
			tabContainer.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.8f));
			tabContainer.AddThemeColorOverride("font_color_selected", new Color(1, 1, 1));
			tabContainer.AddThemeColorOverride("bg_color", new Color(0.15f, 0.15f, 0.15f));
			tabContainer.AddThemeColorOverride("hovered", new Color(0.2f, 0.2f, 0.2f));
			tabContainer.AddThemeColorOverride("selected", new Color(0.25f, 0.25f, 0.25f));
			detailPanel.AddChild(tabContainer);

			// 创建属性标签页
			var statsTab = new VBoxContainer();
			tabContainer.AddChild(statsTab);
			tabContainer.SetTabTitle(tabContainer.GetTabCount() - 1, "属性");
			AddStatsUI(statsTab, unit);

			// 创建装备标签页
			var equipmentTab = new VBoxContainer();
			tabContainer.AddChild(equipmentTab);
			tabContainer.SetTabTitle(tabContainer.GetTabCount() - 1, "装备");
			AddEquipmentUI(equipmentTab, unit);

			// 创建背包标签页
			var inventoryTab = new VBoxContainer();
			tabContainer.AddChild(inventoryTab);
			tabContainer.SetTabTitle(tabContainer.GetTabCount() - 1, "背包");
			AddInventoryUI(inventoryTab, unit);

			// 添加关闭按钮
			var closeButton = new Button();
			closeButton.Size = new Vector2(120, 45);
			closeButton.Position = new Vector2(900 - closeButton.Size.X - 20, 650 - closeButton.Size.Y - 20);
			closeButton.Text = "关闭";
			closeButton.Name = "CloseButton";
			// 设置按钮样式
			var buttonStyle = new StyleBoxFlat();
			buttonStyle.BgColor = new Color(0.2f, 0.2f, 0.2f);
			buttonStyle.BorderColor = new Color(0.4f, 0.4f, 0.4f);
			buttonStyle.BorderWidthTop = 1;
			buttonStyle.BorderWidthBottom = 1;
			buttonStyle.BorderWidthLeft = 1;
			buttonStyle.BorderWidthRight = 1;
			closeButton.AddThemeStyleboxOverride("normal", buttonStyle);
			var hoverStyle = new StyleBoxFlat();
			hoverStyle.BgColor = new Color(0.25f, 0.25f, 0.25f);
			hoverStyle.BorderColor = new Color(0.5f, 0.5f, 0.5f);
			hoverStyle.BorderWidthTop = 1;
			hoverStyle.BorderWidthBottom = 1;
			hoverStyle.BorderWidthLeft = 1;
			hoverStyle.BorderWidthRight = 1;
			closeButton.AddThemeStyleboxOverride("hover", hoverStyle);
			var pressedStyle = new StyleBoxFlat();
			pressedStyle.BgColor = new Color(0.15f, 0.15f, 0.15f);
			pressedStyle.BorderColor = new Color(0.4f, 0.4f, 0.4f);
			pressedStyle.BorderWidthTop = 1;
			pressedStyle.BorderWidthBottom = 1;
			pressedStyle.BorderWidthLeft = 1;
			pressedStyle.BorderWidthRight = 1;
			closeButton.AddThemeStyleboxOverride("pressed", pressedStyle);
			closeButton.AddThemeColorOverride("font_color", new Color(1, 1, 1));
			closeButton.Pressed += OnCloseButtonPressed;
			detailPanel.AddChild(closeButton);
		}

		private Texture2D GetUnitTexture(Unit unit)
		{
			// 根据单位类型返回对应的纹理
			string texturePath = "res://Resources/";
			
			// 玩家单位使用warrior.png
			if (unit.IsPlayer)
			{
				return ResourceLoader.Load<Texture2D>(texturePath + "warrior.png");
			}
			
			switch (unit.Class)
			{
				case Unit.UnitClass.Warrior:
					return ResourceLoader.Load<Texture2D>(texturePath + "warrior.png");
				case Unit.UnitClass.WarAngel:
					return ResourceLoader.Load<Texture2D>(texturePath + "archangel.png");
				case Unit.UnitClass.ElfArcher:
					return ResourceLoader.Load<Texture2D>(texturePath + "elfmale_ranger.png");
				case Unit.UnitClass.Acolyte:
					return ResourceLoader.Load<Texture2D>(texturePath + "acolyte.png");
				case Unit.UnitClass.Goblin:
					return ResourceLoader.Load<Texture2D>(texturePath + "goblin.png");
				case Unit.UnitClass.Skeleton:
					return ResourceLoader.Load<Texture2D>(texturePath + "skeleton.png");
				default:
					return ResourceLoader.Load<Texture2D>(texturePath + "player.png");
			}
		}

		private Texture2D GetItemTexture(Item item)
		{
			// 根据物品类型返回对应的纹理
			string texturePath = "res://Resources/";
			Texture2D originalTexture = null;
			
			if (item is Equipment equipment)
			{
				switch (equipment.Slot)
				{
					case Equipment.EquipmentSlot.Weapon:
						originalTexture = ResourceLoader.Load<Texture2D>(texturePath + "sword.png");
						break;
					case Equipment.EquipmentSlot.Shield:
						originalTexture = ResourceLoader.Load<Texture2D>(texturePath + "rock.png");
						break;
					default:
						originalTexture = ResourceLoader.Load<Texture2D>(texturePath + "Backpack.png");
						break;
				}
			}
			else
			{
				originalTexture = ResourceLoader.Load<Texture2D>(texturePath + "Backpack.png");
			}

			// 调整纹理大小为32x32
			if (originalTexture != null)
			{
				Image image = originalTexture.GetImage();
				if (image != null)
				{
					// 调整图像大小为64x64，确保图标清晰可见
				image.Resize(64, 64);
					// 创建新的纹理
					ImageTexture resizedTexture = ImageTexture.CreateFromImage(image);
					return resizedTexture;
				}
			}

			return originalTexture;
		}

		private string GetUnitClassName(Unit.UnitClass unitClass)
		{
			switch (unitClass)
			{
				case Unit.UnitClass.Goblin:
					return "哥布林";
				case Unit.UnitClass.ElfArcher:
					return "精灵弓手";
				case Unit.UnitClass.WarAngel:
					return "战争天使";
				case Unit.UnitClass.Skeleton:
					return "骷髅士兵";
				case Unit.UnitClass.Acolyte:
					return "生命法师";
				case Unit.UnitClass.Warrior:
					return "战士";
				default:
					return "未知";
			}
		}

		private string GetSlotName(Equipment.EquipmentSlot slot)
		{
			switch (slot)
			{
				case Equipment.EquipmentSlot.Head:
					return "头部";
				case Equipment.EquipmentSlot.Chest:
					return "胸部";
				case Equipment.EquipmentSlot.Legs:
					return "腿部";
				case Equipment.EquipmentSlot.Feet:
					return "脚部";
				case Equipment.EquipmentSlot.Weapon:
					return "武器";
				case Equipment.EquipmentSlot.Shield:
					return "盾牌";
				case Equipment.EquipmentSlot.Accessory1:
					return "饰品1";
				case Equipment.EquipmentSlot.Accessory2:
					return "饰品2";
				default:
					return "未知";
			}
		}

		private void AddStatsUI(VBoxContainer container, Unit unit)
		{
			// 添加标题
			var titleLabel = new Label();
			titleLabel.Text = "角色属性";
			titleLabel.AddThemeColorOverride("font_color", new Color(1, 0.8f, 0.4f)); // 金色标题
			titleLabel.AddThemeFontSizeOverride("font_size", 20);
			titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
			titleLabel.Size = new Vector2(860, 40);
			titleLabel.Position = new Vector2(0, 10);
			container.AddChild(titleLabel);

			// 创建属性容器
			var statsContainer = new VBoxContainer();
			statsContainer.Size = new Vector2(840, 420);
			statsContainer.Position = new Vector2(20, 60);
			statsContainer.AddThemeConstantOverride("separation", 12);
			container.AddChild(statsContainer);

			// 添加属性行
			AddStatRow(statsContainer, "等级", unit.Level.ToString(), new Color(1, 0.8f, 0.4f)); // 金色
			AddStatRow(statsContainer, "经验", unit.Experience + "/" + unit.ExperienceToNextLevel, new Color(0.8f, 1, 0.8f)); // 绿色
			AddStatRow(statsContainer, "生命值", unit.CurrentHealth + "/" + unit.GetEffectiveMaxHealth(), new Color(1, 0.6f, 0.6f)); // 红色
			AddStatRow(statsContainer, "攻击力", unit.GetEffectiveAttack().ToString(), new Color(1, 1, 0.6f)); // 黄色
			AddStatRow(statsContainer, "攻击范围", unit.GetEffectiveAttackRange().ToString(), new Color(0.6f, 1, 1)); // 青色
			AddStatRow(statsContainer, "移动范围", unit.GetEffectiveMoveRange().ToString(), new Color(0.8f, 0.6f, 1)); // 紫色
			AddStatRow(statsContainer, "速度", unit.GetEffectiveSpeed().ToString(), new Color(1, 0.8f, 1)); // 粉色
			AddStatRow(statsContainer, "幸运", unit.GetEffectiveLuck().ToString(), new Color(0.6f, 1, 0.6f)); // 深绿色
		}

		private void AddStatRow(VBoxContainer container, string label, string value, Color valueColor)
		{
			// 创建属性行容器
			var statRow = new HBoxContainer();
			statRow.Size = new Vector2(840, 45);
			container.AddChild(statRow);

			// 添加背景效果
			var background = new ColorRect();
			background.Size = new Vector2(840, 45);
			background.Color = new Color(0.15f, 0.15f, 0.15f, 0.5f);
			background.ZIndex = -1;
			statRow.AddChild(background);

			// 添加标签
			var labelControl = new Label();
			labelControl.Text = label + ":";
			labelControl.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.8f));
			labelControl.AddThemeFontSizeOverride("font_size", 16);
			labelControl.Size = new Vector2(400, 45);
			statRow.AddChild(labelControl);

			// 添加值
			var valueControl = new Label();
			valueControl.Text = value;
			valueControl.AddThemeColorOverride("font_color", valueColor);
			valueControl.AddThemeFontSizeOverride("font_size", 16);
			valueControl.Size = new Vector2(400, 45);
			valueControl.HorizontalAlignment = HorizontalAlignment.Right;
			statRow.AddChild(valueControl);

			// 添加分隔线
			var separator = new ColorRect();
			separator.Size = new Vector2(840, 1);
			separator.Color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
			separator.Position = new Vector2(0, 44);
			separator.ZIndex = -1;
			statRow.AddChild(separator);
		}

		private void AddEquipmentUI(VBoxContainer container, Unit unit)
		{
			// 添加标题
			var titleLabel = new Label();
			titleLabel.Text = "装备栏";
			titleLabel.AddThemeColorOverride("font_color", new Color(1, 0.8f, 0.4f)); // 金色标题
			titleLabel.AddThemeFontSizeOverride("font_size", 20);
			titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
			titleLabel.Size = new Vector2(860, 40);
			titleLabel.Position = new Vector2(0, 10);
			container.AddChild(titleLabel);

			var equipmentGrid = new GridContainer();
			equipmentGrid.Size = new Vector2(840, 420);
			equipmentGrid.Position = new Vector2(20, 60);
			equipmentGrid.Columns = 2; // 改为2列，增加每个装备的显示空间
			container.AddChild(equipmentGrid);

			// 装备插槽
			Equipment.EquipmentSlot[] slots = new Equipment.EquipmentSlot[]
			{
				Equipment.EquipmentSlot.Head,
				Equipment.EquipmentSlot.Chest,
				Equipment.EquipmentSlot.Legs,
				Equipment.EquipmentSlot.Feet,
				Equipment.EquipmentSlot.Weapon,
				Equipment.EquipmentSlot.Shield,
				Equipment.EquipmentSlot.Accessory1,
				Equipment.EquipmentSlot.Accessory2
			};

			foreach (var slot in slots)
			{
				// 创建装备容器
				var equipmentContainer = new VBoxContainer();
				equipmentContainer.Size = new Vector2(450, 100);
				equipmentContainer.AddThemeConstantOverride("separation", 8);
				equipmentGrid.AddChild(equipmentContainer);

				// 添加插槽标签
				var slotLabel = new Label();
				slotLabel.Text = GetSlotName(slot);
				slotLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.8f));
				slotLabel.AddThemeFontSizeOverride("font_size", 14);
				equipmentContainer.AddChild(slotLabel);

				// 创建装备信息容器
				var itemInfoContainer = new HBoxContainer();
				itemInfoContainer.Size = new Vector2(450, 70);
				itemInfoContainer.AddThemeConstantOverride("separation", 15);
				equipmentContainer.AddChild(itemInfoContainer);

				// 添加装备背景
				var equipmentBackground = new ColorRect();
				equipmentBackground.Size = new Vector2(450, 70);
				equipmentBackground.Color = new Color(0.15f, 0.15f, 0.15f, 0.5f);
				equipmentBackground.ZIndex = -1;
				itemInfoContainer.AddChild(equipmentBackground);

				// 添加装备图标
				var iconTexture = new TextureRect();
				iconTexture.CustomMinimumSize = new Vector2(64, 64);
				iconTexture.Size = new Vector2(64, 64);
				iconTexture.StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered;
				itemInfoContainer.AddChild(iconTexture);

				// 添加装备按钮
				var equipmentButton = new Button();
				equipmentButton.Size = new Vector2(350, 60);
				equipmentButton.MouseFilter = Control.MouseFilterEnum.Stop;
				// 设置按钮样式
				var buttonStyle = new StyleBoxFlat();
				buttonStyle.BgColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
				buttonStyle.BorderColor = new Color(0.4f, 0.4f, 0.4f);
				buttonStyle.BorderWidthTop = 1;
				buttonStyle.BorderWidthBottom = 1;
				buttonStyle.BorderWidthLeft = 1;
				buttonStyle.BorderWidthRight = 1;
				equipmentButton.AddThemeStyleboxOverride("normal", buttonStyle);
				var hoverStyle = new StyleBoxFlat();
				hoverStyle.BgColor = new Color(0.25f, 0.25f, 0.25f, 0.8f);
				hoverStyle.BorderColor = new Color(0.5f, 0.5f, 0.5f);
				hoverStyle.BorderWidthTop = 1;
				hoverStyle.BorderWidthBottom = 1;
				hoverStyle.BorderWidthLeft = 1;
				hoverStyle.BorderWidthRight = 1;
				equipmentButton.AddThemeStyleboxOverride("hover", hoverStyle);
				equipmentButton.AddThemeColorOverride("font_color", new Color(1, 1, 1));

				if (unit.Equipment.ContainsKey(slot))
				{
					var equipment = unit.Equipment[slot];
					equipmentButton.Text = equipment.Name;
					equipmentButton.Pressed += () => OnEquipmentClicked(equipment);
					iconTexture.Texture = GetItemTexture(equipment);
				}
				else
				{
					equipmentButton.Text = "空";
					equipmentButton.Disabled = true;
					iconTexture.Texture = null;
				}

				itemInfoContainer.AddChild(equipmentButton);
			}
		}

		private void AddInventoryUI(VBoxContainer container, Unit unit)
		{
			// 添加标题
			var titleLabel = new Label();
			titleLabel.Text = "背包";
			titleLabel.AddThemeColorOverride("font_color", new Color(1, 0.8f, 0.4f)); // 金色标题
			titleLabel.AddThemeFontSizeOverride("font_size", 20);
			titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
			titleLabel.Size = new Vector2(860, 40);
			titleLabel.Position = new Vector2(0, 10);
			container.AddChild(titleLabel);

			var inventoryGrid = new GridContainer();
			inventoryGrid.Size = new Vector2(840, 420);
			inventoryGrid.Position = new Vector2(20, 60);
			inventoryGrid.Columns = 2; // 改为2列，增加每个物品的显示空间
			container.AddChild(inventoryGrid);

			foreach (var slot in unit.Inventory.Slots)
			{
				// 创建物品容器
				var itemContainer = new HBoxContainer();
				itemContainer.Size = new Vector2(450, 70);
				itemContainer.AddThemeConstantOverride("separation", 15);
				inventoryGrid.AddChild(itemContainer);

				// 添加物品背景
				var itemBackground = new ColorRect();
				itemBackground.Size = new Vector2(450, 70);
				itemBackground.Color = new Color(0.15f, 0.15f, 0.15f, 0.5f);
				itemBackground.ZIndex = -1;
				itemContainer.AddChild(itemBackground);

				// 添加物品图标
				var iconTexture = new TextureRect();
				iconTexture.CustomMinimumSize = new Vector2(64, 64);
				iconTexture.Size = new Vector2(64, 64);
				iconTexture.StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered;
				iconTexture.Texture = GetItemTexture(slot.Item);
				itemContainer.AddChild(iconTexture);

				// 添加物品按钮
				var itemButton = new Button();
				itemButton.Size = new Vector2(350, 60);
				itemButton.Text = slot.Item.Name + " (" + slot.Quantity + ")";
				itemButton.Pressed += () => OnItemClicked(slot.Item);
				// 设置按钮样式
				var buttonStyle = new StyleBoxFlat();
				buttonStyle.BgColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
				buttonStyle.BorderColor = new Color(0.4f, 0.4f, 0.4f);
				buttonStyle.BorderWidthTop = 1;
				buttonStyle.BorderWidthBottom = 1;
				buttonStyle.BorderWidthLeft = 1;
				buttonStyle.BorderWidthRight = 1;
				itemButton.AddThemeStyleboxOverride("normal", buttonStyle);
				var hoverStyle = new StyleBoxFlat();
				hoverStyle.BgColor = new Color(0.25f, 0.25f, 0.25f, 0.8f);
				hoverStyle.BorderColor = new Color(0.5f, 0.5f, 0.5f);
				hoverStyle.BorderWidthTop = 1;
				hoverStyle.BorderWidthBottom = 1;
				hoverStyle.BorderWidthLeft = 1;
				hoverStyle.BorderWidthRight = 1;
				itemButton.AddThemeStyleboxOverride("hover", hoverStyle);
				itemButton.AddThemeColorOverride("font_color", new Color(1, 1, 1));
				itemContainer.AddChild(itemButton);
			}

			if (unit.Inventory.Slots.Count == 0)
			{
				var emptyLabel = new Label();
				emptyLabel.Text = "背包为空";
				emptyLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.8f));
				emptyLabel.HorizontalAlignment = HorizontalAlignment.Center;
				emptyLabel.Size = new Vector2(860, 50);
				emptyLabel.Position = new Vector2(0, 120);
				container.AddChild(emptyLabel);
			}
		}

		private void OnEquipmentClicked(Equipment equipment)
		{
			// 显示装备详情
			if (currentUnit.IsPlayer)
			{
				// 玩家单位可以卸下装备
				var dialog = new AcceptDialog();
			dialog.Title = "装备详情";
			dialog.Size = new Vector2I(400, 200);

			var label = new Label();
			label.Text = $"{equipment.Name}\n\n{equipment.Description}\n\n" +
				$"攻击力: +{equipment.AttackBonus}\n" +
				$"防御力: +{equipment.DefenseBonus}\n" +
				$"速度: +{equipment.SpeedBonus}\n" +
				$"幸运: +{equipment.LuckBonus}";
			label.Size = new Vector2I(380, 120);
			label.Position = new Vector2I(10, 10);
			dialog.AddChild(label);

			// 使用自定义按钮处理
			var unequipButton = new Button();
			unequipButton.Text = "卸下";
			unequipButton.Pressed += () =>
			{
				// 卸下装备
				currentUnit.Inventory.AddItem(equipment);
				currentUnit.Equipment.Remove(equipment.Slot);
				// 重新显示详情页
				Close();
				ShowUnitDetail(currentUnit);
				dialog.QueueFree();
			};
			dialog.AddChild(unequipButton);

			var closeButton = new Button();
			closeButton.Text = "关闭";
			closeButton.Pressed += () =>
			{
				dialog.QueueFree();
			};
			dialog.AddChild(closeButton);

				GetTree().Root.AddChild(dialog);
				dialog.PopupCentered();
			}
		}

		private void OnItemClicked(Item item)
		{
			// 显示物品详情
			if (currentUnit.IsPlayer && item is Equipment equipment)
			{
				// 玩家单位可以装备物品
				var dialog = new AcceptDialog();
			dialog.Title = "物品详情";
			dialog.Size = new Vector2I(400, 200);

			var label = new Label();
			label.Text = $"{item.Name}\n\n{item.Description}\n\n";

			if (equipment != null)
			{
				label.Text += $"攻击力: +{equipment.AttackBonus}\n" +
					$"防御力: +{equipment.DefenseBonus}\n" +
					$"速度: +{equipment.SpeedBonus}\n" +
					$"幸运: +{equipment.LuckBonus}";
			}

			label.Size = new Vector2I(380, 120);
			label.Position = new Vector2I(10, 10);
			dialog.AddChild(label);

			// 使用自定义按钮处理
			var equipButton = new Button();
			equipButton.Text = "装备";
			equipButton.Pressed += () =>
			{
				// 装备物品
				if (currentUnit.Equipment.ContainsKey(equipment.Slot))
				{
					// 卸下当前装备
					currentUnit.Inventory.AddItem(currentUnit.Equipment[equipment.Slot]);
				}
				// 装备新物品
				currentUnit.Equipment[equipment.Slot] = equipment;
				currentUnit.Inventory.RemoveItem(equipment);
				// 重新显示详情页
				Close();
				ShowUnitDetail(currentUnit);
				dialog.QueueFree();
			};
			dialog.AddChild(equipButton);

			var closeButton = new Button();
			closeButton.Text = "关闭";
			closeButton.Pressed += () =>
			{
				dialog.QueueFree();
			};
			dialog.AddChild(closeButton);

				GetTree().Root.AddChild(dialog);
				dialog.PopupCentered();
			}
		}

		private void OnCloseButtonPressed()
		{
			Close();
		}

		private void OnBackgroundPressed()
		{
			Close();
		}

		public void Close()
		{
			if (canvasLayer != null)
			{
				canvasLayer.QueueFree();
				canvasLayer = null;
			}
		}

		private SceneTree GetTree()
		{
			var tree = Engine.GetMainLoop() as SceneTree;
			if (tree == null)
			{
				throw new System.Exception("SceneTree not found");
			}
			return tree;
		}
	}
}