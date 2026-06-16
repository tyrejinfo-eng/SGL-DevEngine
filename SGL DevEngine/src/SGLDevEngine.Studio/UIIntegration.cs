using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using SGLDevEngine.GraphEngine;
using SGLDevEngine.TypeSystem;
using SGLDevEngine.BlueprintRuntime;
using SGLDevEngine.CodeGeneration;
using SGLDevEngine.Core;
using SGLDevEngine.AIBuilder;

namespace SGLDevEngine.Studio
{
    /// <summary>
    /// Coordinates UI tabs with runtime pipelines
    /// </summary>
    public class UIIntegration
    {
        private Graph _graph;
        private BlueprintRuntime.BlueprintRuntime _runtime;
        private EventBus _eventBus;
        private LocalLLMProvider _llmProvider;

        public UIIntegration(EventBus eventBus, LocalLLMProvider llmProvider)
        {
            _eventBus = eventBus;
            _llmProvider = llmProvider;
        }

        /// <summary>
        /// Initialize graph and runtime
        /// </summary>
        public void InitializeGraph(Graph graph)
        {
            _graph = graph;
            _runtime = new BlueprintRuntime.BlueprintRuntime(graph);
            RegisterExecutors();
        }

        /// <summary>
        /// Register all node executors
        /// </summary>
        private void RegisterExecutors()
        {
            _runtime.RegisterExecutor("HttpRequest", new HttpRequestExecutor());
            _runtime.RegisterExecutor("DataTransform", new DataTransformExecutor());
            _runtime.RegisterExecutor("DatabaseQuery", new DatabaseQueryExecutor());
            _runtime.RegisterExecutor("EventPublish", new EventPublisherExecutor(_eventBus));
            _runtime.RegisterExecutor("ControlFlow", new ControlFlowExecutor());
            _runtime.RegisterExecutor("AIInference", new AIInferenceExecutor());
        }

        /// <summary>
        /// Blueprint Editor - render graph visualization
        /// </summary>
        public void RenderBlueprint(Canvas canvas)
        {
            if (_graph == null || _graph.Nodes.Count == 0)
                return;

            canvas.Children.Clear();

            double x = 50, y = 50;
            var nodePositions = new Dictionary<string, (double, double)>();

            // Draw nodes
            foreach (var node in _graph.Nodes.Values)
            {
                var rect = new System.Windows.Shapes.Rectangle
                {
                    Width = 140,
                    Height = 70,
                    Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 120, 215)),
                    Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White),
                    StrokeThickness = 2,
                    RadiusX = 5,
                    RadiusY = 5
                };

                Canvas.SetLeft(rect, x);
                Canvas.SetTop(rect, y);
                canvas.Children.Add(rect);

