using Deflector.Data.BehaviorTree;
using Deflector.Data.Shared;
using Godot;

namespace Deflector.Data.Mobs.Mob2;

public partial class Mob2: MobBehavior, IDamageable
{
	public override void _Ready()
	{
		WeaponAnimationHelper = new AnimationHelper(GetNode<AnimationPlayer>("WeaponsGroup/AnimationPlayer"), this);
		Init();
		Body.Modulate = new Color(0.5f, 0.5f, 0.5f);
		Eye.Modulate  = new Color(1.0f, 0.3f, 0.3f);
	}

	public void TakeDamage(int damage)
	{
		GD.Print("Damage taken", damage);
	}

	private bool ReadyStance(State fromState)
	{
		if (fromState != State.Idle)
		{
			return false;
		}
		
		if (WeaponState != State.Ready)
		{
			WeaponStateMap.SetToState(State.Ready, WeaponState);
			WeaponState = State.Ready;
		}

		return true;
	}

	protected override BTNode BuildBehavioralTree()
	{
		return new Selector([
			// 1. Wary/Investigation branch
			new Sequence([
				new ActionNode(HasSomethingToInvestigate),
				new ActionNode(Investigate),
				new ActionNode(SearchAreaLookAround)
			]),
			// 2. MELEE COMBAT BRANCH (with 2 variations)
			new Sequence([
				new Selector([
					// Variation A: Combo Chain (Only if cooldown is done)
					new Sequence([
					]),
				])
			]),
		]);
	}
}
