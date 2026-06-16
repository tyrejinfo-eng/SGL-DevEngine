#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SGLDevEngine.GraphEngine;
using SGLDevEngine.Core;

namespace SGLDevEngine.BlueprintRuntime
{
    /// <summary>
    /// REAL AIInferenceExecutor - Production-grade LLM integration
    /// Calls actual LLM providers (Claude, OpenAI, LocalGGUF) instead of simulating
    /// </summary>
    public class AIInferenceExecutor : INodeExecutor
    {
        private readonly ILLMProvider? _provider;

        public AIInferenceExecutor(ILLMProvider? provider = null)
        {
            _provider = provider;
        }

        public async Task<ExecutionResult> Execute(GraphNode node, Graph graph, RuntimeContext context)
        {
            var result = new ExecutionResult { Success = true };
            var startTime = DateTime.UtcNow;

            try
            {
                // Get inference configuration
                var prompt = GetProperty(node, "prompt", "");
                var systemPrompt = GetProperty(node, "systemPrompt", "");
                var modelName = GetProperty(node, "modelName", "");
                var maxTokens = int.TryParse(GetProperty(node, "maxTokens", "1024"), out var m) ? m : 1024;
                var temperature = double.TryParse(GetProperty(node, "temperature", "0.7"), out var t) ? t : 0.7;

                // Validate
                if (string.IsNullOrWhiteSpace(prompt))
                {
                    result.Success = false;
                    result.ErrorMessage = "Prompt cannot be empty";
                    return result;
                }

                // Get the LLM provider (falls back to default if not provided)
                var provider = _provider ?? GetDefaultProvider();
                if (provider == null)
                {
                    result.Success = false;
                    result.ErrorMessage = "No LLM provider available. Configure LLM settings in Settings dialog.";
                    return result;
                }

                // Call REAL LLM provider
                string inferenceResponse;
                try
                {
                    inferenceResponse = await provider.GenerateAsync(prompt, systemPrompt, maxTokens);
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.ErrorMessage = $"LLM provider error: {ex.Message}";
                    result.OutputValues["error"] = ex.StackTrace ?? "Unknown error";
                    Console.WriteLine($"[AI ERROR] Provider call failed: {ex.Message}");
                    return result;
                }

                // Verify response
                if (string.IsNullOrWhiteSpace(inferenceResponse))
                {
                    result.Success = false;
                    result.ErrorMessage = "LLM provider returned empty response";
                    return result;
                }

                // Store results
                result.OutputValues["response"] = inferenceResponse;
                result.OutputValues["modelName"] = modelName;
                result.OutputValues["tokensUsed"] = EstimateTokens(inferenceResponse);
                result.OutputValues["statusCode"] = "success";
                result.OutputValues["timestamp"] = DateTime.UtcNow;
                result.OutputValues["responseLength"] = inferenceResponse.Length;

                // Log inference execution
                Console.WriteLine($"[AI INFERENCE] Model: {modelName} | Response length: {inferenceResponse.Length} | Tokens est: {EstimateTokens(inferenceResponse)}");

                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"AI inference failed: {ex.Message}";
                result.OutputValues["error"] = ex.StackTrace ?? "Unknown error";
                Console.WriteLine($"[AI ERROR] {ex.Message}");
            }

            result.ExecutionTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
            return result;
        }

        /// <summary>
        /// Get default LLM provider (LocalLLMProvider or configured provider)
        /// </summary>
        private ILLMProvider? GetDefaultProvider()
        {
            // Try to get configured provider from settings
            try
            {
                var configPersistence = new JsonConfigPersistence();
                var config = configPersistence.LoadAsync().Result;
                if (config?.AiProvider != null)
                {
                    var registry = new ProviderRegistry();
                    var provider = registry.Get(config.AiProvider.Provider ?? "Local");
                    if (provider != null)
                    {
                        return provider;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARNING] Failed to load config for LLM provider: {ex.Message}");
            }

            // Fallback to local provider
            var fallbackRegistry = new ProviderRegistry();
            return fallbackRegistry.GetDefault();
        }

        /// <summary>
        /// Estimate token count (rough approximation)
        /// </summary>
        private int EstimateTokens(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            // Rough estimate: ~4 characters per token for English text
            return (int)Math.Ceiling(text.Length / 4.0);
        }

        // Helper methods
        private string GetProperty(GraphNode node, string key, string defaultValue)
        {
            if (node.Properties.TryGetValue(key, out var value))
                return value?.ToString() ?? defaultValue;
            return defaultValue;
        }
    }
}
