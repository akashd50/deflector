using Godot;

namespace Deflector.scripts;

public partial class HitBox: Area2D
{
    [Export] public int Damage = 10;

    public HitBox()
    {
        CollisionLayer = 4;
        CollisionMask = 2;
    }
}