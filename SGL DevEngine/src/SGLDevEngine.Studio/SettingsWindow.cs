#nullable enable

using System;
using System.Windows;
using System.Windows.Controls;
using SGLDevEngine.Core;

namespace SGLDevEngine.Studio
{
    /// <summary>
    /// Settings dialog for SGL DevEngine - LLM, GitHub, and application configuration
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private ComboBox? _llmProviderCombo;
        private TextBox? _modelNameBox;
        private PasswordBox? _apiKeyBox;
        private TextBox? _maxTokensBox;
        private Slider? _temperatureSlider;
        private IConfigPersistence? _configPersistence;
        private AppConfig? _currentConfig;

        public SettingsWindow()
        {
            _configPersistence = new JsonConfigPersistence();
            _currentConfig = new AppConfig();
            Title = "SGL DevEngine - Settings";
            Width = 600;
            Height = 700;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30));
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);

            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            // Load config asynchronously
            if (_configPersistence != null)
            {
                _currentConfig = await _configPersistence.LoadAsync() ?? new AppConfig();
            }

            CreateUI();
        }

        private void CreateUI()
        {
#pragma warning disable CS8602,CS8600
            var mainPanel = new Grid();
            mainPanel.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
            mainPanel.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

            // Scrollable content area
            var scrollViewer = new ScrollViewer();
            var contentPanel = new StackPanel() { Orientation = Orientation.Vertical, Margin = new Thickness(15) };

            // ===== AI / LLM Settings Tab =====
            var llmGroup = new GroupBox()
            {
                Header = "LLM & AI Settings",
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White),
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 45)),
                Margin = new Thickness(0, 0, 0, 15),
                Padding = new Thickness(15)
            };

            var llmPanel = new StackPanel() { Orientation = Orientation.Vertical, Margin = new Thickness(0, 5, 0, 5) };

            // Provider selection
            llmPanel.Children.Add(CreateLabel("LLM Provider:"));
            _llmProviderCombo = new ComboBox()
            {
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(60, 60, 60)),
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White),
                Height = 30,
                Padding = new Thickness(8),
                Margin = new Thickness(0, 0, 0, 10)
            };
            _llmProviderCombo.Items.Add("Local");
            _llmProviderCombo.Items.Add("Claude");
            _llmProviderCombo.Items.Add("OpenAI");
            _llmProviderCombo.Items.Add("LocalGGUF");
            _llmProviderCombo.SelectedItem = _currentConfig!.AiProvider.Provider;
            llmPanel.Children.Add(_llmProviderCombo);

            // Model name
            llmPanel.Children.Add(CreateLabel("Model Name:"));
            _modelNameBox = new TextBox()
            {
                Text = _currentConfig!.AiProvider.Model ?? "claude-3-5-sonnet-20241022",
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(60, 60, 60)),
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White),
                Padding = new Thickness(8),
                Height = 30,
                Margin = new Thickness(0, 0, 0, 10)
            };
            llmPanel.Children.Add(_modelNameBox);

            // API Key / Token
            llmPanel.Children.Add(CreateLabel("API Key (encrypted):"));
            _apiKeyBox = new PasswordBox()
            {
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(60, 60, 60)),
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White),
                Padding = new Thickness(8),
                Height = 30,
                Margin = new Thickness(0, 0, 0, 10)
            };
            if (!string.IsNullOrEmpty(_currentConfig?.AiProvider.ApiKey))
            {
                _apiKeyBox!.Password = _currentConfig!.AiProvider.ApiKey;
            }
            llmPanel.Children.Add(_apiKeyBox);

            // Max Tokens
            llmPanel.Children.Add(CreateLabel($"Max Tokens: ({_currentConfig!.AiProvider.MaxTokens})"));
            _maxTokensBox = new TextBox()
            {
                Text = _currentConfig!.AiProvider.MaxTokens.ToString(),
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(60, 60, 60)),
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White),
                Padding = new Thickness(8),
                Height = 30,
                Margin = new Thickness(0, 0, 0, 10)
            };
            llmPanel.Children.Add(_maxTokensBox);

            // Temperature Slider
            llmPanel.Children.Add(CreateLabel($"Temperature: {_currentConfig!.AiProvider.Temperature:F2}"));
            _temperatureSlider = new Slider()
            {
                Minimum = 0,
                Maximum = 2,
                Value = _currentConfig!.AiProvider.Temperature,
                TickFrequency = 0.1,
                IsSnapToTickEnabled = true,
                Height = 35,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White),
                Margin = new Thickness(0, 0, 0, 10)
            };
            _temperatureSlider.ValueChanged += (s, e) =>
            {
                var label = (TextBlock)llmPanel.Children[llmPanel.Children.IndexOf(_temperatureSlider) - 1];
                label.Text = $"Temperature: {_temperatureSlider.Value:F2}";
            };
            llmPanel.Children.Add(_temperatureSlider);

            llmGroup.Content = llmPanel;
            contentPanel.Children.Add(llmGroup);

            // ===== API Endpoint / Connection =====
            var connectionGroup = new GroupBox()
            {
                Header = "API Connection",
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White),
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 45)),
                Margin = new Thickness(0, 0, 0, 15),
                Padding = new Thickness(15)
            };

            var connPanel = new StackPanel() { Orientation = Orientation.Vertical, Margin = new Thickness(0, 5, 0, 5) };
            connPanel.Children.Add(new TextBlock()
            {
                Text = $"API Base URL: {_currentConfig!.AiProvider.ApiBaseUrl}",
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White),
                Margin = new Thickness(0, 0, 0, 8)
            });
            connPanel.Children.Add(new TextBlock()
            {
                Text = $"Local Endpoint: {_currentConfig!.AiProvider.LocalEndpoint}",
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White),
                Margin = new Thickness(0, 0, 0, 8)
            });

            connectionGroup.Content = connPanel;
            contentPanel.Children.Add(connectionGroup);

            // ===== GitHub Integration =====
            var githubGroup = new GroupBox()
            {
                Header = "GitHub Integration",
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White),
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 45)),
                Margin = new Thickness(0, 0, 0, 15),
                Padding = new Thickness(15)
            };

            var githubPanel = new StackPanel() { Orientation = Orientation.Vertical, Margin = new Thickness(0, 5, 0, 5) };
            var statusText = new TextBlock()
            {
                Text = _currentConfig!.GitHub.Connected ? $"✓ Connected as {_currentConfig!.GitHub.Username}" : "Not connected",
                Foreground = new System.Windows.Media.SolidColorBrush(_currentConfig!.GitHub.Connected ? System.Windows.Media.Colors.LimeGreen : System.Windows.Media.Colors.Orange),
                FontWeight = System.Windows.FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10),
                FontSize = 12
            };
            githubPanel.Children.Add(statusText);

            var loginButton = new Button()
            {
                Content = "GitHub OAuth Login",
                Width = 150,
                Height = 35,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 122, 204)),
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White),
                Margin = new Thickness(0, 0, 0, 10)
            };
            loginButton.Click += (s, e) =>
            {
                MessageBox.Show("GitHub OAuth integration coming in Phase 7", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            };
            githubPanel.Children.Add(loginButton);

            githubGroup.Content = githubPanel;
            contentPanel.Children.Add(githubGroup);

            scrollViewer.Content = contentPanel;
            scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            Grid.SetRow(scrollViewer, 0);
            mainPanel.Children.Add(scrollViewer);

            // ===== Buttons =====
            var buttonPanel = new StackPanel()
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(15),
                Height = 40
            };

            var saveButton = new Button()
            {
                Content = "Save",
                Width = 120,
                Height = 40,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 180, 0)),
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White),
                FontWeight = System.Windows.FontWeights.Bold,
                Margin = new Thickness(5, 0, 5, 0)
            };
            saveButton.Click += SaveSettings_Click;
            buttonPanel.Children.Add(saveButton);

            var cancelButton = new Button()
            {
                Content = "Cancel",
                Width = 100,
                Height = 40,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 100, 100)),
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White),
                Margin = new Thickness(5, 0, 5, 0)
            };
            cancelButton.Click += (s, e) => this.DialogResult = false;
            buttonPanel.Children.Add(cancelButton);

            Grid.SetRow(buttonPanel, 1);
            mainPanel.Children.Add(buttonPanel);

            this.Content = mainPanel;
