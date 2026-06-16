using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SGLDevEngine.AIBuilder
{
    /// <summary>
    /// Configuration for LLM provider settings
    /// </summary>
    public class LLMProviderConfig
    {
        [JsonPropertyName("provider")]
        public string Provider { get; set; } = "claude";

        [JsonPropertyName("apiKey")]
        public string ApiKey { get; set; }

        [JsonPropertyName("apiBaseUrl")]
        public string ApiBaseUrl { get; set; }

        [JsonPropertyName("model")]
        public string Model { get; set; } = "claude-3-5-sonnet-20241022";

        [JsonPropertyName("maxTokens")]
        public int MaxTokens { get; set; } = 4096;

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; } = 0.7;

        [JsonPropertyName("localModelPath")]
        public string LocalModelPath { get; set; }

        [JsonPropertyName("localEndpoint")]
        public string LocalEndpoint { get; set; } = "http://localhost:8000";

        [JsonPropertyName("enableStreaming")]
        public bool EnableStreaming { get; set; } = false;

        [JsonPropertyName("timeout")]
        public int TimeoutSeconds { get; set; } = 60;

        public static LLMProviderConfig LoadFromEnvironment()
        {
            return new LLMProviderConfig
            {
                Provider = Environment.GetEnvironmentVariable("LLM_PROVIDER") ?? "claude",
                ApiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY") ??
                         Environment.GetEnvironmentVariable("LLM_API_KEY"),
                ApiBaseUrl = Environment.GetEnvironmentVariable("LLM_API_BASE_URL") ??
                             "https://api.anthropic.com",
                Model = Environment.GetEnvironmentVariable("LLM_MODEL") ?? "claude-3-5-sonnet-20241022",
                MaxTokens = int.TryParse(Environment.GetEnvironmentVariable("LLM_MAX_TOKENS"), out var mt) ? mt : 4096,
                Temperature = double.TryParse(Environment.GetEnvironmentVariable("LLM_TEMPERATURE"), out var t) ? t : 0.7,
                LocalModelPath = Environment.GetEnvironmentVariable("LLM_LOCAL_MODEL_PATH"),
                LocalEndpoint = Environment.GetEnvironmentVariable("LLM_LOCAL_ENDPOINT") ?? "http://localhost:8000",
                EnableStreaming = bool.TryParse(Environment.GetEnvironmentVariable("LLM_ENABLE_STREAMING"), out var s) && s,
                TimeoutSeconds = int.TryParse(Environment.GetEnvironmentVariable("LLM_TIMEOUT"), out var to) ? to : 60
            };
        }

        public static LLMProviderConfig LoadFromJson(string jsonPath)
        {
            try
            {
                var json = File.ReadAllText(jsonPath);
                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                return JsonSerializer.Deserialize<LLMProviderConfig>(json, options) ?? new LLMProviderConfig();
            }
            catch
            {
                return new LLMProviderConfig();
            }
        }
    }

    /// <summary>
    /// Claude API request/response models
    /// </summary>
    internal class ClaudeApiRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; }

        [JsonPropertyName("messages")]
        public List<ClaudeMessage> Messages { get; set; } = new();

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; } = 0.7;

        [JsonPropertyName("system")]
        public string System { get; set; }

        [JsonPropertyName("stream")]
        public bool Stream { get; set; } = false;
    }

    internal class ClaudeMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }
    }

    internal class ClaudeApiResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("content")]
        public List<ClaudeContent> Content { get; set; }

        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("stop_reason")]
        public string StopReason { get; set; }

        [JsonPropertyName("stop_sequence")]
        public string StopSequence { get; set; }

        [JsonPropertyName("usage")]
        public ClaudeUsage Usage { get; set; }
    }

    internal class ClaudeContent
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; }
    }

    internal class ClaudeUsage
    {
        [JsonPropertyName("input_tokens")]
        public int InputTokens { get; set; }

        [JsonPropertyName("output_tokens")]
        public int OutputTokens { get; set; }
    }

    /// <summary>
    /// Production-grade Claude API provider implementation
    /// </summary>
    public class ClaudeProvider : ILLMProvider
    {
        private readonly LLMProviderConfig _config;
        private readonly HttpClient _httpClient;
        private readonly string _apiUrl;

        public ClaudeProvider(LLMProviderConfig config, HttpClient httpClient = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            if (string.IsNullOrWhiteSpace(_config.ApiKey))
            {
                throw new InvalidOperationException(
                    "Claude API key not configured. Set ANTHROPIC_API_KEY environment variable or LLM_API_KEY.");
            }

            _httpClient = httpClient ?? new HttpClient();
            _apiUrl = (_config.ApiBaseUrl ?? "https://api.anthropic.com").TrimEnd('/') + "/v1/messages";

            _httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("x-api-key", _config.ApiKey);
            _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "SGL-DevEngine/1.1.4");
        }

        public async Task<string> GenerateArchitecture(string prompt)
        {
            var systemPrompt = @"You are an expert software architect. Generate a valid JSON architecture specification for the described system.
The JSON must include:
- systemType (microservice, webapp, or monolith)
- services array with name and methods
- eventbus flag
- database object with type (SQL, NoSQL, or Graph)

Return ONLY valid JSON, no markdown, no explanation.";

            return await CallClaudeApi(systemPrompt, prompt);
        }

        public async Task<string> GenerateCode(string specification)
        {
            var systemPrompt = @"You are a professional C# code generator. Generate production-ready C# code based on the architecture specification provided.
Generate clean, well-structured code with proper error handling, logging, and following SOLID principles.
Include XML documentation comments.";

            return await CallClaudeApi(systemPrompt, specification);
        }

        private async Task<string> CallClaudeApi(string systemPrompt, string userMessage)
        {
            try
            {
                var request = new ClaudeApiRequest
                {
                    Model = _config.Model,
                    MaxTokens = _config.MaxTokens,
                    Temperature = _config.Temperature,
                    System = systemPrompt,
                    Stream = false,
                    Messages = new List<ClaudeMessage>
                    {
                        new ClaudeMessage
                        {
                            Role = "user",
                            Content = userMessage
                        }
                    }
                };

                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                var jsonContent = JsonSerializer.Serialize(request, options);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_apiUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException(
                        $"Claude API error ({response.StatusCode}): {errorContent}");
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<ClaudeApiResponse>(responseJson, options);

                if (apiResponse?.Content == null || apiResponse.Content.Count == 0)
                {
                    throw new InvalidOperationException("Unexpected API response format: no content returned.");
                }

                return apiResponse.Content[0].Text;
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException(
                    $"Failed to call Claude API: {ex.Message}", ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new InvalidOperationException(
                    $"Claude API request timed out after {_config.TimeoutSeconds} seconds", ex);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException(
                    $"Failed to parse Claude API response: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Local GGUF model provider using llama.cpp integration
    /// </summary>
    public class LocalGGUFProvider : ILLMProvider
    {
        private readonly LLMProviderConfig _config;
        private readonly HttpClient _httpClient;

        public LocalGGUFProvider(LLMProviderConfig config, HttpClient httpClient = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            if (string.IsNullOrWhiteSpace(_config.LocalEndpoint))
            {
                throw new InvalidOperationException(
                    "Local endpoint not configured. Set LLM_LOCAL_ENDPOINT environment variable.");
            }

            _httpClient = httpClient ?? new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "SGL-DevEngine/1.1.4");
        }

        public async Task<string> GenerateArchitecture(string prompt)
        {
            var systemPrompt = @"You are an expert software architect. Generate a valid JSON architecture specification for the described system.
The JSON must include:
- systemType (microservice, webapp, or monolith)
- services array with name and methods
- eventbus flag
- database object with type (SQL, NoSQL, or Graph)

Return ONLY valid JSON, no markdown, no explanation.";

            return await CallLocalApi(systemPrompt, prompt);
        }

        public async Task<string> GenerateCode(string specification)
        {
            var systemPrompt = @"You are a professional C# code generator. Generate production-ready C# code based on the architecture specification provided.
Generate clean, well-structured code with proper error handling, logging, and following SOLID principles.
Include XML documentation comments.";

            return await CallLocalApi(systemPrompt, specification);
        }

        private async Task<string> CallLocalApi(string systemPrompt, string userMessage)
        {
            try
            {
                var endpoint = (_config.LocalEndpoint ?? "http://localhost:8000").TrimEnd('/') + "/v1/chat/completions";

                var request = new
                {
                    model = _config.Model,
                    messages = new object[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = userMessage }
                    },
                    temperature = _config.Temperature,
                    max_tokens = _config.MaxTokens,
                    stream = false
                };

                var jsonContent = JsonSerializer.Serialize(request);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(endpoint, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException(
                        $"Local LLM API error ({response.StatusCode}): {errorContent}");
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                var responseData = JsonSerializer.Deserialize<JsonElement>(responseJson);

                if (responseData.TryGetProperty("choices", out var choices) &&
                    choices.GetArrayLength() > 0)
                {
                    var firstChoice = choices[0];
                    if (firstChoice.TryGetProperty("message", out var message) &&
                        message.TryGetProperty("content", out var textContent))
                    {
                        return textContent.GetString();
                    }
                }

                throw new InvalidOperationException("Unexpected API response format: no message content found.");
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException(
                    $"Failed to call local LLM API at {_config.LocalEndpoint}: {ex.Message}", ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new InvalidOperationException(
                    $"Local LLM API request timed out after {_config.TimeoutSeconds} seconds", ex);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException(
                    $"Failed to parse local LLM API response: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Factory for creating appropriate LLM provider instances
    /// </summary>
    public class LLMProviderFactory
    {
        public static ILLMProvider CreateProvider(LLMProviderConfig config = null, HttpClient httpClient = null)
        {
            config ??= LLMProviderConfig.LoadFromEnvironment();

            var providerType = (config.Provider ?? "claude").ToLowerInvariant();

            return providerType switch
            {
                "claude" => new ClaudeProvider(config, httpClient),
                "local" or "gguf" or "llama.cpp" => new LocalGGUFProvider(config, httpClient),
                _ => throw new InvalidOperationException(
                    $"Unknown LLM provider: {config.Provider}. Supported providers: claude, local, gguf, llama.cpp")
            };
        }

        public static ILLMProvider CreateProvider(string providerName, LLMProviderConfig config = null, HttpClient httpClient = null)
        {
            config ??= LLMProviderConfig.LoadFromEnvironment();
            config.Provider = providerName;
            return CreateProvider(config, httpClient);
        }

        public static ILLMProvider CreateClaudeProvider(string apiKey = null, string model = null, HttpClient httpClient = null)
        {
            var config = LLMProviderConfig.LoadFromEnvironment();
            config.Provider = "claude";
            if (!string.IsNullOrWhiteSpace(apiKey)) config.ApiKey = apiKey;
            if (!string.IsNullOrWhiteSpace(model)) config.Model = model;
            return new ClaudeProvider(config, httpClient);
        }

        public static ILLMProvider CreateLocalProvider(string endpoint = null, string model = null, HttpClient httpClient = null)
        {
            var config = LLMProviderConfig.LoadFromEnvironment();
            config.Provider = "local";
            if (!string.IsNullOrWhiteSpace(endpoint)) config.LocalEndpoint = endpoint;
            if (!string.IsNullOrWhiteSpace(model)) config.Model = model;
            return new LocalGGUFProvider(config, httpClient);
        }
    }

    /// <summary>
    /// Fallback provider that uses templates when real API is unavailable
    /// </summary>
    public class FallbackLLMProvider : ILLMProvider
    {
        private readonly ILLMProvider _primaryProvider;
        private readonly LocalLLMProvider _fallbackProvider;

        public FallbackLLMProvider(ILLMProvider primaryProvider)
        {
            _primaryProvider = primaryProvider ?? throw new ArgumentNullException(nameof(primaryProvider));
            _fallbackProvider = new LocalLLMProvider();
        }

        public async Task<string> GenerateArchitecture(string prompt)
        {
            try
            {
                return await _primaryProvider.GenerateArchitecture(prompt);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Primary provider failed: {ex.Message}. Using fallback.");
                return await _fallbackProvider.GenerateArchitecture(prompt);
            }
        }

        public async Task<string> GenerateCode(string specification)
        {
            try
            {
                return await _primaryProvider.GenerateCode(specification);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Primary provider failed: {ex.Message}. Using fallback.");
                return await _fallbackProvider.GenerateCode(specification);
            }
        }
    }

    /// <summary>
    /// Composite provider that load-balances or combines multiple providers
    /// </summary>
    public class CompositeProvider : ILLMProvider
    {
        private readonly List<ILLMProvider> _providers;
        private int _currentIndex = 0;

        public CompositeProvider(params ILLMProvider[] providers)
        {
            if (providers == null || providers.Length == 0)
            {
                throw new ArgumentException("At least one provider must be specified.", nameof(providers));
            }
            _providers = new List<ILLMProvider>(providers);
        }

        public async Task<string> GenerateArchitecture(string prompt)
        {
            var provider = GetNextProvider();
            try
            {
                return await provider.GenerateArchitecture(prompt);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Provider {provider.GetType().Name} failed: {ex.Message}");
                if (_providers.Count == 1) throw;

                _providers.RemoveAt(_currentIndex);
                _currentIndex = _currentIndex % _providers.Count;
                return await GenerateArchitecture(prompt);
            }
        }

        public async Task<string> GenerateCode(string specification)
        {
            var provider = GetNextProvider();
            try
            {
                return await provider.GenerateCode(specification);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Provider {provider.GetType().Name} failed: {ex.Message}");
                if (_providers.Count == 1) throw;

                _providers.RemoveAt(_currentIndex);
                _currentIndex = _currentIndex % _providers.Count;
                return await GenerateCode(specification);
            }
        }

        private ILLMProvider GetNextProvider()
        {
            var provider = _providers[_currentIndex];
            _currentIndex = (_currentIndex + 1) % _providers.Count;
            return provider;
        }
    }
}
