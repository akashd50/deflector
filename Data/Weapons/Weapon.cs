using System;
using Deflector.Data.Mobs;
using Deflector.Data.Shared;
using Godot;

namespace Deflector.Data.Weapons;

public partial class Weapon: Node2D, IUsesAnimationHelper
{
	public  bool            IsAttacking { get; private set; } = false;
	public  State           State       { get; set; }         = State.Reset;
	public  GenericHitBox   WeaponHitBox;
	private AnimationHelper _animationHelper;

	[Signal]
	public delegate void OnAnimationFinishedEventHandler(string name);
	
	public override void _Ready()
	{
		WeaponHitBox     = GetNode<GenericHitBox>("./WeaponSprite/WeaponHitBox");
		_animationHelper = new AnimationHelper(GetNode<AnimationPlayer>("AnimationPlayer"), this);
	}

	public void OnResetAnim()
	{
		IsAttacking = false;
	}

	public void OnFinishedAnim(string name)
	{
		EmitSignal(SignalName.OnAnimationFinished, name);
	}

	public void AfterFinishedAnim()
	{
		IsAttacking = false;
	}

	public void OnStartAnim()
	{
		IsAttacking = true;
	}

	public AnimationHelper GetAnimationHelper() => _animationHelper;
}
