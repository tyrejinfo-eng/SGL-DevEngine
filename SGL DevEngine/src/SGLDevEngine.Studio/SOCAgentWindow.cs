using System.Windows;
using System.Collections.Generic;

namespace SGLDevEngine.Studio
{
    /// <summary>
    /// SOC (Security Operations Center) Agent Window
    /// Floating window for security monitoring and autonomous agent control
    /// </summary>
    public partial class SOCAgentWindow : Window
    {
        public SOCAgentWindow()
        {
            // No InitializeComponent - building UI programmatically
            Initialize();
        }

        private void Initialize()
        {
            Title = "SGL DevEngine - SOC Agent";
            Width = 400;
            Height = 500;
            WindowStyle = WindowStyle.SingleBorderWindow;
            ResizeMode = ResizeMode.CanResize;
            // TopMost = true; // Disabled for deployment - can be enabled in settings
            Background = System.Windows.Media.Brushes.White;
            Icon = null;

            CreateUI();
        }

        private void CreateUI()
        {
            var grid = new System.Windows.Controls.Grid();
            grid.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30));

            var panel = new System.Windows.Controls.StackPanel() { Orientation = System.Windows.Controls.Orientation.Vertical, Margin = new System.Windows.Thickness(10) };

            // Header
            var header = new System.Windows.Controls.TextBlock()
            {
                Text = "SOC Autonomous Agent",
                FontSize = 16,
                FontWeight = System.Windows.FontWeights.Bold,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White),
                Margin = new System.Windows.Thickness(0, 0, 0, 15)
            };
            panel.Children.Add(header);

            // Status
            var statusLabel = new System.Windows.Controls.TextBlock()
            {
                Text = "Status: ACTIVE",
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 255, 0)),
                Margin = new System.Windows.Thickness(0, 0, 0, 10),
                FontWeight = System.Windows.FontWeights.Bold
            };
            panel.Children.Add(statusLabel);

            // Threat Level
            var threatLevel = new System.Windows.Controls.TextBlock()
            {
                Text = "Threat Level: LOW",
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Yellow),
                Margin = new System.Windows.Thickness(0, 0, 0, 10)
            };
            panel.Children.Add(threatLevel);

            // Agent Control
            var controlGroup = new System.Windows.Controls.GroupBox()
            {
                Header = "Agent Control",
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White),
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 45)),
                Margin = new System.Windows.Thickness(0, 0, 0, 10),
                Padding = new System.Windows.Thickness(10)
            };

            var controlPanel = new System.Windows.Controls.StackPanel() { Orientation = System.Windows.Controls.Orientation.Vertical };

            var enableBtn = new System.Windows.Controls.Button()
            {
                Content = "Enable Autonomous Mode",
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 122, 204)),
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White),
                Padding = new System.Windows.Thickness(8),
                Margin = new System.Windows.Thickness(0, 0, 0, 5)
            };
            controlPanel.Children.Add(enableBtn);

            var disableBtn = new System.Windows.Controls.Button()
            {
                Content = "Disable Autonomous Mode",
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(170, 0, 0)),
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White),
                Padding = new System.Windows.Thickness(8)
            };
            controlPanel.Children.Add(disableBtn);

            controlGroup.Content = controlPanel;
            panel.Children.Add(controlGroup);

            // Activity Log
            var logGroup = new System.Windows.Controls.GroupBox()
            {
                Header = "Activity Log",
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White),
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 45)),
                Padding = new System.Windows.Thickness(10),
                Height = 250
            };

            var logBox = new System.Windows.Controls.TextBox()
            {
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30)),
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 255, 0)),
                IsReadOnly = true,
                TextWrapping = System.Windows.TextWrapping.Wrap,
                FontFamily = new System.Windows.Media.FontFamily("Courier New"),
                FontSize = 10,
                VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
                Padding = new System.Windows.Thickness(5),
                Text = "[SOC-AGENT] Initialized\n[SOC-AGENT] Monitoring active\n[SOC-AGENT] Ready for commands\n"
            };
            logGroup.Content = logBox;
            panel.Children.Add(logGroup);

            // Settings
            var settingsGroup = new System.Windows.Controls.GroupBox()
            {
                Header = "Agent Settings",
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White),
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 45)),
                Padding = new System.Windows.Thickness(10)
            };

            var settingsPanel = new System.Windows.Controls.StackPanel() { Orientation = System.Windows.Controls.Orientation.Vertical };

            var agentModeCheckBox = new System.Windows.Controls.CheckBox()
            {
                Content = "Autonomous Mode",
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White),
                IsChecked = false,
                Margin = new System.Windows.Thickness(0, 0, 0, 5)
            };
            settingsPanel.Children.Add(agentModeCheckBox);

            var aiAssistanceCheckBox = new System.Windows.Controls.CheckBox()
            {
                Content = "AI Assistance Enabled",
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White),
                IsChecked = true
            };
            settingsPanel.Children.Add(aiAssistanceCheckBox);

            settingsGroup.Content = settingsPanel;
            panel.Children.Add(settingsGroup);

            grid.Children.Add(panel);
            this.Content = grid;
        }
    }
}
