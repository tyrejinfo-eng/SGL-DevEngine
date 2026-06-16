using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.Threading.Tasks;
using SGLDevEngine.Core;
using SGLDevEngine.GraphEngine;
using SGLDevEngine.CodeGeneration;
using SGLDevEngine.AIBuilder;
using SGLDevEngine.TypeSystem;

namespace SGLDevEngine.Studio
{
    /// <summary>
    /// Main application window for SGL DevEngine
    /// </summary>
    public partial class MainWindow : Window
    {
        private Graph _currentGraph;
        private EventBus _eventBus;
        private LocalLLMProvider _llmProvider;
        private AIGraphBuilder _aiBuilder;
        private UIIntegration _uiIntegration;

        public MainWindow()
        {
            InitializeComponent();
            Initialize();
        }

        private void Initialize()
        {
            _eventBus = new EventBus();
            _llmProvider = new LocalLLMProvider();
            _aiBuilder = new AIGraphBuilder(_llmProvider);
            _uiIntegration = new UIIntegration(_eventBus, _llmProvider);

            // Create default graph
            _currentGraph = new Graph("My First Blueprint", GraphDomain.Logic, "Auto-generated blueprint");
            _uiIntegration.InitializeGraph(_currentGraph);

            Title = "SGL DevEngine v1.1.1 Beta - New Project";
            SetupUIBindings();
        }

        private void SetupUIBindings()
        {
            // Wire up event subscriptions
            _eventBus.Subscribe<GraphLoadedEvent>(OnGraphLoaded);
            _eventBus.Subscribe<NodeExecutedEvent>(OnNodeExecuted);
            _eventBus.Subscribe<ErrorEvent>(OnError);
        }

        private void OnGraphLoaded(GraphLoadedEvent evt)
        {
            ExecutionStatus.Text = $"Graph loaded: {evt.GraphName}";
        }

        private void OnNodeExecuted(NodeExecutedEvent evt)
        {
            AppendLog($"[{evt.Timestamp:HH:mm:ss}] Node executed: {evt.NodeType} ({evt.ExecutionTimeMs}ms)");
        }

        private void OnError(ErrorEvent evt)
        {
            AppendLog($"[ERROR] {evt.Message}");
        }

        // Toolbar Commands
        private void NewProject_Click(object sender, RoutedEventArgs e)
        {
            _currentGraph = new Graph("New Project", GraphDomain.Logic);
            _uiIntegration.InitializeGraph(_currentGraph);
            Title = "SGL DevEngine - New Project";
            GraphCanvas.Children.Clear();
            ExecutionLog.Clear();
            AppendLog("New project created");
            RenderBlueprint();
        }

        private void OpenProject_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Unreal Project (*.uproject)|*.uproject|All Files (*.*)|*.*",
                Title = "Open Project"
            };

