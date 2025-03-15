using System.Collections.Generic;
using Godot;

namespace Deflector.Data.Shared;

public class ActionQueue<T>
{
    private readonly Queue<AnimationAction<T>> _queue = new Queue<AnimationAction<T>>();
    
    public ActionQueue()
    {
        
    }

    public void Push(T item)
    {
        _queue.Enqueue(new AnimationAction<T>()
        {
            Data = item,
            QueuedTime = Time.GetTicksMsec(),
        });
    }

    public AnimationAction<T> Pop()
    { 
        return _queue.Dequeue();
    }

    public void Clear()
    {
        _queue.Clear();
    }

    public bool Any()
    {
        return _queue.Count > 0;
    }

}