namespace AvalonInjectLib
{
    /// <summary>
    /// Estructura que representa una dirección de memoria calculada
    /// Se crea automáticamente cuando sumas ModuleBase + offset
    /// </summary>
    public struct CalculatedAddress
    {
        public IntPtr Address { get; }
        public ProcessEntry Process { get; }

        internal CalculatedAddress(ProcessEntry process, IntPtr address)
        {
            Process = process;
            Address = address;
        }

        // Conversión implícita a IntPtr para usar en Read()
        public static implicit operator IntPtr(CalculatedAddress calcAddr)
        {
            return calcAddr.Address;
        }

        // Permite seguir sumando más offsets
        public static CalculatedAddress operator +(CalculatedAddress calcAddr, int offset)
        {
            return new CalculatedAddress(calcAddr.Process, IntPtr.Add(calcAddr.Address, offset));
        }

        public static CalculatedAddress operator +(CalculatedAddress calcAddr, long offset)
        {
            return new CalculatedAddress(calcAddr.Process, new IntPtr(calcAddr.Address.ToInt64() + offset));
        }

        public override string ToString()
        {
            return $"0x{Address.ToInt64():X}";
        }
    }
}
