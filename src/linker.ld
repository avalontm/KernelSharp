/* linker.ld - Script de enlazado para KernelSharp */

/* Punto de entrada - debe coincidir con la etiqueta en loader.asm */
ENTRY (_start)

/* Constantes para configuración de memoria */
KERNEL_BASE = 0x00100000;  /* 1MB - Después del modo real y área reservada de BIOS */
PAGE_SIZE = 0x1000;        /* 4KB por página */

/* Secciones de memoria */
SECTIONS
{
    /* La dirección base donde se cargará el kernel */
    . = KERNEL_BASE;

    /* Sección .text - Código ejecutable */
    .text ALIGN(PAGE_SIZE) : 
    {
        /* Marcar inicio de código */
        _code = .;
        _text_start = .;
        
        /* Código del bootloader primero */
        *(.multiboot)      /* Header multiboot si existe */
        *(.text.boot)      /* Código específico de arranque */
        
        /* Código principal */
        *(.text)
        *(.text.*)
        
        /* Marcar fin de código */
        _text_end = .;
    }

    /* Sección .rodata - Datos de solo lectura */
    .rodata ALIGN(PAGE_SIZE) : 
    {
        _rodata_start = .;
        
        /* Tablas de solo lectura */
        *(.rodata)
        *(.rodata.*)
        
        /* Información de CoreLib */
        *(.corelib_modules)
        *(.corelib_sections)
        
        _rodata_end = .;
    }

    /* Sección .data - Datos inicializados */
    .data ALIGN(PAGE_SIZE) : 
    {
        _data_start = .;
        
        /* Datos inicializados */
        *(.data)
        *(.data.*)
        
        _data_end = .;
    }

    /* Sección .bss - Datos no inicializados (serán inicializados a 0) */
    .bss ALIGN(PAGE_SIZE) : 
    {
        _bss_start = .;
        
        /* Variables comunes y bss */
        *(COMMON)
        *(.bss)
        *(.bss.*)
        
        /* Áreas de memoria para CoreLib */
        *(.gc_static_region)
        *(.cctor_region)
        
        _bss_end = .;
    }
    
    /* Sección para información de depuración */
    .debug : 
    {
        *(.debug*)
    }
    
    /* Sección para símbolos de debug DWARF */
    .eh_frame : 
    {
        *(.eh_frame*)
    }
    
    /* Descartar secciones innecesarias */
    /DISCARD/ : 
    {
        *(.comment)
        *(.note.GNU-stack)
    }
    
    /* Marcar el final del kernel */
    _kernel_end = .;
}

/* Exportar símbolos útiles para el código del kernel */
__kernel_start = KERNEL_BASE;
__kernel_size = _kernel_end - KERNEL_BASE;
__code_size = _text_end - _text_start;
__data_size = _data_end - _data_start;
__bss_size = _bss_end - _bss_start;
__rodata_size = _rodata_end - _rodata_start;