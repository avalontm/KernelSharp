/* linker.ld - Generic Linker Script for 32-bit Kernel */

/* Output format and architecture */
OUTPUT_ARCH(i386)

/* Entry point */
ENTRY(_start)

/* Memory layout constants */
KERNEL_PHYS_BASE  = 0x00100000;  /* 1MB - Standard kernel load address */
KERNEL_VIRT_BASE  = 0xC0000000;  /* 3GB - Higher-half kernel base */
KERNEL_STACK_SIZE = 0x4000;      /* 16KB kernel stack */

/* Define symbols for runtime and memory management */
PROVIDE(_kernel_physical_start = KERNEL_PHYS_BASE);
PROVIDE(_kernel_virtual_start  = KERNEL_VIRT_BASE);
PROVIDE(_kernel_stack_size     = KERNEL_STACK_SIZE);

/* Memory regions */
MEMORY {
    /* Physical memory region for kernel */
    kernel_phys (RWX) : ORIGIN = KERNEL_PHYS_BASE, LENGTH = 64M
    
    /* Virtual memory region for higher-half kernel */
    kernel_virt (RWX) : ORIGIN = KERNEL_VIRT_BASE, LENGTH = 256M
}

SECTIONS
{
    /* Start at physical load address */
    . = KERNEL_PHYS_BASE;

    /* Multiboot header - must be in first 8K */
    .multiboot ALIGN(4) : {
        *(.multiboot_header)
        *(.multiboot)
    }

    /* Kernel text (code) section */
    .text ALIGN(4K) : {
        _text_start = .;
        
        /* Initialization code */
        *(.text.init)
        
        /* Runtime critical sections */
        *(.text.fastcall)
        
        /* All other code */
        *(.text)
        *(.text.*)
        
        _text_end = .;
    }

    /* Read-only data section */
    .rodata ALIGN(4K) : {
        _rodata_start = .;
        
        /* Constant data */
        *(.rodata)
        *(.rodata.*)
        
        /* Runtime type information */
        *(.rtti)
        *(.eh_frame_hdr)
        
        _rodata_end = .;
    }

    /* Initialized data section */
    .data ALIGN(4K) : {
        _data_start = .;
        
        /* Global/static initialized variables */
        *(.data)
        *(.data.*)
        
        /* Constructor/destructor tables */
        *(.init_array)
        *(.fini_array)
        
        _data_end = .;
    }

    /* Uninitialized data section */
    .bss ALIGN(4K) : {
        _bss_start = .;
        
        /* Uninitialized global variables */
        *(COMMON)
        *(.bss)
        *(.bss.*)
        
        /* Kernel stack at end of bss */
        . = ALIGN(4K);
        _kernel_stack_bottom = .;
        . += KERNEL_STACK_SIZE;
        _kernel_stack_top = .;
        
        _bss_end = .;
    }

    /* Debug and unnecessary sections to discard */
    /DISCARD/ : {
        *(.comment)
        *(.eh_frame)
        *(.note.gnu.build-id)
        *(.gcc_except_table)
        *(.dtors)
        *(.dynamic)
        *(.interp)
        *(.hash)
        *(.note)
    }

    /* Symbol definitions for kernel memory layout */
    PROVIDE(_kernel_phys_end = .);
}