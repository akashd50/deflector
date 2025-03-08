using Deflector.levels.Shared;
using Godot;

namespace Deflector.levels.Mobs;

public partial class Mob1: CharacterBody2D, ICharacterWithHp
{
    public void TakeDamage(int damage)
    {
        GD.Print("Damage taken", damage);
    }
}