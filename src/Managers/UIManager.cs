using Godot;
using System;

namespace CSharpTestGame
{
	public class UIManager
	{
		// 辅助方法，获取场景树
		private SceneTree GetTree()
		{
			return Engine.GetMainLoop() as SceneTree;
		}

		public void ShowSettlementMenu(bool isVictory)
		{
			// 创建一个新的CanvasLayer，确保它在所有其他节点之上
			var canvasLayer = new CanvasLayer();
			canvasLayer.Name = Constants.SETTLEMENT_CANVAS_LAYER_NAME;
			// 获取根节点并添加
			var root = GetTree().Root;
			root.AddChild(canvasLayer);

			// 创建背景遮罩
			var blurBackground = new ColorRect();
			blurBackground.Size = root.Size;
			blurBackground.Position = Vector2.Zero;
			blurBackground.Color = new Color(0, 0, 0, Constants.MENU_BACKGROUND_ALPHA); // 增加透明度，确保完全盖住背景
			blurBackground.Name = Constants.BLUR_BACKGROUND_NAME;
			blurBackground.MouseFilter = Control.MouseFilterEnum.Stop; // 阻止点击穿透
			canvasLayer.AddChild(blurBackground);

			// 创建结算菜单面板
			var menuPanel = new ColorRect();
			menuPanel.Size = new Vector2(Constants.MENU_WIDTH, Constants.MENU_HEIGHT);
			menuPanel.Position = (root.Size - menuPanel.Size) / 2;
			menuPanel.Color = Constants.MENU_BACKGROUND_COLOR;
			menuPanel.Name = Constants.SETTLEMENT_MENU_NAME;
			canvasLayer.AddChild(menuPanel);

			// 添加标题
			var titleLabel = new Label();
			titleLabel.Size = new Vector2(Constants.MENU_WIDTH, 50);
			titleLabel.Position = new Vector2(0, 20);
			titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
			titleLabel.VerticalAlignment = VerticalAlignment.Center;
			titleLabel.AddThemeColorOverride("font_color", Constants.MENU_TEXT_COLOR);
			titleLabel.Text = isVictory ? "胜利！" : "失败！";
			titleLabel.Name = Constants.TITLE_LABEL_NAME;
			menuPanel.AddChild(titleLabel);

			// 添加Restart按钮
			var restartButton = new Button();
			restartButton.Size = new Vector2(Constants.BUTTON_WIDTH, Constants.BUTTON_HEIGHT);
			restartButton.Position = new Vector2(30, 100);
			restartButton.Text = "重新开始";
			restartButton.Name = Constants.RESTART_BUTTON_NAME;
			restartButton.Pressed += OnRestartButtonPressed;
			menuPanel.AddChild(restartButton);

			// 添加Quit按钮
			var quitButton = new Button();
			quitButton.Size = new Vector2(Constants.BUTTON_WIDTH, Constants.BUTTON_HEIGHT);
			quitButton.Position = new Vector2(150, 100);
			quitButton.Text = "退出";
			quitButton.Name = Constants.QUIT_BUTTON_NAME;
			quitButton.Pressed += OnQuitButtonPressed;
			menuPanel.AddChild(quitButton);
		}

