using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using SGLDevEngine.GraphEngine;

namespace SGLDevEngine.Studio
{
    /// <summary>
    /// Manages project file operations, including open, save, and folder tree rendering
    /// </summary>
    public class ProjectService
    {
        public static ProjectService Instance { get; } = new ProjectService();

        private string _currentProjectPath;
        private Graph _currentGraph;

        /// <summary>
        /// Opens a project from a .uproject file or directory path
        /// </summary>
        public void OpenProject(string path)
        {
            try
            {
                if (File.Exists(path) && Path.GetExtension(path) == ".uproject")
                {
                    _currentProjectPath = Path.GetDirectoryName(path);
                }
                else if (Directory.Exists(path))
                {
                    _currentProjectPath = path;
                }
                else
                {
                    throw new FileNotFoundException($"Project path not found: {path}");
                }

                LoadProjectFiles(_currentProjectPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open project: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Loads project files and populates the tree view
        /// </summary>
        private void LoadProjectFiles(string path)
        {
            try
            {
                // Load Graph.json if it exists
                var graphPath = Path.Combine(path, "Graph.json");
                if (File.Exists(graphPath))
                {
                    var json = File.ReadAllText(graphPath);
                    _currentGraph = JsonSerializer.Deserialize<Graph>(json);
                }
                else
                {
                    _currentGraph = new Graph("Project", GraphDomain.Logic);
                }

                // Build tree structure
                var rootItem = new TreeViewItem
                {
                    Header = Path.GetFileName(path),
                    IsExpanded = true
                };

                // Add directories
                foreach (var directory in Directory.GetDirectories(path))
                {
                    var dirName = Path.GetFileName(directory);
                    rootItem.Items.Add(new TreeViewItem { Header = $"📁 {dirName}", Tag = directory });
                }

                // Add files
                foreach (var file in Directory.GetFiles(path))
                {
                    var fileName = Path.GetFileName(file);
                    rootItem.Items.Add(new TreeViewItem { Header = $"📄 {fileName}", Tag = file });
                }

                // Update UI on main thread
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var mainWindow = (MainWindow)Application.Current.MainWindow;
                    if (mainWindow.ArchitectureTree != null)
                    {
                        mainWindow.ArchitectureTree.Items.Clear();
                        mainWindow.ArchitectureTree.Items.Add(rootItem);
                    }

                    mainWindow.Title = $"SGL DevEngine v1.1.5 - {Path.GetFileName(path)}";
                    mainWindow.LogMessage($"✓ Project opened: {path}");
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load project files: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Saves the current project
        /// </summary>
        public void SaveProject(string savePath = null)
        {
            try
            {
                if (string.IsNullOrEmpty(_currentProjectPath) && string.IsNullOrEmpty(savePath))
                {
                    throw new InvalidOperationException("No project path set. Use 'Save As' to specify a location.");
                }

                var projectPath = savePath ?? _currentProjectPath;
                Directory.CreateDirectory(projectPath);

                // Save Graph.json
                var graphPath = Path.Combine(projectPath, "Graph.json");
                var graphJson = JsonSerializer.Serialize(_currentGraph, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(graphPath, graphJson);

                // Update current project path if saving to new location
                if (savePath != null)
                {
                    _currentProjectPath = savePath;
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    var mainWindow = (MainWindow)Application.Current.MainWindow;
                    mainWindow.LogMessage($"✓ Project saved to: {projectPath}");
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save project: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Gets the current project path
        /// </summary>
        public string GetCurrentProjectPath() => _currentProjectPath;

        /// <summary>
        /// Gets the current graph
        /// </summary>
        public Graph GetCurrentGraph() => _currentGraph;

        /// <summary>
        /// Sets the current graph
        /// </summary>
        public void SetCurrentGraph(Graph graph) => _currentGraph = graph;
    }
}
