using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvalonInjectLib
{
    public static class LuaRegistrationExtensions
    {
        public static void RegisterObject<T>(this Script script, T obj, string tableName) where T : class
        {
            var table = new Table(script);

            // Registrar métodos públicos automáticamente
            var methods = typeof(T).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                .Where(m => !m.IsSpecialName && m.DeclaringType != typeof(object));

            foreach (var method in methods)
            {
                var methodName = method.Name;

                // Crear delegate básico para métodos simples
                if (method.ReturnType == typeof(void))
                {
                    var parameters = method.GetParameters();
                    if (parameters.Length == 0)
                    {
                        table[methodName] = (Action)(() => method.Invoke(obj, null));
                    }
                    else if (parameters.Length == 1 && parameters[0].ParameterType == typeof(string))
                    {
                        table[methodName] = (Action<string>)(arg => method.Invoke(obj, new object[] { arg }));
                    }
                }
                else if (method.ReturnType == typeof(string) && method.GetParameters().Length == 0)
                {
                    table[methodName] = (Func<string>)(() => (string)method.Invoke(obj, null));
                }
            }

            script.Globals[tableName] = table;
        }
    }
}
