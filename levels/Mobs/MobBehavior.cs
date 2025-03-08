using System;
using Godot;

namespace Deflector.levels.Mobs;

public class MobBehavior
{
    private enum MobState
    {
        Idle,
        SpottedPlayer,
        GoingToPlayer,
        PlayerReached,
        Attacking,
    }

    private const int DetectionRange = 200;
    private const int VisibleConeAngle = 45;
    private const int WalkSpeed = 100;
    private const int AttackRange = 100;

    private readonly Player.Player _player;
    private readonly Mob1 _actor;
    
    public MobBehavior(Mob1 actor, Player.Player player)
    {
        _actor = actor;
        _player = player;
    }

    public bool LookForPlayer()
    {
        if (!IsWithinDetectionRange())
        {
            return false;
        }
        
        var toPlayer = ToPlayer();
        var forward = Vector2.Right.Rotated(_actor.Rotation);
        var angleToPlayer = forward.AngleTo(toPlayer.Normalized());
        if (Math.Abs(angleToPlayer) > double.DegreesToRadians(VisibleConeAngle))
        {
            return false;
        }
        
        return true;
    }
    
    
    private bool IsWithinDetectionRange()
    {
        var toPlayer = ToPlayer();
        var distance = toPlayer.Length();
        return distance <= DetectionRange;
    }

    private Vector2 ToPlayer()
    {
        return _player.GlobalPosition - _actor.GlobalPosition;
    }
}