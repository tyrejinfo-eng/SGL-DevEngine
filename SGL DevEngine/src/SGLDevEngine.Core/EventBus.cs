using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SGLDevEngine.Core
{
    /// <summary>
    /// Central event bus for module communication
    /// </summary>
    public class EventBus
    {
        private readonly Dictionary<Type, List<Delegate>> _handlers = new();
        private readonly object _lockObj = new object();

        /// <summary>
        /// Subscribe to event
        /// </summary>
        public void Subscribe<T>(Action<T> handler) where T : class
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            lock (_lockObj)
            {
                var eventType = typeof(T);
                if (!_handlers.ContainsKey(eventType))
                {
                    _handlers[eventType] = new List<Delegate>();
                }
                _handlers[eventType].Add(handler);
            }
        }

        /// <summary>
        /// Subscribe to async event
        /// </summary>
        public void Subscribe<T>(Func<T, Task> handler) where T : class
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            lock (_lockObj)
            {
                var eventType = typeof(T);
                if (!_handlers.ContainsKey(eventType))
                {
                    _handlers[eventType] = new List<Delegate>();
                }
                _handlers[eventType].Add(handler);
            }
        }

        /// <summary>
        /// Publish event
        /// </summary>
        public void Publish<T>(T evt) where T : class
        {
            if (evt == null) throw new ArgumentNullException(nameof(evt));

            List<Delegate> handlersToCall;
            lock (_lockObj)
            {
                if (!_handlers.TryGetValue(typeof(T), out var handlers))
                    return;

                handlersToCall = new List<Delegate>(handlers);
            }

            foreach (var handler in handlersToCall)
            {
                try
                {
                    if (handler is Action<T> syncHandler)
                    {
                        syncHandler(evt);
                    }
                    else if (handler is Func<T, Task> asyncHandler)
                    {
                        asyncHandler(evt).GetAwaiter().GetResult();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"EventBus error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Publish event asynchronously
        /// </summary>
        public async Task PublishAsync<T>(T evt) where T : class
        {
            if (evt == null) throw new ArgumentNullException(nameof(evt));

            List<Delegate> handlersToCall;
            lock (_lockObj)
            {
                if (!_handlers.TryGetValue(typeof(T), out var handlers))
                    return;

                handlersToCall = new List<Delegate>(handlers);
            }

            foreach (var handler in handlersToCall)
            {
                try
                {
                    if (handler is Action<T> syncHandler)
                    {
                        syncHandler(evt);
                    }
                    else if (handler is Func<T, Task> asyncHandler)
                    {
                        await asyncHandler(evt);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"EventBus error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Clear all subscriptions
        /// </summary>
        public void Clear()
        {
            lock (_lockObj)
            {
                _handlers.Clear();
            }
        }
    }

    /// <summary>
    /// Base event for system events
    /// </summary>
    public class SystemEvent
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Source { get; set; }
    }

    /// <summary>
    /// Event fired when graph is loaded
    /// </summary>
    public class GraphLoadedEvent : SystemEvent
    {
        public string GraphId { get; set; }
        public string GraphName { get; set; }
    }

    /// <summary>
    /// Event fired when node executes
    /// </summary>
    public class NodeExecutedEvent : SystemEvent
    {
        public string NodeId { get; set; }
        public string NodeType { get; set; }
        public long ExecutionTimeMs { get; set; }
    }

    /// <summary>
    /// Event fired on error
    /// </summary>
    public class ErrorEvent : SystemEvent
    {
        public string Message { get; set; }
        public string StackTrace { get; set; }
        public string Component { get; set; }
    }
}
