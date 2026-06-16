using System.Configuration;
using System.Data;
using System.Windows;
using System.Threading.Tasks;
using SGLDevEngine.Core;

namespace SGLDevEngine.Studio;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Load persisted settings and initialize LLM provider
        try
        {
            var configPersistence = new JsonConfigPersistence();
            var config = await configPersistence.LoadAsync();

            // NOTE: RealLLMProvider initialization will be wired in Phase 5
            // RealLLMProvider.InitializeFromConfig(config);

            System.Diagnostics.Debug.WriteLine($"✓ Config loaded on startup: {config.AiProvider.Provider}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Warning: Failed to load config on startup: {ex.Message}");
        }
    }
}

