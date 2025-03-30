using System;
using System.Runtime.InteropServices;
using Kernel.Diagnostics;

namespace Kernel.Memory
{
    /// <summary>
    /// Administrador de memoria fisica sin dependencia de MultibootInfo
    /// </summary>
    public unsafe class PhysicalMemoryManager
    {
        // Tamaño de pagina estandar: 4KB
        public const uint PAGE_SIZE = 4096;

        // Limites de memoria predeterminados para sistemas de 32 bits
        private const uint DEFAULT_MEMORY_START = 0x100000;  // 1MB (después del area reservada para BIOS/Video)
        private const uint DEFAULT_MEMORY_SIZE = 0x1F00000;  // 31MB (asumimos 32MB total - 1MB reservado)

        // Limite maximo de memoria para sistemas de 32 bits (4GB)
        private const uint MAX_PHYSICAL_MEMORY = 0xFFFFFFFF;

        // Array de bits para seguimiento de paginas disponibles
        private static uint* _pageMap;

        // Número total de paginas fisicas disponibles
        private static uint _totalPages;

        // Número de paginas fisicas libres
        private static uint _freePages;

        // Dirección de inicio de memoria disponible
        private static uint _memoryStart;

        // Tamaño total de memoria disponible
        private static uint _memorySize;

        // Indica si el administrador ha sido inicializado
        private static bool _initialized = false;

        /// <summary>
        /// Inicializa el administrador de memoria fisica con valores predeterminados
        /// </summary>
        public static void Initialize()
        {
            if (_initialized)
                return;

            SerialDebug.Info("Inicializando administrador de memoria fisica con valores predeterminados...");

            // Usar valores predeterminados
            _memoryStart = DEFAULT_MEMORY_START;
            _memorySize = DEFAULT_MEMORY_SIZE;

            // Calcular el número total de paginas fisicas
            _totalPages = (uint)(_memorySize / PAGE_SIZE);
            _freePages = _totalPages;

            SerialDebug.Info("Memoria fisica - creando mapa de bits");
            SerialDebug.Info($"Memoria predeterminada: {(_memorySize / 1024 / 1024).ToString()} MB desde 0x{_memoryStart.ToString()}");
            SerialDebug.Info($"Total de paginas: {_totalPages.ToString()}");

            // Inicializar el mapa de bits para seguimiento de paginas
            InitializePageMap();

            // Marcar areas reservadas como no disponibles
            MarkReservedAreas();

            _initialized = true;

            SerialDebug.Info($"Inicialización de memoria fisica completada. Paginas libres: {_freePages.ToString()}");
        }

        /// <summary>
        /// Inicializa el mapa de bits para seguimiento de paginas disponibles
        /// </summary>
        private static void InitializePageMap()
        {
            // Calcular tamaño necesario para el mapa de bits (1 bit por pagina)
            uint mapSize = (_totalPages / 32) + ((_totalPages % 32 > 0) ? 1u : 0u);

            // Reservar espacio para el mapa de bits al inicio de la memoria disponible
            _pageMap = (uint*)_memoryStart;

            // Inicializar todo el mapa de bits a 0 (todas las paginas libres)
            for (uint i = 0; i < mapSize; i++)
            {
                _pageMap[i] = 0;
            }

            // Ajustar la dirección de inicio de memoria disponible para usuarios
            uint pagesUsedByMap = (uint)((mapSize * 4 + PAGE_SIZE - 1) / PAGE_SIZE);
            uint bytesUsedByMap = pagesUsedByMap * PAGE_SIZE;

            // Marcar las paginas utilizadas por el mapa como ocupadas
            for (uint i = 0; i < pagesUsedByMap; i++)
            {
                MarkPageUsed(i);
            }

            SerialDebug.Info($"Mapa de paginas inicializado. Paginas reservadas para el mapa: {pagesUsedByMap.ToString()}");
        }

        /// <summary>
        /// Marca las areas reservadas conocidas como no disponibles
        /// </summary>
        private static void MarkReservedAreas()
        {
            // area 1: BIOS y area de video (0x00000000 - 0x000FFFFF)
            // Esta area ya esta reservada porque empezamos desde 1MB

            // area 2: Asumimos que el kernel esta cargado desde 1MB hasta 3MB
            uint kernelStart = 0x100000;  // 1MB
            uint kernelEnd = 0x300000;    // 3MB

            for (uint addr = kernelStart; addr < kernelEnd; addr += PAGE_SIZE)
            {
                uint page = (addr - _memoryStart) / PAGE_SIZE;
                if (page < _totalPages)
                {
                    MarkPageUsed(page);
                }
            }

            //SerialDebug.Info($"Marcada area del kernel (0x{kernelStart.ToString()} - 0x{kernelEnd.ToString()}) como reservada");

            // area 3: ACPI y otras tablas (asumimos que estan después de 16MB)
            uint acpiStart = 0x1000000;  // 16MB
            uint acpiEnd = 0x1100000;    // 17MB

            for (uint addr = acpiStart; addr < acpiEnd; addr += PAGE_SIZE)
            {
                uint page = (addr - _memoryStart) / PAGE_SIZE;
                if (page < _totalPages)
                {
                    MarkPageUsed(page);
                }
            }

           // SerialDebug.Info($"Marcada area ACPI (0x{acpiStart.ToString()} - 0x{acpiEnd.ToString()}) como reservada");
        }

        /// <summary>
        /// Extiende el mapa de memoria después de la detección tardia
        /// </summary>
        /// <param name="additionalMemory">Cantidad adicional de memoria en bytes</param>
        public static void ExtendMemory(uint additionalMemory)
        {
            if (additionalMemory == 0)
                return;

            SerialDebug.Info($"Extendiendo memoria en {(additionalMemory / 1024 / 1024).ToString()} MB");

            // Guardar estado actual
            uint oldTotalPages = _totalPages;
            uint oldMemorySize = _memorySize;
            uint* oldPageMap = _pageMap;

            // Actualizar tamaño total
            _memorySize += additionalMemory;

            // Calcular nuevo número de paginas
            _totalPages = _memorySize / PAGE_SIZE;

            // Crear un nuevo mapa de bits mas grande
            uint* newPageMap = (uint*)_memoryStart;
            uint mapSize = (_totalPages / 32) + ((_totalPages % 32 > 0) ? 1u : 0u);

            // Inicializar todo el mapa de bits a 0 (todas las paginas libres)
            for (uint i = 0; i < mapSize; i++)
            {
                if (i < oldTotalPages / 32)
                {
                    // Copiar del mapa anterior
                    newPageMap[i] = oldPageMap[i];
                }
                else
                {
                    // Nuevas paginas, todas libres
                    newPageMap[i] = 0;
                }
            }

            // Actualizar contador de paginas libres
            _freePages = _totalPages - (oldTotalPages - _freePages);

            SerialDebug.Info($"Memoria extendida. Nuevas paginas totales: {_totalPages.ToString()}, Libres: {_freePages.ToString()}");
        }

