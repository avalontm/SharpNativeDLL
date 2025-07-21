using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvalonInjectLib.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class RuntimeExportAttribute : Attribute
    {
        public string Name { get; }

        public RuntimeExportAttribute(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public RuntimeExportAttribute()
        {
            Name = string.Empty;
        }
    }
}
