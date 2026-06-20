using System;
using System.Collections.Generic;
using SGLDevEngine.TypeSystem;

namespace SGLDevEngine.GraphEngine
{
    /// <summary>
    /// Supported graph domains
    /// </summary>
    public enum GraphDomain
    {
        Architecture,
        Logic,
        AI,
        Infrastructure,
        DataPipeline,
        Service
    }

    /// <summary>
    /// Base class for all graph nodes
    /// </summary>
    public abstract class GraphNode
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public Vector2 Position { get; set; }
        public List<PortDefinition> InputPorts { get; set; } = new();
        public List<PortDefinition> OutputPorts { get; set; } = new();
        public Dictionary<string, object> Properties { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        protected GraphNode(string title, string type, string description = "")
        {
            Title = title;
            Type = type;
            Description = description;
        }

        /// <summary>
        /// Define input port
        /// </summary>
        protected void DefineInput(string name, PortType type, object defaultValue = null)
        {
            InputPorts.Add(new PortDefinition(name, PortDirection.Input, type, false, defaultValue));
        }

        /// <summary>
        /// Define output port
        /// </summary>
        protected void DefineOutput(string name, PortType type)
        {
            OutputPorts.Add(new PortDefinition(name, PortDirection.Output, type));
        }

        public override string ToString() => $"{Title} ({Type})";
    }

    /// <summary>
    /// Execution node performs operations
    /// </summary>
    public abstract class ExecutionNode : GraphNode
    {
        protected ExecutionNode(string title, string type, string description = "")
            : base(title, type, description)
        {
        }
    }

    /// <summary>
    /// Data node handles transformations
    /// </summary>
    public abstract class DataNode : GraphNode
    {
        protected DataNode(string title, string type, string description = "")
            : base(title, type, description)
        {
        }
    }

    /// <summary>
    /// Control flow node
    /// </summary>
    public abstract class ControlFlowNode : GraphNode
    {
        protected ControlFlowNode(string title, string type, string description = "")
            : base(title, type, description)
        {
        }
    }

    /// <summary>
    /// Event node triggers graph execution
    /// </summary>
    public abstract class EventNode : GraphNode
    {
        public string EventName { get; set; }

        protected EventNode(string title, string type, string eventName, string description = "")
            : base(title, type, description)
        {
            EventName = eventName;
        }
    }

    /// <summary>
    /// Service node for microservices
    /// </summary>
    public abstract class ServiceNode : GraphNode
    {
        public string ServiceName { get; set; }
        public string ServiceVersion { get; set; } = "1.0";

        protected ServiceNode(string title, string type, string serviceName, string description = "")
            : base(title, type, description)
        {
            ServiceName = serviceName;
        }
    }

    /// <summary>
    /// Connection between nodes
    /// </summary>
    public class GraphEdge
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string SourceNodeId { get; set; }
        public string SourcePortId { get; set; }
        public string TargetNodeId { get; set; }
        public string TargetPortId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public override string ToString()
            => $"{SourceNodeId}:{SourcePortId} -> {TargetNodeId}:{TargetPortId}";
    }

    /// <summary>
    /// Main graph container
    /// </summary>
    public class Graph
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public string Description { get; set; }
        public GraphDomain Domain { get; set; }
        public Dictionary<string, GraphNode> Nodes { get; set; } = new();
        public List<GraphEdge> Edges { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string Version { get; set; } = "1.0";

        public Graph(string name, GraphDomain domain, string description = "")
        {
            Name = name;
            Domain = domain;
            Description = description;
        }

        /// <summary>
        /// Add node to graph
        /// </summary>
        public void AddNode(GraphNode node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (Nodes.ContainsKey(node.Id)) throw new InvalidOperationException("Node already exists");

            Nodes[node.Id] = node;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Remove node from graph
        /// </summary>
        public void RemoveNode(string nodeId)
        {
            if (!Nodes.Remove(nodeId)) return;

            // Remove connected edges
            Edges.RemoveAll(e => e.SourceNodeId == nodeId || e.TargetNodeId == nodeId);
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Connect two nodes
        /// </summary>
        public GraphEdge ConnectNodes(string sourceNodeId, string sourcePortId,
            string targetNodeId, string targetPortId)
        {
            if (!Nodes.ContainsKey(sourceNodeId) || !Nodes.ContainsKey(targetNodeId))
                throw new ArgumentException("Node not found");

            var sourceNode = Nodes[sourceNodeId];
            var targetNode = Nodes[targetNodeId];

            var sourcePort = sourceNode.OutputPorts.Find(p => p.Id == sourcePortId);
            var targetPort = targetNode.InputPorts.Find(p => p.Id == targetPortId);

            if (sourcePort == null || targetPort == null)
                throw new ArgumentException("Port not found");

            if (!targetPort.Type.IsAssignableFrom(sourcePort.Type))
                throw new InvalidOperationException(
                    $"Cannot connect {sourcePort.Type.Name} to {targetPort.Type.Name}");

            var edge = new GraphEdge
            {
                SourceNodeId = sourceNodeId,
                SourcePortId = sourcePortId,
                TargetNodeId = targetNodeId,
                TargetPortId = targetPortId
            };

            Edges.Add(edge);
            UpdatedAt = DateTime.UtcNow;
            return edge;
        }

        /// <summary>
        /// Get all nodes in topological order
        /// </summary>
        public List<GraphNode> GetTopologicalOrder()
        {
            var visited = new HashSet<string>();
            var order = new List<GraphNode>();

            foreach (var nodeId in Nodes.Keys)
            {
                if (!visited.Contains(nodeId))
                {
                    DepthFirstSearch(nodeId, visited, order);
                }
            }

            return order;
        }

        private void DepthFirstSearch(string nodeId, HashSet<string> visited, List<GraphNode> order)
        {
            visited.Add(nodeId);

            var edges = Edges.FindAll(e => e.SourceNodeId == nodeId);
            foreach (var edge in edges)
            {
                if (!visited.Contains(edge.TargetNodeId))
                {
                    DepthFirstSearch(edge.TargetNodeId, visited, order);
                }
            }

            if (Nodes.TryGetValue(nodeId, out var node))
            {
                order.Add(node);
            }
        }

        /// <summary>
        /// Detect cycles in graph
        /// </summary>
        public bool HasCycle()
        {
            var visited = new HashSet<string>();
            var recursionStack = new HashSet<string>();

            foreach (var nodeId in Nodes.Keys)
            {
                if (!visited.Contains(nodeId))
                {
                    if (HasCycleDFS(nodeId, visited, recursionStack))
                        return true;
                }
            }

            return false;
        }

        private bool HasCycleDFS(string nodeId, HashSet<string> visited, HashSet<string> recursionStack)
        {
            visited.Add(nodeId);
            recursionStack.Add(nodeId);

            var edges = Edges.FindAll(e => e.SourceNodeId == nodeId);
            foreach (var edge in edges)
            {
                if (!visited.Contains(edge.TargetNodeId))
                {
                    if (HasCycleDFS(edge.TargetNodeId, visited, recursionStack))
                        return true;
                }
                else if (recursionStack.Contains(edge.TargetNodeId))
                {
                    return true;
                }
            }

            recursionStack.Remove(nodeId);
            return false;
        }

        public override string ToString() => $"Graph: {Name} ({Domain}) - {Nodes.Count} nodes";
    }

    /// <summary>
    /// 2D position helper
    /// </summary>
    public struct Vector2
    {
        public float X { get; set; }
        public float Y { get; set; }

        public Vector2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public override string ToString() => $"({X}, {Y})";
    }
}

\\Hope You Found this Helpfull Ty.Rejinfo