        /// <summary>
        /// Marca una pagina como utilizada en el mapa de bits
        /// </summary>
        /// <param name="pageIndex">indice de la pagina a marcar</param>
        public static void MarkPageUsed(uint pageIndex)
        {
            if (pageIndex >= _totalPages)
            {
                SerialDebug.Error($"Intento de marcar pagina fuera de rango: {pageIndex.ToString()}");
                return;
            }

            uint index = pageIndex / 32;
            uint bit = pageIndex % 32;

            // Si la pagina ya esta marcada como utilizada, salir
            if ((_pageMap[index] & (1u << (int)bit)) != 0)
                return;

            // Marcar la pagina como utilizada
            _pageMap[index] |= (1u << (int)bit);
            _freePages--;
        }

        /// <summary>
        /// Marca una pagina como disponible en el mapa de bits
        /// </summary>
        /// <param name="pageIndex">indice de la pagina a liberar</param>
        public static void MarkPageFree(uint pageIndex)
        {
            if (pageIndex >= _totalPages)
            {
                SerialDebug.Error($"Intento de liberar pagina fuera de rango: {pageIndex.ToString()}");
                return;
            }

            uint index = pageIndex / 32;
            uint bit = pageIndex % 32;

            // Si la pagina ya esta marcada como libre, salir
            if ((_pageMap[index] & (1u << (int)bit)) == 0)
                return;

            // Marcar la pagina como libre
            _pageMap[index] &= ~(1u << (int)bit);
            _freePages++;
        }

        /// <summary>
        /// Verifica si una pagina esta disponible
        /// </summary>
        /// <param name="pageIndex">indice de la pagina a verificar</param>
        /// <returns>true si la pagina esta disponible, false si esta en uso</returns>
        public static bool IsPageFree(uint pageIndex)
        {
            if (pageIndex >= _totalPages)
                return false;

            uint index = pageIndex / 32;
            uint bit = pageIndex % 32;

            return (_pageMap[index] & (1u << (int)bit)) == 0;
        }

        /// <summary>
        /// Busca una pagina libre y la marca como utilizada
        /// </summary>
        /// <returns>indice de la pagina asignada, o uint.MaxValue si no hay paginas disponibles</returns>
        public static uint AllocatePage()
        {
            // Si no hay paginas libres, devolver error
            if (_freePages == 0)
            {
                SerialDebug.Error("No hay paginas fisicas disponibles");
                return uint.MaxValue;
            }

            // Buscar la primera pagina libre
            for (uint i = 0; i < _totalPages; i++)
            {
                if (IsPageFree(i))
                {
                    MarkPageUsed(i);
                    return i;
                }
            }

            // No deberia llegar aqui si _freePages > 0
            SerialDebug.Error("Error en la contabilidad de paginas libres");
            return uint.MaxValue;
        }

        /// <summary>
        /// Asigna un bloque contiguo de paginas fisicas
        /// </summary>
        /// <param name="count">Número de paginas a asignar</param>
        /// <returns>indice de la primera pagina asignada, o uint.MaxValue si no hay suficientes paginas contiguas</returns>
        public static uint AllocatePages(uint count)
        {
            // Si no hay suficientes paginas, devolver error
            if (_freePages < count)
            {
                SerialDebug.Error($"No hay suficientes paginas disponibles. Solicitadas: {count.ToString()}, Disponibles: {_freePages.ToString()}");
                return uint.MaxValue;
            }

            uint contiguousPages = 0;
            uint startPage = 0;

            // Buscar un bloque contiguo de paginas libres
            for (uint i = 0; i < _totalPages; i++)
            {
                if (IsPageFree(i))
                {
                    if (contiguousPages == 0)
                    {
                        startPage = i;
                    }

                    contiguousPages++;

                    if (contiguousPages == count)
                    {
                        // Marcar todas las paginas como utilizadas
                        for (uint j = 0; j < count; j++)
                        {
                            MarkPageUsed(startPage + j);
                        }

                        return startPage;
                    }
                }
                else
                {
                    contiguousPages = 0;
                }
            }

            SerialDebug.Error($"No se encontraron {count.ToString()} paginas contiguas");
            return uint.MaxValue;
        }

        /// <summary>
        /// Libera una pagina fisica
        /// </summary>
        /// <param name="pageIndex">indice de la pagina a liberar</param>
        public static void FreePage(uint pageIndex)
        {
            if (pageIndex >= _totalPages)
            {
                SerialDebug.Error($"Intento de liberar una pagina fuera de rango: {pageIndex.ToString()}");
                return;
            }

            MarkPageFree(pageIndex);
        }

        /// <summary>
        /// Libera un bloque contiguo de paginas fisicas
        /// </summary>
        /// <param name="startPage">indice de la primera pagina a liberar</param>
        /// <param name="count">Número de paginas a liberar</param>
        public static void FreePage(uint startPage, uint count)
        {
            if (startPage + count > _totalPages)
            {
                SerialDebug.Error($"Intento de liberar paginas fuera de rango: {startPage.ToString()} - {(startPage + count - 1).ToString()}");
                return;
            }

            for (uint i = 0; i < count; i++)
            {
                MarkPageFree(startPage + i);
            }
        }

        /// <summary>
        /// Convierte un indice de pagina a dirección fisica
        /// </summary>
        /// <param name="pageIndex">indice de la pagina</param>
        /// <returns>Dirección fisica correspondiente</returns>
        public static uint PageToAddress(uint pageIndex)
        {
            return _memoryStart + (pageIndex * PAGE_SIZE);
        }

        /// <summary>
        /// Convierte una dirección fisica a indice de pagina
        /// </summary>
        /// <param name="address">Dirección fisica</param>
        /// <returns>indice de la pagina correspondiente</returns>
        public static uint AddressToPage(uint address)
        {
            if (address < _memoryStart)
            {
                //SerialDebug.Warning($"Dirección 0x{address.ToString()} esta por debajo del inicio de la memoria ({_memoryStart.ToString()})");
                return uint.MaxValue;
            }

            return (address - _memoryStart) / PAGE_SIZE;
        }

        /// <summary>
        /// Obtiene la cantidad total de memoria fisica libre en bytes
        /// </summary>
        /// <returns>Cantidad de memoria libre en bytes</returns>
        public static uint FreeMemory()
        {
            return (uint)_freePages * PAGE_SIZE;
        }

