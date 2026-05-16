using Godot;

namespace Deflector.Data.Mobs;

public partial class MobWeaponData: Node2D
{
	[Export] public string         WeaponId;
	public          MobAttackRange MobAttackRange {  get; private set; }
	
	public override void _Ready()
	{
		MobAttackRange = GetNode<MobAttackRange>("Range");
	}
}