#pragma warning restore CS8602,CS8600
        }

        private async void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentConfig == null || _configPersistence == null)
                {
                    MessageBox.Show("Settings not initialized", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Update configuration from UI
#pragma warning disable CS8600
                _currentConfig.AiProvider.Provider = (string)_llmProviderCombo?.SelectedItem ?? "Local";
#pragma warning restore CS8600
                _currentConfig.AiProvider.Model = _modelNameBox?.Text ?? "claude-3-5-sonnet-20241022";
                _currentConfig.AiProvider.ApiKey = _apiKeyBox?.Password;
                _currentConfig.AiProvider.MaxTokens = int.Parse(_maxTokensBox?.Text ?? "4096");
                _currentConfig.AiProvider.Temperature = _temperatureSlider?.Value ?? 0.7;

                // Save to persistent storage
                await _configPersistence.SaveAsync(_currentConfig);

                // Reinitialize RealLLMProvider with new configuration
                // TODO: Wire RealLLMProvider.InitializeFromConfig(_currentConfig);

                MessageBox.Show("Settings saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true;
            }
            catch (FormatException)
            {
                MessageBox.Show("Invalid value in Max Tokens field. Please enter a valid number.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private TextBlock CreateLabel(string text)
        {
            return new TextBlock()
            {
                Text = text,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White),
                FontWeight = System.Windows.FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5)
            };
        }
    }
}
