using Deflector.levels.Shared;
using Godot;

namespace Deflector.levels.Mobs;

public partial class Mob1: CharacterBody2D, ICharacterWithHp
{
	private MobBehavior _behavior;
	public override void _Ready()
	{
		var playerNode = GetTree().GetFirstNodeInGroup("player");
		if (playerNode is Player.Player player)
		{
			_behavior = new MobBehavior(this, player);
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		_behavior?.Loop();
	}

	public void TakeDamage(int damage)
	{
		GD.Print("Damage taken", damage);
	}
}