        /// <summary>
        /// Obtiene la cantidad total de memoria fisica libre en kilobytes
        /// </summary>
        /// <returns>Cantidad de memoria libre en KB</returns>
        public static uint FreeMemoryKB()
        {
            return FreeMemory() / 1024;
        }

        /// <summary>
        /// Obtiene la cantidad total de memoria fisica libre en megabytes
        /// </summary>
        /// <returns>Cantidad de memoria libre en MB</returns>
        public static uint FreeMemoryMB()
        {
            return FreeMemoryKB() / 1024;
        }

        /// <summary>
        /// Obtiene el porcentaje de memoria fisica libre
        /// </summary>
        /// <returns>Porcentaje de memoria libre (0-100)</returns>
        public static int FreeMemoryPercentage()
        {
            if (_totalPages == 0)
                return 0;

            return (int)((_freePages * 100) / _totalPages);
        }

        /// <summary>
        /// Obtiene el número total de paginas fisicas
        /// </summary>
        public static uint TotalPages => _totalPages;

        /// <summary>
        /// Obtiene el número de paginas fisicas libres
        /// </summary>
        public static uint FreePages => _freePages;

        /// <summary>
        /// Obtiene el número de paginas fisicas utilizadas
        /// </summary>
        public static uint UsedPages => _totalPages - _freePages;

        /// <summary>
        /// Obtiene la dirección de inicio de la memoria disponible
        /// </summary>
        public static uint MemoryStart => _memoryStart;

        /// <summary>
        /// Obtiene el tamaño total de memoria disponible
        /// </summary>
        public static uint MemorySize => _memorySize;

        /// <summary>
        /// Imprime información detallada sobre el estado de la memoria fisica
        /// </summary>
        public static void PrintMemoryInfo()
        {
            SerialDebug.Info("=== Estado de la Memoria Fisica ===");
            //SerialDebug.Info($"Memoria total: {(_memorySize / 1024 / 1024).ToString()} MB desde 0x{_memoryStart.ToString()}");
            SerialDebug.Info($"Paginas totales: {_totalPages.ToString()}");
           // SerialDebug.Info($"Paginas libres: {_freePages.ToString()} ({FreeMemoryPercentage().ToString()}%)");
           // SerialDebug.Info($"Paginas usadas: {UsedPages.ToString()} ({(100 - FreeMemoryPercentage()).ToString()}%)");
            SerialDebug.Info($"Memoria libre: {FreeMemoryMB().ToString()} MB");
            SerialDebug.Info("===============================");
        }
    }

    /// <summary>
    /// Administrador de memoria virtual para x86 (32 bits) sin dependencia de MultibootInfo
    /// </summary>
    public unsafe class VirtualMemoryManager
    {
        // En x86 (32 bits), estos son los valores principales para paginación
        private const uint PAGE_DIRECTORY_ENTRIES = 1024;
        private const uint PAGE_TABLE_ENTRIES = 1024;
        private const uint PAGE_SIZE = PhysicalMemoryManager.PAGE_SIZE;

        // Dirección fisica del directorio de paginas
        private static uint* _pageDirectory;

        // Flags para entradas de directorio y tablas de paginas
        public const uint PG_PRESENT = 0x01;      // La pagina esta presente en memoria
        public const uint PG_WRITABLE = 0x02;     // La pagina es de lectura/escritura
        public const uint PG_USER = 0x04;         // La pagina es accesible desde nivel usuario
        public const uint PG_WRITETHROUGH = 0x08; // Write-through habilitado
        public const uint PG_CACHEDISABLE = 0x10; // Caché deshabilitado
        public const uint PG_ACCESSED = 0x20;     // La pagina ha sido accedida
        public const uint PG_DIRTY = 0x40;        // La pagina ha sido modificada
        public const uint PG_GLOBAL = 0x100;      // La pagina es global

        // Rangos de direcciones virtuales
        public const uint KERNEL_VIRTUAL_BASE = 0xC0000000;  // 3GB - Espacio del kernel
        public const uint USER_VIRTUAL_BASE = 0x00400000;    // 4MB - Inicio del espacio de usuario

        // Indica si el administrador ha sido inicializado
        private static bool _initialized = false;

        /// <summary>
        /// Inicializa el administrador de memoria virtual
        /// </summary>
        public static void Initialize()
        {
            if (_initialized)
                return;

            SerialDebug.Info("Inicializando gestor de memoria virtual...");

            // Asignar una pagina fisica para el directorio de paginas
            uint pdPhysical = PhysicalMemoryManager.PageToAddress(PhysicalMemoryManager.AllocatePage());

            if (pdPhysical == uint.MaxValue)
            {
                SerialDebug.Error("No se pudo reservar memoria para el directorio de paginas");
                return;
            }

            _pageDirectory = (uint*)pdPhysical;

            // Inicializar directorio de paginas (todas las entradas marcadas como no presentes)
            for (uint i = 0; i < PAGE_DIRECTORY_ENTRIES; i++)
            {
                _pageDirectory[i] = 0;
            }

            // Identity mapping para los primeros 4MB (importante para el kernel)
            // Esto mapea direcciones virtuales 0-4MB directamente a fisicas 0-4MB
            IdentityMapFirstMB();

            // Mapear el kernel al espacio virtual alto (3GB+)
            MapKernel();

            // Habilitar paginación
            EnablePaging();

            _initialized = true;

            SerialDebug.Info("Gestion de memoria virtual inicializada correctamente");
        }

        /// <summary>
        /// Mapea los primeros 4MB de memoria fisica a las mismas direcciones virtuales
        /// </summary>
        private static void IdentityMapFirstMB()
        {
            // Crear tabla de paginas para los primeros 4MB
            uint* firstPageTable = (uint*)PhysicalMemoryManager.PageToAddress(PhysicalMemoryManager.AllocatePage());

            if (firstPageTable == null)
            {
                SerialDebug.Error("No se pudo reservar memoria para la tabla de paginas inicial");
                return;
            }

            // Configurar entradas para mapear 1:1 los primeros 4MB
            for (uint i = 0; i < PAGE_TABLE_ENTRIES; i++)
            {
                uint physAddr = i * PAGE_SIZE;
                firstPageTable[i] = physAddr | PG_PRESENT | PG_WRITABLE;
            }

            // Agregar la tabla al directorio (primera entrada)
            _pageDirectory[0] = (uint)firstPageTable | PG_PRESENT | PG_WRITABLE;

            SerialDebug.Info("Primeros 4MB mapeados 1:1 (identity mapping)");
        }

