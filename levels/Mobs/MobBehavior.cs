using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Deflector.levels.Mobs;

public class MobBehavior
{
	private enum MobState
	{
		Idle,
		GoingToPlayer,
		Attacking,
	}

	private const int DetectionRange = 800;
	private const int VisibleConeAngle = 45;
	private const int WalkSpeed = 100;
	private const int AttackRange = 150;

	private MobState _state;
	private readonly Player.Player _player;
	private readonly Mob1 _actor;
	private readonly Dictionary<MobState, StateInfo> _stateMap;
	
	public MobBehavior(Mob1 actor, Player.Player player)
	{
		_actor = actor;
		_player = player;
		_state =  MobState.Idle;
		_stateMap = GetStateMap();
	}
	
	public void Loop()
	{
		var stateInfo = _stateMap[_state];
		if (stateInfo.PossibleStates.Length == 0)
		{
			stateInfo.Tick?.Invoke();
		}

		var updatedState = false;
		foreach (var state in stateInfo.PossibleStates)
		{
			if (state.Condition())
			{
				updatedState = true;
				_state = state.ToState;
				break;
			}
		}

		if (!updatedState)
		{
			stateInfo.Tick?.Invoke();
		}
		
		_actor.MoveAndSlide();
	}

	private bool ShouldStartGoingToPlayer()
	{
		if (!IsWithinDetectionRange())
		{
			return false;
		}
		
		var toPlayer = ToPlayer();
		var forward = Vector2.Right.Rotated(_actor.Rotation);
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
		var forward = Vector2.Right.Rotated(_actor.Rotation);
		if (toPlayer.Length() > AttackRange)
		{
			_actor.Velocity = forward * WalkSpeed;
			return false;
		}

		_actor.Velocity = Vector2.Zero;
		return true;
	}
	
	private bool TrackPlayerIfNeeded()
	{
		var toPlayer = ToPlayer();
		var forward = Vector2.Right.Rotated(_actor.Rotation);
		var angleToPlayer = forward.AngleTo(toPlayer.Normalized());
		if (Math.Abs(angleToPlayer) > double.DegreesToRadians(VisibleConeAngle))
		{
			if (angleToPlayer > 0)
			{
				_actor.Rotate(-angleToPlayer);
			}
			else
			{
				_actor.Rotate(angleToPlayer);
			}
		}
		return true;
	}

	private bool AttackPlayer()
	{
		_actor.Velocity = Vector2.Zero;

		return true;
	}

	private Dictionary<MobState, StateInfo> GetStateMap()
	{
		return new Dictionary<MobState, StateInfo>
		{
			{MobState.Idle, new StateInfo([
				new TState(MobState.GoingToPlayer, ShouldStartGoingToPlayer),
				new TState(MobState.Attacking, IsWithinAttackRange),
			])},
			{MobState.GoingToPlayer, new StateInfo([
				new TState(MobState.Attacking, IsWithinAttackRange),
				new TState(MobState.Idle, () => !IsWithinDetectionRange()),
			], GoToPlayer)},
			{MobState.Attacking, new StateInfo([
				new TState(MobState.GoingToPlayer, () => !IsWithinAttackRange()),
				new TState(MobState.Idle, () => !IsWithinDetectionRange()),
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
		return _player.GlobalPosition - _actor.GlobalPosition;
	}

	private record StateInfo(TState[] PossibleStates, Func<bool>? Tick = null);
	private record TState(MobState ToState, Func<bool> Condition);
}
