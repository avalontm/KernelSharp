using Kernel.Diagnostics;
using System;
using System.Runtime.InteropServices;

namespace Kernel.Hardware
{
    /// <summary>
    /// Represents CPU feature flags from CPUID
    /// </summary>
    [Flags]
    public enum CPUFeatures : ulong
    {
        SSE = 1 << 0,
        SSE2 = 1 << 1,
        SSE3 = 1 << 2,
        SSSE3 = 1 << 3,
        SSE41 = 1 << 4,
        SSE42 = 1 << 5,
        AVX = 1 << 6,
        AVX2 = 1 << 7,
        FMA = 1 << 8,
        POPCNT = 1 << 9,
        AES = 1 << 10,
        VMX = 1 << 11,          // Intel VT-x
        SVM = 1 << 12,          // AMD-V
        X64 = 1 << 13,          // Long mode (64-bit)
        NX = 1 << 14,           // No-Execute bit
        RDRAND = 1 << 15,       // Hardware random number
        RDSEED = 1 << 16,       // Enhanced hardware random number
        BMI1 = 1 << 17,
        BMI2 = 1 << 18,
        ADX = 1 << 19,
        PCLMULQDQ = 1 << 20,
        XSAVE = 1 << 21,
        OSXSAVE = 1 << 22,
        RDTSCP = 1 << 23,
        LAHF_LM = 1 << 24,
        ABM = 1 << 25,          // Advanced bit manipulation
        MOVBE = 1 << 26,
        APIC = 1 << 27          // Local APIC support
    }

    /// <summary>
    /// CPU vendor identification
    /// </summary>
    public enum CPUVendor
    {
        Unknown,
        Intel,
        AMD,
        Centaur,
        Cyrix,
        Transmeta,
        NSC,
        VIA,
        Other
    }

    /// <summary>
    /// Information about a single CPU core
    /// </summary>
    public struct CPUCoreInfo
    {
        public byte ApicID;          // Local APIC ID of this core
        public byte CoreID;          // Core identifier
        public byte PhysicalID;      // Physical package ID
        public bool IsBootProcessor; // Is this the bootstrap processor?
        public bool IsEnabled;       // Is this core enabled?
    }

    /// <summary>
    /// Main CPU information structure
    /// </summary>
    public class CPUInfo
    {
        public string VendorString { get; internal set; }
        public string BrandString { get; internal set; }
        public CPUVendor Vendor { get; internal set; }
        public uint Family { get; internal set; }
        public uint Model { get; internal set; }
        public uint Stepping { get; internal set; }
        public uint PhysicalCores { get; internal set; }
        public uint LogicalCores { get; internal set; }
        public CPUFeatures Features { get; internal set; }
        public uint MaxBasicCpuId { get; internal set; }
        public uint MaxExtendedCpuId { get; internal set; }
        public bool SupportsAPIC { get; internal set; }
        public bool SupportsX2APIC { get; internal set; }
        public CPUCoreInfo[] Cores { get; internal set; }

        public CPUInfo()
        {
            VendorString = "Unknown";
            BrandString = "Unknown";
            Vendor = CPUVendor.Unknown;
            Family = 0;
            Model = 0;
            Stepping = 0;
            PhysicalCores = 1;
            LogicalCores = 1;
            Features = 0;
            MaxBasicCpuId = 0;
            MaxExtendedCpuId = 0;
            SupportsAPIC = false;
            SupportsX2APIC = false;
            Cores = new CPUCoreInfo[0];
        }
    }

    /// <summary>
    /// Detects CPU information and available cores
    /// </summary>
    public static unsafe class CPUDetector
    {
        private static CPUInfo _cpuInfo;

        /// <summary>
        /// Initialize CPU detection
        /// </summary>
        public static void Initialize()
        {
            SerialDebug.Info("Detecting CPU features and cores...");

            _cpuInfo = new CPUInfo();

            // CPUID Leaf 0: Vendor ID
            DetectVendor();

            // CPUID Leaf 1: Family, Model, Stepping
            DetectFamilyModel();

            // CPUID Leaf 2+ and Extended: Features
            DetectFeatures();

            // CPUID Extended: Brand String
            DetectBrandString();

            // Detect logical and physical cores
            DetectCores();

            // Display CPU information
            PrintCPUInfo();
        }

        /// <summary>
        /// Gets the CPU information
        /// </summary>
        public static CPUInfo GetCPUInfo()
        {
            return _cpuInfo;
        }

