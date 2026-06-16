#nullable enable
using System;
using System.Collections.Generic;

namespace SGLDevEngine.TypeSystem
{
    /// <summary>
    /// Defines the fundamental port types used throughout the graph system
    /// </summary>
    public enum PortTypeKind
    {
        Integer,
        Float,
        String,
        Boolean,
        Object,
        JSON,
        Event,
        Stream,
        AIEmbedding,
        Binary,
        DateTime,
        Array,
        Dictionary
    }

    /// <summary>
    /// Represents a port type definition with validation
    /// </summary>
    public class PortType
    {
        public string Name { get; set; }
        public PortTypeKind Kind { get; set; }
        public Type DotNetType { get; set; }
        public string Description { get; set; }

        public PortType(string name, PortTypeKind kind, Type dotNetType, string description = "")
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Kind = kind;
            DotNetType = dotNetType ?? throw new ArgumentNullException(nameof(dotNetType));
            Description = description;
        }

        /// <summary>
        /// Determines if this type can accept data from another type
        /// </summary>
        public bool IsAssignableFrom(PortType other)
        {
            if (other == null) return false;

            // Exact match
            if (this.Kind == other.Kind) return true;

            // Allow implicit conversions
            if (this.Kind == PortTypeKind.Object) return true; // Object accepts anything
            if (other.Kind == PortTypeKind.JSON && this.Kind == PortTypeKind.String) return true;

            return false;
        }

        public override string ToString() => $"{Name} ({Kind})";
        public override bool Equals(object? obj) => obj is PortType pt && pt.Kind == this.Kind;
        public override int GetHashCode() => Kind.GetHashCode();
    }

    /// <summary>
    /// Registry of all available port types
    /// </summary>
    public static class PortTypeRegistry
    {
        private static readonly Dictionary<PortTypeKind, PortType> _registry = new();

        static PortTypeRegistry()
        {
            Register(new PortType("Integer", PortTypeKind.Integer, typeof(int), "32-bit integer value"));
            Register(new PortType("Float", PortTypeKind.Float, typeof(float), "32-bit floating point value"));
            Register(new PortType("String", PortTypeKind.String, typeof(string), "Text string"));
            Register(new PortType("Boolean", PortTypeKind.Boolean, typeof(bool), "True/False value"));
            Register(new PortType("Object", PortTypeKind.Object, typeof(object), "Generic object"));
            Register(new PortType("JSON", PortTypeKind.JSON, typeof(string), "JSON formatted data"));
            Register(new PortType("Event", PortTypeKind.Event, typeof(EventData), "Event with data payload"));
            Register(new PortType("Stream", PortTypeKind.Stream, typeof(IAsyncEnumerable<object>), "Data stream"));
            Register(new PortType("Embedding", PortTypeKind.AIEmbedding, typeof(float[]), "AI vector embedding"));
            Register(new PortType("Binary", PortTypeKind.Binary, typeof(byte[]), "Binary data"));
            Register(new PortType("DateTime", PortTypeKind.DateTime, typeof(DateTime), "Date and time"));
        }

        public static void Register(PortType portType)
        {
            _registry[portType.Kind] = portType;
        }

        public static PortType? Get(PortTypeKind kind)
        {
            return _registry.TryGetValue(kind, out var type) ? type : null;
        }

        public static IEnumerable<PortType> GetAll() => _registry.Values;
    }

    /// <summary>
    /// Represents event data passed through event ports
    /// </summary>
    public class EventData
    {
        public string EventName { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object?> Payload { get; set; }

        public EventData(string name, Dictionary<string, object?>? payload = null)
        {
            EventName = name;
            Timestamp = DateTime.UtcNow;
            Payload = payload ?? new Dictionary<string, object?>();
        }
    }

    /// <summary>
    /// Port definition with direction and type
    /// </summary>
    public class PortDefinition
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public PortDirection Direction { get; set; }
        public PortType Type { get; set; } = null!;
        public bool IsMultiConnection { get; set; } = false;
        public object? DefaultValue { get; set; }

        public PortDefinition(string name, PortDirection direction, PortType type,
            bool multiConnection = false, object? defaultValue = null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Direction = direction;
            Type = type ?? throw new ArgumentNullException(nameof(type));
            IsMultiConnection = multiConnection;
            DefaultValue = defaultValue;
        }

        public override string ToString() => $"{Name} ({Direction} {Type.Name})";
    }

    public enum PortDirection
    {
        Input,
        Output
    }
}
