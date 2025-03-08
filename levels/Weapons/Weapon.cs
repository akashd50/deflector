using Godot;

namespace Deflector.levels.Weapons;

public partial class Weapon: Node2D
{
	public bool IsAttacking;
	
	private AnimationPlayer _animationPlayer;

	[Signal]
	public delegate void OnAnimationFinishedEventHandler(string name);
	
	public override void _Ready()
	{
		_animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
		_animationPlayer.Connect("animation_finished", Callable.From((string name) => AnimationFinished(name)));
	}

	public void Attack1()
	{
		IsAttacking = true;
		_animationPlayer.Play("slash-1");
	}

	private void AnimationFinished(string name)
	{
		GD.Print(name);

		if (name == "RESET") return;
		
		_animationPlayer.Play("RESET");
		EmitSignal(SignalName.OnAnimationFinished, name);
		
		IsAttacking = false;
	}
}
