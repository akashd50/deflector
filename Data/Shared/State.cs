namespace Deflector.Data.Shared;

public enum State
{
    Idle = 0,
    Null,
    Wary,
    Investigate,
    GoingToPlayer,
    Attacking,
    Reposition,
    
    Reset,
    Ready,
    RToSlash1Start,
    Slash1,
    Slash2,
    Slash3,
    Slash4,
    SpinAttackStart,
    SpinAttackLoop,
    StabStart,
    Stab,
}