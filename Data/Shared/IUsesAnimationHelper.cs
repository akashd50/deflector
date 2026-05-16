namespace Deflector.Data.Shared;

public interface IUsesAnimationHelper
{
    public void OnResetAnim();
    public void OnFinishedAnim(string name);

    public void AfterFinishedAnim();
    public void OnStartAnim();

    public AnimationHelper GetAnimationHelper();
}