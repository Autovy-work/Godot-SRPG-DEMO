using Godot;
using System.Collections.Generic;

namespace CSharpTestGame
{
	public class GameStateManager
	{
		private UnitManager unitManager;
		private RichTextLabel battleLog;
		private bool isBattleOver = false;

		public bool IsBattleOver { get { return isBattleOver; } }

		public GameStateManager(UnitManager unitManager, RichTextLabel battleLog)
		{
			this.unitManager = unitManager;
			this.battleLog = battleLog;
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
				ShowSettlementMenu(allEnemiesDead);
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
				ShowGameOverMenu(false);
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
				ShowGameOverMenu(true);
			}
		}

		private void ShowSettlementMenu(bool isVictory)
		{
			// 创建一个新的CanvasLayer，确保它在所有其他节点之上
			var canvasLayer = new CanvasLayer();
			canvasLayer.Name = "SettlementCanvasLayer";
			// 获取根节点并添加
			var root = GetTree().Root;
			root.AddChild(canvasLayer);

			// 创建背景遮罩
			var blurBackground = new ColorRect();
			blurBackground.Size = root.Size;
			blurBackground.Position = Vector2.Zero;
			blurBackground.Color = new Color(0, 0, 0, 0.8f); // 增加透明度，确保完全盖住背景
			blurBackground.Name = "BlurBackground";
			blurBackground.MouseFilter = Control.MouseFilterEnum.Stop; // 阻止点击穿透
			canvasLayer.AddChild(blurBackground);



			// 创建结算菜单面板
			var menuPanel = new ColorRect();
			menuPanel.Size = new Vector2(300, 200);
			menuPanel.Position = (root.Size - menuPanel.Size) / 2;
			menuPanel.Color = new Color(0.2f, 0.2f, 0.2f);
			menuPanel.Name = "SettlementMenu";
			canvasLayer.AddChild(menuPanel);

			// 添加标题
			var titleLabel = new Label();
			titleLabel.Size = new Vector2(300, 50);
			titleLabel.Position = new Vector2(0, 20);
			titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
			titleLabel.VerticalAlignment = VerticalAlignment.Center;
			titleLabel.AddThemeColorOverride("font_color", new Color(1, 1, 1));
			titleLabel.Text = isVictory ? "胜利！" : "失败！";
			titleLabel.Name = "TitleLabel";
			menuPanel.AddChild(titleLabel);

			// 添加Restart按钮
			var restartButton = new Button();
			restartButton.Size = new Vector2(120, 40);
			restartButton.Position = new Vector2(30, 100);
			restartButton.Text = "重新开始";
			restartButton.Name = "RestartButton";
			restartButton.Pressed += OnRestartButtonPressed;
			menuPanel.AddChild(restartButton);

			// 添加Quit按钮
			var quitButton = new Button();
			quitButton.Size = new Vector2(120, 40);
			quitButton.Position = new Vector2(150, 100);
			quitButton.Text = "退出";
			quitButton.Name = "QuitButton";
			quitButton.Pressed += OnQuitButtonPressed;
			menuPanel.AddChild(quitButton);
		}

		private void ShowGameOverMenu(bool isVictory)
		{
			// 创建一个新的CanvasLayer，确保它在所有其他节点之上
			var canvasLayer = new CanvasLayer();
			canvasLayer.Name = "GameOverCanvasLayer";
			// 获取根节点并添加
			var root = GetTree().Root;
			root.AddChild(canvasLayer);

			// 创建背景遮罩
			var blurBackground = new ColorRect();
			blurBackground.Size = root.Size;
			blurBackground.Position = Vector2.Zero;
			blurBackground.Color = new Color(0, 0, 0, 0.8f); // 增加透明度，确保完全盖住背景
			blurBackground.Name = "BlurBackground";
			blurBackground.MouseFilter = Control.MouseFilterEnum.Stop; // 阻止点击穿透
			canvasLayer.AddChild(blurBackground);

			// 创建游戏结束菜单面板
			var menuPanel = new ColorRect();
			menuPanel.Size = new Vector2(300, 200);
			menuPanel.Position = (root.Size - menuPanel.Size) / 2;
			menuPanel.Color = new Color(0.2f, 0.2f, 0.2f);
			menuPanel.Name = "GameOverMenu";
			canvasLayer.AddChild(menuPanel);

			// 添加标题
			var titleLabel = new Label();
			titleLabel.Size = new Vector2(300, 50);
			titleLabel.Position = new Vector2(0, 20);
			titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
			titleLabel.VerticalAlignment = VerticalAlignment.Center;
			titleLabel.AddThemeColorOverride("font_color", new Color(1, 1, 1));
			titleLabel.Text = isVictory ? "胜利！" : "失败！";
			titleLabel.Name = "TitleLabel";
			menuPanel.AddChild(titleLabel);

			// 添加Restart按钮
			var restartButton = new Button();
			restartButton.Size = new Vector2(120, 40);
			restartButton.Position = new Vector2(30, 100);
			restartButton.Text = "重新开始";
			restartButton.Name = "RestartButton";
			restartButton.Pressed += OnRestartButtonPressed;
			menuPanel.AddChild(restartButton);

			// 添加Quit按钮
			var quitButton = new Button();
			quitButton.Size = new Vector2(120, 40);
			quitButton.Position = new Vector2(150, 100);
			quitButton.Text = "退出";
			quitButton.Name = "QuitButton";
			quitButton.Pressed += OnQuitButtonPressed;
			menuPanel.AddChild(quitButton);
		}

		private void OnRestartButtonPressed()
		{
			// 重新加载当前场景
			GetTree().ReloadCurrentScene();
		}

		private void OnQuitButtonPressed()
		{
			// 退出程序
			GetTree().Quit();
		}

		// 辅助方法，获取场景树
		private SceneTree GetTree()
		{
			return Engine.GetMainLoop() as SceneTree;
		}
	}
}
