namespace MDriver.Native
{
    using System.Runtime.InteropServices;

    using MDriver.Native.Enums.Memory;

    [StructLayout(LayoutKind.Sequential)] 
    public struct MemoryBasicInformation
    { 
        public ulong BaseAddress;
        public ulong AllocationBase;
        public MemoryPagePermissions AllocationProtect;
        public ulong RegionSize;
        public MemoryPageState State;
        public MemoryPagePermissions Protect;
        public MemoryPageType Type;

        public ulong EndAddress
        {
            get
            {
                return this.BaseAddress + this.RegionSize;
            }
        }
    }
}
