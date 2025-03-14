using Deflector.levels.Shared;
using Godot;

namespace Deflector.levels.Weapons;

public partial class Weapon: Node2D
{
	public bool IsAttacking { get; private set; } = false;
	public bool IsAnimating { get; private set; }  = false;
	public State State { get; set; }

	private AnimationPlayer _animationPlayer;

	[Signal]
	public delegate void OnAnimationFinishedEventHandler(string name);
	
	public override void _Ready()
	{
		State = State.Reset;
		_animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
		_animationPlayer.Connect("animation_finished", Callable.From((string name) => AnimationFinished(name)));
	}

	public void Attack1()
	{
		IsAttacking = true;
		_animationPlayer.Play("slash-1");
	}

	public bool PlayAnimation(string name)
	{
		if (IsAnimating) return false;
		
		IsAnimating = true;
		IsAttacking = true;
		_animationPlayer.Play(name);
		return true;
	}

	public bool ResetAnimation()
	{
		_animationPlayer.Play("RESET");
		return true;
	}

	private void AnimationFinished(string name)
	{
		GD.Print(name);
		if (name == "RESET") return;
		
		EmitSignal(SignalName.OnAnimationFinished, name);
		IsAnimating = false;
		IsAttacking = false;
	}
}
