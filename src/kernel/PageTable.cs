using System;
using Kernel.Diagnostics;

namespace Kernel
{
    /// <summary>
    /// Manages virtual memory paging for the kernel
    /// </summary>
    public static unsafe class PageTable
    {
        // Constants for page attributes
        private const ulong PAGE_PRESENT = 1UL;
        private const ulong PAGE_WRITABLE = 1UL << 1;
        private const ulong PAGE_USER = 1UL << 2;
        private const ulong PAGE_WRITE_THROUGH = 1UL << 3;
        private const ulong PAGE_CACHE_DISABLE = 1UL << 4;
        private const ulong PAGE_ACCESSED = 1UL << 5;
        private const ulong PAGE_DIRTY = 1UL << 6;
        private const ulong PAGE_LARGE = 1UL << 7;
        private const ulong PAGE_GLOBAL = 1UL << 8;
        private const ulong PAGE_NX = 1UL << 63;  // No Execute bit

        // Address mask to extract physical address from page entry
        private const ulong ADDRESS_MASK = 0x000F_FFFF_FFFF_F000;

        // Standard page sizes
        public enum PageSize
        {
            Small = 4096,        // 4KB
            Large = 0x200000,    // 2MB
            Huge = 0x40000000    // 1GB (for PDPT entries)
        }

        // Page access rights
        [Flags]
        public enum PageFlags : ulong
        {
            None = 0,
            Present = PAGE_PRESENT,
            Writable = PAGE_WRITABLE,
            User = PAGE_USER,
            WriteThrough = PAGE_WRITE_THROUGH,
            CacheDisable = PAGE_CACHE_DISABLE,
            Accessed = PAGE_ACCESSED,
            Dirty = PAGE_DIRTY,
            LargePage = PAGE_LARGE,
            Global = PAGE_GLOBAL,
            NoExecute = PAGE_NX,

            // Common combinations
            KernelRO = Present,                               // Kernel read-only
            KernelRW = Present | Writable,                    // Kernel read-write
            KernelRX = Present,                               // Kernel read-execute (no NX)
            KernelRWX = Present | Writable,                   // Kernel read-write-execute (no NX)
            UserRO = Present | User,                          // User read-only
            UserRW = Present | Writable | User,               // User read-write
            UserRX = Present | User,                          // User read-execute (no NX)
            UserRWX = Present | Writable | User,              // User read-write-execute (no NX)
            Uncached = Present | Writable | CacheDisable,     // For memory-mapped I/O
        }

        // The top-level page table (PML4)
        public static ulong* PML4;

        /// <summary>
        /// Initializes the page table system using the loader's setup
        /// </summary>
        internal static void Initialize()
        {
            Console.WriteLine("[PageTable] Initializing page management...");

            // Use the page table created by the loader
            PML4 = (ulong*)Native.ReadCR3();
            Console.WriteLine($"[PageTable] PML4 at 0x{((ulong)PML4).ToStringHex()}");

            // Map additional memory as needed
            MapKernelSpace();

            // Optionally set up memory protection
            SetupMemoryProtection();

            Console.WriteLine("[PageTable] Page management initialized successfully");
        }

        /// <summary>
        /// Maps the kernel space and ensures all necessary memory is accessible
        /// </summary>
        private static void MapKernelSpace()
        {
            Console.WriteLine("[PageTable] Mapping kernel space...");

            ulong totalMapped = 0;

            // Map the first 4GB (using 2MB pages for efficiency)
            // This ensures all physical memory is accessible via identity mapping
            for (ulong virt = 0; virt < 0x1_0000_0000; virt += (ulong)PageSize.Large)
            {
                // Only map if not already mapped
                if (!IsPageMapped(virt, PageSize.Large))
                {
                    Map(virt, virt, PageSize.Large, PageFlags.KernelRW);
                    totalMapped += (ulong)PageSize.Large;
                }
            }

            Console.WriteLine($"[PageTable] Mapped {totalMapped / 1024 / 1024}MB of kernel space");
        }

        /// <summary>
        /// Sets up memory protection for critical areas
        /// </summary>
        private static void SetupMemoryProtection()
        {
            // Example: Mark the first 1MB as read-only (contains BIOS/bootloader data)
            for (ulong addr = 0; addr < 0x100000; addr += (ulong)PageSize.Small)
            {
                // Only change flags, don't remap
                ulong* page = GetPage(addr);
                if (page != null && (*page & PAGE_PRESENT) != 0)
                {
                    // Clear writable flag
                    *page &= ~PAGE_WRITABLE;
                    Native.Invlpg(addr);
                }
            }

            // You could mark kernel code as non-writable here as well
            Console.WriteLine("[PageTable] Memory protection configured");
        }