		public void ShowGameOverMenu(bool isVictory)
		{
			// 创建一个新的CanvasLayer，确保它在所有其他节点之上
			var canvasLayer = new CanvasLayer();
			canvasLayer.Name = Constants.GAME_OVER_CANVAS_LAYER_NAME;
			// 获取根节点并添加
			var root = GetTree().Root;
			root.AddChild(canvasLayer);

			// 创建背景遮罩
			var blurBackground = new ColorRect();
			blurBackground.Size = root.Size;
			blurBackground.Position = Vector2.Zero;
			blurBackground.Color = new Color(0, 0, 0, Constants.MENU_BACKGROUND_ALPHA); // 增加透明度，确保完全盖住背景
			blurBackground.Name = Constants.BLUR_BACKGROUND_NAME;
			blurBackground.MouseFilter = Control.MouseFilterEnum.Stop; // 阻止点击穿透
			canvasLayer.AddChild(blurBackground);

			// 创建游戏结束菜单面板
			var menuPanel = new ColorRect();
			menuPanel.Size = new Vector2(Constants.MENU_WIDTH, Constants.MENU_HEIGHT);
			menuPanel.Position = (root.Size - menuPanel.Size) / 2;
			menuPanel.Color = Constants.MENU_BACKGROUND_COLOR;
			menuPanel.Name = Constants.GAME_OVER_MENU_NAME;
			canvasLayer.AddChild(menuPanel);

			// 添加标题
			var titleLabel = new Label();
			titleLabel.Size = new Vector2(Constants.MENU_WIDTH, 50);
			titleLabel.Position = new Vector2(0, 20);
			titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
			titleLabel.VerticalAlignment = VerticalAlignment.Center;
			titleLabel.AddThemeColorOverride("font_color", Constants.MENU_TEXT_COLOR);
			titleLabel.Text = isVictory ? "胜利！" : "失败！";
			titleLabel.Name = Constants.TITLE_LABEL_NAME;
			menuPanel.AddChild(titleLabel);

			// 添加Restart按钮
			var restartButton = new Button();
			restartButton.Size = new Vector2(Constants.BUTTON_WIDTH, Constants.BUTTON_HEIGHT);
			restartButton.Position = new Vector2(30, 100);
			restartButton.Text = "重新开始";
			restartButton.Name = Constants.RESTART_BUTTON_NAME;
			restartButton.Pressed += OnRestartButtonPressed;
			menuPanel.AddChild(restartButton);

			// 添加Quit按钮
			var quitButton = new Button();
			quitButton.Size = new Vector2(Constants.BUTTON_WIDTH, Constants.BUTTON_HEIGHT);
			quitButton.Position = new Vector2(150, 100);
			quitButton.Text = "退出";
			quitButton.Name = Constants.QUIT_BUTTON_NAME;
			quitButton.Pressed += OnQuitButtonPressed;
			menuPanel.AddChild(quitButton);
		}

		private void OnRestartButtonPressed()
		{
			// 清除所有游戏结束相关的CanvasLayer
			var root = GetTree().Root;
			
			// 强制同步删除所有CanvasLayer，确保UI完全清理
			// 不仅删除游戏结束相关的，还要删除所有可能的CanvasLayer，确保彻底清理
			System.Collections.Generic.List<Node> nodesToRemove = new System.Collections.Generic.List<Node>();
			for (int i = 0; i < root.GetChildCount(); i++)
			{
				var child = root.GetChild(i);
				// 删除所有CanvasLayer，确保彻底清理UI
				if (child is CanvasLayer)
				{
					nodesToRemove.Add(child);
				}
			}
			
			// 立即删除节点
			foreach (var node in nodesToRemove)
			{
				if (node.IsInsideTree())
				{
					// 先从父节点移除
					node.GetParent().RemoveChild(node);
					// 然后立即释放
					node.QueueFree();
				}
			}
			
			// 移除MessageQueue引用，因为Godot C# API中不存在这个类型
			// 直接依赖Godot的节点清理机制
			
			// 延迟一帧后重新加载场景，确保所有节点都已被清理
			// 这样可以避免场景切换时的UI层级冲突
			var timer = new Timer();
			timer.WaitTime = 0.01f; // 最小延迟，确保一帧内完成
			timer.OneShot = true;
			root.AddChild(timer);
			timer.Timeout += () => {
				GetTree().ReloadCurrentScene();
				timer.QueueFree();
			};
			timer.Start();
		}

		private void OnQuitButtonPressed()
		{
			// 退出程序
			GetTree().Quit();
		}
	}
}