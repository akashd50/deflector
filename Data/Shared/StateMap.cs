using System.Collections.Generic;
using Godot;

namespace Deflector.Data.Shared;

public class StateMap : Dictionary<State, StateInfo>
{
	private ulong _lastTransitionCheck = 0;
	private readonly double _timeBetweenTransitionChecks = 0;
	private bool _currentlyExecuting = false;
	private readonly bool _log = false;
	
	public State State { get; private set; }
	
	public StateMap(double timeBetweenTransitionChecks, bool? log = false) : base()
	{
		_log = log ?? false;
		_timeBetweenTransitionChecks = timeBetweenTransitionChecks;
	}
	
	public State Execute(State currentState)
	{
		if (_currentlyExecuting)
		{
			return currentState;
		}
		
		_currentlyExecuting = true;
		
		var newState = currentState;
		var stateInfo = this[currentState];

		if (stateInfo.ReEval != null && !stateInfo.ReEval())
		{
			stateInfo.Tick?.Invoke();
			_currentlyExecuting = false;
			return newState;
		}

		if (Time.GetTicksMsec() - _lastTransitionCheck > _timeBetweenTransitionChecks)
		{
			if (TryTransitionState(stateInfo, currentState, out var toTransitionTo))
			{
				newState = toTransitionTo;
			}
		}
		
		if (newState == currentState)
		{
			if (this[newState].Tick == null)
			{
				this[newState].Enter?.Invoke(currentState);
			}
			else
			{
				this[newState].Tick?.Invoke();   
			}
		}
		
		if (_log)
		{
			GD.Print("Transition from/to: ", $"{currentState} - {newState}");
		}

		_currentlyExecuting = false;
		return newState;
	}

	private bool TryTransitionState(StateInfo stateInfo, State lastState, out State newState)
	{
		_lastTransitionCheck = Time.GetTicksMsec();
		newState = State.Null;

		var stateScores = GetStateScores(stateInfo);
		if (stateScores.Count == 0)
		{
			return false;
		}

		var bestState = GetHighestRankedState(stateScores);
		if (bestState == State.Null)
		{
			return false;
		}

		newState = bestState;
		if (newState != lastState)
		{
			stateInfo.Exit?.Invoke();
			this[newState].Enter?.Invoke(lastState);
		}
		return true;
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

	// Picks the highest-scoring transition. A score of 0 means "do not consider",
	// so when every candidate scores 0 we return Null to signal "stay put".
	// Strict > favors the first-iterated state on ties, which is stable and
	// lets callers express priority via insertion order.
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

	public void SetToState(State newState, State oldState)
	{
		var stateInfo = this[newState];
		stateInfo.Enter?.Invoke(oldState);
	}
}
