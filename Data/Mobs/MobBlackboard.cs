using Godot;

namespace Deflector.Data.Mobs;

public class MobBlackboard
{
    public float    Awareness;
    public Vector2? LastKnownPlayerPos;
    public ulong    LastSeenTimeMs;

    // Wander leash: when NowMs >= WanderUntilMs, pick a new direction.
    public Vector2 WanderDirection = Vector2.Zero;
    public ulong   WanderUntilMs;

    // Strafe direction flip schedule (used while circling the player).
    public int   StrafeSign = 1;
    public ulong StrafeUntilMs;

    // Self-managed timer for the LookAround leaf: 0 = not started.
    public ulong LookAroundStartMs;

    // Weapon BT state.
    public bool  IsWeaponDrawn;
    public ulong NextAttackReadyMs;

    public ulong NowMs           => Time.GetTicksMsec();
    public ulong TimeSinceSeenMs => NowMs - LastSeenTimeMs;

    public void OnPlayerSeen(Vector2 playerPos)
    {
        LastKnownPlayerPos = playerPos;
        LastSeenTimeMs     = NowMs;
    }
}