        /// <summary>
        /// Gets a pointer to the page table entry for a given virtual address
        /// </summary>
        public static ulong* GetPage(ulong virtualAddress, PageSize pageSize = PageSize.Small)
        {
            // Check address alignment
            if ((virtualAddress % (ulong)pageSize) != 0)
            {
                Console.WriteLine($"[ERROR] Invalid address alignment 0x{virtualAddress.ToStringHex()}, must be {(ulong)pageSize} bytes aligned");
                return null;
            }

            // Extract indexes for each level
            ulong pml4Index = (virtualAddress >> 39) & 0x1FF;
            ulong pdpIndex = (virtualAddress >> 30) & 0x1FF;
            ulong pdIndex = (virtualAddress >> 21) & 0x1FF;

            // Traverse the page tables
            ulong* pdp = Next(PML4, pml4Index);
            if (pdp == null) return null;

            // Check for 1GB pages (if supported)
            if (pageSize == PageSize.Huge)
            {
                return &pdp[pdpIndex];
            }

            ulong* pd = Next(pdp, pdpIndex);
            if (pd == null) return null;

            // For 2MB pages, return PD entry
            if (pageSize == PageSize.Large)
            {
                return &pd[pdIndex];
            }

            // For 4KB pages, continue to PT
            ulong ptIndex = (virtualAddress >> 12) & 0x1FF;
            ulong* pt = Next(pd, pdIndex);
            if (pt == null) return null;

            return &pt[ptIndex];
        }

        /// <summary>
        /// Checks if a virtual address is already mapped
        /// </summary>
        public static bool IsPageMapped(ulong virtualAddress, PageSize pageSize = PageSize.Small)
        {
            ulong* page = GetPage(virtualAddress, pageSize);
            return page != null && (*page & PAGE_PRESENT) != 0;
        }

        /// <summary>
        /// Maps a virtual address to a physical address
        /// </summary>
        public static void Map(ulong virtualAddress, ulong physicalAddress,
                             PageSize pageSize = PageSize.Small,
                             PageFlags flags = PageFlags.KernelRW)
        {
            // Get the page entry
            ulong* pageEntry = GetPage(virtualAddress, pageSize);
            if (pageEntry == null)
            {
                Console.WriteLine($"[ERROR] Failed to get page entry for 0x{virtualAddress.ToStringHex()}");
                return;
            }

            // Set flags based on page size
            ulong pageFlags = (ulong)flags;
            if (pageSize == PageSize.Large)
            {
                pageFlags |= PAGE_LARGE;
            }
            else if (pageSize == PageSize.Huge)
            {
                pageFlags |= PAGE_LARGE; // Same bit is used for 1GB pages in PDPT
            }

            // Set the page entry
            *pageEntry = (physicalAddress & ADDRESS_MASK) | pageFlags;

            // Invalidate TLB for this address
            Native.Invlpg(virtualAddress);
        }

        /// <summary>
        /// Unmaps a virtual address
        /// </summary>
        public static void Unmap(ulong virtualAddress, PageSize pageSize = PageSize.Small)
        {
            ulong* pageEntry = GetPage(virtualAddress, pageSize);
            if (pageEntry != null && (*pageEntry & PAGE_PRESENT) != 0)
            {
                *pageEntry = 0; // Clear the entry
                Native.Invlpg(virtualAddress);
            }
        }

        /// <summary>
        /// Gets or creates the next level page table
        /// </summary>
        private static ulong* Next(ulong* directory, ulong index)
        {
            // Check if the entry is present
            if ((directory[index] & PAGE_PRESENT) != 0)
            {
                return (ulong*)(directory[index] & ADDRESS_MASK);
            }

            // Allocate new page table
            ulong* newTable = (ulong*)Allocator.malloc(0x1000);
            if (newTable == null)
            {
                Console.WriteLine("[ERROR] Failed to allocate new page table");
                return null;
            }

            // Zero the new table
            Native.Stosb(newTable, 0, 0x1000);

            // Set the entry (present + writable)
            directory[index] = ((ulong)newTable & ADDRESS_MASK) | PAGE_PRESENT | PAGE_WRITABLE;

            return newTable;
        }

        /// <summary>
        /// Gets the physical address mapped to a virtual address
        /// </summary>
        public static ulong GetPhysicalAddress(ulong virtualAddress, PageSize pageSize = PageSize.Small)
        {
            ulong* pageEntry = GetPage(virtualAddress, pageSize);
            if (pageEntry == null || (*pageEntry & PAGE_PRESENT) == 0)
            {
                return 0; // Not mapped
            }

            // Extract the physical address
            ulong physicalBase = *pageEntry & ADDRESS_MASK;

            // Add the offset within the page
            ulong offset = virtualAddress & ((ulong)pageSize - 1);

            return physicalBase + offset;
        }

        /// <summary>
        /// Changes the flags for an existing mapping
        /// </summary>
        public static bool SetPageFlags(ulong virtualAddress, PageFlags flags, PageSize pageSize = PageSize.Small)
        {
            ulong* pageEntry = GetPage(virtualAddress, pageSize);
            if (pageEntry == null || (*pageEntry & PAGE_PRESENT) == 0)
            {
                return false; // Page not mapped
            }

            // Keep the physical address, update flags
            ulong physAddr = *pageEntry & ADDRESS_MASK;
            ulong pageFlags = (ulong)flags;

            if (pageSize == PageSize.Large)
            {
                pageFlags |= PAGE_LARGE;
            }

            *pageEntry = physAddr | pageFlags;
            Native.Invlpg(virtualAddress);

            return true;
        }
    }
}