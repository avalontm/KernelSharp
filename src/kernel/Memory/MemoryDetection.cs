using Kernel.Diagnostics;
using System.Runtime.InteropServices;
using System;

namespace Kernel.Memory
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct MemoryMapEntry
    {
        public ulong BaseAddress;
        public ulong Length;
        public uint Type;
        public uint Reserved;
    }

    unsafe class MemoryDetection
    {
        // External assembly functions
        [DllImport("*", EntryPoint = "_detect_memory")]
        private static extern void DetectMemory();

        [DllImport("*", EntryPoint = "_get_memory_map")]
        private static extern IntPtr GetMemoryMap(out int entryCount);

        [DllImport("*", EntryPoint = "_get_total_memory")]
        private static extern ulong GetTotalMemory();

        [DllImport("*", EntryPoint = "_get_lower_memory")]
        private static extern uint GetLowerMemory();

        [DllImport("*", EntryPoint = "_get_upper_memory")]
        private static extern uint GetUpperMemory();

        public static void InitializeMemoryDetection()
        {
            SerialDebug.Info("InitializeMemoryDetection");
            // Detect memory
            DetectMemory();

            // Get memory details
            uint lowerMemory = GetLowerMemory();
            uint upperMemory = GetUpperMemory();
            ulong totalMemory = GetTotalMemory();

            SerialDebug.Info($"Lower Memory: {lowerMemory.ToString()} KB");
            SerialDebug.Info($"Upper Memory: {upperMemory.ToString()} KB");
            SerialDebug.Info($"Total Memory: {(totalMemory / (1024 * 1024)).ToString()} MB");

            // Get memory map
            int entryCount;
            IntPtr memoryMapPtr = GetMemoryMap(out entryCount);

            // Process memory map entries
            MemoryMapEntry* entries = (MemoryMapEntry*)memoryMapPtr;
            for (int i = 0; i < entryCount; i++)
            {
                MemoryMapEntry entry = entries[i];
                SerialDebug.Info($"Region {i}: " +
                    $"Base: 0x{entry.BaseAddress.ToStringHex()}, " +
                    $"Length: {entry.Length.ToString()} bytes, " +
                    $"Type: {entry.Type.ToString()}");
            }
        }
    }
}