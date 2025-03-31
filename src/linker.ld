ENTRY(_start)

SECTIONS {
    . = 0x100000;  /* Typical kernel load address */

    .multiboot_header : {
        *(.multiboot_header)
    }

    .text : {
        *(.text)
    }

    .rodata : {
        *(.rodata)
    }

    .data : {
        *(.data)
    }

    .bss : {
        *(COMMON)
        *(.bss)
    }
}