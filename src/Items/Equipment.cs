using Godot;

namespace CSharpTestGame.Items
{
	[GlobalClass]
	public partial class Equipment : Item
	{
		public enum EquipmentSlot
		{
			Head,
			Chest,
			Legs,
			Feet,
			Weapon,
			Shield,
			Accessory1,
			Accessory2
		}

		public EquipmentSlot Slot { get; set; }
		public int AttackBonus { get; set; }
		public int DefenseBonus { get; set; }
		public int SpeedBonus { get; set; }
		public int LuckBonus { get; set; }

		public Equipment() : base()
		{
		}

		public static Equipment Create(string name, string description, EquipmentSlot slot, int attackBonus, int defenseBonus, int speedBonus, int luckBonus, int value, Texture2D icon = null)
		{
			var equipment = new Equipment();
			equipment.Name = name;
			equipment.Description = description;
			equipment.Type = ItemType.Weapon;
			if (slot == EquipmentSlot.Head || slot == EquipmentSlot.Chest || slot == EquipmentSlot.Legs || slot == EquipmentSlot.Feet || slot == EquipmentSlot.Shield)
			{
				equipment.Type = ItemType.Armor;
			}
			else if (slot == EquipmentSlot.Accessory1 || slot == EquipmentSlot.Accessory2)
			{
				equipment.Type = ItemType.Accessory;
			}
			equipment.Slot = slot;
			equipment.AttackBonus = attackBonus;
			equipment.DefenseBonus = defenseBonus;
			equipment.SpeedBonus = speedBonus;
			equipment.LuckBonus = luckBonus;
			equipment.Value = value;
			equipment.Icon = icon;
			return equipment;
		}
	}
}