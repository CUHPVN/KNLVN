using System;
using UnityEngine;
using System.Collections.Generic;

    public class EventBus : IDisposable
    {
        private readonly Dictionary<Type, List<Delegate>> _subscribers = new();

        private readonly object _lock = new object();

        public EventBus()
        {
            KNLVN.GameDebug.Log("EventBus initialized.");
        }

        public void Subscribe<T>(Action<T> handler)
        {
            lock (_lock)
            {
                var type = typeof(T);
                if (!_subscribers.ContainsKey(type))
                {
                    _subscribers[type] = new List<Delegate>();
                }

                _subscribers[type].Add(handler);
                KNLVN.GameDebug.Log($"[EventBus] Subscribed: {handler.Method.Name} to {type.Name}");
            }
        }

        public void Unsubscribe<T>(Action<T> handler)
        {
            lock (_lock)
            {
                var type = typeof(T);
                if (_subscribers.TryGetValue(type, out var handlers))
                {
                    if (handlers.Remove(handler))
                    {
                        KNLVN.GameDebug.Log($"[EventBus] Unsubscribed: {handler.Method.Name} from {type.Name}");
                    }
                }
            }
        }

        public void Publish<T>(T eventData)
        {
            List<Delegate> handlersCopy;
            var type = typeof(T);

            lock (_lock)
            {
                if (!_subscribers.TryGetValue(type, out var handlers) || handlers.Count == 0)
                {
                    return;
                }

                handlersCopy = new List<Delegate>(handlers);
            }

            KNLVN.GameDebug.Log($"[EventBus] Publishing: {type.Name}");

            foreach (var handler in handlersCopy)
            {
                try
                {
                    (handler as Action<T>)?.Invoke(eventData);
                }
                catch (Exception ex)
                {
                    KNLVN.GameDebug.LogError($"[EventBus] Error invoking {handler.Method.Name}: {ex.Message}");
                }
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _subscribers.Clear();
                KNLVN.GameDebug.Log("[EventBus] Disposed and cleared all subscribers.");
            }
        }
    }