        /// <summary>
        /// Mapea el kernel al espacio virtual alto (3GB+)
        /// </summary>
        private static void MapKernel()
        {
            // Calcular el indice del directorio para la base virtual del kernel
            uint pdIndex = KERNEL_VIRTUAL_BASE >> 22;

            // Crear una tabla de paginas para el kernel
            uint* kernelPageTable = (uint*)PhysicalMemoryManager.PageToAddress(PhysicalMemoryManager.AllocatePage());

            if (kernelPageTable == null)
            {
                SerialDebug.Error("No se pudo reservar memoria para la tabla de paginas del kernel");
                return;
            }

            // Mapear 4MB del kernel a direcciones fisicas bajas (asumimos que el kernel esta en los primeros 4MB)
            for (uint i = 0; i < PAGE_TABLE_ENTRIES; i++)
            {
                uint physAddr = i * PAGE_SIZE;
                kernelPageTable[i] = physAddr | PG_PRESENT | PG_WRITABLE;
            }

            // Agregar la tabla al directorio
            _pageDirectory[pdIndex] = (uint)kernelPageTable | PG_PRESENT | PG_WRITABLE;

            SerialDebug.Info($"Kernel mapeado a espacio virtual alto (0x{KERNEL_VIRTUAL_BASE.ToString()})");
        }

        /// <summary>
        /// Mapea una pagina fisica a una dirección virtual
        /// </summary>
        /// <param name="virtualAddress">Dirección virtual a mapear</param>
        /// <param name="physicalAddress">Dirección fisica a mapear</param>
        /// <param name="flags">Flags para la entrada de la tabla de paginas</param>
        public static void MapPage(uint virtualAddress, uint physicalAddress, uint flags)
        {
            if (!_initialized)
            {
                SerialDebug.Error("Intento de mapear pagina antes de inicializar el gestor de memoria virtual");
                return;
            }

            // Calcular indices para el directorio y tabla de paginas
            uint pdIndex = virtualAddress >> 22;
            uint ptIndex = (virtualAddress >> 12) & 0x3FF;

            // Obtener la entrada del directorio de paginas
            uint pdEntry = _pageDirectory[pdIndex];
            uint* pageTable;

            // Si la tabla de paginas no existe, crearla
            if ((pdEntry & PG_PRESENT) == 0)
            {
                uint pageTablePhys = PhysicalMemoryManager.PageToAddress(PhysicalMemoryManager.AllocatePage());

                if (pageTablePhys == uint.MaxValue)
                {
                    SerialDebug.Error($"No se pudo asignar tabla de paginas para la dirección 0x{virtualAddress.ToString()}");
                    return;
                }

                pageTable = (uint*)pageTablePhys;

                // Inicializar la nueva tabla de paginas (todas las entradas no presentes)
                for (uint i = 0; i < PAGE_TABLE_ENTRIES; i++)
                {
                    pageTable[i] = 0;
                }

                // Agregar la tabla al directorio
                _pageDirectory[pdIndex] = pageTablePhys | PG_PRESENT | PG_WRITABLE | PG_USER;
            }
            else
            {
                // Obtener dirección de la tabla de paginas existente
                pageTable = (uint*)(pdEntry & 0xFFFFF000);
            }

            // Configurar la entrada en la tabla de paginas
            pageTable[ptIndex] = (physicalAddress & 0xFFFFF000) | (flags & 0xFFF);

            // Invalidar la entrada en la TLB (Translation Lookaside Buffer)
            InvalidatePage(virtualAddress);
        }

        /// <summary>
        /// Desmapea una pagina virtual
        /// </summary>
        /// <param name="virtualAddress">Dirección virtual a desmapear</param>
        public static void UnmapPage(uint virtualAddress)
        {
            if (!_initialized)
            {
                SerialDebug.Error("Intento de desmapear pagina antes de inicializar el gestor de memoria virtual");
                return;
            }

            // Calcular indices para el directorio y tabla de paginas
            uint pdIndex = virtualAddress >> 22;
            uint ptIndex = (virtualAddress >> 12) & 0x3FF;

            // Obtener la entrada del directorio de paginas
            uint pdEntry = _pageDirectory[pdIndex];

            // Si la tabla de paginas no existe, no hay nada que hacer
            if ((pdEntry & PG_PRESENT) == 0)
            {
                return;
            }

            // Obtener dirección de la tabla de paginas
            uint* pageTable = (uint*)(pdEntry & 0xFFFFF000);

            // Marcar la entrada como no presente
            pageTable[ptIndex] = 0;

            // Invalidar la entrada en la TLB
            InvalidatePage(virtualAddress);
        }

        /// <summary>
        /// Obtiene la dirección fisica para una dirección virtual
        /// </summary>
        /// <param name="virtualAddress">Dirección virtual</param>
        /// <returns>Dirección fisica correspondiente, o 0 si no esta mapeada</returns>
        public static uint GetPhysicalAddress(uint virtualAddress)
        {
            if (!_initialized)
            {
                SerialDebug.Error("Intento de obtener dirección fisica antes de inicializar el gestor de memoria virtual");
                return 0;
            }

            // Calcular indices para el directorio y tabla de paginas
            uint pdIndex = virtualAddress >> 22;
            uint ptIndex = (virtualAddress >> 12) & 0x3FF;
            uint offset = virtualAddress & 0xFFF;

            // Obtener la entrada del directorio de paginas
            uint pdEntry = _pageDirectory[pdIndex];

            // Si la tabla de paginas no existe, la dirección no esta mapeada
            if ((pdEntry & PG_PRESENT) == 0)
            {
                return 0;
            }

            // Obtener dirección de la tabla de paginas
            uint* pageTable = (uint*)(pdEntry & 0xFFFFF000);

            // Obtener la entrada de la tabla de paginas
            uint ptEntry = pageTable[ptIndex];

            // Si la pagina no esta presente, la dirección no esta mapeada
            if ((ptEntry & PG_PRESENT) == 0)
            {
                return 0;
            }

            // Calcular la dirección fisica (dirección base + offset)
            return (ptEntry & 0xFFFFF000) | offset;
        }

        /// <summary>
        /// Habilita la paginación en el CPU
        /// </summary>
        private static void EnablePaging()
        {
            // Cargar el registro CR3 con la dirección del directorio de paginas
            SetCR3((uint)_pageDirectory);

            // Habilitar paginación en CR0
            uint cr0 = GetCR0();
            SetCR0(cr0 | 0x80000000); // Bit 31 de CR0 habilita la paginación

            SerialDebug.Info("Paginacion habilitada");
        }

        /// <summary>
        /// Invalida una entrada de la TLB para una dirección especifica
        /// </summary>
        /// <param name="address">Dirección a invalidar</param>
        private static void InvalidatePage(uint address)
        {
            // Instrucción assembler 'invlpg' que invalida una entrada de la TLB
            // Para simplificar, recargamos todo CR3 (menos eficiente pero funcional)
            SetCR3(GetCR3());
        }

