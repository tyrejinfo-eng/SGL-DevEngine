#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SGLDevEngine.Core
{
    /// <summary>
    /// Configuration model for AI provider settings
    /// </summary>
    public class AiProviderConfig
    {
        [JsonPropertyName("provider")]
        public string Provider { get; set; } = "Local"; // Local, Claude, OpenAI, LocalGGUF

        [JsonPropertyName("model")]
        public string Model { get; set; } = "claude-3-5-sonnet-20241022";

        [JsonPropertyName("apiKey")]
        public string? ApiKey { get; set; }

        [JsonPropertyName("apiBaseUrl")]
        public string ApiBaseUrl { get; set; } = "https://api.anthropic.com";

        [JsonPropertyName("maxTokens")]
        public int MaxTokens { get; set; } = 4096;

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; } = 0.7;

        [JsonPropertyName("localModelPath")]
        public string? LocalModelPath { get; set; }

        [JsonPropertyName("localEndpoint")]
        public string LocalEndpoint { get; set; } = "http://localhost:8000";

        [JsonPropertyName("timeout")]
        public int TimeoutSeconds { get; set; } = 60;
    }

    /// <summary>
    /// GitHub settings
    /// </summary>
    public class GitHubConfig
    {
        [JsonPropertyName("connected")]
        public bool Connected { get; set; } = false;

        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("accessTokenEncrypted")]
        public string? AccessTokenEncrypted { get; set; }

        [JsonPropertyName("authorizedRepos")]
        public List<string> AuthorizedRepos { get; set; } = new();

        [JsonPropertyName("defaultRepo")]
        public string? DefaultRepo { get; set; }
    }

    /// <summary>
    /// Application-wide configuration
    /// </summary>
    public class AppConfig
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.1.5";

        [JsonPropertyName("theme")]
        public string Theme { get; set; } = "Dark";

        [JsonPropertyName("aiProvider")]
        public AiProviderConfig AiProvider { get; set; } = new();

        [JsonPropertyName("github")]
        public GitHubConfig GitHub { get; set; } = new();

        [JsonPropertyName("lastProjectPath")]
        public string? LastProjectPath { get; set; }

        [JsonPropertyName("autoSave")]
        public bool AutoSave { get; set; } = true;
    }

    /// <summary>
    /// Interface for configuration persistence
    /// </summary>
    public interface IConfigPersistence
    {
        Task<AppConfig> LoadAsync();
        Task SaveAsync(AppConfig config);
        void ClearCache();
    }

    /// <summary>
    /// JSON-based configuration persistence with encryption for sensitive data
    /// </summary>
    public class JsonConfigPersistence : IConfigPersistence
    {
        private readonly string _configPath;
        private AppConfig? _cachedConfig;
        private readonly object _lockObj = new();

        private static readonly string _encryptionKey = "SGLDevEngine.Encryption.Key.2026";

        public JsonConfigPersistence(string? configPath = null)
        {
            if (configPath == null)
            {
                var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var configDir = Path.Combine(appDataPath, "SGL DevEngine");
                Directory.CreateDirectory(configDir);
                _configPath = Path.Combine(configDir, "config.json");
            }
            else
            {
                _configPath = configPath;
            }
        }

        /// <summary>
        /// Load configuration from disk or return defaults
        /// </summary>
        public async Task<AppConfig> LoadAsync()
        {
            lock (_lockObj)
            {
                if (_cachedConfig != null)
                    return _cachedConfig;
            }

            try
            {
                if (!File.Exists(_configPath))
                    return new AppConfig();

                var json = await File.ReadAllTextAsync(_configPath);
                var config = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();

                // Decrypt sensitive fields
                if (!string.IsNullOrEmpty(config.GitHub.AccessTokenEncrypted))
                {
                    try
                    {
                        config.GitHub.AccessTokenEncrypted = Decrypt(config.GitHub.AccessTokenEncrypted);
                    }
                    catch
                    {
                        config.GitHub.Connected = false;
                        config.GitHub.AccessTokenEncrypted = null;
                    }
                }

                lock (_lockObj)
                {
                    _cachedConfig = config;
                }

                return config;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading config: {ex.Message}");
                return new AppConfig();
            }
        }

        /// <summary>
        /// Save configuration to disk with encryption for sensitive data
        /// </summary>
        public async Task SaveAsync(AppConfig config)
        {
            try
            {
                // Encrypt sensitive fields before saving
                var configToSave = config;
                if (!string.IsNullOrEmpty(configToSave.GitHub.AccessTokenEncrypted))
                {
                    configToSave.GitHub.AccessTokenEncrypted = Encrypt(configToSave.GitHub.AccessTokenEncrypted);
                }

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                var json = JsonSerializer.Serialize(configToSave, options);

                var configDir = Path.GetDirectoryName(_configPath);
                if (!string.IsNullOrEmpty(configDir))
                    Directory.CreateDirectory(configDir);

                await File.WriteAllTextAsync(_configPath, json);

                lock (_lockObj)
                {
                    _cachedConfig = config;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving config: {ex.Message}");
                throw;
            }
        }

        public void ClearCache()
        {
            lock (_lockObj)
            {
                _cachedConfig = null;
            }
        }

        /// <summary>
        /// Simple encryption for sensitive data (tokens, API keys)
        /// Uses Windows DPAPI-style approach with a fixed key for portability
        /// </summary>
        private static string Encrypt(string plainText)
        {
            try
            {
                using (var md5 = System.Security.Cryptography.MD5.Create())
                {
                    var keyBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(_encryptionKey));
                    using (var aes = Aes.Create())
                    {
                        aes.Key = keyBytes;
                        aes.Mode = CipherMode.CBC;
                        aes.Padding = PaddingMode.PKCS7;

                        var iv = aes.IV;
                        using (var encryptor = aes.CreateEncryptor(aes.Key, iv))
                        {
                            using (var ms = new MemoryStream())
                            {
                                ms.Write(iv, 0, iv.Length);
                                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                                {
                                    using (var sw = new StreamWriter(cs))
                                    {
                                        sw.Write(plainText);
                                    }
                                }
                                return Convert.ToBase64String(ms.ToArray());
                            }
                        }
                    }
                }
            }
            catch
            {
                return plainText;
            }
        }

        /// <summary>
        /// Simple decryption for sensitive data
        /// </summary>
        private static string Decrypt(string cipherText)
        {
            try
            {
                var buffer = Convert.FromBase64String(cipherText);
                using (var md5 = System.Security.Cryptography.MD5.Create())
                {
                    var keyBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(_encryptionKey));
                    using (var aes = Aes.Create())
                    {
                        aes.Key = keyBytes;
                        aes.Mode = CipherMode.CBC;
                        aes.Padding = PaddingMode.PKCS7;

                        var iv = new byte[aes.IV.Length];
                        Array.Copy(buffer, 0, iv, 0, iv.Length);
                        aes.IV = iv;

                        using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                        {
                            using (var ms = new MemoryStream(buffer, iv.Length, buffer.Length - iv.Length))
                            {
                                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                                {
                                    using (var sr = new StreamReader(cs))
                                    {
                                        return sr.ReadToEnd();
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                return cipherText;
            }
        }
    }

    /// <summary>
    /// Manager for AI model configuration
    /// </summary>
    public class AiModelConfigManager
    {
        private readonly IConfigPersistence _persistence;
        private AppConfig _config;

        public AiModelConfigManager(IConfigPersistence? persistence = null)
        {
            _persistence = persistence ?? new JsonConfigPersistence();
            _config = new AppConfig();
        }

        public async Task InitializeAsync()
        {
            _config = await _persistence.LoadAsync();
        }

        public AiProviderConfig GetAiConfig() => _config.AiProvider;

        public GitHubConfig GetGitHubConfig() => _config.GitHub;

        public async Task SetAiProviderAsync(string provider, string? apiKey = null, string? model = null)
        {
            _config.AiProvider.Provider = provider;
            if (apiKey != null)
                _config.AiProvider.ApiKey = apiKey;
            if (model != null)
                _config.AiProvider.Model = model;

            await _persistence.SaveAsync(_config);
        }

        public async Task SetGitHubTokenAsync(string token, string username)
        {
            _config.GitHub.Connected = true;
            _config.GitHub.Username = username;
            _config.GitHub.AccessTokenEncrypted = token;
            await _persistence.SaveAsync(_config);
        }

        public async Task ClearGitHubTokenAsync()
        {
            _config.GitHub.Connected = false;
            _config.GitHub.Username = null;
            _config.GitHub.AccessTokenEncrypted = null;
            await _persistence.SaveAsync(_config);
        }

        public AppConfig GetFullConfig() => _config;

        public async Task SaveAsync()
        {
            await _persistence.SaveAsync(_config);
        }
    }
}
