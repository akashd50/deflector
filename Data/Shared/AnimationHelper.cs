using System;
using Godot;

namespace Deflector.Data.Shared;

public class AnimationHelper
{
    private AnimationPlayer      _animationPlayer;
    public  bool                 IsAnimating { get; private set; }  = false;
    public  IUsesAnimationHelper Parent      { get; private set; }
    public AnimationAction<string> QueuedAnimationAction  { get; private set; }
    public AnimationAction<string> CurrentAnimationAction { get; private set; }
    
    public AnimationHelper(AnimationPlayer animationPlayer, IUsesAnimationHelper parent)
    {
        Parent = parent;
        _animationPlayer = animationPlayer; 
        _animationPlayer.Connect("animation_finished", Callable.From((string name) => AnimationFinished(name)));
    }
    
    public bool QueueAnimation(string name, Func<bool>? onDone = null, bool? playAlways = false)
    {
        if (IsAnimating)
        {
            QueuedAnimationAction = new AnimationAction<string>()
            {
                Data       = name,
                QueuedTime = Time.GetTicksMsec(),
                PlayAlways = playAlways ?? false,
                OnDone     = onDone,
            };	
        }
        else
        {
            CurrentAnimationAction = new AnimationAction<string>()
            {
                Data       = name,
                QueuedTime = Time.GetTicksMsec(),
                PlayAlways = playAlways ?? false,
                OnDone     = onDone,
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
    
    private bool PlayAnimation(string name)
    {
        if (IsAnimating) return false;

        if (!_animationPlayer.HasAnimation(name))
        {
            GD.PrintErr("Animation doesn't exist: " + name);
            return false;
        }
		
        IsAnimating = true;
        Parent.OnStartAnim();
        _animationPlayer.Play(name);
        return true;
    }
    
    private void AnimationFinished(string name)
    {
        GD.Print("Finished: ", name);

        if (name == "RESET")
        {
            IsAnimating = false;
            Parent.OnResetAnim();
            return;
        }
        
        Parent.OnFinishedAnim(name);
        
        if (CurrentAnimationAction != null)
        {
            var callback = CurrentAnimationAction.OnDone;
            CurrentAnimationAction = null;
            callback?.Invoke();
        }
		
        IsAnimating = false;
        Parent.AfterFinishedAnim();

        if (QueuedAnimationAction != null && (QueuedAnimationAction.PlayAlways || Time.GetTicksMsec() - QueuedAnimationAction.QueuedTime < 500))
        {
            CurrentAnimationAction = QueuedAnimationAction;
            QueuedAnimationAction  = null;
            PlayAnimation(CurrentAnimationAction.Data);
        }
    }
}