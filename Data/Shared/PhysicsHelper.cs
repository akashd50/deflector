using Godot;

namespace Deflector.Data.Shared;

public static class PhysicsHelper
{
    private const double PhysicsFPS = 20;

    public static bool ShouldExecuteBehaviorLoop(ulong lastExecutedTime, double delta)
    {
        const double timePerFrame = 1000/PhysicsFPS;
        var timeSinceLastExecuted = Time.GetTicksMsec() -  (lastExecutedTime + delta);
        return timeSinceLastExecuted > timePerFrame;
    }
}