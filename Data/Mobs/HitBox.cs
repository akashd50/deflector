using Deflector.Data.Shared;
using Godot;

namespace Deflector.Data.Mobs;

public partial class HitBox: GenericHitBox
{
    [Export] public int Damage = 10;
    
    public HitBox(): base()
    {
        CollisionLayer = 4;
        CollisionMask = 2;
    }
}