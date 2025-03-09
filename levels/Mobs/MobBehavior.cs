using System;
using System.Collections.Generic;
using Deflector.levels.Shared;
using Deflector.levels.Weapons;
using Godot;

namespace Deflector.levels.Mobs;

public partial class MobBehavior: CharacterBody2D
{
	[Export] public int DetectionRange = 800;
	[Export] public int WalkSpeed = 100;
	
	protected Weapon Weapon;
	
	private const int VisibleConeAngle = 45;
	private const int AttackRange = 150;

	private State _state;
	private Player.Player _player;
	private StateMap _stateMap;
	private StateMap _weaponStateMap;

	protected void Init(Player.Player player)
	{
		_player = player;
		_state =  State.Idle;
		_stateMap = GetStateMap();
		_weaponStateMap = GetWeaponStateMap();
	}

	protected void Loop()
	{
		_state = _stateMap.Execute(_state);
		MoveAndSlide();
	}
	
	private StateMap GetStateMap()
	{
		return new StateMap
		{
			{State.Idle, new StateInfo([
				new TState(State.GoingToPlayer, ShouldStartGoingToPlayer),
				new TState(State.Attacking, IsWithinAttackRange),
			])},
			{State.GoingToPlayer, new StateInfo([
				new TState(State.Attacking, IsWithinAttackRange),
				new TState(State.Idle, () => !IsWithinDetectionRange()),
			], Tick: GoToPlayer)},
			{State.Attacking, new StateInfo([
				new TState(State.GoingToPlayer, () => !IsWithinAttackRange() && !Weapon.IsAttacking),
				new TState(State.Idle, () => !IsWithinDetectionRange() && !Weapon.IsAttacking),
			], Tick: AttackPlayer, Exit: ResetAttack )},
		};
	}

	private StateMap GetWeaponStateMap()
	{
		return new StateMap
		{
			{ State.Reset, new StateInfo([
				new TState(State.RToSlash1Start, () => !Weapon.IsAnimating && IsWithinAttackRange()),
			], () => Weapon.ResetAnimation())},
			{ State.RToSlash1Start, new StateInfo([
				new TState(State.Slash1, () => !Weapon.IsAnimating),
			], () => Weapon.PlayAnimation("reset-to-slash-1-start"))},
			{ State.Slash1, new StateInfo([
				new TState(State.Slash2, () => !Weapon.IsAnimating),
				new TState(State.Reset, () => !Weapon.IsAnimating),
			], () => Weapon.PlayAnimation("slash-1"))},
			{ State.Slash2, new StateInfo([
				new TState(State.Slash1, () => !Weapon.IsAnimating),
				new TState(State.Reset, () => !Weapon.IsAnimating),
			], () => Weapon.PlayAnimation("slash-2"))}
		};
	}

	private bool ShouldStartGoingToPlayer()
	{
		if (!IsWithinDetectionRange())
		{
			return false;
		}
		
		var toPlayer = ToPlayer();
		var forward = Vector2.Right.Rotated(Rotation);
		var angleToPlayer = forward.AngleTo(toPlayer.Normalized());
		if (Math.Abs(angleToPlayer) > double.DegreesToRadians(VisibleConeAngle))
		{
			return false;
		}
		
		return true;
	}
	
	private bool GoToPlayer()
	{
		TrackPlayerIfNeeded();
		
		var toPlayer = ToPlayer();
		var forward = Vector2.Right.Rotated(Rotation);
		if (toPlayer.Length() > AttackRange)
		{
			Velocity = forward * WalkSpeed;
			return false;
		}

		Velocity = Vector2.Zero;
		return true;
	}
	
	private bool TrackPlayerIfNeeded()
	{
		var toPlayer = ToPlayer();
		var forward = Vector2.Right.Rotated(Rotation);
		var angleToPlayer = forward.AngleTo(toPlayer.Normalized());
		if (Math.Abs(angleToPlayer) > double.DegreesToRadians(VisibleConeAngle))
		{
			if (angleToPlayer > 0)
			{
				Rotate(-angleToPlayer);
			}
			else
			{
				Rotate(angleToPlayer);
			}
		}
		return true;
	}

	private bool AttackPlayer()
	{
		Velocity = Vector2.Zero;
		Weapon.State = _weaponStateMap.Execute(Weapon.State);
		return true;
	}

	private bool ResetAttack()
	{
		Weapon.State = State.Reset;
		_weaponStateMap.SetToState(Weapon.State);
		return true;
	}
	
	private bool IsWithinDetectionRange()
	{
		return ToPlayer().Length() <= DetectionRange;
	}


	private bool IsWithinAttackRange()
	{
		return ToPlayer().Length() <= AttackRange;
	}

	
	private Vector2 ToPlayer()
	{
		return _player.GlobalPosition - GlobalPosition;
	}

}
