using Godot;
using CSharpTestGame.Items;
using System.Collections.Generic;

namespace CSharpTestGame.Managers
{
	[GlobalClass]
	public partial class EquipmentManager : RefCounted
	{
		private ResourceManager resourceManager;
		private DataLoader dataLoader;

		public EquipmentManager(ResourceManager resourceManager, DataLoader dataLoader)
		{
			this.resourceManager = resourceManager;
			this.dataLoader = dataLoader;
		}



		/// <summary>
		/// 根据名称创建装备或物品
		/// </summary>
		/// <param name="itemName">物品名称</param>
		/// <returns>装备或物品实例</returns>
		public Item CreateEquipmentItem(string itemName)
		{
			var data = dataLoader.GetEquipmentData(itemName);
			if (data == null)
			{
				return null;
			}

			var iconPath = data.ContainsKey("icon") ? data["icon"].ToString() : "";
			var icon = resourceManager.LoadTexture(iconPath);

			// 检查是否是消耗品
			if (data.ContainsKey("type") && data["type"].ToString() == "Consumable")
			{
				int value = GetIntValue(data, "value", 0);
				return Item.Create(
					data["name"].ToString(), 
					data["description"].ToString(), 
					Item.ItemType.Consumable, 
					value, 
					icon
				);
			}
			else
			{
				// 否则创建装备
				var slotStr = data.ContainsKey("slot") ? data["slot"].ToString() : "Weapon";
				var slot = (Equipment.EquipmentSlot)System.Enum.Parse(typeof(Equipment.EquipmentSlot), slotStr);

				int attackBonus = GetIntValue(data, "attack_bonus", 0);
				int defenseBonus = GetIntValue(data, "defense_bonus", 0);
				int speedBonus = GetIntValue(data, "speed_bonus", 0);
				int luckBonus = GetIntValue(data, "luck_bonus", 0);
				int value = GetIntValue(data, "value", 0);

				return Equipment.Create(
					data["name"].ToString(), 
					data["description"].ToString(), 
					slot, 
					attackBonus, 
					defenseBonus, 
					speedBonus, 
					luckBonus, 
					value, 
					icon
				);
			}
		}

		/// <summary>
		/// 安全获取整数值
		/// </summary>
		/// <param name="data">数据字典</param>
		/// <param name="key">键</param>
		/// <param name="defaultValue">默认值</param>
		/// <returns>整数值</returns>
		private int GetIntValue(Godot.Collections.Dictionary data, string key, int defaultValue)
		{
			if (data.ContainsKey(key))
			{
				var value = data[key];
				if (value.VariantType == Variant.Type.Int)
				{
					return value.AsInt32();
				}
				else if (value.VariantType == Variant.Type.Float)
				{
					return (int)value.AsDouble();
				}
			}
			return defaultValue;
		}
	}
}