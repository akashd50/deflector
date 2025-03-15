using System;

namespace Deflector.Data.Shared;

public record AnimationAction<T>
{
    public T Data { get; init; }
    public ulong QueuedTime { get; init; }
    public bool PlayAlways { get; init; }
    public Func<bool>? OnDone { get; init; }
}