        private static void DetectVendor()
        {
            SerialDebug.Info("DetectVendor");
            uint eax = 0, ebx = 0, ecx = 0, edx = 0;
            Cpuid(0, ref eax, ref ebx, ref ecx, ref edx);

            // Save the maximum supported CPUID leaf
            _cpuInfo.MaxBasicCpuId = eax;

            // Build vendor string from EBX, EDX, ECX
            byte* vendorPtr = stackalloc byte[13];
            *(uint*)(vendorPtr) = ebx;
            *(uint*)(vendorPtr + 4) = edx;
            *(uint*)(vendorPtr + 8) = ecx;
            vendorPtr[12] = 0; // Null terminator

            _cpuInfo.VendorString = new string((char*)vendorPtr);

            // Determine vendor
            if (_cpuInfo.VendorString == "GenuineIntel")
                _cpuInfo.Vendor = CPUVendor.Intel;
            else if (_cpuInfo.VendorString == "AuthenticAMD")
                _cpuInfo.Vendor = CPUVendor.AMD;
            else if (_cpuInfo.VendorString == "CentaurHauls")
                _cpuInfo.Vendor = CPUVendor.Centaur;
            else if (_cpuInfo.VendorString == "CyrixInstead")
                _cpuInfo.Vendor = CPUVendor.Cyrix;
            else if (_cpuInfo.VendorString.Contains("TransmetaCPU"))
                _cpuInfo.Vendor = CPUVendor.Transmeta;
            else if (_cpuInfo.VendorString == "Geode by NSC")
                _cpuInfo.Vendor = CPUVendor.NSC;
            else
                _cpuInfo.Vendor = CPUVendor.Other;

            // Get maximum extended CPUID
            Cpuid(0x80000000, ref eax, ref ebx, ref ecx, ref edx);
            _cpuInfo.MaxExtendedCpuId = eax;
        }

        private static void DetectFamilyModel()
        {
            if (_cpuInfo.MaxBasicCpuId >= 1)
            {
                uint eax = 0, ebx = 0, ecx = 0, edx = 0;
                Cpuid(1, ref eax, ref ebx, ref ecx, ref edx);

                // Extract family, model, stepping from EAX
                uint stepping = eax & 0xF;
                uint model = (eax >> 4) & 0xF;
                uint family = (eax >> 8) & 0xF;
                uint extModel = (eax >> 16) & 0xF;
                uint extFamily = (eax >> 20) & 0xFF;

                // Calculate actual family and model values
                if (family == 0xF)
                    family += extFamily;

                if (family == 0x6 || family == 0xF)
                    model += (extModel << 4);

                _cpuInfo.Family = family;
                _cpuInfo.Model = model;
                _cpuInfo.Stepping = stepping;

                // Check for APIC support
                _cpuInfo.SupportsAPIC = (edx & (1 << 9)) != 0;
            }
        }

