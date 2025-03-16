using System;
using System.Collections.Generic;
using Deflector.Data.Shared;
using Godot;

namespace Deflector.Data.Player;

public class PlayerHelper(Player player)
{
	public Vector2 Velocity;
	public Vector2 Position;
	public float Rotation;
	
	private int speed = 100;
	
	private const int DashDuration = 150;
	private bool _isDashing = false;
	private ulong _dashStarted = 0;
	private Vector2 _walkDirection = Vector2.Zero;
	private Vector2 _faceDirection = Vector2.Zero;
	
	// Deflection
	private bool _isDeflecting = false;
	private ulong _deflectStarted = 0;
	private const int DeflectDuration = 200;
	private StateMap _weaponStateMap;
	
	// lock on
	private CharacterBody2D _lockedOnEnemy;

	public void Init()
	{
		player.DeflectIndicator.Hide();
		player.DeflectIndicator.AnimationFinished += () => player.DeflectIndicator.Hide();
	}
	
	public bool HandleDash(InputEvent @event)
	{
		if (!_isDashing && !player.Weapon.IsAttacking && @event.IsActionPressed("dash"))
		{
			_isDashing = true;

			if (_isDeflecting)
			{
				StopBlocking();
			}

			_dashStarted = Time.GetTicksMsec();
			Velocity = _walkDirection * (speed * 12);
			return true;
		}
		
		return false;
	}

	public bool HandleDeflect(InputEvent @event)
	{
		if (!@event.IsAction("deflect"))
		{
			return false;
		}
		
		if (!_isDashing && !player.Weapon.IsAttacking)
		{
			if (!_isDeflecting && @event.IsPressed())
			{
				StartBlocking();
				return true;
			}
			
			if (_isDeflecting && @event.IsReleased())
			{
				StopBlocking();
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
			if (player.Weapon.State == State.Slash2 || player.Weapon.State == State.Reset)
			{
				player.Weapon.QueueAnimation("slash-1", () =>
				{
					player.Weapon.State = State.Slash1;
					return true;
				});
			} else if (player.Weapon.State == State.Slash1)
			{
				player.Weapon.QueueAnimation("slash-2", () =>
				{
					player.Weapon.State = State.Slash2;
					return true;
				});
			}
			
			return true;
		}
		return false;
	}

	private void StartBlocking()
	{
		_isDeflecting = true;
		_deflectStarted = Time.GetTicksMsec();
		player.Weapon.QueueAnimation("start-blocking", () =>
		{
			if (!Input.IsActionPressed("deflect"))
			{
				StopBlocking();
			}
			return true;
		});
	}

	private void StopBlocking()
	{
		player.Weapon.QueueAnimation("stop-blocking", () =>
		{
			_isDeflecting = false;
			return true;
		});
	}

	public bool ShouldDeflectAttack()
	{
		return Time.GetTicksMsec() - _deflectStarted < DeflectDuration;
	}

	public void DeflectAttack()
	{
		player.DeflectIndicator.Show();
		player.DeflectIndicator.Play("deflect");
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

		/*if (Input.IsActionPressed("deflect") && !_isDeflecting)
		{
			StartBlocking();
		}
		else if (!Input.IsActionPressed("deflect") && _isDeflecting)
		{
			StopBlocking();
		}*/
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
			var angleToEnemy = Math.Abs(_faceDirection.AngleTo(toEnemy.Normalized()));
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
