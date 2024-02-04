using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Collections.Concurrent;

public class ExecuteOnMainThread : MonoBehaviour
{
    public static readonly ConcurrentQueue<Action> RunOnMainThread = new ConcurrentQueue<Action>();

    private void Update()
    {
        if (!RunOnMainThread.IsEmpty)
        {
            while (RunOnMainThread.TryDequeue(out var action))
            {
                action?.Invoke();
            }
        }
    }
}