using Godot;
using System.Collections.Generic;

namespace CSharpTestGame.Items
{
	[GlobalClass]
	public partial class Inventory : Resource
	{
		public class InventorySlot
		{
			public Item Item { get; set; }
			public int Quantity { get; set; }

			public InventorySlot(Item item, int quantity)
			{
				Item = item;
				Quantity = quantity;
			}
		}

		public int Capacity { get; set; }
		public List<InventorySlot> Slots { get; set; }

		public Inventory() : base()
		{
			Capacity = 20;
			Slots = new List<InventorySlot>();
		}

		public bool AddItem(Item item, int quantity = 1)
		{
			// 检查是否已有相同物品
			foreach (var slot in Slots)
			{
				if (slot.Item.Name == item.Name)
				{
					slot.Quantity += quantity;
					return true;
				}
			}

			// 检查背包是否已满
			if (Slots.Count >= Capacity)
			{
				return false;
			}

			// 添加新物品
			Slots.Add(new InventorySlot(item, quantity));
			return true;
		}

		public bool RemoveItem(Item item, int quantity = 1)
		{
			foreach (var slot in Slots)
			{
				if (slot.Item.Name == item.Name)
				{
					if (slot.Quantity >= quantity)
					{
						slot.Quantity -= quantity;
						if (slot.Quantity <= 0)
						{
							Slots.Remove(slot);
						}
						return true;
					}
					break;
				}
			}
			return false;
		}

		public bool HasItem(Item item)
		{
			foreach (var slot in Slots)
			{
				if (slot.Item.Name == item.Name && slot.Quantity > 0)
				{
					return true;
				}
			}
			return false;
		}

		public int GetItemQuantity(Item item)
		{
			foreach (var slot in Slots)
			{
				if (slot.Item.Name == item.Name)
				{
					return slot.Quantity;
				}
			}
			return 0;
		}
	}
}