        private static void DetectFeatures()
        {
            CPUFeatures features = 0;

            if (_cpuInfo.MaxBasicCpuId >= 1)
            {
                uint eax = 0, ebx = 0, ecx = 0, edx = 0;
                Cpuid(1, ref eax, ref ebx, ref ecx, ref edx);

                // Feature bits from EDX
                if ((edx & (1 << 25)) != 0) features |= CPUFeatures.SSE;
                if ((edx & (1 << 26)) != 0) features |= CPUFeatures.SSE2;
                if ((edx & (1 << 9)) != 0) features |= CPUFeatures.APIC;

                // Feature bits from ECX
                if ((ecx & (1 << 0)) != 0) features |= CPUFeatures.SSE3;
                if ((ecx & (1 << 9)) != 0) features |= CPUFeatures.SSSE3;
                if ((ecx & (1 << 19)) != 0) features |= CPUFeatures.SSE41;
                if ((ecx & (1 << 20)) != 0) features |= CPUFeatures.SSE42;
                if ((ecx & (1 << 28)) != 0) features |= CPUFeatures.AVX;
                if ((ecx & (1 << 12)) != 0) features |= CPUFeatures.FMA;
                if ((ecx & (1 << 23)) != 0) features |= CPUFeatures.POPCNT;
                if ((ecx & (1 << 25)) != 0) features |= CPUFeatures.AES;
                if ((ecx & (1 << 5)) != 0) features |= CPUFeatures.VMX;
                if ((ecx & (1 << 30)) != 0) features |= CPUFeatures.RDRAND;
                if ((ecx & (1 << 1)) != 0) features |= CPUFeatures.PCLMULQDQ;
                if ((ecx & (1 << 26)) != 0) features |= CPUFeatures.XSAVE;
                if ((ecx & (1 << 27)) != 0) features |= CPUFeatures.OSXSAVE;
            }

            // Extended features
            if (_cpuInfo.MaxExtendedCpuId >= 0x80000001)
            {
                uint eax = 0, ebx = 0, ecx = 0, edx = 0;
                Cpuid(0x80000001, ref eax, ref ebx, ref ecx, ref edx);

                // Feature bits from EDX
                if ((edx & (1 << 29)) != 0) features |= CPUFeatures.X64;
                if ((edx & (1 << 20)) != 0) features |= CPUFeatures.NX;
                if ((edx & (1 << 27)) != 0) features |= CPUFeatures.RDTSCP;

                // Feature bits from ECX
                if ((ecx & (1 << 2)) != 0) features |= CPUFeatures.SVM;
                if ((ecx & (1 << 0)) != 0) features |= CPUFeatures.LAHF_LM;
                if ((ecx & (1 << 5)) != 0) features |= CPUFeatures.ABM;
            }

            // Check for AVX2, BMI1, BMI2, etc. (CPUID leaf 7)
            if (_cpuInfo.MaxBasicCpuId >= 7)
            {
                uint eax = 0, ebx = 0, ecx = 0, edx = 0;
                Cpuid(7, ref eax, ref ebx, ref ecx, ref edx);

                // Feature bits from EBX
                if ((ebx & (1 << 5)) != 0) features |= CPUFeatures.AVX2;
                if ((ebx & (1 << 3)) != 0) features |= CPUFeatures.BMI1;
                if ((ebx & (1 << 8)) != 0) features |= CPUFeatures.BMI2;
                if ((ebx & (1 << 19)) != 0) features |= CPUFeatures.ADX;
                if ((ebx & (1 << 18)) != 0) features |= CPUFeatures.RDSEED;
                if ((ebx & (1 << 22)) != 0) features |= CPUFeatures.MOVBE;

                // Check for x2APIC support
                if ((ecx & (1 << 21)) != 0)
                    _cpuInfo.SupportsX2APIC = true;
            }

            _cpuInfo.Features = features;
        }

        private static void DetectBrandString()
        {
            if (_cpuInfo.MaxExtendedCpuId >= 0x80000004)
            {
                byte* brandString = stackalloc byte[49];

                for (uint i = 0; i < 3; i++)
                {
                    uint eax = 0, ebx = 0, ecx = 0, edx = 0;
                    Cpuid(0x80000002 + i, ref eax, ref ebx, ref ecx, ref edx);

                    // Copy 16 bytes of brand string (4 bytes from each register)
                    *(uint*)(brandString + i * 16) = eax;
                    *(uint*)(brandString + i * 16 + 4) = ebx;
                    *(uint*)(brandString + i * 16 + 8) = ecx;
                    *(uint*)(brandString + i * 16 + 12) = edx;
                }

                brandString[48] = 0; // Null terminator
                _cpuInfo.BrandString = new string((char*)brandString);

                // Trim leading and trailing spaces
                _cpuInfo.BrandString = _cpuInfo.BrandString.Trim();
            }
        }

