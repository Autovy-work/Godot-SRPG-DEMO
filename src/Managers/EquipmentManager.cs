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
		/// 为玩家单位添加初始装备和物品
		/// </summary>
		/// <param name="unit">玩家单位</param>
		public void AddInitialEquipmentAndItems(Unit unit)
		{
			if (unit == null || !unit.IsPlayer)
			{
				return;
			}

			// 创建初始装备
			var sword = CreateIronSword();
			var shield = CreateWoodenShield();
			var helmet = CreateLeatherHelmet();

			// 装备到单位
			unit.Equipment[Equipment.EquipmentSlot.Weapon] = sword;
			unit.Equipment[Equipment.EquipmentSlot.Shield] = shield;
			unit.Equipment[Equipment.EquipmentSlot.Head] = helmet;

			// 创建背包物品
			var potion = CreateHealingPotion();
			var bow = CreateShortBow();

			// 添加到背包
			unit.Inventory.AddItem(potion, 2);
			unit.Inventory.AddItem(bow);
		}

		/// <summary>
		/// 创建铁剑
		/// </summary>
		/// <returns>铁剑装备</returns>
		public Equipment CreateIronSword()
		{
			return CreateEquipmentItem("铁剑") as Equipment;
		}

		/// <summary>
		/// 创建木盾
		/// </summary>
		/// <returns>木盾装备</returns>
		public Equipment CreateWoodenShield()
		{
			return CreateEquipmentItem("木盾") as Equipment;
		}

		/// <summary>
		/// 创建皮头盔
		/// </summary>
		/// <returns>皮头盔装备</returns>
		public Equipment CreateLeatherHelmet()
		{
			return CreateEquipmentItem("皮头盔") as Equipment;
		}

		/// <summary>
		/// 创建治疗药水
		/// </summary>
		/// <returns>治疗药水物品</returns>
		public Item CreateHealingPotion()
		{
			return CreateEquipmentItem("治疗药水");
		}

		/// <summary>
		/// 创建短弓
		/// </summary>
		/// <returns>短弓装备</returns>
		public Equipment CreateShortBow()
		{
			return CreateEquipmentItem("短弓") as Equipment;
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