using System;
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
	
	public MobBehavior(Mob1 actor, Player.Player player)
	{
		_actor = actor;
		_player = player;
	}

	public void Loop()
	{
		_state = RefreshState();
		
		switch (_state)
		{
			case MobState.Idle:
				if (LookForPlayer())
				{
					_state = MobState.GoingToPlayer;
				}
				break;
			case MobState.GoingToPlayer:
				TrackPlayerIfNeeded();
				if (GoToPlayer())
				{
					_state = MobState.Attacking;
				}
				break;
			case MobState.Attacking:

				break;
			default:
				throw new ArgumentOutOfRangeException();
		}

		_actor.MoveAndSlide();
	}

	private bool LookForPlayer()
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

	private bool GoToPlayer()
	{
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

	private MobState RefreshState()
	{
		if (!IsWithinDetectionRange())
		{
			return MobState.Idle;
		}

		if (IsWithinAttackRange())
		{
			_actor.Velocity = Vector2.Zero;
			return MobState.Attacking;
		}

		return MobState.GoingToPlayer;
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
}
