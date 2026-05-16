using Deflector.Data.BehaviorTree;
using Deflector.Data.Shared;
using Deflector.Data.Weapons;
using Godot;

namespace Deflector.Data.Mobs.Mob1;

public partial class Mob1: MobBehavior, IDamageable
{
	protected Weapon Weapon;

	public override void _Ready()
	{
		Init();
		Weapon                        =  GetNode<Mob1Weapon>("MobWeapon");
		Weapon.WeaponHitBox.OnHitDone += OnHitDone;
	}

	public void TakeDamage(int damage)
	{
		GD.Print("Damage taken", damage);
	}

	// Mob1's weapon lives on a separate Weapon node, so animations route
	// through the Weapon's own AnimationHelper rather than the mob's.
	protected override AnimationHelper GetWeaponAnimationHelper() => Weapon?.GetAnimationHelper();

	// Weapon subtree. ChooseAndPlayAttack randomly picks between slash-1 and
	// slash-2 each swing — replacing the old state-map's weighted transitions.
	protected override BTNode BuildWeaponTree()
	{
		return new Selector([
			new Sequence([
				new Condition(IsWeaponAnimPlaying),
				new ActionNode(WaitForAnim),
			]),
			new Sequence([
				new Inverter(new Condition(IsWeaponDrawn)),
				new ActionNode(() => PlayDrawAnim("reset-to-slash-1-start")),
			]),
			new Sequence([
				new Condition(IsWithinAttackRange),
				new Condition(IsOffCooldown),
				new ActionNode(ChooseAndPlayAttack),
			]),
			new ActionNode(ReadyStance),
		]);
	}

	private NodeState ChooseAndPlayAttack()
	{
		var attack = ActionScoreRoll(0) > 50 ? "slash-1" : "slash-2";
		return PlayAttack(attack);
	}
}
