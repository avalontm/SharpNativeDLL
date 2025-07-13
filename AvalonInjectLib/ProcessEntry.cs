using System.Runtime.CompilerServices;

namespace AvalonInjectLib
{
    public class ProcessEntry : IDisposable
    {
        public uint ProcessId { get; }
        public IntPtr Handle { get; }
        private readonly IntPtr _moduleBase;

        public ModuleBaseWrapper ModuleBase => new ModuleBaseWrapper(this, _moduleBase);

        public bool IsOpen => ProcessManager.IsOpen(Handle);

        public ProcessEntry(uint processId, IntPtr hProcess, IntPtr moduleBase)
        {
            ProcessId = processId;
            Handle = hProcess;
            _moduleBase = moduleBase;
        }

        #region Read Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Read<T>(IntPtr address) where T : unmanaged
        {
            return MemoryManager.Read<T>(Handle, address);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Read<T>(IntPtr address, params int[] offsets) where T : unmanaged
        {
            return MemoryManager.Read<T>(Handle, address, offsets);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Read<T>(CalculatedAddress calculatedAddress) where T : unmanaged
        {
            return MemoryManager.ReadDirect<T>(Handle, calculatedAddress.Address);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Read<T>(CalculatedAddress calculatedAddress, params int[] offsets) where T : unmanaged
        {
            return MemoryManager.Read<T>(Handle, calculatedAddress.Address, offsets);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Read<T>(int offset) where T : unmanaged
        {
            return MemoryManager.Read<T>(Handle, _moduleBase + offset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Read<T>(int offset, params int[] offsets) where T : unmanaged
        {
            return MemoryManager.Read<T>(Handle, _moduleBase + offset, offsets);
        }

        #endregion

        #region ReadString Methods

        public string ReadString(IntPtr address, int maxLength = 256, bool unicode = false)
        {
            return MemoryManager.ReadString(Handle, address, maxLength, unicode);
        }

        public string ReadString(IntPtr address, int offset, int maxLength = 256, bool unicode = false)
        {
            return MemoryManager.ReadString(Handle, address + offset, maxLength, unicode);
        }

        public string ReadString(IntPtr address, int maxLength = 256, bool unicode = false, params int[] offsets)
        {
            return MemoryManager.ReadString(Handle, address, maxLength, unicode, offsets);
        }

        public string ReadString(CalculatedAddress calculatedAddress, int maxLength = 256, bool unicode = false)
        {
            return MemoryManager.ReadString(Handle, calculatedAddress.Address, maxLength, unicode);
        }

        public string ReadString(CalculatedAddress calculatedAddress, int offset, int maxLength = 256, bool unicode = false)
        {
            return MemoryManager.ReadString(Handle, calculatedAddress.Address + offset, maxLength, unicode);
        }

        public string ReadString(CalculatedAddress calculatedAddress, int maxLength = 256, bool unicode = false, params int[] offsets)
        {
            return MemoryManager.ReadString(Handle, calculatedAddress.Address, maxLength, unicode, offsets);
        }

        public string ReadString(int offset, int maxLength = 256, bool unicode = false)
        {
            return MemoryManager.ReadString(Handle, _moduleBase + offset, maxLength, unicode);
        }

        public string ReadString(int offset, int maxLength = 256, bool unicode = false, params int[] offsets)
        {
            return MemoryManager.ReadString(Handle, _moduleBase + offset, maxLength, unicode, offsets);
        }

        #endregion

        #region Write Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write<T>(IntPtr address, T value) where T : unmanaged
        {
            MemoryManager.Write(Handle, address, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write<T>(IntPtr address, T value, params int[] offsets) where T : unmanaged
        {
            MemoryManager.Write(Handle, address, value, offsets);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write<T>(CalculatedAddress calculatedAddress, T value) where T : unmanaged
        {
            MemoryManager.Write(Handle, calculatedAddress.Address, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write<T>(CalculatedAddress calculatedAddress, T value, params int[] offsets) where T : unmanaged
        {
            MemoryManager.Write(Handle, calculatedAddress.Address, value, offsets);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write<T>(int offset, T value) where T : unmanaged
        {
            MemoryManager.Write(Handle, _moduleBase + offset, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write<T>(int offset, T value, params int[] offsets) where T : unmanaged
        {
            MemoryManager.Write(Handle, _moduleBase + offset, value, offsets);
        }

        #endregion

        #region WriteString Methods

        public void WriteString(IntPtr address, string value, bool unicode = false)
        {
            MemoryManager.WriteString(Handle, address, value, unicode);
        }

        public void WriteString(IntPtr address, int offset, string value, bool unicode = false)
        {
            MemoryManager.WriteString(Handle, address + offset, value, unicode);
        }

        public void WriteString(IntPtr address, string value, bool unicode = false, params int[] offsets)
        {
            MemoryManager.WriteString(Handle, address, value, unicode, offsets);
        }

        public void WriteString(CalculatedAddress calculatedAddress, string value, bool unicode = false)
        {
            MemoryManager.WriteString(Handle, calculatedAddress.Address, value, unicode);
        }

        public void WriteString(CalculatedAddress calculatedAddress, int offset, string value, bool unicode = false)
        {
            MemoryManager.WriteString(Handle, calculatedAddress.Address + offset, value, unicode);
        }

        public void WriteString(CalculatedAddress calculatedAddress, string value, bool unicode = false, params int[] offsets)
        {
            MemoryManager.WriteString(Handle, calculatedAddress.Address, value, unicode, offsets);
        }

        public void WriteString(int offset, string value, bool unicode = false)
        {
            MemoryManager.WriteString(Handle, _moduleBase + offset, value, unicode);
        }

        public void WriteString(int offset, string value, bool unicode = false, params int[] offsets)
        {
            MemoryManager.WriteString(Handle, _moduleBase + offset, value, unicode, offsets);
        }

        #endregion

        public void Dispose()
        {
            
        }
    }
}