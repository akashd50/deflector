using Deflector.Data.Shared;
using Deflector.Data.Weapons;
using Godot;

namespace Deflector.Data.Mobs.Mob2;

public partial class Mob2: MobBehavior, IDamageable
{
	public override void _Ready()
	{
		var playerNode = GetTree().GetFirstNodeInGroup("player");
		if (playerNode is Player.Player player)
		{
			Init(player);
		}
		
		Weapon = GetNode<Weapon>("MobWeapon");
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
				new TState(State.Attacking, () => IsWithinAttackRange() ? ActionScoreRoll(25) : 0),
			], Enter: ReadyStance, Tick: ActWary)},
			{State.GoingToPlayer, new StateInfo([
				new TState(State.GoingToPlayer, () => !IsWithinAttackRange() ? ActionScoreRoll(60) : 0),
				new TState(State.Wary, () => ActionScoreRoll(60)),
				new TState(State.Attacking, () => IsWithinAttackRange() ? ActionScoreRoll(25) : 0),
				new TState(State.Idle, () => !IsWithinDetectionRange() ? ActionScoreRoll(90) : 0),
			], Tick: GoToPlayer)},
			{State.Attacking, new StateInfo([
				new TState(State.Attacking, () => IsWithinAttackRange() ? ActionScoreRoll(70) : 0),
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
				new TState(State.Slash1, () => !Weapon.IsAnimating && IsWithinAttackRange() ? ActionScoreRoll(100) : 0),
			], (State fromState) =>
			{
				return fromState switch
				{
					State.Reset => Weapon.QueueAnimation("reset-to-ready"),
					State.Slash1 => Weapon.QueueAnimation("slash-1-to-ready"),
					_ => true,
				};
			})},
			{ State.Slash1, new StateInfo([
				new TState(State.Slash2, () => !Weapon.IsAnimating && IsWithinAttackRange() ? ActionScoreRoll(80) : 0),
				new TState(State.Ready, () => !Weapon.IsAnimating && !IsWithinAttackRange() ? ActionScoreRoll(100) : 0),
			], _ => Weapon.QueueAnimation("slash-1"))},
			{ State.Slash2, new StateInfo([
				new TState(State.Slash1, () => !Weapon.IsAnimating && IsWithinAttackRange() ? ActionScoreRoll(50) : 0),
				new TState(State.Ready, () => !Weapon.IsAnimating && !IsWithinAttackRange() ? ActionScoreRoll(100) : 0),
			], _ => Weapon.QueueAnimation("slash-2"))}
		};
	}
}
