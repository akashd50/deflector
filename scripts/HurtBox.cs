using Deflector.levels.Shared;
using Godot;

namespace Deflector.scripts;

public partial class HurtBox: Area2D
{
    public HurtBox()
    {
        CollisionLayer = 0;
        CollisionMask = 2;
        Connect(Area2D.SignalName.AreaEntered, Callable.From((HitBox hitBox) => OnAreaEntered(hitBox)));
    }

    private void OnAreaEntered(HitBox hitBox)
    {
        if (Owner is ICharacterWithHp characterWithHp)
        {
            characterWithHp.TakeDamage(hitBox.Damage);
        }
    }
}