using Godot;

namespace CSharpTestGame
{
	public static class Constants
	{
		// 地图相关常量
		public const int TILE_SIZE = 56;
		public const int DEFAULT_MAP_WIDTH = 10;
		public const int DEFAULT_MAP_HEIGHT = 10;

		// 单位相关常量
		public const int DEFAULT_PLAYER_HEALTH = 15;
		public const int DEFAULT_PLAYER_ATTACK = 5;
		public const int DEFAULT_PLAYER_ATTACK_RANGE = 4;
		public const int DEFAULT_PLAYER_MOVE_RANGE = 4;
		public const int DEFAULT_PLAYER_SPEED = 6;
		public const int DEFAULT_PLAYER_LUCK = 5;

		// 敌人生成常量
		public const int MAX_ENEMY_SPAWN_ATTEMPTS = 100;
		public const float ELITE_FACTOR_MIN = 0.8f;
		public const float ELITE_FACTOR_MAX = 0.9f;
		public const float NORMAL_FACTOR_MIN = 0.4f;
		public const float NORMAL_FACTOR_MAX = 0.6f;

		// UI相关常量
		public const float MENU_BACKGROUND_ALPHA = 0.8f;
		public const int MENU_WIDTH = 300;
		public const int MENU_HEIGHT = 200;
		public const int BUTTON_WIDTH = 120;
		public const int BUTTON_HEIGHT = 40;
		public const int HP_BAR_WIDTH = 40;
		public const int HP_BAR_HEIGHT = 5;

		// 颜色常量
		public static readonly Color PLAYER_COLOR = new Color(0.2f, 0.8f, 0.2f);
		public static readonly Color ENEMY_COLOR = new Color(0.8f, 0.2f, 0.2f);
		public static readonly Color HP_BAR_COLOR = new Color(0.8f, 0.2f, 0.2f);
		public static readonly Color MENU_BACKGROUND_COLOR = new Color(0.2f, 0.2f, 0.2f);
		public static readonly Color MENU_TEXT_COLOR = new Color(1, 1, 1);

		// 资源路径
		public const string PLAYER_TEXTURE_PATH = "res://Resources/warrior.png";
		public const string MELEE_ENEMY_TEXTURE_PATH = "res://Resources/goblin.png";
		public const string RANGED_ENEMY_TEXTURE_PATH = "res://Resources/elfmale_ranger.png";
		public const string ELITE_ENEMY_TEXTURE_PATH = "res://Resources/skeleton.png";

		// 游戏状态相关常量
		public const string SETTLEMENT_CANVAS_LAYER_NAME = "SettlementCanvasLayer";
		public const string GAME_OVER_CANVAS_LAYER_NAME = "GameOverCanvasLayer";
		public const string BLUR_BACKGROUND_NAME = "BlurBackground";
		public const string SETTLEMENT_MENU_NAME = "SettlementMenu";
		public const string GAME_OVER_MENU_NAME = "GameOverMenu";
		public const string TITLE_LABEL_NAME = "TitleLabel";
		public const string RESTART_BUTTON_NAME = "RestartButton";
		public const string QUIT_BUTTON_NAME = "QuitButton";

		// 单位节点相关常量
		public const string UNIT_NODE_PREFIX = "Unit_";
		public const string HP_BAR_NAME = "HPBar";
		public const string UNIT_META_KEY = "unit";

		// 游戏日志相关常量
		public const string GAME_START_MESSAGE = "游戏开始！\n";
		public const string GAME_OVER_ENEMY_WIN_MESSAGE = "游戏结束！敌人获胜！\n";
		public const string GAME_OVER_PLAYER_WIN_MESSAGE = "胜利！玩家获胜！\n";
		public const string ALREADY_ATTACKED_MESSAGE = "该回合已攻击过！\n";
		public const string PLAYER_TURN_MESSAGE = "回合 {0}: 玩家回合\n";
		public const string ENEMY_TURN_MESSAGE = "回合 {0}: 敌人 {1} 回合\n";

		// 提示信息常量
		public const string NO_PLAYER_UNIT_FOUND = "No player unit found";
		public const string COULD_NOT_FIND_VALID_POSITION = "Could not find valid position, using default";
		public const string ENEMY_OUT_OF_ATTACK_RANGE = "Enemy out of attack range! Distance: {0}, Min: {1}, Max: {2}";
		public const string MELEE_ATTACK_RANGED_UNITS_CANNOT_USE = "Melee attack: Ranged units cannot use melee attacks";
		public const string RANGED_ATTACK_MELEE_UNITS_CANNOT_USE = "Ranged attack: Melee units cannot use ranged attacks";
		public const string MELEE_ATTACK_PLAYER_HAS_ALREADY_ATTACKED = "Melee attack: player has already attacked this turn";
		public const string RANGED_ATTACK_PLAYER_HAS_ALREADY_ATTACKED = "Ranged attack: player has already attacked this turn";
		public const string MELEE_ATTACK_NO_UNIT_SELECTED_OR_NOT_PLAYER_TURN = "Melee attack: no unit selected or not player turn";
		public const string RANGED_ATTACK_NO_UNIT_SELECTED_OR_NOT_PLAYER_TURN = "Ranged attack: no unit selected or not player turn";
		public const string END_TURN_NOT_PLAYER_TURN = "Not player turn, cannot end turn";
		public const string ACTION_PANEL_BUTTON_CLICKED = "Action panel button clicked, skipping _input";
		public const string PLAYER_HAS_ALREADY_MOVED = "Player has already moved this turn, not showing movable cells";
	}
}