using Deflector.levels.Player;
using Deflector.levels.Shared;
using Godot;

namespace Deflector.scripts;

public partial class HurtBox: Area2D
{
    public HurtBox()
    {
        CollisionLayer = 4;
        CollisionMask = 2;
        Connect(Area2D.SignalName.AreaEntered, Callable.From((Area2D area2D) => OnAreaEntered(area2D)));
    }

    private void OnAreaEntered(Area2D area2D)
    {
        if (area2D is not PlayerHitBox hitBox)
        {
            return;
        }
        
        if (Owner is IDamageable damageable)
        {
            damageable.TakeDamage(hitBox.Damage);
        }
    }
}