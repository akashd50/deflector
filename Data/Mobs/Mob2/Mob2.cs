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
	
	/*
	protected override StateMap GetWeaponStateMap()
	{
		return new StateMap(100, true)
		{
			{ State.Reset, new StateInfo([
				new TState(State.Ready, () => !Weapon.IsAnimating && IsWithinDetectionRange() ? ActionScoreRoll(100) : 0),
			], _ => Weapon.ResetAnimation())},
			{ State.Ready, new StateInfo([
				new TState(State.Slash1, () => !Weapon.IsAnimating && IsWithinAttackRange() ? ActionScoreRoll(50) : 0),
				new TState(State.SpinAttackStart, () => !Weapon.IsAnimating ? ActionScoreRoll(50) : 0),
				new TState(State.StabStart, () => !Weapon.IsAnimating && IsOverAttackRange() ? ActionScoreRoll(50) : 0),
			], (State fromState) =>
			{
				return fromState switch
				{
					State.Reset => Weapon.QueueAnimation("reset-to-ready"),
					State.Slash1 => QueueAnimationAndSetCooldown("slash-1-to-ready", 0),
					State.Slash2 => QueueAnimationAndSetCooldown("slash-2-to-ready", 0),
					State.Slash4 => QueueAnimationAndSetCooldown("slash-4-to-ready", 0),
					State.SpinAttackStart or State.SpinAttackLoop => QueueAnimationAndSetCooldown("spin-attack-to-ready", 0),
					_ => Weapon.QueueAnimation("reset-to-ready"),
				};
			})},
			{ State.Slash1, new StateInfo([
				new TState(State.Slash2, () => !Weapon.IsAnimating && IsWithinAttackRange() ? ActionScoreRoll(80) : 0),
				new TState(State.Ready, () => !Weapon.IsAnimating && !IsWithinAttackRange() ? ActionScoreRoll(100) : 0),
			], _ => Weapon.QueueAnimation("slash-1"))},
			{ State.Slash2, new StateInfo([
				new TState(State.Slash3, () => !Weapon.IsAnimating && IsWithinAttackRange() ? ActionScoreRoll(50) : 0),
				new TState(State.Ready, () => !Weapon.IsAnimating && !IsWithinAttackRange() ? ActionScoreRoll(100) : 0),
			], _ => Weapon.QueueAnimation("slash-2"))},
			{ State.Slash3, new StateInfo([
				new TState(State.Slash4, () => !Weapon.IsAnimating ? ActionScoreRoll(100) : 0),
			], _ => Weapon.QueueAnimation("slash-3"))},
			{ State.Slash4, new StateInfo([
				new TState(State.Ready, () => !Weapon.IsAnimating ? ActionScoreRoll(100) : 0),
			], _ => Weapon.QueueAnimation("slash-4"))},
			{ State.SpinAttackStart, new StateInfo([
				new TState(State.SpinAttackLoop, () => !Weapon.IsAnimating ? ActionScoreRoll(60) : 0),
			], _ => QueueAnimationAndChase("spin-attack-start"), Exit: StopChase)},
			{ State.SpinAttackLoop, new StateInfo([
				new TState(State.SpinAttackLoop, () => !Weapon.IsAnimating ? ActionScoreRoll(60) : 0),
				new TState(State.Ready, () => !Weapon.IsAnimating ? ActionScoreRoll(30) : 0),
			], _ => QueueAnimationAndChase("spin-attack-loop"), Exit: StopChase)},
			{ State.StabStart, new StateInfo([
				new TState(State.Stab, () => 100),
			], _ => QueueAnimationAndTrackPlayer("stab-start"), Exit: StopTrack)},
			{ State.Stab, new StateInfo([
				new TState(State.Ready, () => 100),
			], _ => QueueAnimationAndDashToPlayer("stab"), Exit: StopDash)},
		};
	}*/
}
