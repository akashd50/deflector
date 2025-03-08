using System;

namespace Deflector.levels.Shared;

public record TState(State ToState, Func<bool> Condition);
