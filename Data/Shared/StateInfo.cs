using System;

namespace Deflector.Data.Shared;

public record StateInfo(TState[] PossibleStates, Func<bool>? Enter = null, Func<bool>? Tick = null, Func<bool>? Exit = null, Func<bool>? ReEval = null);
