using System.Collections.Generic;

namespace Deflector.levels.Shared;

public class StateMap : Dictionary<State, StateInfo> {

    public State Execute(State currentState)
    {
        var newState = currentState;
        var stateInfo = this[currentState];

        foreach (var state in stateInfo.PossibleStates)
        {
            if (!state.Condition()) continue;

            newState = state.ToState;

            stateInfo.Exit?.Invoke();
            this[newState].Enter?.Invoke();
			
            break;
        }

        if (newState == currentState)
        {
            stateInfo.Tick?.Invoke();   
        }

        return newState;
    }

    public void SetToState(State newState)
    {
        var stateInfo = this[newState];
        stateInfo.Enter?.Invoke();
    }
}