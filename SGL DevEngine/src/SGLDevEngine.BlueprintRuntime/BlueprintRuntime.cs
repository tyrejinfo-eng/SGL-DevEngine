using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using SGLDevEngine.GraphEngine;
using SGLDevEngine.TypeSystem;

namespace SGLDevEngine.BlueprintRuntime
{
    /// <summary>
    /// Runtime context passed to executing nodes
    /// </summary>
    public class RuntimeContext
    {
        public Dictionary<string, object> Variables { get; set; } = new();
        public Dictionary<string, object> PortValues { get; set; } = new();
        public int ExecutionDepth { get; set; } = 0;
        public const int MaxExecutionDepth = 100;
        public DateTime StartTime { get; set; }
        public long ExecutionTimeMs { get; set; }
    }

    /// <summary>
    /// Result of node execution
    /// </summary>
    public class ExecutionResult
    {
        public bool Success { get; set; }
        public Dictionary<string, object> OutputValues { get; set; } = new();
        public string ErrorMessage { get; set; }
        public long ExecutionTimeMs { get; set; }
    }

    /// <summary>
    /// Blueprint virtual machine - executes compiled graphs
    /// </summary>
    public class BlueprintRuntime
    {
        private readonly Graph _graph;
        private readonly Dictionary<string, INodeExecutor> _executors = new();

        public BlueprintRuntime(Graph graph)
        {
            _graph = graph ?? throw new ArgumentNullException(nameof(graph));
        }

        /// <summary>
        /// Register executor for node type
        /// </summary>
        public void RegisterExecutor(string nodeType, INodeExecutor executor)
        {
            _executors[nodeType] = executor;
        }

        /// <summary>
        /// Execute graph
        /// </summary>
        public async Task<(bool Success, RuntimeContext Context, string Error)> Execute(
            RuntimeContext initialContext = null)
        {
            var context = initialContext ?? new RuntimeContext();
            context.StartTime = DateTime.UtcNow;

            try
            {
                // Validate graph
                if (_graph.HasCycle())
                    return (false, context, "Execution cycle detected in graph");

                // Get topological order
                var orderedNodes = _graph.GetTopologicalOrder();

                // Execute nodes in order
                foreach (var node in orderedNodes)
                {
                    context.ExecutionDepth++;

                    if (context.ExecutionDepth > RuntimeContext.MaxExecutionDepth)
                        return (false, context, "Max execution depth exceeded");

                    if (!_executors.TryGetValue(node.Type, out var executor))
                    {
                        System.Diagnostics.Debug.WriteLine($"No executor for node type: {node.Type}");
                        continue;
                    }

                    var result = await executor.Execute(node, _graph, context);

                    if (!result.Success && !ContinueOnError)
                    {
                        return (false, context, result.ErrorMessage);
                    }

                    // Store output values
                    foreach (var kvp in result.OutputValues)
                    {
                        context.PortValues[$"{node.Id}:{kvp.Key}"] = kvp.Value;
                    }

                    context.ExecutionDepth--;
                }

                context.ExecutionTimeMs = (long)(DateTime.UtcNow - context.StartTime).TotalMilliseconds;
                return (true, context, null);
            }
            catch (Exception ex)
            {
                return (false, context, $"Runtime error: {ex.Message}");
            }
        }

        public bool ContinueOnError { get; set; } = false;
    }

    /// <summary>
    /// Interface for node executors
    /// </summary>
    public interface INodeExecutor
    {
        Task<ExecutionResult> Execute(GraphNode node, Graph graph, RuntimeContext context);
    }

    /// <summary>
    /// Default HTTP Request node executor
    /// </summary>
    public class HttpRequestExecutor : INodeExecutor
    {
        public async Task<ExecutionResult> Execute(GraphNode node, Graph graph, RuntimeContext context)
        {
            var result = new ExecutionResult { Success = true };
            var startTime = DateTime.UtcNow;

            try
            {
                var url = node.Properties.TryGetValue("url", out var u) ? u?.ToString() : "";
                var method = node.Properties.TryGetValue("method", out var m) ? m?.ToString() ?? "GET" : "GET";

                using (var client = new System.Net.Http.HttpClient())
                {
                    System.Net.Http.HttpResponseMessage response = null;

                    if (method.ToUpper() == "GET")
                        response = await client.GetAsync(url);
                    else if (method.ToUpper() == "POST")
                        response = await client.PostAsync(url, null);

                    var content = await response.Content.ReadAsStringAsync();

                    result.OutputValues["status"] = (int)response.StatusCode;
                    result.OutputValues["body"] = content;
                    result.OutputValues["headers"] = response.Headers.ToString();
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            result.ExecutionTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
            return result;
        }
    }

    /// <summary>
    /// Default Data Transformation executor
    /// </summary>
    public class DataTransformExecutor : INodeExecutor
    {
        public async Task<ExecutionResult> Execute(GraphNode node, Graph graph, RuntimeContext context)
        {
            var result = new ExecutionResult { Success = true };
            var startTime = DateTime.UtcNow;

            try
            {
                // Get input values
                var inputs = new Dictionary<string, object>();
                foreach (var port in node.InputPorts)
                {
                    var portKey = $"{node.Id}:{port.Id}";
                    if (context.PortValues.TryGetValue(portKey, out var value))
                        inputs[port.Name] = value;
                    else if (port.DefaultValue != null)
                        inputs[port.Name] = port.DefaultValue;
                }

                var operation = node.Properties.TryGetValue("operation", out var op) ? op?.ToString() : "pass";

                object output = null;
                switch (operation)
                {
                    case "uppercase":
                        output = inputs.TryGetValue("input", out var val) ?
                            val?.ToString()?.ToUpper() : "";
                        break;
                    case "lowercase":
                        output = inputs.TryGetValue("input", out var val2) ?
                            val2?.ToString()?.ToLower() : "";
                        break;
                    case "concat":
                        var a = inputs.TryGetValue("a", out var av) ? av?.ToString() : "";
                        var b = inputs.TryGetValue("b", out var bv) ? bv?.ToString() : "";
                        output = $"{a}{b}";
                        break;
                    default:
                        output = inputs.TryGetValue("input", out var pval) ? pval : null;
                        break;
                }

                result.OutputValues["output"] = output;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            result.ExecutionTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
            await Task.CompletedTask;
            return result;
        }
    }

    // NOTE: Real executor implementations moved to separate files:
    // - DatabaseQueryExecutor.cs (Real SQL support for SQLite, SQL Server, MySQL, PostgreSQL)
    // - EventPublisherExecutor.cs (Real event delivery with EventBus)
    // - ControlFlowExecutor.cs (Real If/Loop/Switch/ForEach/While logic)
    // - AIInferenceExecutor.cs (Real LLM integration)
}
