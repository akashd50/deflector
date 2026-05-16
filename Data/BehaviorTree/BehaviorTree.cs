using System;
using System.Collections.Generic;

namespace Deflector.Data.BehaviorTree;

public enum NodeState { Success, Failure, Running }

public abstract class BTNode
{
    public abstract NodeState Evaluate();
}

// Selector: Succeeds if ANY child succeeds (OR)
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

// Sequence: Succeeds only if ALL children succeed (AND)
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

// Leaf Node: Executes actual code via delegates
public class ActionNode(Func<NodeState> action) : BTNode
{
    public override NodeState Evaluate() => action();
}