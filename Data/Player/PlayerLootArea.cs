using Godot;
using System.Collections.Generic;

namespace Deflector.Data.Player;

public partial class PlayerLootArea : Area2D
{
	private readonly List<LootableItem.LootableItem> _lootableItemsInRange = [];
	
	public PlayerLootArea()
	{
		Connect(Area2D.SignalName.AreaEntered, Callable.From((Area2D area2D) => OnAreaEntered(area2D)));
		Connect(Area2D.SignalName.AreaExited, Callable.From((Area2D area2D) => OnAreaExited(area2D)));
	}
	
	private void OnAreaEntered(Area2D area2D)
	{
		if (area2D is not LootableArea lootableArea)
		{
			return;
		}

		if (lootableArea.Owner is LootableItem.LootableItem lootableItem)
		{
			_lootableItemsInRange.Add(lootableItem);
		}

		GD.Print("Lootable Items", _lootableItemsInRange.Count);

		/*if (Owner is Player player)
		{
			// player.OnHitTaken(hitBox.Damage);
			GD.Print("PlayerLootArea::OnAreaEntered");
		}*/
	}
	
	private void OnAreaExited(Area2D area2D)
	{
		if (area2D is not LootableArea lootableArea)
		{
			return;
		}
		
		if (lootableArea.Owner is LootableItem.LootableItem lootableItem)
		{
			_lootableItemsInRange.Remove(lootableItem);
		}
		
		GD.Print("Lootable Items", _lootableItemsInRange.Count);
	}
}
