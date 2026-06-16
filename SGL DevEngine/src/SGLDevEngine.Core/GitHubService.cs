#nullable enable

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using SGLDevEngine.Core;

namespace SGLDevEngine.Core
{
    /// <summary>
    /// GitHub service for OAuth2 authentication and repository operations
    /// </summary>
    public class GitHubService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfigPersistence? _configPersistence;
        private string? _accessToken;
        public string? CurrentUser { get; private set; }

        public GitHubService(IConfigPersistence? configPersistence = null)
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            _configPersistence = configPersistence ?? new JsonConfigPersistence();
        }

        /// <summary>
        /// Initiate GitHub OAuth2 login flow
        /// Note: Full OAuth implementation requires a backend server to handle code exchange
        /// This is a placeholder for the standard GitHub OAuth2 flow
        /// </summary>
        public async Task<string?> GitHubLoginAsync()
        {
            try
            {
                // GitHub OAuth2 parameters
                // In production, these would be stored securely
                const string clientId = "YOUR_GITHUB_CLIENT_ID";
                const string redirectUri = "http://localhost:51042/oauth/callback";
                const string scope = "repo user";
                const string state = "random_state_value";

                // Build authorization URL
                var authUrl = $"https://github.com/login/oauth/authorize?" +
                    $"client_id={clientId}" +
                    $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                    $"&scope={Uri.EscapeDataString(scope)}" +
                    $"&state={state}";

                // In a production system, this would:
                // 1. Open browser to authUrl
                // 2. User grants permission
                // 3. GitHub redirects to redirectUri with authorization code
                // 4. Backend server exchanges code for access token
                // 5. Token is stored encrypted in ConfigPersistence

                // For now, show the authorization URL for manual demo
                System.Diagnostics.Debug.WriteLine($"GitHub OAuth URL: {authUrl}");

                // Placeholder for actual OAuth token exchange
                _accessToken = null; // Would be set from OAuth code exchange
                CurrentUser = null;

                return await Task.FromResult(_accessToken);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GitHub Auth Error] {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// List repositories for authenticated user
        /// </summary>
        public async Task<List<string>> ListReposAsync()
        {
            if (string.IsNullOrEmpty(_accessToken))
            {
                throw new InvalidOperationException("Not authenticated with GitHub. Call GitHubLoginAsync first.");
            }

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user/repos");
                request.Headers.Add("Authorization", $"Bearer {_accessToken}");
                request.Headers.Add("Accept", "application/vnd.github+json");

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);

                var repos = new List<string>();
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in doc.RootElement.EnumerateArray())
                    {
                        if (item.TryGetProperty("name", out var name))
                        {
                            repos.Add(name.GetString() ?? "");
                        }
                    }
                }

                return repos;
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GitHub API Error] {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Commit or update a file in a GitHub repository
        /// </summary>
        public async Task<bool> CommitFileAsync(string owner, string repo, string filePath, string content, string commitMessage)
        {
            if (string.IsNullOrEmpty(_accessToken))
            {
                throw new InvalidOperationException("Not authenticated with GitHub.");
            }

            try
            {
                var apiUrl = $"https://api.github.com/repos/{owner}/{repo}/contents/{filePath}";

                // First, get the current file SHA (if it exists) for updates
                var getShaRequest = new HttpRequestMessage(HttpMethod.Get, apiUrl);
                getShaRequest.Headers.Add("Authorization", $"Bearer {_accessToken}");

                var shaResponse = await _httpClient.SendAsync(getShaRequest);
                string? fileSha = null;

                if (shaResponse.IsSuccessStatusCode)
                {
                    var shaContent = await shaResponse.Content.ReadAsStringAsync();
                    using var shaDoc = JsonDocument.Parse(shaContent);
                    if (shaDoc.RootElement.TryGetProperty("sha", out var sha))
                    {
                        fileSha = sha.GetString();
                    }
                }

                // Prepare commit payload
                var payload = new
                {
                    message = commitMessage,
                    content = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(content)),
                    sha = fileSha
                };

                var request = new HttpRequestMessage(HttpMethod.Put, apiUrl)
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(payload),
                        System.Text.Encoding.UTF8,
                        "application/json"
                    )
                };
                request.Headers.Add("Authorization", $"Bearer {_accessToken}");

                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GitHub Commit Error] {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Get authenticated user information
        /// </summary>
        public async Task<(string? username, string? email)> GetUserInfoAsync()
        {
            if (string.IsNullOrEmpty(_accessToken))
            {
                throw new InvalidOperationException("Not authenticated with GitHub.");
            }

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user");
                request.Headers.Add("Authorization", $"Bearer {_accessToken}");
                request.Headers.Add("Accept", "application/vnd.github+json");

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);

                var username = doc.RootElement.TryGetProperty("login", out var user) ? user.GetString() : null;
                var email = doc.RootElement.TryGetProperty("email", out var emailProp) ? emailProp.GetString() : null;

                CurrentUser = username;
                return (username, email);
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GitHub User Info Error] {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Set access token manually (for testing or token restoration)
        /// </summary>
        public void SetAccessToken(string token)
        {
            _accessToken = token;
        }

        /// <summary>
        /// Check if currently authenticated
        /// </summary>
        public bool IsAuthenticated => !string.IsNullOrEmpty(_accessToken);
    }
}
