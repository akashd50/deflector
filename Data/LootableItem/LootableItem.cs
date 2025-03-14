using Deflector.Data.Shared;
using Godot;

namespace Deflector.Data.LootableItem;

public partial class LootableItem : Node2D
{
	[Export] public ItemType Type { get; set; }
	[Export] public ItemLevel Level { get; set; } = 0;
	[Export] public int Quantity { get; set; } = 1;

	private Sprite2D _itemSprite;
	
	public override void _Ready()
	{
		_itemSprite = GetNode<Sprite2D>("ItemSprite");
		_itemSprite.Texture = GD.Load("res://assets/blue_ball.png") as Texture2D;
	}
}
