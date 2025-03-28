namespace Kernel
{
    public static unsafe class PageTable
    {
        public enum PageSize
        {
            Typical = 4096
        }

        public static uint* PageDirectory;  // Cambio de ulong* a uint*

        public static void Initialize()
        {
            PageDirectory = (uint*)SMP.SharedPageTable;

            // Limpiar directorio de páginas
            Native.Stosb(PageDirectory, 0, 0x1000);

            // Mapear primeros 4MB
            for (uint i = (uint)PageSize.Typical; i < 1024 * 1024 * 4; i += (uint)PageSize.Typical)
            {
                Map(i, i, PageSize.Typical);
            }

            // Escribir directorio de páginas en CR3
            Native.WriteCR3((uint)PageDirectory);
        }

        public static uint* GetPage(uint VirtualAddress, PageSize pageSize = PageSize.Typical)
        {
            if ((VirtualAddress % (uint)PageSize.Typical) != 0)
                Program.console.PrintLine("Invalid address");

            // Cálculo de índices para 32 bits
            uint pageDirectoryIndex = (VirtualAddress >> 22) & 0x3FF;
            uint pageTableIndex = (VirtualAddress >> 12) & 0x3FF;

            // Obtener tabla de páginas
            uint* pageTable = Next(PageDirectory, pageDirectoryIndex);

            return &pageTable[pageTableIndex];
        }

        public static void Map(uint VirtualAddress, uint PhysicalAddress, PageSize pageSize = PageSize.Typical)
        {
            if (pageSize == PageSize.Typical)
            {
                // Establecer entrada de página con permisos de lectura/escritura
                *GetPage(VirtualAddress, pageSize) = PhysicalAddress | 0b11;
            }

            // Invalidar entrada de página
            Native.Invlpg(PhysicalAddress);
        }

        public static uint* Next(uint* Directory, uint Entry)
        {
            uint* p = null;
            if ((Directory[Entry] & 0x01) != 0)
            {
                // Extraer dirección base de la entrada
                p = (uint*)(Directory[Entry] & 0xFFFFF000);
            }
            else
            {
                // Crear nueva tabla de páginas
                p = (uint*)Allocator.Allocate(0x1000);
                Native.Stosb(p, 0, 0x1000);

                // Establecer entrada con permisos
                Directory[Entry] = (((uint)p) & 0xFFFFF000) | 0b11;
            }
            return p;
        }
    }
}