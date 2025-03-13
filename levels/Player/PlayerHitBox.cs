using Godot;

namespace Deflector.levels.Player;

public partial class PlayerHitBox: Area2D
{
	[Export] public int Damage = 10;

	public PlayerHitBox()
	{
		CollisionLayer = 2;
		CollisionMask = 4;
	}
}

/*
 * Enemy  HurtBox on	Layer 4		Detects from Layer 2
 * Player HitBox on		Layer 2		Hits on Layer 4
 * 
 * Enemy  HitBox on		Layer 4		Hits on Layer 2
 * Player HurtBox on	Layer 2		Detects from Layer 4
 */
