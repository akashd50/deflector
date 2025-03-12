using Godot;

namespace Deflector.levels.Player;

public partial class PlayerHitBox: Area2D
{
	[Export] public int Damage = 10;

	public PlayerHitBox()
	{
		CollisionLayer = 4;
		CollisionMask = 2;
	}
}

/*
 * Enemy  HurtBox on	Layer 2		Detects from Layer 4
 * Player HitBox on		Layer 4		Hits on Layer 2
 * 
 * Enemy  HitBox on		Layer 2		Hits on Layer 0
 * Player HurtBox on	Layer 0		Detects from Layer 2
 */