using Godot;

namespace CSharpTestGame
{
    public class Grid
    {
        public Vector2 GridSize { get; private set; }
        private bool[,] cells;

        public Grid(int width, int height = 1)
        {
            GridSize = new Vector2(width, height);
            cells = new bool[height, width];
            // 初始化所有单元格为可通行（false）
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    cells[y, x] = false;
                }
            }
        }

        public bool IsValidPosition(Vector2 pos)
        {
            return pos.X >= 0 && pos.X < GridSize.X && pos.Y >= 0 && pos.Y < GridSize.Y;
        }

        public bool IsPassable(Vector2 pos, System.Collections.Generic.List<Unit>? units = null)
        {
            if (!IsValidPosition(pos))
            {
                return false;
            }
            
            // 检查单元格是否有障碍物
            if (cells[(int)pos.Y, (int)pos.X])
            {
                return false;
            }
            
            // 检查单元格是否有单位
            if (units != null)
            {
                foreach (var unit in units)
                {
                    if (unit.Position == pos)
                    {
                        return false;
                    }
                }
            }
            
            return true;
        }

        public void SetObstacle(Vector2 pos, bool isObstacle)
        {
            if (IsValidPosition(pos))
            {
                cells[(int)pos.Y, (int)pos.X] = isObstacle;
            }
        }

        // 重载方法，只检查障碍物
        public bool IsPassable(Vector2 pos)
        {
            return IsPassable(pos, null);
        }
    }
}
