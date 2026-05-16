using System;
using Godot;

namespace Deflector.Data.Mobs;

public partial class MobAttackRange: Area2D
{
	private CollisionShape2D _collisionShape2D;
	
	public MobAttackRange()
	{
		CollisionMask = 2;
	}

	public override void _Ready()
	{
		_collisionShape2D = GetNode<CollisionShape2D>("CollisionShape2D");
	}

	public bool IsInRange(Player.Player player)
	{
		var overlappingAreas = GetOverlappingAreas();
		foreach (var area in overlappingAreas)
		{
			if (area == player.PlayerHurtBox) 
			{
				return true;
			}
		}
		return false;
	}

	public double GetRange()
	{
		return _collisionShape2D.Shape switch
		{
			RectangleShape2D rectangle => Math.Min(rectangle.Size.X, rectangle.Size.Y),
			CircleShape2D circle       => circle.Radius,
			_                          => 0
		};
	}
}
