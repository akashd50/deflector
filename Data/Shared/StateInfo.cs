using System;

namespace Deflector.levels.Shared;

public record StateInfo(TState[] PossibleStates, Func<bool>? Enter = null, Func<bool>? Tick = null, Func<bool>? Exit = null, Func<bool>? ReEval = null);
