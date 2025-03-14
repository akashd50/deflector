using System;

namespace Deflector.Data.Shared;

public record TState(State ToState, Func<int> Condition);
