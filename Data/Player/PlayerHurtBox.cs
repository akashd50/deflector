using Deflector.levels.Shared;
using Deflector.scripts;
using Godot;

namespace Deflector.levels.Player;

public partial class PlayerHurtBox: Area2D
{
    public PlayerHurtBox()
    {
        CollisionLayer = 2;
        CollisionMask = 4;
        Connect(Area2D.SignalName.AreaEntered, Callable.From((Area2D area2D) => OnAreaEntered(area2D)));
    }
    
    private void OnAreaEntered(Area2D area2D)
    {
        if (area2D is not HitBox hitBox)
        {
            return;
        }
        
        if (Owner is Player player)
        {
            player.OnHitTaken(hitBox.Damage);
        }
    }
}