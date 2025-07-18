using AvalonInjectLib.Interfaces;

namespace AvalonInjectLib.Scripting
{
    public class MoonSharpScriptLoader
    {
        private static readonly List<AvalonScript> _scripts = new();
        public static IReadOnlyList<AvalonScript> Scripts => _scripts.AsReadOnly();

        // Diccionario para organizar scripts por categoría
        public Dictionary<string, List<AvalonScript>> ScriptsByCategory { get; private set; } = new();

        public void LoadScripts(string scriptsDirectory, IAvalonEngine engine)
        {
            if (!Directory.Exists(scriptsDirectory))
            {
                Console.WriteLine($"[MoonSharpLoader] Directorio no encontrado: {scriptsDirectory}");
                return;
            }

            // Limpiar colecciones anteriores
            _scripts.Clear();
            ScriptsByCategory.Clear();

            var luaFiles = Directory.GetFiles(scriptsDirectory, "*.lua", SearchOption.AllDirectories);

            foreach (var file in luaFiles)
            {
                try
                {
                    // Determinar la categoría basándose en la estructura de directorios
                    string category = GetCategoryFromPath(file, scriptsDirectory);

                    var adapter = new MoonSharpScriptAdapter(file, category);
                    adapter.Initialize(engine);
                    _scripts.Add(adapter.Script);

                    // Organizar por categoría
                    if (!ScriptsByCategory.ContainsKey(category))
                        ScriptsByCategory[category] = new List<AvalonScript>();

                    ScriptsByCategory[category].Add(adapter.Script);

                    Console.WriteLine($"[MoonSharpLoader] Script cargado: {adapter.Script.Name} (Categoría: {category})");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MoonSharpLoader] Error cargando script '{file}': {ex.Message}");
                }
            }

            // Ordenar las categorías alfabéticamente
            var sortedCategories = ScriptsByCategory.Keys.OrderBy(k => k).ToList();

            // Ordenar los scripts dentro de cada categoría por nombre
            foreach (var category in sortedCategories)
            {
                ScriptsByCategory[category] = ScriptsByCategory[category]
                    .OrderBy(s => s.Name)
                    .ToList();
            }

            // Opcional: Crear un nuevo diccionario ordenado
            var orderedScriptsByCategory = new Dictionary<string, List<AvalonScript>>();
            foreach (var category in sortedCategories)
            {
                orderedScriptsByCategory.Add(category, ScriptsByCategory[category]);
            }

            // Si prefieres reemplazar el diccionario original
            ScriptsByCategory = orderedScriptsByCategory;
        }

        private string GetCategoryFromPath(string filePath, string baseDirectory)
        {
            // Normalizar las rutas
            string normalizedFilePath = Path.GetFullPath(filePath);
            string normalizedBaseDirectory = Path.GetFullPath(baseDirectory);

            // Obtener la ruta relativa
            string relativePath = Path.GetRelativePath(normalizedBaseDirectory, normalizedFilePath);

            // Obtener el directorio del archivo
            string directoryPath = Path.GetDirectoryName(relativePath);

            // Si está en el directorio raíz, categoría General
            if (string.IsNullOrEmpty(directoryPath) || directoryPath == ".")
                return "General";

            // Crear la categoría con la estructura de directorios
            // Reemplazar separadores de directorio con " > " para crear jerarquía visual
            string category = directoryPath.Replace(Path.DirectorySeparatorChar, '>');

            return category;
        }

        public void UpdateAll()
        {
            foreach (var script in _scripts)
                script.Update();
        }

        public void DrawAll()
        {
            foreach (var script in _scripts)
                script.Draw();
        }

        // Método para obtener todas las categorías
        public IEnumerable<string> GetCategories()
        {
            return ScriptsByCategory.Keys.OrderBy(x => x);
        }

        // Método para obtener scripts de una categoría específica
        public IEnumerable<AvalonScript> GetScriptsByCategory(string category)
        {
            return ScriptsByCategory.ContainsKey(category) ? ScriptsByCategory[category] : new List<AvalonScript>();
        }

        // Método para imprimir la estructura de categorías (útil para debugging)
        public void PrintCategoryStructure()
        {
            Console.WriteLine("[MoonSharpLoader] Estructura de categorías:");
            foreach (var category in GetCategories())
            {
                Console.WriteLine($"  {category}:");
                foreach (var script in ScriptsByCategory[category])
                {
                    Console.WriteLine($"    - {script.Name}");
                }
            }
        }
    }
}