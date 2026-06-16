#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SGLDevEngine.Core
{
    /// <summary>
    /// Interface for LLM providers
    /// </summary>
    public interface ILLMProvider
    {
        string Name { get; }
        Task<string> GenerateAsync(string prompt, string? systemPrompt = null, int? maxTokens = null);
        bool IsConfigured { get; }
    }

    /// <summary>
    /// OpenAI API provider (GPT-4, GPT-4 Turbo, GPT-4o, etc.)
    /// </summary>
    public class OpenAIProvider : ILLMProvider
    {
        private readonly string? _apiKey;
        private readonly string _model;
        private readonly int _maxTokens;
        private readonly double _temperature;
        private readonly HttpClient _httpClient;

        public string Name => "OpenAI";
        public bool IsConfigured => !string.IsNullOrEmpty(_apiKey);

        public OpenAIProvider(string? apiKey = null, string model = "gpt-4o-mini", int maxTokens = 4096, double temperature = 0.7)
        {
            _apiKey = apiKey ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            _model = model;
            _maxTokens = maxTokens;
            _temperature = temperature;
            _httpClient = new HttpClient();
        }

        public async Task<string> GenerateAsync(string prompt, string? systemPrompt = null, int? maxTokens = null)
        {
            if (!IsConfigured)
                throw new InvalidOperationException("OpenAI API key not configured");

            try
            {
                var messages = new List<object>();

                if (!string.IsNullOrEmpty(systemPrompt))
                {
                    messages.Add(new { role = "system", content = systemPrompt });
                }

                messages.Add(new { role = "user", content = prompt });

                var request = new
                {
                    model = _model,
                    messages = messages,
                    max_tokens = maxTokens ?? _maxTokens,
                    temperature = _temperature,
                    top_p = 1.0
                };

                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
                httpRequest.Headers.Add("Authorization", $"Bearer {_apiKey}");

                var json = JsonSerializer.Serialize(request);
                httpRequest.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                using var response = await _httpClient.SendAsync(httpRequest);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new InvalidOperationException($"OpenAI API error ({response.StatusCode}): {errorContent}");
                }

                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);

                var choicesElement = doc.RootElement.GetProperty("choices");
                if (choicesElement.GetArrayLength() == 0)
                    throw new InvalidOperationException("No response from OpenAI API");

                var message = choicesElement[0].GetProperty("message").GetProperty("content").GetString();
                return message ?? "";
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"OpenAI API call failed: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Anthropic Claude provider (via API)
    /// </summary>
    public class ClaudeProvider : ILLMProvider
    {
        private readonly string? _apiKey;
        private readonly string _model;
        private readonly int _maxTokens;
        private readonly double _temperature;
        private readonly HttpClient _httpClient;

        public string Name => "Claude";
        public bool IsConfigured => !string.IsNullOrEmpty(_apiKey);

        public ClaudeProvider(string? apiKey = null, string model = "claude-3-5-sonnet-20241022", int maxTokens = 4096, double temperature = 0.7)
        {
            _apiKey = apiKey ?? Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
            _model = model;
            _maxTokens = maxTokens;
            _temperature = temperature;
            _httpClient = new HttpClient();
        }

        public async Task<string> GenerateAsync(string prompt, string? systemPrompt = null, int? maxTokens = null)
        {
            if (!IsConfigured)
                throw new InvalidOperationException("Anthropic API key not configured");

            try
            {
                var request = new
                {
                    model = _model,
                    max_tokens = maxTokens ?? _maxTokens,
                    temperature = _temperature,
                    system = systemPrompt ?? "You are a helpful AI assistant specializing in software architecture and code generation.",
                    messages = new[] {
                        new { role = "user", content = prompt }
                    }
                };

                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
                httpRequest.Headers.Add("x-api-key", _apiKey);
                httpRequest.Headers.Add("anthropic-version", "2023-06-01");

                var json = JsonSerializer.Serialize(request);
                httpRequest.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                using var response = await _httpClient.SendAsync(httpRequest);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new InvalidOperationException($"Claude API error ({response.StatusCode}): {errorContent}");
                }

                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);

                var content_block = doc.RootElement.GetProperty("content")[0];
                var text = content_block.GetProperty("text").GetString();
                return text ?? "";
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Claude API call failed: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Local GGUF model provider (via llama.cpp HTTP server)
    /// </summary>
    public class LocalGGUFProvider : ILLMProvider
    {
        private readonly string _endpoint;
        private readonly int _maxTokens;
        private readonly double _temperature;
        private readonly HttpClient _httpClient;

        public string Name => "LocalGGUF";
        public bool IsConfigured => !string.IsNullOrEmpty(_endpoint);

        public LocalGGUFProvider(string? endpoint = null, int maxTokens = 2048, double temperature = 0.7)
        {
            _endpoint = endpoint ?? Environment.GetEnvironmentVariable("LLM_LOCAL_ENDPOINT") ?? "http://localhost:8000";
            _maxTokens = maxTokens;
            _temperature = temperature;
            _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        }

        public async Task<string> GenerateAsync(string prompt, string? systemPrompt = null, int? maxTokens = null)
        {
            try
            {
                var request = new
                {
                    prompt = prompt,
                    n_predict = maxTokens ?? _maxTokens,
                    temperature = _temperature,
                    top_k = 40,
                    top_p = 0.9,
                    stop = new[] { "\n\n" }
                };

                var uri = _endpoint.TrimEnd('/') + "/completion";
                var json = JsonSerializer.Serialize(request);

                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, uri);
                httpRequest.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                using var response = await _httpClient.SendAsync(httpRequest);

                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException($"Local model error ({response.StatusCode})");
                }

                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);

                var text = doc.RootElement.GetProperty("content").GetString();
                return text ?? "";
            }
            catch (HttpRequestException)
            {
                throw new InvalidOperationException($"Cannot connect to local LLM at {_endpoint}. Ensure llama.cpp server is running.");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Local model call failed: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Provider registry - manages available LLM providers
    /// </summary>
    public class ProviderRegistry
    {
        private readonly Dictionary<string, ILLMProvider> _providers = new(StringComparer.OrdinalIgnoreCase);
        private string _defaultProvider = "Local";

        public ProviderRegistry()
        {
            // Register built-in providers
            Register("Claude", () => new ClaudeProvider());
            Register("OpenAI", () => new OpenAIProvider());
            Register("LocalGGUF", () => new LocalGGUFProvider());
            Register("Local", () => new LocalGGUFProvider());
        }

        /// <summary>
        /// Register a provider factory
        /// </summary>
        public void Register(string name, Func<ILLMProvider> factory)
        {
            _providers[name] = factory();
        }

        /// <summary>
        /// Register a provider instance
        /// </summary>
        public void RegisterInstance(string name, ILLMProvider provider)
        {
            _providers[name] = provider;
        }

        /// <summary>
        /// Get a provider by name
        /// </summary>
        public ILLMProvider? Get(string name)
        {
            return _providers.TryGetValue(name, out var provider) ? provider : null;
        }

        /// <summary>
        /// Get default provider
        /// </summary>
        public ILLMProvider GetDefault()
        {
            return _providers.TryGetValue(_defaultProvider, out var provider)
                ? provider
                : _providers.Values.First();
        }

        /// <summary>
        /// Set default provider
        /// </summary>
        public void SetDefault(string name)
        {
            if (_providers.ContainsKey(name))
                _defaultProvider = name;
        }

        /// <summary>
        /// List all available providers
        /// </summary>
        public IEnumerable<string> ListAvailable() => _providers.Keys;

        /// <summary>
        /// Get configured providers (those with API keys or endpoints)
        /// </summary>
        public IEnumerable<string> ListConfigured() => _providers.Where(x => x.Value.IsConfigured).Select(x => x.Key);
    }
}
