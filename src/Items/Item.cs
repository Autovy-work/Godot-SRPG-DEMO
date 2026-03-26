using Godot;

namespace CSharpTestGame.Items
{
	[GlobalClass]
	public partial class Item : Resource
	{
		public enum ItemType
		{
			Weapon,
			Armor,
			Accessory,
			Consumable
		}

		public string Name { get; set; }
		public string Description { get; set; }
		public ItemType Type { get; set; }
		public int Value { get; set; }
		public Texture2D Icon { get; set; }

		public Item() : base()
		{
		}

		public static Item Create(string name, string description, ItemType type, int value, Texture2D icon = null)
		{
			var item = new Item();
			item.Name = name;
			item.Description = description;
			item.Type = type;
			item.Value = value;
			item.Icon = icon;
			return item;
		}
	}
}