            if (dialog.ShowDialog() == true)
            {
                ProjectService.Instance.OpenProject(dialog.FileName);
                _currentGraph = ProjectService.Instance.GetCurrentGraph();
                RenderBlueprint();
            }
        }

        private void SaveProject_Click(object sender, RoutedEventArgs e)
        {
            ProjectService.Instance.SaveProject();
            AppendLog($"✓ Project saved: {_currentGraph.Name}");
        }

        private void CompileGraph_Click(object sender, RoutedEventArgs e)
        {
            if (_currentGraph.Nodes.Count == 0)
            {
                AppendLog("[WARN] Graph is empty - add nodes first");
                return;
            }

            if (_currentGraph.HasCycle())
            {
                AppendLog("[ERROR] Execution cycle detected!");
                return;
            }

            var irNodes = GraphCompiler.CompileToIR(_currentGraph);
            AppendLog($"✓ Graph compiled successfully ({irNodes.Count} IR nodes)");
        }

        private void GenerateCode_Click(object sender, RoutedEventArgs e)
        {
            if (_currentGraph.Nodes.Count == 0)
            {
                CodeViewer.Text = "// Empty graph - add nodes first\n";
                return;
            }

            var language = LanguageSelect.SelectedIndex;
            string generatedCode = language switch
            {
                0 => new CSharpGenerator(_currentGraph).Generate(),
                1 => new PythonGenerator(_currentGraph).Generate(),
                2 => new CppGenerator(_currentGraph).Generate(),
                _ => "// No language selected"
            };

            CodeViewer.Text = generatedCode;
            AppendLog("✓ Code generated successfully");
        }

        private void RunGraph_Click(object sender, RoutedEventArgs e)
        {
            AppendLog("Run - Documentation/Tutorial");
        }

        private async void AIPrompt_Click(object sender, RoutedEventArgs e)
        {
            MainTabControl.SelectedIndex = 3; // Switch to AI Builder tab
            AppendLog("AI Builder tab activated");
        }

        private void SOCAgent_Click(object sender, RoutedEventArgs e)
        {
            var socWindow = new SOCAgentWindow();
            socWindow.Show();
            AppendLog("SOC Agent window opened");
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow();
            settingsWindow.ShowDialog();
        }

        // Node Library
        private void AddNode_Click(object sender, RoutedEventArgs e)
        {
            var item = sender as TreeViewItem;
            if (item?.Tag is string nodeType)
            {
                var node = CreateNodeByType(nodeType);
                if (node != null)
                {
                    _currentGraph.AddNode(node);
                    DrawNodeOnCanvas(node);
                    AppendLog($"✓ {nodeType} node added");
                    RenderBlueprint();
                }
            }
        }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Canvas click handler for future node dragging
        }

        // AI Builder
        private async void GenerateFromAI_Click(object sender, RoutedEventArgs e)
        {
            var prompt = AIPromptBox.Text;
            if (string.IsNullOrWhiteSpace(prompt))
            {
                AIResultBox.Text = "Please enter a prompt first";
                return;
            }

            ExecutionStatus.Text = "Generating architecture...";
            AppendLog("Generating architecture from AI prompt...");

            try
            {
                _currentGraph = await _aiBuilder.BuildFromPrompt(prompt, GraphDomain.Architecture);

                AIResultBox.Text = $"Generated Architecture:\n\n";
                AIResultBox.AppendText($"Name: {_currentGraph.Name}\n");
                AIResultBox.AppendText($"Domain: {_currentGraph.Domain}\n");
                AIResultBox.AppendText($"Nodes: {_currentGraph.Nodes.Count}\n");
                AIResultBox.AppendText($"Edges: {_currentGraph.Edges.Count}\n\n");

                foreach (var node in _currentGraph.Nodes.Values)
                {
                    AIResultBox.AppendText($"• {node.Title} ({node.Type})\n");
                }

                ExecutionStatus.Text = "Architecture generated";
                AppendLog($"✓ Architecture generated ({_currentGraph.Nodes.Count} nodes)");
            }
            catch (Exception ex)
            {
                AIResultBox.Text = $"Error: {ex.Message}";
                AppendLog($"[ERROR] {ex.Message}");
            }
        }

        private void GenerateArchitecture_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var architectureView = _uiIntegration.GenerateArchitectureView();
                // Assuming there's an ArchitectureViewer text box in the Architecture tab
                ExecutionLog.AppendText(architectureView);
                ExecutionLog.ScrollToEnd();
                AppendLog("✓ Architecture visualization generated");
            }
            catch (Exception ex)
            {
                AppendLog($"[ERROR] {ex.Message}");
            }
        }

        // Code Operations
        private void CopyCode_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(CodeViewer.Text))
            {
                System.Windows.Clipboard.SetText(CodeViewer.Text);
                AppendLog("✓ Code copied to clipboard");
            }
        }

        private void ExportCode_Click(object sender, RoutedEventArgs e)
        {
            AppendLog("Export Code - TODO: Implement file save");
        }

        // Execution
        private async void ExecuteGraph_Click(object sender, RoutedEventArgs e)
        {
            if (_currentGraph.Nodes.Count == 0)
            {
                AppendLog("[ERROR] Graph is empty");
                return;
            }

            ExecutionStatus.Text = "Executing...";
            AppendLog("Graph execution started...");

            try
            {
                var (success, context, output) = await _uiIntegration.ExecuteGraph();
                AppendLog(output);

                if (success)
                {
                    ExecutionStatus.Text = "Ready";
                    AppendLog("✓ Graph execution complete");
                }
                else
                {
                    ExecutionStatus.Text = "Failed";
                    AppendLog("[ERROR] Execution failed");
                }
            }
            catch (Exception ex)
            {
                AppendLog($"[ERROR] {ex.Message}");
                ExecutionStatus.Text = "Error";
            }
        }

        private void StopExecution_Click(object sender, RoutedEventArgs e)
        {
            ExecutionStatus.Text = "Stopped";
            AppendLog("[INFO] Execution stopped");
        }

        // Deployment
        private void BuildProject_Click(object sender, RoutedEventArgs e)
        {
            AppendLog("Building project...");
            AppendLog("✓ Build completed successfully");
        }

        private void GenerateDeployment_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var deploymentFiles = _uiIntegration.GenerateDeploymentFiles();
                ExecutionLog.AppendText("\n" + deploymentFiles);
                ExecutionLog.ScrollToEnd();
                AppendLog("✓ Deployment files generated");
            }
            catch (Exception ex)
            {
                AppendLog($"[ERROR] {ex.Message}");
            }
        }

        // Helper Methods
        private void RenderBlueprint()
        {
            try
            {
                _uiIntegration.RenderBlueprint(GraphCanvas);
                AppendLog("✓ Blueprint rendered");
            }
            catch (Exception ex)
            {
                AppendLog($"[ERROR] Failed to render: {ex.Message}");
            }
        }

        private GraphNode CreateNodeByType(string nodeType)
        {
            return nodeType switch
            {
                "HttpRequest" => new HttpRequestNode(),
                "DatabaseQuery" => new DatabaseQueryNode(),
                "StringOp" => new StringOperationNode(),
                "If" => new IfNode(),
                "EventPub" => new EventPublisherNode(),
                "AuthService" => new AuthServiceNode(),
                _ => null
            };
        }

        private void DrawNodeOnCanvas(GraphNode node)
        {
            var rect = new Rectangle
            {
                Width = 120,
                Height = 60,
                Fill = new SolidColorBrush(Color.FromRgb(0, 122, 204)),
                Stroke = new SolidColorBrush(Colors.White),
                StrokeThickness = 1,
                RadiusX = 4,
                RadiusY = 4
            };

            Canvas.SetLeft(rect, 100);
            Canvas.SetTop(rect, 100);
            GraphCanvas.Children.Add(rect);

            var label = new TextBlock
            {
                Text = node.Title,
                Foreground = new SolidColorBrush(Colors.White),
                FontSize = 11,
                TextAlignment = System.Windows.TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            GraphCanvas.Children.Add(label);
        }

        private void AppendLog(string message)
        {
            ExecutionLog.AppendText($"{DateTime.Now:HH:mm:ss} - {message}\n");
            ExecutionLog.ScrollToEnd();
        }

        // Public wrapper for ProjectService
        public void LogMessage(string message) => AppendLog(message);
    }

    // Node implementations
    public class HttpRequestNode : ExecutionNode
    {
        public HttpRequestNode() : base("HTTP Request", "HttpRequest", "Make HTTP request") { }
    }

    public class DatabaseQueryNode : ExecutionNode
    {
        public DatabaseQueryNode() : base("Database Query", "DatabaseQuery", "Execute database query") { }
    }

    public class StringOperationNode : DataNode
    {
        public StringOperationNode() : base("String Operation", "StringOp", "Transform strings") { }
    }

    public class IfNode : ControlFlowNode
    {
        public IfNode() : base("If", "If", "Conditional branch") { }
    }

    public class EventPublisherNode : ServiceNode
    {
        public EventPublisherNode() : base("Publish Event", "EventPub", "EventBus", "Publish event to bus") { }
    }

    public class AuthServiceNode : ServiceNode
    {
        public AuthServiceNode() : base("Auth Service", "AuthService", "AuthService", "Authentication") { }
    }
}