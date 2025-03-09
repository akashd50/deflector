namespace Deflector.levels.Shared;

public enum State
{
    Idle = 0,
    GoingToPlayer,
    Attacking,
    
    Reset,
    RToSlash1Start,
    Slash1,
    Slash2,
}