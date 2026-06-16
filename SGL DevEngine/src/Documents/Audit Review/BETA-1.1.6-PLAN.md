# SGL DevEngine Beta 1.1.6 - IMPLEMENTATION PLAN OF ACTION

**Scope**: Full UI + Backend Integration | **Timeline**: This Session | **Target**: 0E/0W Build + Comprehensive Audit

## PHASE 1: ADD NUGET PACKAGES & FIX BUILD (30 mins)

### Step 1.1: Update SGLDevEngine.BlueprintRuntime.csproj
Add real database drivers:
```xml
<ItemGroup>
  <PackageReference Include="System.Data.SQLite" Version="1.0.118.0" />
  <PackageReference Include="Npgsql" Version="8.0.0" />
  <PackageReference Include="MySqlConnector" Version="2.3.0" />
</ItemGroup>
```

### Step 1.2: Verify Build
```bash
cd "SGL DevEngine/SourceCode"
dotnet build -c Debug
# Expected: 0 Errors, 0 Warnings
```

---

## PHASE 2: CREATE PROJECTSERVICE (1.5 hours)

### Step 2.1: Create ProjectService.cs
```csharp
// SGLDevEngine.Studio\ProjectService.cs
public class ProjectService
{
    private string _currentProjectPath;
    private Graph _currentGraph;

    public void OpenProject(string path)
    {
        if (File.Exists(path) && Path.GetExtension(path) == ".uproject")
        {
            _currentProjectPath = Path.GetDirectoryName(path);
        }
        else if (Directory.Exists(path))
        {
            _currentProjectPath = path;
        }
        
        LoadProjectFiles(_currentProjectPath);
    }

    private void LoadProjectFiles(string path)
    {
        // Load Graph.json if exists
        var graphPath = Path.Combine(path, "Graph.json");
        if (File.Exists(graphPath))
        {
            var json = File.ReadAllText(graphPath);
            _currentGraph = JsonSerializer.Deserialize<Graph>(json);
        }

        // Populate TreeView with folders/files
        var tree = new TreeViewItem { Header = Path.GetFileName(path) };
        foreach (var folder in Directory.GetDirectories(path))
        {
            tree.Items.Add(new TreeViewItem { Header = Path.GetFileName(folder) });
        }
        foreach (var file in Directory.GetFiles(path))
        {
            tree.Items.Add(new TreeViewItem { Header = Path.GetFileName(file) });
        }
        
        // Update UI (delegate to MainWindow)
        Application.Current.Dispatcher.Invoke(() =>
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.ArchitectureTree.Items.Clear();
            mainWindow.ArchitectureTree.Items.Add(tree);
            mainWindow.LoadGraph(_currentGraph);
        });
    }

    public void SaveProject()
    {
        var graphPath = Path.Combine(_currentProjectPath, "Graph.json");
        var graphJson = JsonSerializer.Serialize(_currentGraph, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(graphPath, graphJson);
    }
}
```

### Step 2.2: Wire MainWindow Event Handlers
```csharp
private void OpenProject_Click(object sender, RoutedEventArgs e)
{
    var dialog = new OpenFileDialog 
    { 
        Filter = "Unreal Project (*.uproject)|*.uproject|Folders (*.*)|*.*" 
    };
    if (dialog.ShowDialog() == true)
    {
        ProjectService.Instance.OpenProject(dialog.FileName);
    }
}

private void SaveProject_Click(object sender, RoutedEventArgs e)
{
    ProjectService.Instance.SaveProject();
}
```

---

## PHASE 3: WIRE LLM SETTINGS TO PERSISTENCE (1 hour)

### Step 3.1: Update SettingsWindow.xaml.cs
```csharp
private async void SaveSettings_Click(object sender, RoutedEventArgs e)
{
    var config = new AppConfig
    {
        AiProvider = new AiProviderConfig
        {
            Provider = (string)LLMProvider Combo.SelectedItem,
            Model = ModelNameBox.Text,
            ApiKey = ApiKeyBox.Password,
            MaxTokens = int.Parse(MaxTokensBox.Text),
            Temperature = (double)TemperatureSlider.Value
        }
    };
    
    await ConfigPersistence.SaveAsync(config);
    RealLLMProvider.InitializeFromConfig(config);
    MessageBox.Show("Settings saved!");
}
```

### Step 3.2: Load Settings on App Startup (App.xaml.cs)
```csharp
protected override void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);
    
    _ = Task.Run(async () =>
    {
        var config = await ConfigPersistence.LoadAsync();
        RealLLMProvider.InitializeFromConfig(config);
    });
}
```