        private static void DetectCores()
        {
            uint logicalCores = 1;
            uint physicalCores = 1;

            // Get logical processor count from CPUID leaf 1 (Intel & AMD)
            if (_cpuInfo.MaxBasicCpuId >= 1)
            {
                uint eax = 0, ebx = 0, ecx = 0, edx = 0;
                Cpuid(1, ref eax, ref ebx, ref ecx, ref edx);

                // EBX[23:16] is the maximum number of addressable IDs for logical processors
                logicalCores = ((ebx >> 16) & 0xFF);
            }

            // Get physical core count
            if (_cpuInfo.Vendor == CPUVendor.Intel)
            {
                // Intel-specific method
                if (_cpuInfo.MaxBasicCpuId >= 4)
                {
                    uint eax = 0, ebx = 0, ecx = 0, edx = 0;

                    // EAX[31:26] + 1 is cores per package
                    ecx = 0; // Thread ID = 0
                    Cpuid(4, ref eax, ref ebx, ref ecx, ref edx);

                    physicalCores = ((eax >> 26) & 0x3F) + 1;
                }
            }
            else if (_cpuInfo.Vendor == CPUVendor.AMD)
            {
                // AMD-specific method
                if (_cpuInfo.MaxExtendedCpuId >= 0x80000008)
                {
                    uint eax = 0, ebx = 0, ecx = 0, edx = 0;
                    Cpuid(0x80000008, ref eax, ref ebx, ref ecx, ref edx);

                    // ECX[7:0] + 1 is the number of cores
                    physicalCores = (ecx & 0xFF) + 1;
                }
            }

            // Make sure we don't report 0 cores
            if (logicalCores == 0) logicalCores = 1;
            if (physicalCores == 0) physicalCores = 1;

            // Logical cores should never be less than physical cores
            if (logicalCores < physicalCores)
                logicalCores = physicalCores;

            _cpuInfo.LogicalCores = logicalCores;
            _cpuInfo.PhysicalCores = physicalCores;

            // Get detailed information about each core
            // This is a simplified approach, a real implementation would
            // need to enumerate APIC IDs, topology extensions, etc.
            _cpuInfo.Cores = new CPUCoreInfo[logicalCores];

            for (byte i = 0; i < logicalCores; i++)
            {
                _cpuInfo.Cores[i] = new CPUCoreInfo
                {
                    ApicID = i,
                    CoreID = (byte)(i % physicalCores),
                    PhysicalID = (byte)(i / physicalCores),
                    IsBootProcessor = (i == 0),
                    IsEnabled = (i == 0) // Only BSP is enabled by default
                };
            }
        }

        private static void PrintCPUInfo()
        {
            SerialDebug.Info($"CPU: {_cpuInfo.BrandString}");
            SerialDebug.Info($"Vendor: {_cpuInfo.VendorString}");
            SerialDebug.Info($"Family: {_cpuInfo.Family}, Model: {_cpuInfo.Model}, Stepping: {_cpuInfo.Stepping}");
            SerialDebug.Info($"Cores: {_cpuInfo.PhysicalCores} physical, {_cpuInfo.LogicalCores} logical");

            // Print features
            SerialDebug.Info("Features:");
            if ((_cpuInfo.Features & CPUFeatures.X64) != 0) SerialDebug.Info("  - 64-bit (Long Mode)");
            if ((_cpuInfo.Features & CPUFeatures.APIC) != 0) SerialDebug.Info("  - APIC");
            if ((_cpuInfo.Features & CPUFeatures.NX) != 0) SerialDebug.Info("  - NX (Execute Disable)");
            if ((_cpuInfo.Features & CPUFeatures.SSE) != 0) SerialDebug.Info("  - SSE");
            if ((_cpuInfo.Features & CPUFeatures.SSE2) != 0) SerialDebug.Info("  - SSE2");
            if ((_cpuInfo.Features & CPUFeatures.SSE3) != 0) SerialDebug.Info("  - SSE3");
            if ((_cpuInfo.Features & CPUFeatures.SSSE3) != 0) SerialDebug.Info("  - SSSE3");
            if ((_cpuInfo.Features & CPUFeatures.SSE41) != 0) SerialDebug.Info("  - SSE4.1");
            if ((_cpuInfo.Features & CPUFeatures.SSE42) != 0) SerialDebug.Info("  - SSE4.2");
            if ((_cpuInfo.Features & CPUFeatures.AVX) != 0) SerialDebug.Info("  - AVX");
            if ((_cpuInfo.Features & CPUFeatures.AVX2) != 0) SerialDebug.Info("  - AVX2");
            if ((_cpuInfo.Features & CPUFeatures.VMX) != 0) SerialDebug.Info("  - Intel VT-x");
            if ((_cpuInfo.Features & CPUFeatures.SVM) != 0) SerialDebug.Info("  - AMD-V");

            // Print core information
            SerialDebug.Info("Cores detected:");
            for (int i = 0; i < _cpuInfo.Cores.Length; i++)
            {
                SerialDebug.Info($"  - Core {i}: APIC ID={_cpuInfo.Cores[i].ApicID}, " +
                           $"Core ID={_cpuInfo.Cores[i].CoreID}, " +
                           $"Package={_cpuInfo.Cores[i].PhysicalID}, " +
                           $"{(_cpuInfo.Cores[i].IsBootProcessor ? "BSP" : "AP")}, " +
                           $"{(_cpuInfo.Cores[i].IsEnabled ? "Enabled" : "Disabled")}");
            }
        }

        /// <summary>
        /// Execute CPUID instruction
        /// </summary>
        [DllImport("*", EntryPoint = "_CPUID")]
        private static extern void Cpuid(uint leaf, ref uint eax, ref uint ebx, ref uint ecx, ref uint edx);
    }
}