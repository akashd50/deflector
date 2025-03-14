using Deflector.Data.Shared;
using Godot;

namespace Deflector.Data.Mobs.Mob1;

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
		base._PhysicsProcess(delta);
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
			], Tick: ActWary)},
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
			], Tick: AttackPlayer, Exit: ResetAttack, ReEval: () => !Weapon.IsAttacking )},
		};
	}
	
	protected override StateMap GetWeaponStateMap()
	{
		return new StateMap(0)
		{
			{ State.Reset, new StateInfo([
				new TState(State.RToSlash1Start, () => !Weapon.IsAnimating && IsWithinAttackRange() ? ActionScoreRoll(100) : 0),
			], () => Weapon.ResetAnimation())},
			{ State.RToSlash1Start, new StateInfo([
				new TState(State.Slash1, () => !Weapon.IsAnimating ? ActionScoreRoll(25) : 0),
			], () => Weapon.PlayAnimation("reset-to-slash-1-start"))},
			{ State.Slash1, new StateInfo([
				new TState(State.Slash2, () => !Weapon.IsAnimating ? ActionScoreRoll(80) : 0),
				new TState(State.Reset, () => !Weapon.IsAnimating ? ActionScoreRoll(25) : 0),
			], () => Weapon.PlayAnimation("slash-1"))},
			{ State.Slash2, new StateInfo([
				new TState(State.Slash1, () => !Weapon.IsAnimating ? ActionScoreRoll(50) : 0),
				new TState(State.Reset, () => !Weapon.IsAnimating ? ActionScoreRoll(25) : 0),
			], () => Weapon.PlayAnimation("slash-2"))}
		};
	}
}
