using AvalonInjectLib.Interfaces;
using System.Collections.Concurrent;
using System.Runtime;

namespace AvalonInjectLib.Scripting
{
    /// <summary>
    /// Manages loading, unloading, and execution of MoonSharp Lua scripts.
    /// Provides categorization and organization of scripts by directory structure.
    /// </summary>
    public class MoonSharpScriptLoader
    {
        private IAvalonEngine _engine;
        private readonly List<AvalonScript> _scripts = new();
        private readonly object _lockObject = new object();
        private volatile bool _isReloading = false;
        private string _currentScriptsDirectory = "";

        /// <summary>
        /// Dictionary of scripts organized by their category (based on directory structure)
        /// </summary>
        public Dictionary<string, List<AvalonScript>> ScriptsByCategory { get; private set; } = new();

        /// <summary>
        /// Read-only collection of all loaded scripts
        /// </summary>
        public IReadOnlyList<AvalonScript> Scripts => _scripts.AsReadOnly();

        /// <summary>
        /// Singleton instance of the script loader
        /// </summary>
        public static MoonSharpScriptLoader? Instance { get; private set; }

        /// <summary>
        /// Initializes a new instance of the script loader
        /// </summary>
        /// <param name="engine">The Avalon engine instance</param>
        public MoonSharpScriptLoader(IAvalonEngine engine)
        {
            if (Instance != null) return;
            Instance = this;
            _engine = engine;
        }

        /// <summary>
        /// Loads all Lua scripts from the specified directory and its subdirectories
        /// </summary>
        /// <param name="scriptsDirectory">The root directory containing Lua scripts</param>
        public void LoadScripts(string scriptsDirectory)
        {
            _currentScriptsDirectory = scriptsDirectory;

            if (!Directory.Exists(scriptsDirectory))
            {
                Logger.Error($"Directory not found: {scriptsDirectory}", "MoonSharp");
                return;
            }

            lock (_lockObject)
            {
                // Clear previous collections
                _scripts.Clear();
                ScriptsByCategory.Clear();

                var luaFiles = Directory.GetFiles(scriptsDirectory, "*.lua", SearchOption.AllDirectories);

                foreach (var file in luaFiles)
                {
                    try
                    {
                        // Determine category based on directory structure
                        string category = GetCategoryFromPath(file, scriptsDirectory);

                        var adapter = new MoonSharpScriptAdapter(file, category);
                        adapter.Initialize(_engine);
                        _scripts.Add(adapter.Script);

                        // Organize by category
                        if (!ScriptsByCategory.ContainsKey(category))
                            ScriptsByCategory[category] = new List<AvalonScript>();

                        ScriptsByCategory[category].Add(adapter.Script);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Error loading script '{file}': {ex.Message}", "MoonSharp");
                    }
                }

                // Sort categories alphabetically
                var sortedCategories = ScriptsByCategory.Keys.OrderBy(k => k).ToList();

                // Sort scripts within each category by name
                foreach (var category in sortedCategories)
                {
                    ScriptsByCategory[category] = ScriptsByCategory[category]
                        .OrderBy(s => s.Name)
                        .ToList();
                }

                // Create a new ordered dictionary
                var orderedScriptsByCategory = new Dictionary<string, List<AvalonScript>>();
                foreach (var category in sortedCategories)
                {
                    orderedScriptsByCategory.Add(category, ScriptsByCategory[category]);
                }

                // Replace the original dictionary
                ScriptsByCategory = orderedScriptsByCategory;
            }
        }

        /// <summary>
        /// Safely reloads all scripts, cleaning up resources and memory
        /// </summary>
        public void ReloadScripts()
        {
            if (_isReloading) return; // Prevent multiple simultaneous reloads

            _isReloading = true;

            try
            {
                Logger.Info("Starting script reload...", "MoonSharp");

                CleanupScripts();

                ForceGarbageCollection();

                Thread.Sleep(16); // ~1 frame at 60fps

                if (!string.IsNullOrEmpty(_currentScriptsDirectory))
                {
                    LoadScripts(_currentScriptsDirectory);
                }

                Logger.Info($"Reload completed. {_scripts.Count} scripts loaded.", "MoonSharp");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error during reload: {ex.Message}", "MoonSharp");
            }
            finally
            {
                _isReloading = false;
            }
        }

