using System;
using System.Collections.Generic;
using Deflector.Data.Shared;
using Deflector.Data.Weapons;
using Godot;

namespace Deflector.Data.Mobs;

public partial class MobBehavior: CharacterBody2D
{
	[Export] public int DetectionRange = 400;
	[Export] public int Aggressiveness = 50;
	[Export] public int WalkSpeed = 15;
	[Export] public int RunSpeed = 100;
	[Export] public int DashSpeed = 1200;
	[Export] public int RotationSpeed = 1;
	[Export] public int AttackRange = 150;
	[Export] public int VisibleConeAngle = 45;

	protected Weapon Weapon;
	protected Vector2 WalkDirection = Vector2.Zero;
	protected Vector2 FaceDirection = Vector2.Zero;
	protected AnimatedSprite2D AnimatedSprite;
	protected StateMap WeaponStateMap;
	protected ulong LastWeaponAttackFinishTime = 0;
	protected ulong CurrentWeaponCooldownTime = 0;
	
	protected bool ChasePlayerDuringAttack = false;
	protected bool TrackPlayerDuringAttack = false;
	protected bool DashToPlayerDuringAttack = false;
	
	
	private State _state;
	private Player.Player _player;
	private StateMap _stateMap;
	private Random _random;
	
	protected void Init(Player.Player player)
	{
		AddToGroup("Enemies");
		AddToGroup("Persist");

		// AnimatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		
		_random = new Random();
		_player = player;
		_state =  State.Idle;
		_stateMap = GetStateMap();
		WeaponStateMap = GetWeaponStateMap();
		FaceDirection = Vector2.Right.Rotated(Rotation);

		Weapon.WeaponHitBox.OnHitDone += OnHitDone;
	}

	protected virtual void OnHitDone()
	{
		
	}

	public override void _PhysicsProcess(double delta)
	{
		_state = _stateMap.Execute(_state);			
		MoveAndCollide(Velocity * (float)delta);
		ApplyDrag();
	}

	protected virtual StateMap GetStateMap()
	{
		return new StateMap(1000)
		{
			{State.Idle, new StateInfo([
				new TState(State.Wary, () => IsWithinVisibleRegion() ? 100 : 0),
			])},
			{State.Wary, new StateInfo([
				new TState(State.Wary, () => ActionScoreRoll(50)),
				new TState(State.GoingToPlayer, () => IsWithinVisibleRegion() ? ActionScoreRoll(25) : 0),
				new TState(State.Attacking, () => IsWithinAttackRange() && IsWeaponCooldownOver() ? ActionScoreRoll(25) : 0),
			], Tick: ActWary)},
			{State.GoingToPlayer, new StateInfo([
				new TState(State.GoingToPlayer, () => !IsWithinAttackRange() ? ActionScoreRoll(60) : 0),
				new TState(State.Wary, () => ActionScoreRoll(60)),
				new TState(State.Attacking, () => IsWithinAttackRange() && IsWeaponCooldownOver() ? ActionScoreRoll(25) : 0),
				new TState(State.Idle, () => !IsWithinDetectionRange() ? ActionScoreRoll(90) : 0),
			], Tick: GoToPlayerIfOutsideAttackRange)},
			{State.Attacking, new StateInfo([
				new TState(State.Attacking, () => IsWithinAttackRange() && IsWeaponCooldownOver() ? ActionScoreRoll(70) : 0),
				new TState(State.GoingToPlayer, () => !IsWithinAttackRange() ? ActionScoreRoll(25) : 0),
				new TState(State.Idle, () => !IsWithinDetectionRange() ? ActionScoreRoll(90) : 0),
			], Tick: AttackPlayer, Exit: AttackPlayer, ReEval: () => !Weapon.IsAnimating )},
		};
	}

	protected bool IsWeaponInReadyState()
	{
		return Weapon.State == State.Ready;
	}

	protected bool IsWeaponCooldownOver()
	{
		return Time.GetTicksMsec() - LastWeaponAttackFinishTime > CurrentWeaponCooldownTime;
	}

	protected virtual StateMap GetWeaponStateMap()
	{
		return new StateMap(0);
	}

	protected bool IsWithinVisibleRegion()
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
	
