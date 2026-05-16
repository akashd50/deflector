using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Deflector.Data.Mobs;

public partial class MobWeaponsGroup: Node2D
{
	public List<MobWeaponData> Weapons { get; private set; }= new List<MobWeaponData>();

	public override void _Ready()
	{
		var children = GetChildren();
		foreach (var child in children)
		{
			if (child is not MobWeaponData weaponData)
			{
				continue;
			}
			
			Weapons.Add(weaponData);
		}
	}

	public bool IsPlayerInRange(Player.Player player)
	{
		foreach (var mobWeaponData in Weapons)
		{
			if (mobWeaponData.MobAttackRange.IsInRange(player))
			{
				return true;
			}
		}

		return false;
	}

	public double GetRandomWeaponRange()
	{
		return Weapons.Select(w => w.MobAttackRange.GetRange()).First();
	}
}
