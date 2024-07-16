using UnityEngine;
using System.Collections.Generic;
using System;

public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static UnityMainThreadDispatcher instance = null;
    private readonly Queue<Action> queue = new Queue<Action>();

    public static UnityMainThreadDispatcher Instance()
    {
        if (instance == null)
        {
            instance = FindObjectOfType<UnityMainThreadDispatcher>();
            if (instance == null)
            {
                var go = new GameObject("UnityMainThreadDispatcher");
                instance = go.AddComponent<UnityMainThreadDispatcher>();
                DontDestroyOnLoad(go);
            }
        }
        return instance;
    }

    private void Update()
    {
        while (queue.Count > 0)
        {
            queue.Dequeue().Invoke();
        }
    }

    public void Enqueue(Action action)
    {
        queue.Enqueue(action);
    }
}