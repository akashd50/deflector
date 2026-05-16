using Godot;

namespace Deflector.Data.Mobs;

public class MobBlackboard
{
    public float Awareness;
    public Vector2? LastKnownPlayerPos;
    public ulong LastSeenTimeMs;
    public ulong StateEnterTimeMs;

    public int AttackBudget;
    public ulong RepositionUntilMs;

    public Vector2 WanderDirection = Vector2.Zero;
    public ulong WanderUntilMs;

    public int StrafeSign = 1;
    public ulong StrafeUntilMs;

    public ulong NowMs => Time.GetTicksMsec();

    public ulong TimeInStateMs => NowMs - StateEnterTimeMs;
    public ulong TimeSinceSeenMs => NowMs - LastSeenTimeMs;

    public void OnStateEntered()
    {
        StateEnterTimeMs = NowMs;
    }

    public void OnPlayerSeen(Vector2 playerPos)
    {
        LastKnownPlayerPos = playerPos;
        LastSeenTimeMs = NowMs;
    }
}
