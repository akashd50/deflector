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
	
	private AnimationAction<string> _queuedAnimationAction;
	private AnimationAction<string> _currentAnimationAction;

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
			_queuedAnimationAction = new AnimationAction<string>()
			{
				Data = name,
				QueuedTime = Time.GetTicksMsec(),
				PlayAlways = playAlways ?? false,
				OnDone = onDone,
			};	
		}
		else
		{
			_currentAnimationAction = new AnimationAction<string>()
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
		
		if (_currentAnimationAction != null)
		{
			var callback = _currentAnimationAction.OnDone;
			_currentAnimationAction = null;
			callback?.Invoke();
		}

		if (_queuedAnimationAction != null && (_queuedAnimationAction.PlayAlways || Time.GetTicksMsec() - _queuedAnimationAction.QueuedTime < 500))
		{
			_currentAnimationAction = _queuedAnimationAction;
			PlayAnimation(_currentAnimationAction.Data);
			_queuedAnimationAction = null;
		}
	}
}
