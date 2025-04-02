using System;

namespace Kernel
{
    public static unsafe class PageTable
    {
        public enum PageSize
        {
            Small = 4096,
            Large = 0x200000 // 2MB
        }

        public static ulong* PML4;

        internal static void Initialize()
        {
            Console.WriteLine("[PageTable] Initializing...");

            // Usar la tabla de páginas creada por el loader
            PML4 = (ulong*)Native.ReadCR3();

            Console.WriteLine($"[PageTable] PML4 at 0x{((ulong)PML4).ToStringHex()}");

            // Mapear memoria adicional si es necesario
            MapKernelSpace();
        }

        private static void MapKernelSpace()
        {
            Console.WriteLine("[PageTable] Mapping kernel space...");

            // Mapear los primeros 4GB (usando páginas de 2MB para eficiencia)
            for (ulong virt = 0; virt < 0x1_0000_0000; virt += (ulong)PageSize.Large)
            {
                // Mapeo identidad
                Map(virt, virt, PageSize.Large);
            }

            Console.WriteLine("[PageTable] Basic mapping completed");
        }

        public static ulong* GetPage(ulong virtualAddress, PageSize pageSize = PageSize.Small)
        {
            if ((virtualAddress % (ulong)pageSize) != 0)
            {
                Console.WriteLine($"[ERROR] Invalid address alignment: 0x{((ulong)virtualAddress).ToStringHex()}");
                return null;
            }

            ulong pml4Index = (virtualAddress >> 39) & 0x1FF;
            ulong pdpIndex = (virtualAddress >> 30) & 0x1FF;
            ulong pdIndex = (virtualAddress >> 21) & 0x1FF;

            // Para páginas de 2MB, devolvemos la entrada en PD
            if (pageSize == PageSize.Large)
            {
                ulong* _pdp = Next(PML4, pml4Index);
                ulong* _pd = Next(_pdp, pdpIndex);
                return &_pd[pdIndex];
            }

            // Para páginas de 4KB
            ulong ptIndex = (virtualAddress >> 12) & 0x1FF;
            ulong* pdp = Next(PML4, pml4Index);
            ulong* pd = Next(pdp, pdpIndex);
            ulong* pt = Next(pd, pdIndex);
            return &pt[ptIndex];
        }

        public static void Map(ulong virtualAddress, ulong physicalAddress, PageSize pageSize = PageSize.Small)
        {
            ulong* pageEntry = GetPage(virtualAddress, pageSize);
            if (pageEntry == null)
            {
                Console.WriteLine($"[ERROR] Failed to get page entry for 0x{((ulong)virtualAddress).ToStringHex()}");
                return;
            }

            ulong flags = 0b11; // Present + Writable
            if (pageSize == PageSize.Large)
            {
                flags |= 1UL << 7; // Page Size flag for 2MB pages
            }

            *pageEntry = (physicalAddress & ~((ulong)0xFFF)) | flags;
            Native.Invlpg(virtualAddress);
        }

        public static ulong* Next(ulong* directory, ulong entry)
        {
            if ((directory[entry] & 0x1) != 0)
            {
                return (ulong*)(directory[entry] & 0x000F_FFFF_FFFF_F000);
            }

            // Asignar nueva tabla
            ulong* newTable = (ulong*)Allocator.Allocate(0x1000);
            if (newTable == null)
            {
                Console.WriteLine($"[ERROR] Failed to allocate new page table");
                return null;
            }

            Native.Stosb(newTable, 0, 0x1000);
            directory[entry] = ((ulong)newTable & 0x000F_FFFF_FFFF_F000) | 0b11;

            return newTable;
        }
    }
}