        /// <summary>
        /// Obtiene el valor del registro CR0
        /// </summary>
        [DllImport("*", EntryPoint = "_GetCR0")]
        public static extern uint GetCR0();

        /// <summary>
        /// Establece el valor del registro CR0
        /// </summary>
        [DllImport("*", EntryPoint = "_SetCR0")]
        public static extern void SetCR0(uint value);

        /// <summary>
        /// Obtiene el valor del registro CR3
        /// </summary>
        [DllImport("*", EntryPoint = "_GetCR3")]
        public static extern uint GetCR3();

        /// <summary>
        /// Establece el valor del registro CR3
        /// </summary>
        [DllImport("*", EntryPoint = "_SetCR3")]
        public static extern void SetCR3(uint value);

        /// <summary>
        /// Obtiene la dirección del directorio de paginas activo
        /// </summary>
        public static uint PageDirectoryAddress => (uint)_pageDirectory;

        /// <summary>
        /// Imprime información sobre la configuración de memoria virtual
        /// </summary>
        public static void PrintInfo()
        {
            SerialDebug.Info("=== Información de Memoria Virtual ===");
            SerialDebug.Info($"Directorio de paginas: 0x{((uint)_pageDirectory).ToString()}");
            SerialDebug.Info($"Base virtual del kernel: 0x{KERNEL_VIRTUAL_BASE.ToString()}");
            SerialDebug.Info($"Base virtual de usuario: 0x{USER_VIRTUAL_BASE.ToString()}");
            SerialDebug.Info($"Estado de paginación: {((GetCR0() & 0x80000000) != 0 ? "Habilitada" : "Deshabilitada")}");
            SerialDebug.Info("==================================");
        }
    }

    /// <summary>
    /// Administrador de heap del kernel sin dependencia de MultibootInfo
    /// </summary>
    public unsafe class KernelHeap
    {
        // Tamaño de pagina estandar
        private const uint PAGE_SIZE = PhysicalMemoryManager.PAGE_SIZE;

        // Inicio y tamaño del heap del kernel
        private static uint _heapStart;
        private static uint _heapSize;
        private static uint _heapEnd;

        // Puntero al primer bloque libre
        private static HeapBlock* _freeList;

        // Flags de configuración
        private static bool _allowGrowth = true;
        private static uint _growthIncrement = 4 * PAGE_SIZE; // 16KB por incremento

        // Estadisticas
        private static uint _totalAllocations = 0;
        private static uint _totalBytesAllocated = 0;
        private static uint _activeAllocations = 0;
        private static uint _activeBytesAllocated = 0;

        /// <summary>
        /// Estructura para un bloque de memoria en el heap
        /// </summary>
        private struct HeapBlock
        {
            public uint Size;       // Tamaño del bloque en bytes
            public bool IsFree;     // Indica si el bloque esta libre
            public HeapBlock* Next; // Puntero al siguiente bloque
        }

        /// <summary>
        /// Inicializa el heap del kernel
        /// </summary>
        /// <param name="startAddress">Dirección de inicio para el heap</param>
        /// <param name="sizeInBytes">Tamaño del heap en bytes</param>
        public static void Initialize(uint startAddress, uint sizeInBytes)
        {
            SerialDebug.Info("Inicializando heap del kernel...");

            _heapStart = startAddress;
            _heapSize = sizeInBytes;
            _heapEnd = _heapStart + _heapSize;

            // Inicializar el primer bloque libre que ocupa todo el heap
            _freeList = (HeapBlock*)_heapStart;
            _freeList->Size = _heapSize - (uint)(uint)sizeof(HeapBlock);
            _freeList->IsFree = true;
            _freeList->Next = null;

            //SerialDebug.Info($"Heap inicializado en 0x{_heapStart.ToString()}, tamaño: {(_heapSize / 1024).ToString()} KB");
        }

        /// <summary>
        /// Configura si el heap puede crecer automaticamente
        /// </summary>
        /// <param name="allow">True para permitir crecimiento, false para deshabilitar</param>
        public static void SetAllowGrowth(bool allow)
        {
            _allowGrowth = allow;
        }

        /// <summary>
        /// Establece el incremento para crecimiento del heap
        /// </summary>
        /// <param name="incrementBytes">Tamaño del incremento en bytes</param>
        public static void SetGrowthIncrement(uint incrementBytes)
        {
            // Alinear a tamaño de pagina
            _growthIncrement = ((incrementBytes + PAGE_SIZE - 1) / PAGE_SIZE) * PAGE_SIZE;
        }

        /// <summary>
        /// Expande el heap
        /// </summary>
        /// <param name="additionalBytes">Bytes adicionales requeridos</param>
        /// <returns>True si la expansión fue exitosa, false en caso contrario</returns>
        private static bool GrowHeap(uint additionalBytes)
        {
            if (!_allowGrowth)
            {
                SerialDebug.Warning("Intento de expandir el heap pero el crecimiento esta deshabilitado");
                return false;
            }

            // Calcular cuantas paginas necesitamos
            uint requiredPages = (additionalBytes + PAGE_SIZE - 1) / PAGE_SIZE;

            // Redondear al incremento configurado
            uint pagesToAllocate = ((requiredPages + (_growthIncrement / PAGE_SIZE) - 1) /
                                    (_growthIncrement / PAGE_SIZE)) * (_growthIncrement / PAGE_SIZE);

            uint bytesToAllocate = pagesToAllocate * PAGE_SIZE;

            //SerialDebug.Info($"Expandiendo heap en {(bytesToAllocate / 1024).ToString()} KB ({pagesToAllocate.ToString()} paginas)");

            // Asignar paginas fisicas
            uint pageIndex = PhysicalMemoryManager.AllocatePages(pagesToAllocate);
            if (pageIndex == uint.MaxValue)
            {
                SerialDebug.Error("No se pudieron asignar paginas fisicas para expandir el heap");
                return false;
            }

            uint physicalAddress = PhysicalMemoryManager.PageToAddress(pageIndex);

            // Mapear las paginas a continuación del heap actual
            for (uint i = 0; i < pagesToAllocate; i++)
            {
                uint virtualAddress = _heapEnd + (i * PAGE_SIZE);
                uint currentPhysicalAddress = physicalAddress + (i * PAGE_SIZE);

                VirtualMemoryManager.MapPage(
                    virtualAddress,
                    currentPhysicalAddress,
                    VirtualMemoryManager.PG_PRESENT | VirtualMemoryManager.PG_WRITABLE
                );
            }

            // Crear un nuevo bloque libre para el area expandida
            HeapBlock* newBlock = (HeapBlock*)_heapEnd;
            newBlock->Size = bytesToAllocate - (uint)(uint)sizeof(HeapBlock);
            newBlock->IsFree = true;
            newBlock->Next = null;

            // Agregar el nuevo bloque a la lista de bloques libres
            HeapBlock* current = _freeList;

            if (current == null)
            {
                // Si no hay bloques libres, este es el primero
                _freeList = newBlock;
            }
            else
            {
                // Encontrar el último bloque
                while (current->Next != null)
                {
                    current = current->Next;
                }

                // Agregar el nuevo bloque al final
                current->Next = newBlock;
            }

            // Actualizar el tamaño y fin del heap
            _heapSize += bytesToAllocate;
            _heapEnd += bytesToAllocate;

            // Intentar fusionar bloques adyacentes
            CoalesceBlocks();

            SerialDebug.Info($"Heap expandido a {(_heapSize / 1024).ToString()} KB");
            return true;
        }

