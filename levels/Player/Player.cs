using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Deflector.levels;
using Deflector.levels.Shared;
using Deflector.levels.Weapons.Sword;

namespace Deflector.levels.Player;

public partial class Player : CharacterBody2D, IDamageable
{
	[Export]
	public int Speed = 100;
	
	public Sprite2D DeflectSprite;
	public AnimatedSprite2D DeflectIndicator;
	public WeaponSword Weapon;
	private PlayerHelper _playerHelper;
	
	public override void _Ready()
	{
		DeflectSprite = GetNode<Sprite2D>("DeflectVis");
		DeflectIndicator = GetNode<AnimatedSprite2D>("DeflectIndicator");
		Weapon = GetNode<WeaponSword>("WeaponSword");
		_playerHelper = new PlayerHelper(this);
		_playerHelper.Init();
	}

	public override void _PhysicsProcess(double delta)
	{
		_playerHelper.UpdateDirections();
		_playerHelper.UpdateRotation();
		_playerHelper.UpdateVelocity();
		
		SetParameters();
		MoveAndSlide();
	}

	public override void _ShortcutInput(InputEvent e)
	{
		if (_playerHelper.HandleDash(e))
		{
			SetParameters();
		} else if (_playerHelper.HandleDeflect(e))
		{
			SetParameters();
		} else
		{
			_playerHelper.HandleAttack(e);
		}

		if (_playerHelper.HandleLockOn(e, GetAllEnemies()))
		{
			SetParameters();
		}
	}

	public void TakeDamage(int damage)
	{
		// GD.Print("Player taking damage");
	}

	private void SetParameters()
	{
		Rotation = _playerHelper.Rotation;
		Velocity = _playerHelper.Velocity;
	}

	private List<Node2D> GetAllEnemies()
	{
		return GetTree().GetNodesInGroup("enemies").OfType<Node2D>().ToList();
	}
}
