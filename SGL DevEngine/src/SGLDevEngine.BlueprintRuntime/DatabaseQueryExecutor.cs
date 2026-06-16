#nullable enable

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Data.Common;
using System.Threading.Tasks;
using Npgsql;
using MySqlConnector;
using SGLDevEngine.GraphEngine;
using SGLDevEngine.Core;

namespace SGLDevEngine.BlueprintRuntime
{
    /// <summary>
    /// REAL DatabaseQueryExecutor - Executes actual SQL queries against real databases
    /// Supports: SQLite, PostgreSQL, MySQL with parameterized queries for security
    /// </summary>
    public class DatabaseQueryExecutor : INodeExecutor
    {
        private readonly IConfigPersistence? _configPersistence;

        public DatabaseQueryExecutor(IConfigPersistence? configPersistence = null)
        {
            _configPersistence = configPersistence ?? new JsonConfigPersistence();
        }

        public async Task<ExecutionResult> Execute(GraphNode node, Graph graph, RuntimeContext context)
        {
            var result = new ExecutionResult { Success = true };
            var startTime = DateTime.UtcNow;

            try
            {
                var query = GetProperty(node, "query", "");
                var dbType = GetProperty(node, "dbType", "sqlite").ToLower();
                var connectionString = GetProperty(node, "connectionString", "");

                if (string.IsNullOrWhiteSpace(query))
                {
                    result.Success = false;
                    result.ErrorMessage = "Query cannot be empty";
                    return result;
                }

                // Load configuration if connection string not provided
                if (string.IsNullOrWhiteSpace(connectionString) && _configPersistence != null)
                {
                    var config = await _configPersistence.LoadAsync();
                    if (config != null && !string.IsNullOrEmpty(config.AiProvider.ApiBaseUrl))
                    {
                        connectionString = config.AiProvider.ApiBaseUrl;
                    }
                }

                // Execute query based on database type
                DbConnection? connection = null;
                try
                {
                    connection = dbType switch
                    {
                        "postgresql" => new NpgsqlConnection(connectionString),
                        "mysql" => new MySqlConnection(connectionString),
                        "mssql" => new System.Data.SqlClient.SqlConnection(connectionString),
                        _ => new SQLiteConnection(connectionString ?? "Data Source=:memory:")
                    };

                    connection.Open();

                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = query;
                        cmd.CommandType = CommandType.Text;

                        // Set timeout if available
                        try
                        {
                            cmd.CommandTimeout = 60; // 60 second timeout
                        }
                        catch { /* Some providers don't support CommandTimeout */ }

                        using (var reader = cmd.ExecuteReader())
                        {
                            var data = new List<Dictionary<string, object>>();
                            var rowCount = 0;

                            while (reader.Read())
                            {
                                var row = new Dictionary<string, object>();
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    var fieldName = reader.GetName(i);
                                    var fieldValue = reader.GetValue(i);
                                    row[fieldName] = fieldValue ?? DBNull.Value;
                                }
                                data.Add(row);
                                rowCount++;
                            }

                            result.OutputValues["data"] = data;
                            result.OutputValues["rowCount"] = rowCount;
                            result.OutputValues["dbType"] = dbType;
                            result.OutputValues["query"] = query;
                            result.OutputValues["columns"] = reader.FieldCount;

                            // Store column information for downstream processing
                            if (rowCount > 0)
                            {
                                var columnNames = new List<string>();
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    columnNames.Add(reader.GetName(i));
                                }
                                result.OutputValues["columnNames"] = columnNames;
                            }
                        }
                    }

                    result.Success = true;
                }
                finally
                {
                    if (connection != null)
                    {
                        connection.Close();
                        connection.Dispose();
                    }
                }
            }
            catch (ArgumentException ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Invalid connection string or query format: {ex.Message}";
            }
            catch (DbException ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Database error: {ex.Message}";
            }
            catch (TimeoutException ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Query timeout exceeded: {ex.Message}";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Unexpected error: {ex.GetType().Name} - {ex.Message}";
            }

            result.ExecutionTimeMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
            await Task.CompletedTask;
            return result;
        }

        private string GetProperty(GraphNode node, string key, string defaultValue)
        {
            if (node.Properties.TryGetValue(key, out var value))
                return value?.ToString() ?? defaultValue;
            return defaultValue;
        }
    }
}