        /// <summary>
        /// Asigna un bloque de memoria del heap
        /// </summary>
        /// <param name="size">Tamaño solicitado en bytes</param>
        /// <returns>Puntero al bloque asignado, o null si no hay memoria suficiente</returns>
        public static void* Allocate(uint size)
        {
            if (size == 0)
                return null;

            // Alinear el tamaño a 4 bytes
            size = (size + 3) & ~3u;

            HeapBlock* current = _freeList;
            HeapBlock* previous = null;

            // Buscar un bloque libre con suficiente espacio
            while (current != null)
            {
                if (current->IsFree && current->Size >= size)
                {
                    // Si el bloque es significativamente mas grande que lo solicitado,
                    // dividirlo en dos
                    if (current->Size >= size + (uint)sizeof(HeapBlock) + 4)
                    {
                        // Calcular dirección del nuevo bloque
                        HeapBlock* newBlock = (HeapBlock*)((byte*)current + (uint)sizeof(HeapBlock) + size);

                        // Inicializar el nuevo bloque
                        newBlock->Size = current->Size - size - (uint)sizeof(HeapBlock);
                        newBlock->IsFree = true;
                        newBlock->Next = current->Next;

                        // Actualizar el bloque actual
                        current->Size = size;
                        current->Next = newBlock;
                    }

                    // Marcar el bloque como ocupado
                    current->IsFree = false;

                    // Actualizar estadisticas
                    _totalAllocations++;
                    _totalBytesAllocated += size;
                    _activeAllocations++;
                    _activeBytesAllocated += size;

                    // Devolver la dirección de los datos (después de la cabecera)
                    return (void*)((byte*)current + (uint)sizeof(HeapBlock));
                }

                previous = current;
                current = current->Next;
            }

            // No se encontró un bloque libre adecuado, intentar expandir el heap
            if (_allowGrowth)
            {
                if (GrowHeap(size + (uint)sizeof(HeapBlock)))
                {
                    // Reintentar la asignación
                    return Allocate(size);
                }
            }

            // No se pudo asignar memoria
            SerialDebug.Error($"No se pudo asignar {size.ToString()} bytes de memoria en el heap");
            return null;
        }

        /// <summary>
        /// Libera un bloque de memoria previamente asignado
        /// </summary>
        /// <param name="ptr">Puntero al bloque a liberar</param>
        public static void Free(void* ptr)
        {
            if (ptr == null)
                return;

            // Obtener la cabecera del bloque
            HeapBlock* block = (HeapBlock*)((byte*)ptr - (uint)sizeof(HeapBlock));

            // Verificar que el bloque esté dentro del heap
            if ((uint)block < _heapStart || (uint)block >= _heapEnd)
            {
                //SerialDebug.Error($"Intento de liberar memoria fuera del heap: 0x{((uint)ptr).ToString()}");
                return;
            }

            // Marcar el bloque como libre
            block->IsFree = true;

            // Actualizar estadisticas
            _activeAllocations--;
            _activeBytesAllocated -= block->Size;

            // Fusionar con bloques adyacentes si estan libres (coalescing)
            CoalesceBlocks();
        }

        /// <summary>
        /// Fusiona bloques libres adyacentes para reducir la fragmentación
        /// </summary>
        private static void CoalesceBlocks()
        {
            HeapBlock* current = _freeList;

            while (current != null && current->Next != null)
            {
                // Si el bloque actual y el siguiente estan libres, fusionarlos
                if (current->IsFree && current->Next->IsFree)
                {
                    // Sumar el tamaño del bloque siguiente al actual
                    current->Size += (uint)sizeof(HeapBlock) + current->Next->Size;

                    // Saltarse el bloque siguiente en la lista
                    current->Next = current->Next->Next;
                }
                else
                {
                    // Avanzar al siguiente bloque
                    current = current->Next;
                }
            }
        }

