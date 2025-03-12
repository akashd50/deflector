using System;

namespace Deflector.levels.Shared;

public record TState(State ToState, Func<int> Condition);
