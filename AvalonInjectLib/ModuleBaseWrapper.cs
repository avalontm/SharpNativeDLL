using System.Diagnostics;

namespace AvalonInjectLib
{
    /// <summary>
    /// Wrapper seguro para ModuleBase que preserva la dirección original
    /// </summary>
    public struct ModuleBaseWrapper
    {
        private readonly IntPtr _originalBase;
        public Process Process { get; }

        public IntPtr OriginalBase => _originalBase;

        internal ModuleBaseWrapper(Process process, IntPtr baseAddress)
        {
            Process = process;
            _originalBase = baseAddress;
        }

        // Permite sumar manteniendo el original
        public CalculatedAddress Add(int offset)
        {
            return new CalculatedAddress(Process, IntPtr.Add(_originalBase, offset));
        }

        public CalculatedAddress Add(long offset)
        {
            return new CalculatedAddress(Process, new IntPtr(_originalBase.ToInt64() + offset));
        }

        // Sobrecarga del operador + para mantener sintaxis limpia
        public static CalculatedAddress operator +(ModuleBaseWrapper wrapper, int offset) => wrapper.Add(offset);
        public static CalculatedAddress operator +(ModuleBaseWrapper wrapper, long offset) => wrapper.Add(offset);

        public override string ToString() => $"0x{_originalBase.ToInt64():X}";
    }
}