---

## PHASE 4: REAL DATABASEQUERYEXECUTOR (2 hours)

### Step 4.1: Update DatabaseQueryExecutor.cs
```csharp
// Replace SimulateInference() calls with real SQL
public async Task<ExecutionResult>Execute(GraphNode node, Graph graph, RuntimeContext context)
{
    var result = new ExecutionResult { Success = true };
    
    try
    {
        var query = node.GetProperty<string>("query");
        var dbType = node.GetProperty<string>("dbType"); // sqlite, postgresql, mysql, mssql
        var connectionString = GetConnectionString(); // from ConfigPersistence

        DbConnection connection = dbType switch
        {
            "postgresql" => new NpgsqlConnection(connectionString),
            "mysql" => new MySqlConnection(connectionString),
            "mssql" => new SqlConnection(connectionString),
            _ => new SqliteConnection(connectionString)
        };

        using (connection)
        {
            connection.Open();
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = query;
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    var data = new List<Dictionary<string, object>>();
                    while (await reader.ReadAsync())
                    {
                        var row = new Dictionary<string, object>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            row[reader.GetName(i)] = reader.GetValue(i);
                        }
                        data.Add(row);
                    }
                    
                    result.OutputValues["data"] = data;
                    result.OutputValues["rowCount"] = data.Count;
                }
            }
        }
    }
    catch (Exception ex)
    {
        result.Success = false;
        result.ErrorMessage = ex.Message;
    }
    
    return result;
}

private string GetConnectionString()
{
    // Load from ConfigPersistence
    var config = ConfigPersistence.LoadAsync().Result;
    return config.DatabaseConnectionString;
}
```

---

## PHASE 5: REAL AIINFERENCEEXECUTOR (30 mins)

### Step 5.1: Update AIInferenceExecutor.cs
```csharp
public async Task<ExecutionResult> Execute(GraphNode node, Graph graph, RuntimeContext context)
{
    var result = new ExecutionResult { Success = true };
    
    try
    {
        var prompt = node.GetProperty<string>("prompt");
        var systemPrompt = node.GetProperty<string>("systemPrompt");
        var maxTokens = int.Parse(node.GetProperty<string>("maxTokens") ?? "1024");
        
        // Use REAL provider
        var provider = RealLLMProvider.GetCurrent();
        var response = await provider.GenerateAsync(prompt, systemPrompt, maxTokens);
        
        result.OutputValues["response"] = response;
        result.OutputValues["tokensUsed"] = EstimateTokens(response);
    }
    catch (Exception ex)
    {
        result.Success = false;
        result.ErrorMessage = ex.Message;
    }
    
    return result;
}

private int EstimateTokens(string text) => (int)Math.Ceiling(text.Length / 4.0);
```

---

## PHASE 6: WIRE ALL TAB EVENT HANDLERS (2 hours)

### Step 6.1: Complete MainWindow.xaml.cs Handlers
```csharp
private async void ExecuteGraph_Click(object sender, RoutedEventArgs e)
{
    var context = new RuntimeContext();
    var (success, runtimeContext, error) = await UIIntegration.ExecuteGraph();
    ExecutionLog.Text = success 
        ? $"SUCCESS - {runtimeContext.ExecutionTimeMs}ms\n" + DisplayResults(runtimeContext)
        : $"ERROR: {error}";
}

private void GenerateCode_Click(object sender, RoutedEventArgs e)
{
    var generator = new CSharpGenerator();
    var code = generator.Generate(UIIntegration.CurrentGraph);
    CodeViewer.Text = code;
}

private async void GenerateFromAI_Click(object sender, RoutedEventArgs e)
{
    var prompt = AIPromptBox.Text;
    var graph = await AIGraphBuilder.BuildFromPrompt(prompt, GraphDomain.Architecture);
    BlueprintCanvas.LoadGraph(graph);
}

private async void GitHubLogin_Click(object sender, RoutedEventArgs e)
{
    var token = await GitHubService.Instance.GitHubLoginAsync();
    MessageBox.Show($"GitHub connected as {GitHubService.Instance.CurrentUser}");
}
```

---

## PHASE 7: CREATE GITHUBSERVICE (2 hours)

