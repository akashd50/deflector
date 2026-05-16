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

	protected override StateMap GetWeaponStateMap()
	{
		return new StateMap(0)
		{
			{ State.Reset, new StateInfo([
				new TState(State.RToSlash1Start, () => !Weapon.GetAnimationHelper().IsAnimating && IsWithinAttackRange() ? ActionScoreRoll(100) : 0),
			], _ => Weapon.GetAnimationHelper().ResetAnimation())},
			{ State.RToSlash1Start, new StateInfo([
				new TState(State.Slash1, () => !Weapon.GetAnimationHelper().IsAnimating ? ActionScoreRoll(25) : 0),
			], _ => Weapon.GetAnimationHelper().QueueAnimation("reset-to-slash-1-start"))},
			{ State.Slash1, new StateInfo([
				new TState(State.Slash2, () => !Weapon.GetAnimationHelper().IsAnimating ? ActionScoreRoll(80) : 0),
				new TState(State.Reset, () => !Weapon.GetAnimationHelper().IsAnimating ? ActionScoreRoll(25) : 0),
			], _ => Weapon.GetAnimationHelper().QueueAnimation("slash-1"))},
			{ State.Slash2, new StateInfo([
				new TState(State.Slash1, () => !Weapon.GetAnimationHelper().IsAnimating ? ActionScoreRoll(50) : 0),
				new TState(State.Reset, () => !Weapon.GetAnimationHelper().IsAnimating ? ActionScoreRoll(25) : 0),
			], _ => Weapon.GetAnimationHelper().QueueAnimation("slash-2"))}
		};
	}
}
