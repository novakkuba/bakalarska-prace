using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// Zprostředkovává bezpečný most mezi asynchronními vlákny (např. MQTT) a hlavním vláknem Unity (Main Thread).
/// Implementuje vzor Singleton a thread-safe frontu úkolů, které jsou následně vykonávány v rámci herní smyčky Update, což umožňuje bezpečnou manipulaci s Unity objekty.
/// </summary>

public class MainThreadDispatcher : MonoBehaviour
{
    private static readonly Queue<Action> _executionQueue = new Queue<Action>();
    public static MainThreadDispatcher Instance { get; private set; }

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(this.gameObject); }
        else { Destroy(this.gameObject); }
    }

    void Update()
    {
        lock (_executionQueue)
        {
            while (_executionQueue.Count > 0)
            {
                _executionQueue.Dequeue().Invoke();
            }
        }
    }

    public void Enqueue(Action action)
    {
        lock (_executionQueue) { _executionQueue.Enqueue(action); }
    }
}