        /// <summary>
        /// Realiza una reasignación de un bloque de memoria
        /// </summary>
        /// <param name="ptr">Puntero al bloque original</param>
        /// <param name="newSize">Nuevo tamaño requerido</param>
        /// <returns>Puntero al bloque reasignado, o null si falló la reasignación</returns>
        public static void* Realloc(void* ptr, uint newSize)
        {
            // Si el puntero es nulo, equivale a Allocate
            if (ptr == null)
                return Allocate(newSize);

            // Si el nuevo tamaño es 0, equivale a Free
            if (newSize == 0)
            {
                Free(ptr);
                return null;
            }

            // Obtener la cabecera del bloque
            HeapBlock* block = (HeapBlock*)((byte*)ptr - (uint)sizeof(HeapBlock));

            // Verificar que el bloque esté dentro del heap
            if ((uint)block < _heapStart || (uint)block >= _heapEnd)
            {
                //SerialDebug.Error($"Intento de reasignar memoria fuera del heap: 0x{((uint)ptr).ToString()}");
                return null;
            }

            // Alinear el nuevo tamaño a 4 bytes
            newSize = (newSize + 3) & ~3u;

            // Si el nuevo tamaño es menor o igual al actual, simplemente ajustar el tamaño
            if (newSize <= block->Size)
            {
                // Si la diferencia es significativa, dividir el bloque
                if (block->Size > newSize + (uint)sizeof(HeapBlock) + 4)
                {
                    // Calcular dirección del nuevo bloque
                    HeapBlock* newBlock = (HeapBlock*)((byte*)block + (uint)sizeof(HeapBlock) + newSize);

                    // Inicializar el nuevo bloque
                    newBlock->Size = block->Size - newSize - (uint)sizeof(HeapBlock);
                    newBlock->IsFree = true;
                    newBlock->Next = block->Next;

                    // Actualizar el bloque actual
                    block->Size = newSize;
                    block->Next = newBlock;

                    // Fusionar bloques libres
                    CoalesceBlocks();
                }

                // Actualizar estadisticas
                _activeBytesAllocated = _activeBytesAllocated - block->Size + newSize;

                return ptr;
            }

            // Necesitamos un bloque mas grande

            // Verificar si el siguiente bloque esta libre y tiene suficiente espacio
            if (block->Next != null && block->Next->IsFree)
            {
                uint availableSize = block->Size + (uint)sizeof(HeapBlock) + block->Next->Size;

                if (availableSize >= newSize)
                {
                    // Fusionar con el siguiente bloque
                    uint remainingSize = availableSize - newSize;

                    // Si queda suficiente espacio, crear un nuevo bloque libre
                    if (remainingSize > (uint)sizeof(HeapBlock) + 4)
                    {
                        // Calcular dirección del nuevo bloque
                        HeapBlock* newBlock = (HeapBlock*)((byte*)block + (uint)sizeof(HeapBlock) + newSize);

                        // Inicializar el nuevo bloque
                        newBlock->Size = remainingSize - (uint)sizeof(HeapBlock);
                        newBlock->IsFree = true;
                        newBlock->Next = block->Next->Next;

                        // Actualizar el bloque actual
                        block->Size = newSize;
                        block->Next = newBlock;
                    }
                    else
                    {
                        // Usar todo el espacio disponible
                        block->Size = availableSize;
                        block->Next = block->Next->Next;
                    }

                    // Actualizar estadisticas
                    _activeBytesAllocated = _activeBytesAllocated - block->Size + newSize;

                    return ptr;
                }
            }

            // No se pudo expandir in-place, asignar un nuevo bloque
            void* newPtr = Allocate(newSize);
            if (newPtr == null)
                return null;

            // Copiar los datos del bloque antiguo al nuevo
            uint copySize = block->Size < newSize ? block->Size : newSize;
            for (uint i = 0; i < copySize; i++)
            {
                ((byte*)newPtr)[i] = ((byte*)ptr)[i];
            }

            // Liberar el bloque antiguo
            Free(ptr);

            return newPtr;
        }

        /// <summary>
        /// Imprime información sobre el estado del heap
        /// </summary>
        public static void DumpHeapInfo()
        {
            uint totalMemory = 0;
            uint usedMemory = 0;
            uint freeMemory = 0;
            uint blockCount = 0;
            uint freeBlockCount = 0;

            HeapBlock* current = _freeList;

            while (current != null)
            {
                totalMemory += current->Size + (uint)sizeof(HeapBlock);

                if (current->IsFree)
                {
                    freeMemory += current->Size;
                    freeBlockCount++;
                }
                else
                {
                    usedMemory += current->Size;
                }

                blockCount++;
                current = current->Next;
            }

            SerialDebug.Info("=== Estado del Heap ===");
            SerialDebug.Info($"Inicio del heap: 0x{_heapStart.ToString()}");
            SerialDebug.Info($"Tamaño del heap: {(_heapSize / 1024).ToString()} KB");
            SerialDebug.Info($"Memoria total: {(totalMemory / 1024).ToString()} KB");
            //SerialDebug.Info($"Memoria usada: {(usedMemory / 1024).ToString()} KB ({usedMemory * 100 / (totalMemory > 0 ? totalMemory : 1)}%)");
            //SerialDebug.Info($"Memoria libre: {freeMemory / 1024} KB ({freeMemory * 100 / (totalMemory > 0 ? totalMemory : 1)}%)");
            SerialDebug.Info($"Bloques totales: {blockCount.ToString()}");
            SerialDebug.Info($"Bloques libres: {freeBlockCount.ToString()}");
            SerialDebug.Info($"Asignaciones activas: {_activeAllocations.ToString()}");
            SerialDebug.Info($"Bytes activos: {(_activeBytesAllocated / 1024).ToString()} KB");
            SerialDebug.Info($"Asignaciones totales: {_totalAllocations.ToString()}");
            SerialDebug.Info($"Bytes totales asignados: {(_totalBytesAllocated / 1024).ToString()} KB");
            SerialDebug.Info("===================");
        }

        /// <summary>
        /// Obtiene el número de asignaciones activas
        /// </summary>
        public static uint ActiveAllocations => _activeAllocations;

        /// <summary>
        /// Obtiene el número de bytes actualmente asignados
        /// </summary>
        public static uint ActiveBytes => _activeBytesAllocated;

        /// <summary>
        /// Obtiene el número total de asignaciones realizadas
        /// </summary>
        public static uint TotalAllocations => _totalAllocations;

        /// <summary>
        /// Obtiene el número total de bytes asignados
        /// </summary>
        public static uint TotalBytes => _totalBytesAllocated;

        /// <summary>
        /// Obtiene el tamaño total del heap
        /// </summary>
        public static uint HeapSize => _heapSize;
    }

    /// <summary>
    /// Clase para la gestión global de memoria del kernel sin dependencia de MultibootInfo
    /// </summary>
    public unsafe class MemoryManager
    {
        // Direcciones virtuales para diferentes areas de memoria
        private const uint KERNEL_VIRTUAL_HEAP_START = 0xD0000000;  // 3.25 GB
        private const uint KERNEL_HEAP_SIZE = 16 * 1024 * 1024;     // 16 MB iniciales

        // Flag para indicar si el sistema ha sido inicializado
        private static bool _initialized = false;

        /// <summary>
        /// Inicializa todos los componentes del sistema de memoria
        /// </summary>
        public static void Initialize()
        {
            if (_initialized)
                return;

            SerialDebug.Info("Inicializando sistema de gestion de memoria...");

            // Inicializar gestor de memoria fisica
            PhysicalMemoryManager.Initialize();

            // Inicializar gestor de memoria virtual y paginación
            VirtualMemoryManager.Initialize();

            // Inicializar heap del kernel
            // Asignar paginas fisicas contiguas para el heap
            uint heapPages = KERNEL_HEAP_SIZE / PhysicalMemoryManager.PAGE_SIZE;
            uint heapPhysicalStart = PhysicalMemoryManager.PageToAddress(PhysicalMemoryManager.AllocatePages(heapPages));

            if (heapPhysicalStart == uint.MaxValue)
            {
                SerialDebug.Error("No se pudo asignar memoria para el heap del kernel");
                return;
            }

            // Mapear cada pagina del heap
            for (uint i = 0; i < heapPages; i++)
            {
                uint virtualAddress = KERNEL_VIRTUAL_HEAP_START + (i * PhysicalMemoryManager.PAGE_SIZE);
                uint physicalAddress = heapPhysicalStart + (i * PhysicalMemoryManager.PAGE_SIZE);

                VirtualMemoryManager.MapPage(
                    virtualAddress,
                    physicalAddress,
                    VirtualMemoryManager.PG_PRESENT | VirtualMemoryManager.PG_WRITABLE
                );
            }

            // Inicializar el heap en la dirección virtual asignada
            KernelHeap.Initialize(KERNEL_VIRTUAL_HEAP_START, KERNEL_HEAP_SIZE);

            _initialized = true;

            SerialDebug.Info("Sistema de gestion de memoria inicializado correctamente");
        }

