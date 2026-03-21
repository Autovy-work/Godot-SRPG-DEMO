using Godot;

namespace CSharpTestGame
{
	public partial class TestScript : Node2D
	{
		public override void _Ready()
		{
			GD.Print("TestScript loaded successfully!");
		}
	}
}