### Step 7.1: Create GitHubService.cs
```csharp
// SGLDevEngine.Core\GitHubService.cs
public class GitHubService
{
    private readonly string _clientId = "YOUR_GITHUB_CLIENT_ID";
    private readonly string _clientSecret = "YOUR_GITHUB_CLIENT_SECRET";
    public string CurrentUser { get; private set; }
    private string _accessToken;

    public async Task<string> GitHubLoginAsync()
    {
        var redirectUri = "http://localhost:51042/oauth/callback";
        var state = Guid.NewGuid().ToString("N");
        
        var authUrl = $"https://github.com/login/oauth/authorize?client_id={_clientId}&redirect_uri={Uri.EscapeDataString(redirectUri)}&scope=repo&state={state}";
        
        Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });
        
        // Set up local HTTP listener for callback...
        var code = await WaitForOAuthCodeAsync();
        
        using (var client = new HttpClient())
        {
            var response = await client.PostAsync(
                "https://github.com/login/oauth/access_token",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "client_id", _clientId },
                    { "client_secret", _clientSecret },
                    { "code", code }
                }));
            
            var content = await response.Content.ReadAsStringAsync();
            var json = JsonSerializer.Parse(content);
            _accessToken = json.GetProperty("access_token").GetString();
            
            return _accessToken;
        }
    }

    public async Task<List<string>> ListReposAsync()
    {
        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");
            var response = await client.GetStringAsync("https://api.github.com/user/repos");
            // Parse and return repo names
            return new List<string>();
        }
    }

    public async Task CommitFileAsync(string repo, string filePath, string content, string message)
    {
        // GitHub API: PUT /repos/{owner}/{repo}/contents/{path}
        // Creates or updates a file and commits it
    }
}
```

---

## PHASE 8: BUILD VERIFICATION & TESTING (1 hour)

### Step 8.1: Verify Clean Build
```bash
dotnet build -c Debug
# Expected: Build succeeded. 0 Error(s), 0 Warning(s)

dotnet build -c Release
# Expected: Build succeeded. 0 Error(s), 0 Warning(s)
```

### Step 8.2: Test Each Tab
- Blueprint Editor: Create nodes → Connect → Execute ✓
- Architecture: Show tree view ✓
- Code: Generate C# code ✓
- Settings: Select provider → Save → Restart → Verify persists ✓

---

## PHASE 9: CREATE COMPREHENSIVE AUDIT DOCUMENTS (2 hours)

### Step 9.1: Generate Master Audit Document
- Real vs. fake analysis (12 critical issues identified)
- Vulnerabilities (injection, rate limiting)
- Remediation steps for each
- System flow diagrams
- Architecture diagrams

### Step 9.2: Create Additional Audits
- API Audit (executors, handlers)
- Services Audit (ProjectService, GitHubService, DatabaseService)
- LLM Audit (provider capabilities, speed)
- UI Audit (6 tabs wired)

---

## PHASE 10: CREATE BETA 1.1.6 PACKAGE (1.5 hours)

### Step 10.1: Copy Release Build
```bash
cp -r "SourceCode/SGLDevEngine.Studio/bin/Release/net8.0-windows/"* \
  "Beta.1.1.6/SGL DevEngine Windows Quick Deploy Version/"
```

### Step 10.2: Create Folder Structure
```
Beta.1.1.6/
├── Audit Review/ (all audit documents)
├── Documents/ (Business Plan, Roadmap, Policies)
├── SGL DevEngine Windows Install Version/ (Inno Setup .exe)
└── SGL DevEngine Windows Quick Deploy Version/ (executables)
```

### Step 10.3: Create Inno Setup Install Script
- Reference existing SGLDevEngine-Setup-1.1.4.iss
- Update version to 1.1.6
- Add new executors and services to file list

---

## IMPLEMENTATION ORDER (EXECUTE IN THIS SEQUENCE)

1. ✅ Add NuGet packages + verify build (30 mins)
2. ✅ ProjectService + MainWindow wiring (1.5 hrs)
3. ✅ Settings persistence (1 hr)
4. ✅ Real DatabaseQueryExecutor (2 hrs)
5. ✅ Real AIInferenceExecutor (30 mins)
6. ✅ Wire all tab handlers (2 hrs)
7. ✅ GitHubService structure (2 hrs)
8. ✅ Build verification (1 hr)
9. ✅ Audit documents (2 hrs)
10. ✅ Package Beta 1.1.6 (1.5 hrs)

---

**Total Time**: ~15 hours | **Effort**: INTENSIVE | **Impact**: CRITICAL

**Next Action**: Execute Phase 1-3 immediately
