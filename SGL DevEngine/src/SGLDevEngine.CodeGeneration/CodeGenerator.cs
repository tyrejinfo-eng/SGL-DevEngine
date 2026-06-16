using System;
using System.Collections.Generic;
using System.Text;
using SGLDevEngine.GraphEngine;

namespace SGLDevEngine.CodeGeneration
{
    /// <summary>
    /// Intermediate Representation - language agnostic model
    /// </summary>
    public class IRNode
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Operation { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
        public List<string> Dependencies { get; set; } = new();
    }

    public class IRClass
    {
        public string Name { get; set; }
        public string Namespace { get; set; }
        public List<IRProperty> Properties { get; set; } = new();
        public List<IRMethod> Methods { get; set; } = new();
    }

    public class IRProperty
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool IsPublic { get; set; } = true;
    }

    public class IRMethod
    {
        public string Name { get; set; }
        public string ReturnType { get; set; } = "void";
        public List<IRParameter> Parameters { get; set; } = new();
        public List<string> Body { get; set; } = new();
        public bool IsAsync { get; set; } = false;
    }

    public class IRParameter
    {
        public string Name { get; set; }
        public string Type { get; set; }
    }

    /// <summary>
    /// Compiler converts graph to IR
    /// </summary>
    public class GraphCompiler
    {
        public static List<IRNode> CompileToIR(Graph graph)
        {
            var irNodes = new List<IRNode>();

            var orderedNodes = graph.GetTopologicalOrder();

            foreach (var node in orderedNodes)
            {
                var irNode = new IRNode
                {
                    Operation = node.Type,
                    Parameters = new Dictionary<string, object>(node.Properties)
                };

                // Track dependencies
                var incomingEdges = graph.Edges.FindAll(e => e.TargetNodeId == node.Id);
                foreach (var edge in incomingEdges)
                {
                    irNode.Dependencies.Add(edge.SourceNodeId);
                }

                irNodes.Add(irNode);
            }

            return irNodes;
        }
    }

    /// <summary>
    /// Base code generator
    /// </summary>
    public abstract class CodeGenerator
    {
        protected Graph _graph;
        protected List<IRNode> _irNodes;

        protected CodeGenerator(Graph graph)
        {
            _graph = graph;
            _irNodes = GraphCompiler.CompileToIR(graph);
        }

        public abstract string Generate();
        protected abstract string GenerateHeader();
        protected abstract string GenerateNode(IRNode node);
        protected abstract string GenerateFooter();

        protected string BuildCode()
        {
            var sb = new StringBuilder();
            sb.AppendLine(GenerateHeader());

            foreach (var irNode in _irNodes)
            {
                sb.AppendLine(GenerateNode(irNode));
            }

            sb.AppendLine(GenerateFooter());
            return sb.ToString();
        }
    }

    /// <summary>
    /// C# code generator
    /// </summary>
    public class CSharpGenerator : CodeGenerator
    {
        public CSharpGenerator(Graph graph) : base(graph) { }

        public override string Generate() => BuildCode();

        protected override string GenerateHeader()
        {
            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine();
            sb.AppendLine($"namespace {GetNamespace()}");
            sb.AppendLine("{");
            sb.AppendLine($"    public class {GetClassName()}");
            sb.AppendLine("    {");
            return sb.ToString();
        }

        protected override string GenerateNode(IRNode node)
        {
            var sb = new StringBuilder();

            switch (node.Operation.ToLower())
            {
                case "httprequest":
                    sb.AppendLine("        public async Task<string> ExecuteHttpRequest()");
                    sb.AppendLine("        {");
                    var url = node.Parameters.TryGetValue("url", out var u) ? u : "";
                    sb.AppendLine($"            using (var client = new System.Net.Http.HttpClient())");
                    sb.AppendLine($"            {{");
                    sb.AppendLine($"                var response = await client.GetAsync(\"{url}\");");
                    sb.AppendLine($"                return await response.Content.ReadAsStringAsync();");
                    sb.AppendLine($"            }}");
                    sb.AppendLine("        }");
                    break;

                case "datatransform":
                    sb.AppendLine("        public string TransformData(string input)");
                    sb.AppendLine("        {");
                    var operation = node.Parameters.TryGetValue("operation", out var op) ? op : "pass";
                    switch (operation?.ToString()?.ToLower())
                    {
                        case "uppercase":
                            sb.AppendLine("            return input?.ToUpper();");
                            break;
                        case "lowercase":
                            sb.AppendLine("            return input?.ToLower();");
                            break;
                        default:
                            sb.AppendLine("            return input;");
                            break;
                    }
                    sb.AppendLine("        }");
                    break;

                case "databasequery":
                    var query = node.Parameters.TryGetValue("query", out var q) ? q : "";
                    sb.AppendLine("        public async Task<List<Dictionary<string, object>>> ExecuteQuery()");
                    sb.AppendLine("        {");
                    sb.AppendLine("            var results = new List<Dictionary<string, object>>();");
                    sb.AppendLine($"            // Query: {query}");
                    sb.AppendLine("            return results;");
                    sb.AppendLine("        }");
                    break;

                default:
                    sb.AppendLine($"        // Node: {node.Operation}");
                    break;
            }

            sb.AppendLine();
            return sb.ToString();
        }

        protected override string GenerateFooter()
        {
            return "    }\n}\n";
        }

        private string GetNamespace()
        {
            return _graph.Metadata.TryGetValue("namespace", out var ns) ?
                ns?.ToString() ?? "AutoGenerated" : "AutoGenerated";
        }

        private string GetClassName()
        {
            return _graph.Name.Replace(" ", "").Replace("-", "");
        }
    }

    /// <summary>
    /// Python code generator
    /// </summary>
    public class PythonGenerator : CodeGenerator
    {
        public PythonGenerator(Graph graph) : base(graph) { }

        public override string Generate() => BuildCode();

        protected override string GenerateHeader()
        {
            var sb = new StringBuilder();
            sb.AppendLine("import requests");
            sb.AppendLine("import json");
            sb.AppendLine("import asyncio");
            sb.AppendLine();
            sb.AppendLine($"class {GetClassName()}:");
            sb.AppendLine();
            return sb.ToString();
        }

        protected override string GenerateNode(IRNode node)
        {
            var sb = new StringBuilder();

            switch (node.Operation.ToLower())
            {
                case "httprequest":
                    var url = node.Parameters.TryGetValue("url", out var u) ? u : "";
                    sb.AppendLine("    async def execute_http_request(self):");
                    sb.AppendLine("        try:");
                    sb.AppendLine($"            response = requests.get('{url}')");
                    sb.AppendLine("            return response.text");
                    sb.AppendLine("        except Exception as e:");
                    sb.AppendLine("            return str(e)");
                    break;

                case "datatransform":
                    sb.AppendLine("    def transform_data(self, input_data):");
                    var op = node.Parameters.TryGetValue("operation", out var o) ? o?.ToString()?.ToLower() : "pass";
                    switch (op)
                    {
                        case "uppercase":
                            sb.AppendLine("        return input_data.upper() if input_data else ''");
                            break;
                        case "lowercase":
                            sb.AppendLine("        return input_data.lower() if input_data else ''");
                            break;
                        default:
                            sb.AppendLine("        return input_data");
                            break;
                    }
                    break;

                default:
                    sb.AppendLine($"    # Node: {node.Operation}");
                    break;
            }

            sb.AppendLine();
            return sb.ToString();
        }

        protected override string GenerateFooter()
        {
            return "";
        }

        private string GetClassName()
        {
            var name = _graph.Name.Replace(" ", "").Replace("-", "");
            return name.Length > 0 ? name.AsClassName() : "AutoGenerated";
        }
    }

    /// <summary>
    /// C++ code generator
    /// </summary>
    public class CppGenerator : CodeGenerator
    {
        public CppGenerator(Graph graph) : base(graph) { }

        public override string Generate() => BuildCode();

        protected override string GenerateHeader()
        {
            var sb = new StringBuilder();
            sb.AppendLine("#include <string>");
            sb.AppendLine("#include <vector>");
            sb.AppendLine("#include <map>");
            sb.AppendLine("#include <iostream>");
            sb.AppendLine();
            sb.AppendLine($"class {GetClassName()} {{");
            sb.AppendLine("public:");
            return sb.ToString();
        }

        protected override string GenerateNode(IRNode node)
        {
            var sb = new StringBuilder();

            switch (node.Operation.ToLower())
            {
                case "httprequest":
                    sb.AppendLine("    std::string executeHttpRequest() {");
                    sb.AppendLine("        // HTTP implementation");
                    sb.AppendLine("        return std::string(\"\");");
                    sb.AppendLine("    }");
                    break;

                case "datatransform":
                    sb.AppendLine("    std::string transformData(const std::string& input) {");
                    sb.AppendLine("        return input;");
                    sb.AppendLine("    }");
                    break;

                default:
                    sb.AppendLine($"    // Node: {node.Operation}");
                    break;
            }

            sb.AppendLine();
            return sb.ToString();
        }

        protected override string GenerateFooter()
        {
            return "};\n";
        }

        private string GetClassName()
        {
            return _graph.Name.Replace(" ", "").Replace("-", "");
        }
    }

    public static class StringExtensions
    {
        public static string AsClassName(this string str)
        {
            if (string.IsNullOrEmpty(str)) return "Class";
            return char.ToUpper(str[0]) + str.Substring(1);
        }
    }
}
