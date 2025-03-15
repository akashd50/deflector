using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Deflector.Data.Shared;

public class StateMap : Dictionary<State, StateInfo>
{
	private ulong _lastTransitionCheck = 0;
	private readonly double _timeBetweenTransitionChecks = 0;
	
	
	public StateMap(double timeBetweenTransitionChecks) : base()
	{
		_timeBetweenTransitionChecks = timeBetweenTransitionChecks;
	}
	
	public State Execute(State currentState)
	{
		var newState = currentState;
		var stateInfo = this[currentState];

		if (stateInfo.ReEval != null && !stateInfo.ReEval())
		{
			return newState;
		}

		if (Time.GetTicksMsec() - _lastTransitionCheck > _timeBetweenTransitionChecks)
		{
			if (TryTransitionState(stateInfo, newState, out var toTransitionTo))
			{
				newState = toTransitionTo;
			}
		}
		
		if (newState == currentState)
		{
			stateInfo.Tick?.Invoke();   
		}

		return newState;
	}

	private bool TryTransitionState(StateInfo stateInfo, State lastState, out State newState)
	{
		_lastTransitionCheck = Time.GetTicksMsec();
		newState = State.Null;

		var stateScores = GetStateScores(stateInfo);
		switch (stateScores.Values.Count)
		{
			case > 1:
			{
				var bestState = GetHighestRankedState(stateScores);
				newState = bestState;
				if (newState != lastState)
				{
					stateInfo.Exit?.Invoke();
					this[newState].Enter?.Invoke();
				}
			   
				break;
			}
			case 1:
				newState = stateScores.Keys.First();
				if (newState != lastState)
				{
					stateInfo.Exit?.Invoke();
					this[newState].Enter?.Invoke();
				}
				break;
		}
		
		return newState != State.Null;
	}
	
	private static Dictionary<State, int> GetStateScores(StateInfo stateInfo)
	{
		var stateScores = new Dictionary<State, int>();
		foreach (var state in stateInfo.PossibleStates)
		{
			stateScores[state.ToState] = state.Condition();
		}
		return stateScores;
	}

	private static State GetHighestRankedState(Dictionary<State, int> stateScores)
	{
		var maxScore = 0;
		var highestRankedState = State.Null;
		foreach (var stateScore in stateScores)
		{
			if (stateScore.Value > maxScore)
			{
				maxScore = stateScore.Value;
				highestRankedState = stateScore.Key;
			}
		}
		return highestRankedState;
	}

	public void SetToState(State newState)
	{
		var stateInfo = this[newState];
		stateInfo.Enter?.Invoke();
	}
}
