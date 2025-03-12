using Deflector.levels.Shared;
using Godot;

namespace Deflector.levels.Mobs;

public partial class Mob1: MobBehavior, IDamageable
{
	public override void _Ready()
	{
		var playerNode = GetTree().GetFirstNodeInGroup("player");
		if (playerNode is Player.Player player)
		{
			Init(player);
		}
		
		Weapon = GetNode<Mob1Weapon>("MobWeapon");
	}

	public override void _PhysicsProcess(double delta)
	{
		PhysicsLoop(delta);
	}

	public void TakeDamage(int damage)
	{
		GD.Print("Damage taken", damage);
	}
}
