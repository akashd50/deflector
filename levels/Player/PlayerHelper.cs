using System;
using System.Collections.Generic;
using Godot;

namespace Deflector.levels.Player;

public class PlayerHelper(Player player)
{
	public Vector2 Velocity;
	public Vector2 Position;
	public float Rotation;
	
	private int speed = 100;
	
	private const int DashDuration = 80;
	private bool _isDashing = false;
	private ulong _dashStarted = 0;
	private Vector2 _walkDirection = Vector2.Zero;
	private Vector2 _faceDirection = Vector2.Zero;
	
	
	// Deflection
	private bool _isDeflecting = false;
	private ulong _deflectStarted = 0;
	private const int DeflectDuration = 200;

	// lock on
	private CharacterBody2D _lockedOnEnemy;

	public void Init()
	{
		player.DeflectSprite.Hide();
		// player.Weapon.Connect("attackAnimationFinished", attackFinished);
	}
	
	public bool HandleDash(InputEvent @event)
	{
		if (!_isDashing && !player.Weapon.IsAttacking && @event.IsActionPressed("dash"))
		{
			_isDashing = true;

			StopDeflecting();

			_dashStarted = Time.GetTicksMsec();
			Velocity = _walkDirection * (speed * 8);
			return true;
		}
		
		return false;
	}

	public bool HandleDeflect(InputEvent @event)
	{
		if (!_isDashing && !player.Weapon.IsAttacking)
		{
			if (!_isDeflecting && @event.IsActionPressed("deflect"))
			{
				StartDeflecting();
				return true;
			}
			else if (_isDeflecting && @event.IsActionReleased("deflect"))
			{
				StopDeflecting();
				return true;
			}
		}
		return false;
	}

	public bool HandleLockOn(InputEvent @event, List<Node2D> enemies)
	{
		if (@event.IsActionPressed("lockon"))
		{
			if (_lockedOnEnemy == null)
			{
				DoLockOn(enemies);
			}
			else
			{
				_lockedOnEnemy = null;
			}
			return true;
		}
		
		return false;
	}

	public bool HandleAttack(InputEvent @event)
	{
		if (!player.Weapon.IsAttacking && @event.IsActionPressed("attack"))
		{
			player.Weapon.Attack1();
			return true;
		}
		return false;
	}

	private void StartDeflecting()
	{
		_isDeflecting = true;
		_deflectStarted = Time.GetTicksMsec();
		player.DeflectSprite.Show();
	}

	private void StopDeflecting()
	{
		_isDeflecting = false;
		player.DeflectSprite.Hide();
	}

	public void UpdateDirections()
	{
		var direction = GetInput();
		if (direction != Vector2.Zero)
		{
			_walkDirection = direction;
			if (_lockedOnEnemy != null)
			{
				_faceDirection = _lockedOnEnemy.Position - player.Position;
			}
			else
			{
				_faceDirection = _walkDirection;
			}
		}
		else
		{
			if (_lockedOnEnemy != null)
			{
				_faceDirection = _lockedOnEnemy.Position - player.Position;
			}

			_walkDirection = Vector2.Zero;
		}
	}

	public void UpdateRotation()
	{
		if (player.Weapon.IsAttacking)
		{
			return;
		}
		
		Rotation = _faceDirection.Angle();
	}

	public void UpdateVelocity()
	{
		if (_isDashing)
		{
			UpdateDash();
		}
		else
		{
			UpdateWalk();
		}
	}

	private void UpdateDash()
	{
		var currTime = Time.GetTicksMsec();
		if (currTime - _dashStarted > DashDuration)
		{
			Velocity = Vector2.Zero;
			_isDashing = false;
		}
		else
		{
			Velocity -= new Vector2(Velocity.X * 0.1f, Velocity.Y * 0.1f);
		}
	}

	private void UpdateWalk()
	{
		if (_isDeflecting)
		{
			Velocity = _walkDirection * speed/3;
		}
		else if (player.Weapon.IsAttacking)
		{
			Velocity = _walkDirection * speed/10;
		}
		else
		{
			Velocity = _walkDirection * speed;
		}
	}

	private void DoLockOn(List<Node2D> enemies)
	{
		var closestEnemyAngle = 99.0f;
		Node2D closestEnemy = null;
		
		foreach (var enemy in enemies)
		{
			var toEnemy = enemy.Position - player.Position;
			var angleToEnemy = _faceDirection.AngleTo(toEnemy.Normalized());
			if (angleToEnemy < closestEnemyAngle && angleToEnemy < double.DegreesToRadians(90))
			{
				closestEnemyAngle = angleToEnemy;
				closestEnemy = enemy;
			}
		}

		if (closestEnemy != null)
		{
			_lockedOnEnemy = closestEnemy as CharacterBody2D;
		}
	}

	private static Vector2 GetInput()
	{
		return Input.GetVector("left", "right", "up", "down");
	}
}