	protected virtual bool GoToPlayerIfOutsideAttackRange()
	{
		TrackPlayerIfNeeded();
		var toPlayer = ToPlayer();
		if (toPlayer.Length() > AttackRange)
		{
			GoToPlayer(RunSpeed);
		}

		return true;
	}

	protected bool ActWary()
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
		TrackPlayer(RotationSpeed);
		return true;
	}

	private void TrackPlayer(int rotationSpeed)
	{
		var toPlayer = ToPlayer();
		var angleToPlayer = FaceDirection.AngleTo(toPlayer.Normalized());
		if (angleToPlayer > 0)
		{
			Rotate(Math.Min(float.DegreesToRadians(rotationSpeed), Math.Abs(angleToPlayer)));
		}
		else
		{
			Rotate(-Math.Min(float.DegreesToRadians(rotationSpeed), Math.Abs(angleToPlayer)));
		}

		FaceDirection = Vector2.Right.Rotated(Rotation);
	}

	protected bool AttackPlayer()
	{
		if (ChasePlayerDuringAttack)
		{
			TrackPlayerIfNeeded();
			GoToPlayerIfOutsideAttackRange();
		}

		if (TrackPlayerDuringAttack)
		{
			TrackPlayer(RotationSpeed * 3);
		}

		if (DashToPlayerDuringAttack)
		{
			WalkDirection = FaceDirection;
			Velocity = WalkDirection * DashSpeed;
			DashToPlayerDuringAttack = false;
		}

		if (!Weapon.IsAnimating)
		{
			Weapon.State = WeaponStateMap.Execute(Weapon.State);
		}
		return true;
	}

	protected bool IsWithinDetectionRange()
	{
		return ToPlayer().Length() <= DetectionRange;
	}


	protected bool IsWithinAttackRange()
	{
		return IsWithinRange(AttackRange);
	}
	
	protected bool IsOverAttackRange()
	{
		return IsGreaterThanRange(AttackRange);
	}
	
	protected bool IsWithinRange(int range)
	{
		return ToPlayer().Length() <= range;
	}
	
	protected bool IsGreaterThanRange(int range)
	{
		return ToPlayer().Length() >= range;
	}
	
	protected int ActionScoreRoll(int minScore)
	{
		return _random.Next(minScore, 101);
	}
	
	protected bool QueueAnimationAndSetCooldown(string name, ulong cooldown)
	{
		LastWeaponAttackFinishTime = Time.GetTicksMsec();
		CurrentWeaponCooldownTime = cooldown;
		return Weapon.QueueAnimation(name);
	}
	
	protected bool QueueAnimationAndChase(string name)
	{
		ChasePlayerDuringAttack = true;
		return Weapon.QueueAnimation(name);
	}
	
	protected bool QueueAnimationAndDashToPlayer(string name)
	{
		DashToPlayerDuringAttack = true;
		return Weapon.QueueAnimation(name);
	}
	
	protected bool QueueAnimationAndTrackPlayer(string name)
	{
		TrackPlayerDuringAttack = true;
		return Weapon.QueueAnimation(name);
	}
	
	protected bool StopChase()
	{
		ChasePlayerDuringAttack = false;
		return true;
	}
	
	protected bool StopDash()
	{
		DashToPlayerDuringAttack = false;
		return true;
	}
	
	protected bool StopTrack()
	{
		TrackPlayerDuringAttack = false;
		return true;
	}
	
	private void GoToPlayer(int speed)
	{
		WalkDirection = FaceDirection;
		Velocity += WalkDirection * speed;
		ClampVelocity(speed);		
	}

	private void ApplyDrag()
	{
		if (Velocity == Vector2.Zero)
		{
			return;
		}

		if (Math.Abs(Velocity.X) <= 1 && Math.Abs(Velocity.Y) <= 1)
		{
			Velocity = Vector2.Zero;
			return;
		}
		
		var dragVec = Velocity * -0.05f;
		Velocity += dragVec;
	}

	private void ClampVelocity(int speed)
	{
		Velocity = Velocity.Clamp(new Vector2(-speed, -speed), new Vector2(speed, speed));			
	}

	private Vector2 ToPlayer()
	{
		return _player.GlobalPosition - GlobalPosition;
	}
}
