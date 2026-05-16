using System;
using System.Collections.Generic;

namespace Deflector.Data.BehaviorTree;

public enum NodeState { Success, Failure, Running }

public abstract class BTNode
{
    public abstract NodeState Evaluate();
}

// Selector: succeeds when the first child succeeds (OR). Returns Running if a
// child is still running. Falls through to Failure if every child fails.
public class Selector(List<BTNode> children) : BTNode
{
    public override NodeState Evaluate()
    {
        foreach (var child in children)
        {
            switch (child.Evaluate())
            {
                case NodeState.Success: return NodeState.Success;
                case NodeState.Running: return NodeState.Running;
                case NodeState.Failure: continue;
            }
        }
        return NodeState.Failure;
    }
}

// Sequence: succeeds only when every child succeeds (AND). Stops on first
// Failure/Running and propagates that result.
public class Sequence(List<BTNode> children) : BTNode
{
    public override NodeState Evaluate()
    {
        foreach (var child in children)
        {
            switch (child.Evaluate())
            {
                case NodeState.Success: continue;
                case NodeState.Running: return NodeState.Running;
                case NodeState.Failure: return NodeState.Failure;
            }
        }
        return NodeState.Success;
    }
}

// Action leaf: evaluates a delegate that already returns a NodeState (use for
// behaviors that can be Running across ticks).
public class ActionNode(Func<NodeState> action) : BTNode
{
    public override NodeState Evaluate() => action();
}

// Condition leaf: evaluates a bool predicate as Success/Failure. Use for pure
// checks (no side effects, never Running).
public class Condition(Func<bool> predicate) : BTNode
{
    public override NodeState Evaluate() => predicate() ? NodeState.Success : NodeState.Failure;
}

// Inverter: flips Success <-> Failure, leaves Running unchanged.
public class Inverter(BTNode child) : BTNode
{
    public override NodeState Evaluate() => child.Evaluate() switch
    {
        NodeState.Success => NodeState.Failure,
        NodeState.Failure => NodeState.Success,
        _                 => NodeState.Running,
    };
}
