ENTRY(_start)

SECTIONS {
    . = 0x100000; /* Kernel base address */

    .text : {
        *(.text)                /* Executable code */
    }

    .data : {
        *(.data)                /* Initialized data */
    }

    .rodata : {
        *(.rodata)              /* Read-only data */
    }

    .bss : {
        *(.bss)                 /* Uninitialized data */
    }

    /* Read-only data */
    .rdata : {
        *(.rdata)                /* Read-only data */
    }

    .reloc : {

        *(.reloc)
    }

    /* Discard unwanted sections */
    /DISCARD/ : {
        *(.note.*)
        *(.comment)
        *(.pdata)  /* Important to remove */
        *(.xdata)
        
    }
}