        /// <summary>
        /// Asigna memoria del heap del kernel
        /// </summary>
        /// <param name="size">Tamaño en bytes a asignar</param>
        /// <returns>Puntero a la memoria asignada, o null si no hay suficiente memoria</returns>
        public static void* Allocate(uint size)
        {
            if (!_initialized)
            {
                SerialDebug.Error("Intento de asignar memoria antes de inicializar el gestor de memoria");
                return null;
            }

            return KernelHeap.Allocate(size);
        }

        /// <summary>
        /// Reasigna un bloque de memoria previamente asignado
        /// </summary>
        /// <param name="ptr">Puntero al bloque original</param>
        /// <param name="size">Nuevo tamaño requerido</param>
        /// <returns>Puntero al bloque reasignado, o null si falló la reasignación</returns>
        public static void* Realloc(void* ptr, uint size)
        {
            if (!_initialized)
            {
                SerialDebug.Error("Intento de reasignar memoria antes de inicializar el gestor de memoria");
                return null;
            }

            return KernelHeap.Realloc(ptr, size);
        }

        /// <summary>
        /// Libera memoria previamente asignada
        /// </summary>
        /// <param name="ptr">Puntero a la memoria a liberar</param>
        public static void Free(void* ptr)
        {
            if (!_initialized)
            {
                SerialDebug.Error("Intento de liberar memoria antes de inicializar el gestor de memoria");
                return;
            }

            KernelHeap.Free(ptr);
        }

        /// <summary>
        /// Realiza una asignación especifica de paginas de memoria virtual
        /// </summary>
        /// <param name="virtualAddress">Dirección virtual deseada (0 para automatica)</param>
        /// <param name="pages">Número de paginas a asignar</param>
        /// <param name="userAccessible">Si la memoria debe ser accesible desde nivel usuario</param>
        /// <returns>Dirección virtual asignada, o 0 si no se pudo asignar</returns>
        public static uint AllocateVirtualMemory(uint virtualAddress, uint pages, bool userAccessible)
        {
            if (!_initialized)
            {
                SerialDebug.Error("Intento de asignar memoria virtual antes de inicializar el gestor de memoria");
                return 0;
            }

            // Si la dirección es 0, buscar una dirección disponible
            if (virtualAddress == 0)
            {
                if (userAccessible)
                {
                    // Espacio de usuario: comenzar desde 4MB
                    virtualAddress = 0x400000;
                }
                else
                {
                    // Espacio del kernel: comenzar desde 3.5GB
                    virtualAddress = 0xE0000000;
                }
            }

            // Asignar paginas fisicas
            uint physicalStart = PhysicalMemoryManager.PageToAddress(PhysicalMemoryManager.AllocatePages(pages));

            if (physicalStart == uint.MaxValue)
            {
                SerialDebug.Error($"No se pudieron asignar {pages.ToString()} paginas para memoria virtual");
                return 0;
            }

            // Configurar flags para paginación
            uint flags = VirtualMemoryManager.PG_PRESENT | VirtualMemoryManager.PG_WRITABLE;

            if (userAccessible)
            {
                flags |= VirtualMemoryManager.PG_USER;
            }

            // Mapear cada pagina
            for (uint i = 0; i < pages; i++)
            {
                uint vaddr = virtualAddress + (i * PhysicalMemoryManager.PAGE_SIZE);
                uint paddr = physicalStart + (i * PhysicalMemoryManager.PAGE_SIZE);

                VirtualMemoryManager.MapPage(vaddr, paddr, flags);
            }

            return virtualAddress;
        }

        /// <summary>
        /// Libera paginas de memoria virtual
        /// </summary>
        /// <param name="virtualAddress">Dirección virtual a liberar</param>
        /// <param name="pages">Número de paginas a liberar</param>
        public static void FreeVirtualMemory(uint virtualAddress, uint pages)
        {
            if (!_initialized)
            {
                SerialDebug.Error("Intento de liberar memoria virtual antes de inicializar el gestor de memoria");
                return;
            }

            for (uint i = 0; i < pages; i++)
            {
                uint vaddr = virtualAddress + (i * PhysicalMemoryManager.PAGE_SIZE);

                // Obtener dirección fisica para liberar la pagina correspondiente
                uint paddr = VirtualMemoryManager.GetPhysicalAddress(vaddr);

                if (paddr != 0)
                {
                    // Desmapear la pagina virtual
                    VirtualMemoryManager.UnmapPage(vaddr);

                    // Liberar la pagina fisica
                    PhysicalMemoryManager.FreePage(PhysicalMemoryManager.AddressToPage(paddr));
                }
            }
        }

        /// <summary>
        /// Imprime información sobre el estado de la memoria del sistema
        /// </summary>
        public static void PrintMemoryInfo()
        {
            SerialDebug.Info("=== Información de Memoria del Sistema ===");

            // Información de memoria fisica
            PhysicalMemoryManager.PrintMemoryInfo();

            // Información de memoria virtual
            VirtualMemoryManager.PrintInfo();

            // Información del heap
            KernelHeap.DumpHeapInfo();

            SerialDebug.Info("=======================================");
        }
    }

    /// <summary>
    /// Implementación simple de operaciones de memoria
    /// </summary>
    public unsafe class MemoryOperations
    {
        /// <summary>
        /// Copia un bloque de memoria
        /// </summary>
        /// <param name="destination">Destino</param>
        /// <param name="source">Origen</param>
        /// <param name="size">Tamaño en bytes</param>
        public static void MemCopy(void* destination, void* source, uint size)
        {
            byte* dest = (byte*)destination;
            byte* src = (byte*)source;

            // Optimización para copiar por bloques de 4 bytes cuando sea posible
            if (((uint)dest & 3) == 0 && ((uint)src & 3) == 0 && (size & 3) == 0)
            {
                uint* destInt = (uint*)dest;
                uint* srcInt = (uint*)src;
                uint intCount = size >> 2;

                for (uint i = 0; i < intCount; i++)
                {
                    destInt[i] = srcInt[i];
                }
            }
            else
            {
                // Copia byte por byte para casos no alineados
                for (uint i = 0; i < size; i++)
                {
                    dest[i] = src[i];
                }
            }
        }
    }
}