        /// <summary>
        /// Cleans up all script resources safely
        /// </summary>
        private void CleanupScripts()
        {
            lock (_lockObject)
            {
                // Clean up each script individually
                foreach (var script in _scripts)
                {
                    try
                    {
                        // Call Dispose if script implements IDisposable
                        if (script is IDisposable disposableScript)
                        {
                            disposableScript.Dispose();
                        }

                        // Call custom cleanup method if exists
                        CallScriptCleanup(script);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Error cleaning script '{script.Name}': {ex.Message}", "MoonSharp");
                    }
                }

                // Clear collections
                _scripts.Clear();
                ScriptsByCategory.Clear();
            }
        }

        /// <summary>
        /// Attempts to call a custom cleanup method on the script if it exists
        /// </summary>
        /// <param name="script">The script to clean up</param>
        private void CallScriptCleanup(AvalonScript script)
        {
            try
            {
                // Placeholder for calling custom cleanup methods in scripts
                // e.g., script.CallFunction("Cleanup");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in custom cleanup for '{script.Name}': {ex.Message}", "MoonSharp");
            }
        }

        /// <summary>
        /// Forces garbage collection to free up memory
        /// </summary>
        private void ForceGarbageCollection()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Optionally compact the large object heap
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect();
        }

        /// <summary>
        /// Determines the category for a script based on its path relative to the base directory
        /// </summary>
        /// <param name="filePath">Full path to the script file</param>
        /// <param name="baseDirectory">Base scripts directory</param>
        /// <returns>The category name derived from directory structure</returns>
        private string GetCategoryFromPath(string filePath, string baseDirectory)
        {
            // Normalize paths
            string normalizedFilePath = Path.GetFullPath(filePath);
            string normalizedBaseDirectory = Path.GetFullPath(baseDirectory);

            // Get relative path
            string relativePath = Path.GetRelativePath(normalizedBaseDirectory, normalizedFilePath);

            // Get containing directory
            string directoryPath = Path.GetDirectoryName(relativePath);

            // If in root directory, use "General" category
            if (string.IsNullOrEmpty(directoryPath) || directoryPath == ".")
                return "General";

            // Create category from directory structure
            // Replace path separators with " > " for visual hierarchy
            string category = directoryPath.Replace(Path.DirectorySeparatorChar, '>');

            return category;
        }

        /// <summary>
        /// Calls the Update method on all loaded scripts
        /// </summary>
        public void UpdateAll()
        {
            if (_isReloading) return; // Skip during reload

            lock (_lockObject)
            {
                foreach (var script in _scripts)
                {
                    script.Update();
                }
            }
        }

        /// <summary>
        /// Calls the Draw method on all loaded scripts
        /// </summary>
        public void DrawAll()
        {
            if (_isReloading) return; // Skip during reload

            lock (_lockObject)
            {
                foreach (var script in _scripts)
                {
                    script.Draw();
                }
            }
        }

        /// <summary>
        /// Gets all available script categories
        /// </summary>
        /// <returns>An ordered list of category names</returns>
        public IEnumerable<string> GetCategories()
        {
            lock (_lockObject)
            {
                return ScriptsByCategory.Keys.OrderBy(x => x).ToList();
            }
        }

        /// <summary>
        /// Gets all scripts in a specific category
        /// </summary>
        /// <param name="category">The category name</param>
        /// <returns>List of scripts in the category, or empty list if category doesn't exist</returns>
        public IEnumerable<AvalonScript> GetScriptsByCategory(string category)
        {
            lock (_lockObject)
            {
                return ScriptsByCategory.ContainsKey(category) ?
                    ScriptsByCategory[category].ToList() :
                    new List<AvalonScript>();
            }
        }

        /// <summary>
        /// Gets whether the loader is currently reloading scripts
        /// </summary>
        public bool IsReloading => _isReloading;

        /// <summary>
        /// Gets the current number of loaded scripts
        /// </summary>
        public int ScriptCount => _scripts.Count;
    }
}