                var label = new TextBlock
                {
                    Text = node.Title,
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White),
                    FontSize = 12,
                    FontWeight = System.Windows.FontWeights.Bold,
                    TextAlignment = System.Windows.TextAlignment.Center,
                    VerticalAlignment = System.Windows.VerticalAlignment.Center,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    Margin = new System.Windows.Thickness(5)
                };

                Canvas.SetLeft(label, x);
                Canvas.SetTop(label, y + 20);
                canvas.Children.Add(label);

                nodePositions[node.Id] = (x, y);
                x += 200;
                if (x > 800) { x = 50; y += 120; }
            }

            // Draw edges
            foreach (var edge in _graph.Edges)
            {
                if (nodePositions.TryGetValue(edge.SourceNodeId, out var src) &&
                    nodePositions.TryGetValue(edge.TargetNodeId, out var tgt))
                {
                    var line = new System.Windows.Shapes.Line
                    {
                        X1 = src.Item1 + 140,
                        Y1 = src.Item2 + 35,
                        X2 = tgt.Item1,
                        Y2 = tgt.Item2 + 35,
                        Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.LimeGreen),
                        StrokeThickness = 2
                    };

                    canvas.Children.Add(line);
                }
            }
        }

        /// <summary>
        /// Architecture visualization
        /// </summary>
        public string GenerateArchitectureView()
        {
            if (_graph == null) return "No graph loaded";

            var view = new System.Text.StringBuilder();
            view.AppendLine($"Architecture: {_graph.Name}");
            view.AppendLine($"Domain: {_graph.Domain}");
            view.AppendLine($"Nodes: {_graph.Nodes.Count}");
            view.AppendLine($"Edges: {_graph.Edges.Count}");
            view.AppendLine();
            view.AppendLine("Node Structure:");

            foreach (var node in _graph.Nodes.Values)
            {
                view.AppendLine($"  • {node.Title} ({node.Type})");
                view.AppendLine($"    Inputs: {node.InputPorts.Count}");
                view.AppendLine($"    Outputs: {node.OutputPorts.Count}");
            }

            return view.ToString();
        }

        /// <summary>
        /// Execute graph and return results
        /// </summary>
        public async Task<(bool Success, RuntimeContext Context, string Output)> ExecuteGraph()
        {
            if (_graph == null || _graph.Nodes.Count == 0)
                return (false, null, "No graph to execute");

            var context = new RuntimeContext();
            var (success, runtimeContext, error) = await _runtime.Execute(context);

            var output = new System.Text.StringBuilder();
            output.AppendLine($"Execution Result: {(success ? "SUCCESS" : "FAILED")}");
            output.AppendLine($"Execution Time: {runtimeContext.ExecutionTimeMs}ms");

            if (!success)
                output.AppendLine($"Error: {error}");

            return (success, runtimeContext, output.ToString());
        }

        /// <summary>
        /// Generate deployment files
        /// </summary>
        public string GenerateDeploymentFiles()
        {
            if (_graph == null) return "No graph to deploy";

            var output = new System.Text.StringBuilder();
            output.AppendLine("=== DEPLOYMENT CONFIGURATION ===");
            output.AppendLine();

            // Dockerfile
            output.AppendLine("# Dockerfile");
            output.AppendLine("FROM mcr.microsoft.com/dotnet/runtime:8.0");
            output.AppendLine("WORKDIR /app");
            output.AppendLine("COPY . .");
            output.AppendLine("ENTRYPOINT [\"dotnet\", \"app.dll\"]");
            output.AppendLine();

            // docker-compose.yml
            output.AppendLine("# docker-compose.yml");
            output.AppendLine("version: '3.8'");
            output.AppendLine("services:");
            output.AppendLine("  app:");
            output.AppendLine("    build: .");
            output.AppendLine("    ports:");
            output.AppendLine("      - '8080:8080'");
            output.AppendLine();

            // Kubernetes
            output.AppendLine("# kubernetes.yaml");
            output.AppendLine("apiVersion: apps/v1");
            output.AppendLine("kind: Deployment");
            output.AppendLine("metadata:");
            output.AppendLine("  name: sgl-devengine");
            output.AppendLine("spec:");
            output.AppendLine("  replicas: 3");
            output.AppendLine("  selector:");
            output.AppendLine("    matchLabels:");
            output.AppendLine("      app: sgl-devengine");
            output.AppendLine("  template:");
            output.AppendLine("    metadata:");
            output.AppendLine("      labels:");
            output.AppendLine("        app: sgl-devengine");
            output.AppendLine("    spec:");
            output.AppendLine("      containers:");
            output.AppendLine("      - name: app");
            output.AppendLine("        image: sgl-devengine:latest");

            return output.ToString();
        }

        /// <summary>
        /// AI Builder - generate architecture from prompt
        /// </summary>
        public async Task<Graph> GenerateFromAIPrompt(string prompt)
        {
            var aiBuilder = new AIGraphBuilder(_llmProvider);
            var newGraph = await aiBuilder.BuildFromPrompt(prompt, GraphDomain.Architecture);
            _graph = newGraph;
            InitializeGraph(newGraph);
            return newGraph;
        }
    }
}
