using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace AvalonInjectLib
{
    internal static unsafe class EmbeddedResourceLoader
    {
        [UnconditionalSuppressMessage("AssemblyLoadTrimming", "IL2026")]
        public static byte[] LoadResource(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            try
            {
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null) return null;

                byte[] buffer = new byte[stream.Length];
                stream.ReadExactly(buffer);
                return buffer;
            }
            catch
            {
                return new byte[0];
            }
        }
    }
}