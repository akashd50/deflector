using System;
using System.Collections.Generic;
using Deflector.levels.Shared;
using Godot;

namespace Deflector.levels.Mobs;

public partial class MobBehavior: CharacterBody2D
{
	private const int DetectionRange = 800;
	private const int VisibleConeAngle = 45;
	private const int WalkSpeed = 100;
	private const int AttackRange = 150;

	private State _state;
	private Player.Player _player;
	private Dictionary<State, StateInfo> _stateMap;

	protected void Init(Player.Player player)
	{
		_player = player;
		_state =  State.Idle;
		_stateMap = GetStateMap();
	}

	protected void Loop()
	{
		var stateInfo = _stateMap[_state];
		if (stateInfo.PossibleStates.Length == 0)
		{
			stateInfo.Tick?.Invoke();
		}

		var updatedState = false;
		foreach (var state in stateInfo.PossibleStates)
		{
			if (!state.Condition()) continue;

			updatedState = true;
			_state = state.ToState;
			break;
		}

		if (!updatedState)
		{
			stateInfo.Tick?.Invoke();
		}
		
		MoveAndSlide();
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

		return true;
	}

	private Dictionary<State, StateInfo> GetStateMap()
	{
		return new Dictionary<State, StateInfo>
		{
			{State.Idle, new StateInfo([
				new TState(State.GoingToPlayer, ShouldStartGoingToPlayer),
				new TState(State.Attacking, IsWithinAttackRange),
			])},
			{State.GoingToPlayer, new StateInfo([
				new TState(State.Attacking, IsWithinAttackRange),
				new TState(State.Idle, () => !IsWithinDetectionRange()),
			], GoToPlayer)},
			{State.Attacking, new StateInfo([
				new TState(State.GoingToPlayer, () => !IsWithinAttackRange()),
				new TState(State.Idle, () => !IsWithinDetectionRange()),
			], AttackPlayer)},
		};
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
