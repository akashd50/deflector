using System.Collections.Generic;
using Deflector.Data.Shared;
using Deflector.Data.Weapons;
using Godot;

namespace Deflector.Data.Mobs.Mob2;

public partial class Mob2: MobBehavior, IDamageable
{
	public override void _Ready()
	{
		Weapon = GetNode<Weapon>("MobWeapon");
		var playerNode = GetTree().GetFirstNodeInGroup("player");
		if (playerNode is Player.Player player)
		{
			Init(player);
		}
	}

	public void TakeDamage(int damage)
	{
		GD.Print("Damage taken", damage);
	}
	
	protected override StateMap GetStateMap()
	{
		return new StateMap(1000)
		{
			{State.Idle, new StateInfo([
				new TState(State.Wary, () => IsWithinVisibleRegion() ? 100 : 0),
			])},
			{State.Wary, new StateInfo([
				new TState(State.Wary, () => ActionScoreRoll(50)),
				new TState(State.GoingToPlayer, () => IsWithinVisibleRegion() ? ActionScoreRoll(25) : 0),
				new TState(State.Attacking, () => IsWeaponCooldownOver() ? ActionScoreRoll(25) : 0),
			], Enter: ReadyStance, Tick: ActWary)},
			{State.GoingToPlayer, new StateInfo([
				new TState(State.GoingToPlayer, () => !IsWithinAttackRange() ? ActionScoreRoll(60) : 0),
				new TState(State.Wary, () => ActionScoreRoll(60)),
				new TState(State.Attacking, () => IsWeaponCooldownOver() ? ActionScoreRoll(25) : 0),
				new TState(State.Idle, () => !IsWithinDetectionRange() ? ActionScoreRoll(90) : 0),
			], Tick: GoToPlayerIfOutsideAttackRange)},
			{State.Attacking, new StateInfo([
				new TState(State.Attacking, () => IsWeaponCooldownOver() ? ActionScoreRoll(70) : 0),
				new TState(State.GoingToPlayer, () => !IsWithinAttackRange() ? ActionScoreRoll(25) : 0),
				new TState(State.Idle, () => !IsWithinDetectionRange() ? ActionScoreRoll(90) : 0),
			], Tick: AttackPlayer, Exit: AttackPlayer, ReEval: () => !Weapon.IsAttacking )},
		};
	}

	private bool ReadyStance(State fromState)
	{
		if (fromState != State.Idle)
		{
			return false;
		}
		
		if (Weapon.State != State.Ready)
		{
			WeaponStateMap.SetToState(State.Ready, Weapon.State);
			Weapon.State = State.Ready;
		}

		return true;
	}
	
	protected override StateMap GetWeaponStateMap()
	{
		return new StateMap(0)
		{
			{ State.Reset, new StateInfo([
				new TState(State.Ready, () => !Weapon.IsAnimating && IsWithinDetectionRange() ? ActionScoreRoll(100) : 0),
			], _ => Weapon.ResetAnimation())},
			{ State.Ready, new StateInfo([
				new TState(State.Slash1, () => !Weapon.IsAnimating && IsWithinAttackRange() ? ActionScoreRoll(50) : 0),
				new TState(State.SpinAttackStart, () => !Weapon.IsAnimating ? ActionScoreRoll(50) : 0),
			], (State fromState) =>
			{
				return fromState switch
				{
					State.Reset => Weapon.QueueAnimation("reset-to-ready"),
					State.Slash1 => QueueAnimationAndSetCooldown("slash-1-to-ready", 0),
					State.Slash2 => QueueAnimationAndSetCooldown("slash-2-to-ready", 0),
					State.Slash4 => QueueAnimationAndSetCooldown("slash-4-to-ready", 0),
					State.SpinAttackStart => QueueAnimationAndSetCooldown("spin-attack-to-ready", 0),
					State.SpinAttackLoop => QueueAnimationAndSetCooldown("spin-attack-to-ready", 0),
					_ => true,
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
		};
	}
}
