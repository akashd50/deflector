﻿namespace Deflector.Data.Shared;

public enum State
{
    Idle = 0,
    Null,
    Wary,
    GoingToPlayer,
    Attacking,
    
    Reset,
    Ready,
    RToSlash1Start,
    Slash1,
    Slash2,
}