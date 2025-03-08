using System;

namespace Deflector.levels.Shared;

public record StateInfo(TState[] PossibleStates, Func<bool>? Tick = null);
