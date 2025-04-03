ENTRY(_start)

SECTIONS {
    . = 0x100000; /* Dirección base del kernel */

    .text : {
        *(.text)                /* Código ejecutable */
    }

    .data : {
        *(.data)                /* Datos inicializados */
    }

    .rodata : {
        *(.rodata)              /* Datos solo lectura */
    }

    .bss : {
        *(.bss)                 /* Datos no inicializados */
    }

    /* Información de reubicación */
    .reloc : {
        *(.reloc)               /* Datos de reubicación */
    }


    /* Datos solo lectura */
    .rdata : {
        *(.rdata)               /* Información de personalidades para excepciones */
    }

    /* Para descartar secciones no deseadas */
    /DISCARD/ : {
        *(.note.*)
        *(.comment)
        *(.pdata)  /* Eliminar .pdata si no es necesario */
        *(.xdata)
    }
}
