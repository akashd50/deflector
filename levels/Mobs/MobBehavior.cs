using System;
using System.Collections.Generic;
using Deflector.levels.Shared;
using Deflector.levels.Weapons;
using Godot;

namespace Deflector.levels.Mobs;

public partial class MobBehavior: CharacterBody2D
{
	[Export] public int DetectionRange = 800;
	[Export] public int Aggressiveness = 50;
	[Export] public int WalkSpeed = 30;
	[Export] public int RunSpeed = 100;
	[Export] public int RotationSpeed = 1;
	
	protected Weapon Weapon;
	protected Vector2 WalkDirection = Vector2.Zero;
	protected Vector2 FaceDirection = Vector2.Zero;
	
	private const int VisibleConeAngle = 45;
	private const int AttackRange = 200;

	private State _state;
	private Player.Player _player;
	private StateMap _stateMap;
	private StateMap _weaponStateMap;
	private ulong _lastPhysicsFrameTime = 0;
	private Random _random;
	
	protected void Init(Player.Player player)
	{
		_random = new Random();
		_player = player;
		_state =  State.Idle;
		_stateMap = GetStateMap();
		_weaponStateMap = GetWeaponStateMap();
		FaceDirection = Vector2.Right.Rotated(Rotation);
	}

	protected void PhysicsLoop(double delta)
	{
		// if (PhysicsHelper.ShouldExecuteBehaviorLoop(_lastPhysicsFrameTime, delta))
		// {
			_lastPhysicsFrameTime =  Time.GetTicksMsec();
			_state = _stateMap.Execute(_state);			
		// }

		MoveAndSlide();
		ApplyDrag();
	}
	
	private StateMap GetStateMap()
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

	private StateMap GetWeaponStateMap()
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

	private bool IsWithinVisibleRegion()
	{
		if (!IsWithinDetectionRange())
		{
			return false;
		}
		
		var toPlayer = ToPlayer();
		var angleToPlayer = FaceDirection.AngleTo(toPlayer.Normalized());
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
		if (toPlayer.Length() > AttackRange)
		{
			WalkDirection = FaceDirection;
			Velocity += WalkDirection * RunSpeed;
			ClampVelocity(RunSpeed);		
			return false;
		}

		Velocity = Vector2.Zero;
		return true;
	}

	private bool ActWary()
	{
		TrackPlayerIfNeeded();

		var shouldChangeDirection = _random.Next(101) < 2;
		if (shouldChangeDirection)
		{
			var randomDirection = Vector2.FromAngle(float.DegreesToRadians(_random.Next(360)));
			WalkDirection = randomDirection.Rotated(Rotation);	
		}
		
		Velocity += WalkDirection * WalkSpeed;
		ClampVelocity(WalkSpeed);
		
		return true;
	}
	
	private bool TrackPlayerIfNeeded()
	{
		var toPlayer = ToPlayer();
		var angleToPlayer = FaceDirection.AngleTo(toPlayer.Normalized());
		if (Math.Abs(angleToPlayer) > double.DegreesToRadians(_random.Next(2, 45)))
		{
			if (angleToPlayer > 0)
			{
				Rotate(Math.Min(float.DegreesToRadians(RotationSpeed), Math.Abs(angleToPlayer)));
			}
			else
			{
				Rotate(-Math.Min(float.DegreesToRadians(RotationSpeed), Math.Abs(angleToPlayer)));
			}

			FaceDirection = Vector2.Right.Rotated(Rotation);
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

	private void ApplyDrag()
	{
		if (Velocity == Vector2.Zero)
		{
			return;
		}

		if (Math.Abs(Velocity.X) < 5 && Math.Abs(Velocity.Y) < 5)
		{
			Velocity = Vector2.Zero;
			return;
		}
		
		var dragVec = Velocity * -0.2f;
		Velocity += dragVec;
	}

	private void ClampVelocity(int speed)
	{
		Velocity = Velocity.Clamp(new Vector2(-speed, -speed), new Vector2(speed, speed));			
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

	private int ActionScoreRoll(int minScore)
	{
		return _random.Next(minScore, 101);
	}
}
