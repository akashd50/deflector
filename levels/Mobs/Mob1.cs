using Deflector.levels.Shared;
using Godot;

namespace Deflector.levels.Mobs;

public partial class Mob1: MobBehavior, ICharacterWithHp
{
	public override void _Ready()
	{
		var playerNode = GetTree().GetFirstNodeInGroup("player");
		if (playerNode is Player.Player player)
		{
			Init(player);
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		Loop();
	}

	public void TakeDamage(int damage)
	{
		GD.Print("Damage taken", damage);
	}
}
