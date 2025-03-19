using Godot;

namespace Deflector.Data.Shared;

public partial class GenericHitBox: Area2D
{
    [Signal]
    public delegate void OnHitDoneEventHandler();
    
    protected GenericHitBox()
    {
        Connect(Area2D.SignalName.AreaEntered, Callable.From((Area2D area2D) => OnAreaEntered(area2D)));
    }
    
    private void OnAreaEntered(Area2D area2D)
    {
        EmitSignal(SignalName.OnHitDone);
    }
}