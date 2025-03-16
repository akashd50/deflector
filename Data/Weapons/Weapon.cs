using System;
using Deflector.Data.Shared;
using Godot;

namespace Deflector.Data.Weapons;

public partial class Weapon: Node2D
{
	public bool IsAttacking { get; private set; } = false;
	public bool IsAnimating { get; private set; }  = false;
	public State State { get; set; } = State.Reset;

	private AnimationPlayer _animationPlayer;
	
	public AnimationAction<string> QueuedAnimationAction { get; private set; }
	public AnimationAction<string> CurrentAnimationAction { get; private set; }

	[Signal]
	public delegate void OnAnimationFinishedEventHandler(string name);
	
	public override void _Ready()
	{
		_animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
		_animationPlayer.Connect("animation_finished", Callable.From((string name) => AnimationFinished(name)));
	}

	private bool PlayAnimation(string name)
	{
		if (IsAnimating) return false;

		if (!_animationPlayer.HasAnimation(name))
		{
			GD.PrintErr("Animation doesn't exist: " + name);
			return false;
		}
		
		IsAnimating = true;
		IsAttacking = true;
		_animationPlayer.Play(name);
		return true;
	}

	public bool QueueAnimation(string name, Func<bool>? onDone = null, bool? playAlways = false)
	{
		if (IsAnimating)
		{
			QueuedAnimationAction = new AnimationAction<string>()
			{
				Data = name,
				QueuedTime = Time.GetTicksMsec(),
				PlayAlways = playAlways ?? false,
				OnDone = onDone,
			};	
		}
		else
		{
			CurrentAnimationAction = new AnimationAction<string>()
			{
				Data = name,
				QueuedTime = Time.GetTicksMsec(),
				PlayAlways = playAlways ?? false,
				OnDone = onDone,
			};	
			PlayAnimation(name);
		}

		return true;
	}

	public bool ResetAnimation()
	{
		_animationPlayer.Play("RESET");
		return true;
	}

	private void AnimationFinished(string name)
	{
		GD.Print("Finished: ", name);
		IsAnimating = false;
		IsAttacking = false;

		if (name == "RESET") return;

		EmitSignal(SignalName.OnAnimationFinished, name);
		
		if (CurrentAnimationAction != null)
		{
			var callback = CurrentAnimationAction.OnDone;
			CurrentAnimationAction = null;
			callback?.Invoke();
		}

		if (QueuedAnimationAction != null && (QueuedAnimationAction.PlayAlways || Time.GetTicksMsec() - QueuedAnimationAction.QueuedTime < 500))
		{
			CurrentAnimationAction = QueuedAnimationAction;
			PlayAnimation(CurrentAnimationAction.Data);
			QueuedAnimationAction = null;
		}
	}
}
