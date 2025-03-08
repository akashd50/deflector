using Godot;

namespace Deflector.levels;

public partial class Mob1: CharacterBody2D
{
    public void TakeDamage(int damage)
    {
        GD.Print("Damage taken", damage);
    }
}