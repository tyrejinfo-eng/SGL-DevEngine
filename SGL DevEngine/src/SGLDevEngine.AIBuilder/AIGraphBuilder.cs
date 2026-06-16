using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using SGLDevEngine.GraphEngine;
using SGLDevEngine.TypeSystem;

namespace SGLDevEngine.AIBuilder
{
    /// <summary>
    /// Specification for AI-generated architecture
    /// </summary>
    public class ArchitectureSpec
    {
        public string SystemType { get; set; } // Microservice, WebApp, etc.
        public List<ServiceModule> Services { get; set; } = new();
        public List<EventBusModule> EventBuses { get; set; } = new();
        public List<DatabaseModule> Databases { get; set; } = new();
        public SecurityConfig Security { get; set; } = new();
    }

    public class ServiceModule
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<string> Methods { get; set; } = new();
        public bool RequiresAuth { get; set; } = false;
    }

    public class EventBusModule
    {
        public string Name { get; set; }
        public List<string> Topics { get; set; } = new();
    }

    public class DatabaseModule
    {
        public string Name { get; set; }
        public string Type { get; set; } = "SQL"; // SQL, NoSQL, Graph
        public List<string> Tables { get; set; } = new();
    }

    public class SecurityConfig
    {
        public bool RequiresJWT { get; set; } = true;
        public bool RequiresEncryption { get; set; } = false;
        public List<string> AllowedOrigins { get; set; } = new() { "*" };
    }

    /// <summary>
    /// Interface for LLM providers
    /// </summary>
    public interface ILLMProvider
    {
        Task<string> GenerateArchitecture(string prompt);
        Task<string> GenerateCode(string specification);
    }

    /// <summary>
    /// Local LLM provider (for testing and offline use)
    /// </summary>
    public class LocalLLMProvider : ILLMProvider
    {
        public async Task<string> GenerateArchitecture(string prompt)
        {
            // Local LLM templates for demonstration
            var templates = new Dictionary<string, string>
            {
                { "login", JsonTemplate.LoginSystem },
                { "ecommerce", JsonTemplate.EcommerceSystem },
                { "social", JsonTemplate.SocialMediaSystem }
            };

            var lower = prompt.ToLower();
            foreach (var kvp in templates)
            {
                if (lower.Contains(kvp.Key))
                {
                    await Task.Delay(500); // Simulate processing
                    return kvp.Value;
                }
            }

            return JsonTemplate.DefaultSystem;
        }

        public async Task<string> GenerateCode(string specification)
        {
            await Task.Delay(100);
            return "// Generated code from specification";
        }
    }

    /// <summary>
    /// AI Graph Builder - converts specs to blueprints
    /// </summary>
    public class AIGraphBuilder
    {
        private readonly ILLMProvider _llmProvider;

        public AIGraphBuilder(ILLMProvider llmProvider)
        {
            _llmProvider = llmProvider ?? throw new ArgumentNullException(nameof(llmProvider));
        }

        /// <summary>
        /// Generate architecture graph from AI prompt
        /// </summary>
        public async Task<Graph> BuildFromPrompt(string prompt, GraphDomain domain = GraphDomain.Architecture)
        {
            var architectureJson = await _llmProvider.GenerateArchitecture(prompt);
            var spec = ParseArchitectureSpec(architectureJson);
            return BuildGraphFromSpec(spec, domain);
        }

        /// <summary>
        /// Parse JSON architecture specification
        /// </summary>
        private ArchitectureSpec ParseArchitectureSpec(string json)
        {
            var spec = new ArchitectureSpec();

            // Simple JSON parsing (in production, use Json.NET or System.Text.Json)
            if (json.Contains("\"microservice\""))
                spec.SystemType = "Microservice";
            else if (json.Contains("\"webapp\""))
                spec.SystemType = "WebApplication";

            // Parse services
            if (json.Contains("\"services\""))
            {
                spec.Services.Add(new ServiceModule
                {
                    Name = "AuthService",
                    Description = "Authentication and authorization",
                    RequiresAuth = false,
                    Methods = new List<string> { "Login", "Register", "Verify" }
                });

                spec.Services.Add(new ServiceModule
                {
                    Name = "UserService",
                    Description = "User management",
                    RequiresAuth = true,
                    Methods = new List<string> { "GetUser", "UpdateUser", "DeleteUser" }
                });
            }

            // Parse event buses
            if (json.Contains("\"eventbus\""))
            {
                spec.EventBuses.Add(new EventBusModule
                {
                    Name = "MainBus",
                    Topics = new List<string> { "auth.login", "user.created", "user.updated" }
                });
            }

            // Parse databases
            if (json.Contains("\"database\""))
            {
                spec.Databases.Add(new DatabaseModule
                {
                    Name = "MainDB",
                    Type = "SQL",
                    Tables = new List<string> { "Users", "Roles", "Permissions" }
                });
            }

            return spec;
        }

        /// <summary>
        /// Build visual graph from architecture specification
        /// </summary>
        private Graph BuildGraphFromSpec(ArchitectureSpec spec, GraphDomain domain)
        {
            var graph = new Graph(spec.SystemType, domain, "AI Generated Architecture");

            // Add service nodes
            var yOffset = 0;
            foreach (var service in spec.Services)
            {
                var node = new ServiceNodeImpl(service.Name, service.Name, $"Service: {service.Description}");
                node.Position = new Vector2(100, yOffset);
                graph.AddNode(node);
                yOffset += 150;
            }

            // Add event bus node
            foreach (var eventBus in spec.EventBuses)
            {
                var node = new EventBusNodeImpl(eventBus.Name, $"Topics: {string.Join(", ", eventBus.Topics)}");
                node.Position = new Vector2(400, 75);
                graph.AddNode(node);
            }

            // Add database nodes
            var dbOffset = 0;
            foreach (var db in spec.Databases)
            {
                var node = new DatabaseNodeImpl(db.Name, $"Type: {db.Type}");
                node.Position = new Vector2(700, dbOffset);
                graph.AddNode(node);
                dbOffset += 150;
            }

            return graph;
        }
    }

    /// <summary>
    /// Service node implementation
    /// </summary>
    public class ServiceNodeImpl : ServiceNode
    {
        public ServiceNodeImpl(string title, string serviceName, string description = "")
            : base(title, "Service", serviceName, description)
        {
            var stringType = PortTypeRegistry.Get(PortTypeKind.String);
            var jsonType = PortTypeRegistry.Get(PortTypeKind.JSON);

            DefineInput("request", jsonType);
            DefineOutput("response", jsonType);
            DefineOutput("error", stringType);
        }
    }

    /// <summary>
    /// Event bus node implementation
    /// </summary>
    public class EventBusNodeImpl : ExecutionNode
    {
        public EventBusNodeImpl(string title, string description = "")
            : base(title, "EventBus", description)
        {
            var eventType = PortTypeRegistry.Get(PortTypeKind.Event);

            DefineInput("event", eventType);
            DefineOutput("published", eventType);
        }
    }

    /// <summary>
    /// Database node implementation
    /// </summary>
    public class DatabaseNodeImpl : ExecutionNode
    {
        public DatabaseNodeImpl(string title, string description = "")
            : base(title, "Database", description)
        {
            var jsonType = PortTypeRegistry.Get(PortTypeKind.JSON);
            var stringType = PortTypeRegistry.Get(PortTypeKind.String);

            DefineInput("query", stringType);
            DefineOutput("result", jsonType);
            DefineOutput("error", stringType);
        }
    }

    /// <summary>
    /// JSON templates for common architectures
    /// </summary>
    internal static class JsonTemplate
    {
        public static string LoginSystem => @"{
            ""systemType"": ""microservice"",
            ""services"": [
                { ""name"": ""AuthService"", ""methods"": [""Login"", ""Register""] },
                { ""name"": ""UserService"", ""methods"": [""GetUser"", ""UpdateProfile""] }
            ],
            ""eventbus"": true,
            ""database"": { ""type"": ""SQL"" }
        }";

        public static string EcommerceSystem => @"{
            ""systemType"": ""microservice"",
            ""services"": [
                { ""name"": ""ProductService"", ""methods"": [""GetProducts"", ""Search""] },
                { ""name"": ""CartService"", ""methods"": [""AddItem"", ""Checkout""] },
                { ""name"": ""OrderService"", ""methods"": [""CreateOrder"", ""GetOrders""] },
                { ""name"": ""PaymentService"", ""methods"": [""ProcessPayment""] }
            ],
            ""eventbus"": true,
            ""database"": { ""type"": ""SQL"" }
        }";

        public static string SocialMediaSystem => @"{
            ""systemType"": ""microservice"",
            ""services"": [
                { ""name"": ""AccountService"", ""methods"": [""Register"", ""Login"", ""GetProfile""] },
                { ""name"": ""PostService"", ""methods"": [""CreatePost"", ""GetFeed"", ""LikePost""] },
                { ""name"": ""CommentService"", ""methods"": [""AddComment"", ""GetComments""] },
                { ""name"": ""NotificationService"", ""methods"": [""SendNotification"", ""GetNotifications""] }
            ],
            ""eventbus"": true,
            ""database"": { ""type"": ""NoSQL"" }
        }";

        public static string DefaultSystem => @"{
            ""systemType"": ""webapp"",
            ""services"": [
                { ""name"": ""MainService"", ""methods"": [""Process""] }
            ],
            ""eventbus"": false,
            ""database"": { ""type"": ""SQL"" }
        }";
    }
}
