; loader.asm - Bootloader for KernelSharp with FastCall compatibility
bits 32
extern Entry                ; External entry point from C#
global _start

; Multiboot constants
MODULEALIGN       equ     1<<0    ; Align modules on page boundaries
MEMINFO           equ     1<<1    ; Provide memory map
FLAGS             equ     MODULEALIGN | MEMINFO  ; Essential flags only
MAGIC             equ     0x1BADB002  ; Multiboot magic number 
CHECKSUM          equ     -(MAGIC + FLAGS)  ; Required checksum

; Memory management constants
PAGE_SIZE         equ     0x1000  ; 4KB - Page size

section .multiboot_header
    ; Multiboot header - must be in first 8KB of file
    align 4                
    dd MAGIC               ; 0x1BADB002
    dd FLAGS               ; Flags
    dd CHECKSUM            ; Checksum

section .data
    ; Debug messages
    init_msg db "KernelSharp: Initializing loader...", 0
    stack_msg db "Setting up stack...", 0
    multiboot_msg db "Saving Multiboot information...", 0
    gdt_setup_msg db "Setting up GDT...", 0
    gdt_success_msg db "GDT configured successfully", 0
    idt_setup_msg db "Setting up IDT...", 0
    idt_success_msg db "IDT configured successfully", 0
    a20_msg db "Checking A20 line...", 0
    a20_enabled_msg db "A20 line enabled", 0
    sse_setup_msg db "Setting up SSE...", 0
    sse_enabled_msg db "SSE enabled successfully", 0
    entry_msg db "Calling Entry function with FastCall...", 0
    trampoline_msg db "Setting up trampoline...", 0
    trampoline_success_msg db "Trampoline ready.", 0
    error_msg db "ERROR: ", 0
    halt_msg db "System halted", 0
    
    ; Screen management
    initial_screen_ptr dd 0xB8000  ; Base address of video memory
    initial_screen_col db 0        ; Initial column
    initial_screen_row db 0        ; Initial row
    
    ; Trampoline information
    trampoline_ready dd 0         ; Flag to indicate if trampoline is ready
    trampoline_target dd 0        ; Address of function to call
    
    ; CPU information variables
    cpu_vendor times 16 db 0       ; Space for vendor ID + NULL
    cpu_features dd 0              ; CPU features
    cpu_info_msg db "CPU: ", 0
    cpu_fpu_msg db "FPU available", 0
    cpu_sse_msg db "SSE available", 0
    cpu_sse2_msg db "SSE2 available", 0
    cpu_no_cpuid_msg db "CPUID not available", 0
    
    ; Additional messages
    memory_test_success db "Memory test successful", 0
    memory_test_failed db "Memory test failed", 0
    paging_setup_msg db "Setting up paging...", 0
    paging_enabled_msg db "Paging enabled", 0
    
    ; Magic number for multiboot
    multiboot_magic dd 0x2BADB002  ; Standard multiboot magic value

section .bss
    ; Important pointers
    multiboot_ptr: resd 1          ; Store multiboot info pointer
    
    ; Screen output pointer
    screen_ptr: resd 1             ; Current screen position
    screen_col: resb 1             ; Current column
    screen_row: resb 1             ; Current row
    
    ; Space for trampoline
    trampoline_stack_space: resb 8192  ; 8KB for trampoline stack
    trampoline_stack_top:              ; Top of trampoline stack
    
    ; Stack area (16-byte aligned for C#)
    align 16
    stack_bottom: resb 32768       ; 32KB stack
    stack_space:                   ; Points to top of stack

section .text
_start:
    ; Disable interrupts initially
    cli
    
    ; Save multiboot pointer immediately (from EBX)
    mov [multiboot_ptr], ebx
    
    ; Initialize screen
    mov eax, [initial_screen_ptr]
    mov [screen_ptr], eax
    mov al, [initial_screen_col]
    mov [screen_col], al
    mov al, [initial_screen_row]
    mov [screen_row], al
    
    ; Clear screen
    call clear_screen
    
    ; Show initialization message
    mov esi, init_msg
    call print_string
    call print_newline
    
    ; Setup stack
    mov esi, stack_msg
    call print_string
    call print_newline
    
    ; Set up the stack (16-byte aligned for C#)
    mov esp, stack_space
    and esp, 0xFFFFFFF0    ; Ensure 16-byte alignment
    
    ; Save multiboot info
    mov esi, multiboot_msg
    call print_string
    call print_newline
    
    ; Verify multiboot info is valid
    cmp dword [multiboot_ptr], 0
    je .no_multiboot
    
    ; Set up GDT
    mov esi, gdt_setup_msg
    call print_string
    call print_newline
    call setup_gdt
    mov esi, gdt_success_msg
    call print_string
    call print_newline
    
    ; Enable SSE (important for C#)
    mov esi, sse_setup_msg
    call print_string
    call print_newline
    call enable_sse
    mov esi, sse_enabled_msg
    call print_string
    call print_newline
    
    ; Set up trampoline
    mov esi, trampoline_msg
    call print_string
    call print_newline
    call setup_trampoline
    mov esi, trampoline_success_msg
    call print_string
    call print_newline
    
    ; About to call Entry
    mov esi, entry_msg
    call print_string
    call print_newline
    
    ; Save Entry address for trampoline
    mov [trampoline_target], dword Entry
    
    ; Call Entry function with FastCall convention
    call execute_trampoline
    
    ; If we return, something went wrong
    jmp system_halt

.no_multiboot:
    mov esi, error_msg
    call print_string
    mov esi, multiboot_msg
    call print_string
    call print_newline
    jmp system_halt

; Function to set up trampoline
setup_trampoline:
    ; Initialize trampoline
    mov dword [trampoline_ready], 1
    ret

; Function to execute code through trampoline using FastCall
execute_trampoline:
    pusha               ; Save all registers
    
    ; Switch to trampoline stack
    mov ebp, esp        ; Save current stack
    mov esp, trampoline_stack_top
    
    ; Ensure 16-byte stack alignment
    and esp, 0xFFFFFFF0
    
    ; Set up FastCall parameters
    mov ecx, [multiboot_ptr]     ; First parameter in ECX: multibootInfo pointer
    mov edx, 0x2BADB002          ; Second parameter in EDX: magic number
    
    ; Call Entry function
    call Entry
    
    ; Restore original stack
    mov esp, ebp
    
    popa                ; Restore registers
    ret

; Function to clear the screen
clear_screen:
    pusha
    mov ecx, 80*25          ; Total characters on screen (80x25)
    mov edi, 0xB8000        ; Base address of video memory
    mov ax, 0x0720          ; Attribute (7) and space (ASCII 32)
    rep stosw               ; Repeat STOSW ECX times (fill screen)
    
    ; Reset cursor position
    mov byte [screen_col], 0
    mov byte [screen_row], 0
    mov dword [screen_ptr], 0xB8000
    
    popa
    ret

; Function to print strings
print_string:
    pusha
    
.loop:
    lodsb                       ; Load byte from ESI into AL and increment ESI
    test al, al                 ; Check if end of string (0)
    jz .done
    
    call print_char
    jmp .loop
    
.done:
    popa
    ret

; Function to print a character
print_char:
    pusha
    
    ; Check if newline
    cmp al, 10                  ; Newline (ASCII 10)
    je .newline
    
    ; Calculate position in video memory
    movzx edx, byte [screen_row] ; Current row
    imul edx, 80*2              ; 80 characters per row, 2 bytes per character
    movzx ecx, byte [screen_col] ; Current column
    imul ecx, 2                 ; 2 bytes per character
    add edx, ecx                ; Total position
    add edx, 0xB8000            ; Add base address
    
    ; Write character
    mov [edx], al               ; Character
    mov byte [edx+1], 0x0F      ; Attribute (white on black)
    
    ; Increment column
    inc byte [screen_col]
    
    ; Check if we reached end of line
    cmp byte [screen_col], 80
    jl .done
    
.newline:
    ; Go to next line
    mov byte [screen_col], 0
    inc byte [screen_row]
    
    ; Check if we need to scroll
    cmp byte [screen_row], 25
    jl .done
    
    ; Implement basic scrolling - set row to last line
    mov byte [screen_row], 24
    
.done:
    ; Update screen pointer
    movzx edx, byte [screen_row] ; Current row
    imul edx, 80*2              ; 80 characters per row, 2 bytes per character
    movzx ecx, byte [screen_col] ; Current column
    imul ecx, 2                 ; 2 bytes per character
    add edx, ecx                ; Total position
    add edx, 0xB8000            ; Add base address
    mov [screen_ptr], edx
    
    popa
    ret

; Function to print newline
print_newline:
    pusha
    mov al, 10                  ; Newline
    call print_char
    popa
    ret

; Function to set up GDT
setup_gdt:
    ; Load GDT
    lgdt [gdt_descriptor]
    
    ; Reload segment registers
    mov ax, 0x10            ; Data segment
    mov ds, ax
    mov es, ax
    mov fs, ax
    mov gs, ax
    mov ss, ax

    ; Far jump to reload CS
    jmp 0x08:.reload_cs

.reload_cs:
    ret

; Function to enable SSE
enable_sse:
    ; Check for SSE support
    mov eax, 0x1
    cpuid
    test edx, 1<<25
    jz .sse_not_supported
    
    ; Enable SSE
    mov eax, cr0
    and ax, 0xFFFB          ; Clear coprocessor emulation CR0.EM
    or ax, 0x2              ; Set coprocessor monitoring CR0.MP
    mov cr0, eax
    mov eax, cr4
    or ax, 3 << 9           ; Set CR4.OSFXSR and CR4.OSXMMEXCPT
    mov cr4, eax
    
    ret
    
.sse_not_supported:
    ret

; Function to print a hex value in EAX
print_hex:
    pusha
    mov ecx, 8              ; 8 digits for 32-bit value
    
    ; Print "0x" prefix
    mov al, '0'
    call print_char
    mov al, 'x'
    call print_char
    
.print_digit:
    rol eax, 4              ; Rotate left to get most significant digit
    mov edx, eax
    and edx, 0xF            ; Isolate digit
    
    ; Convert to ASCII
    cmp edx, 10
    jl .decimal
    add edx, 'A' - 10 - '0'
    
.decimal:
    add edx, '0'
    
    ; Print digit
    mov al, dl
    call print_char
    
    loop .print_digit
    
    popa
    ret

; System halt
system_halt:
    ; Show halt message
    mov esi, halt_msg
    call print_string
    call print_newline
    
    ; Disable interrupts
    cli
    
    ; Halt CPU
    hlt
    
    ; Just in case, infinite loop
    jmp system_halt

; GDT Definition
align 8
gdt_start:
    ; Null descriptor
    dq 0                    ; 8 bytes of zeros
    
    ; Code segment
    dw 0xFFFF               ; Limit [0:15]
    dw 0x0000               ; Base [0:15]
    db 0x00                 ; Base [16:23]
    db 10011010b            ; Access (P=1, DPL=00, S=1, E=1, DC=0, RW=1, A=0)
    db 11001111b            ; Granularity (G=1, D=1, L=0, AVL=0) + Limit [16:19]
    db 0x00                 ; Base [24:31]
    
    ; Data segment
    dw 0xFFFF               ; Limit [0:15]
    dw 0x0000               ; Base [0:15]
    db 0x00                 ; Base [16:23]
    db 10010010b            ; Access (P=1, DPL=00, S=1, E=0, DC=0, RW=1, A=0)
    db 11001111b            ; Granularity (G=1, D=1, L=0, AVL=0) + Limit [16:19]
    db 0x00                 ; Base [24:31]
gdt_end:

; GDT descriptor
gdt_descriptor: 
    dw gdt_end - gdt_start - 1 ; Size of GDT (minus 1) 
    dd gdt_start               ; Start address of GDT

; IDT
align 8
idt_start:
    times 256 dq 0         ; 256 empty entries
idt_end:

idt_descriptor:
    dw idt_end - idt_start - 1   ; Size of IDT (minus 1)
    dd idt_start                 ; Start address of IDT

; Function to set up IDT
setup_idt:
    ; Load IDT
    lidt [idt_descriptor]
    ret

; PIC (Programmable Interrupt Controller) handling functions
pic_remap:
    ; Send initialization commands to PICs
    mov al, 0x11           ; Cascade mode initialization
    out 0x20, al           ; Send to PIC 1
    out 0xA0, al           ; Send to PIC 2
    
    ; Set the vectors offset
    mov al, 0x20           ; PIC 1 starts at 0x20 (32)
    out 0x21, al
    mov al, 0x28           ; PIC 2 starts at 0x28 (40)
    out 0xA1, al
    
    ; Tell PICs how they are connected
    mov al, 0x04           ; PIC 1 has PIC 2 at IRQ 2
    out 0x21, al
    mov al, 0x02           ; PIC 2 is connected through IRQ 2
    out 0xA1, al
    
    ; Put PICs in 8086 mode
    mov al, 0x01           ; 8086 mode
    out 0x21, al
    out 0xA1, al
    
    ; Mask all interrupts except IRQ 1 (keyboard)
    mov al, 0xFD           ; Enable only IRQ 1
    out 0x21, al
    mov al, 0xFF           ; Disable all IRQs of PIC 2
    out 0xA1, al
    
    ret

; Function to enable basic paging (identity mapping)
setup_paging:
    ; Display message
    mov esi, paging_setup_msg
    call print_string
    call print_newline
    
    ; Reserve space for page tables
    ; This is a very basic setup for identity paging
    
    ; 1. Clear page directory
    mov edi, 0x100000      ; Directory at 1MB
    mov ecx, 1024          ; 1024 entries
    xor eax, eax           ; Value 0
    rep stosd              ; Repeat STOSD (store doubleword) ECX times
    
    ; 2. Set up first page table (0-4MB)
    mov edi, 0x101000      ; First page table (after directory)
    mov eax, 0x003         ; Present + Read/Write
    mov ecx, 1024          ; 1024 entries
    
.setup_page_table:
    stosd                  ; Store EAX and advance EDI
    add eax, 0x1000        ; Next page (4KB)
    loop .setup_page_table
    
    ; 3. Point first directory entry to page table
    mov dword [0x100000], 0x101003   ; Present + Read/Write + table at 0x101000
    
    ; 4. Load CR3 with directory address
    mov eax, 0x100000
    mov cr3, eax
    
    ; 5. Enable paging
    mov eax, cr0
    or eax, 0x80000000     ; Set PG bit
    mov cr0, eax
    
    ; Display success message
    mov esi, paging_enabled_msg
    call print_string
    call print_newline
    
    ret

; Function to test memory access
test_memory:
    pusha
    
    ; Simple test: try to write to some addresses
    ; and verify if the value is maintained
    
    ; Base address
    mov edi, 0x100000      ; 1MB
    
    ; Save original value
    mov edx, [edi]
    
    ; Write a pattern
    mov dword [edi], 0xAA55AA55
    
    ; Verify if the value was maintained
    cmp dword [edi], 0xAA55AA55
    jne .failed
    
    ; Restore original value
    mov [edi], edx
    
    ; Test successful
    mov esi, memory_test_success
    call print_string
    call print_newline
    
    popa
    ret
    
.failed:
    ; Test failed
    mov esi, memory_test_failed
    call print_string
    call print_newline
    
    popa
    ret

; Function to get CPU information
get_cpu_info:
    pusha
    
    ; Check if CPUID is available
    pushfd                 ; Save EFLAGS
    pop eax                ; Load EFLAGS into EAX
    mov ecx, eax           ; Save original
    xor eax, 0x200000      ; Flip ID bit (bit 21)
    push eax               ; Save modified
    popfd                  ; Load modified EFLAGS
    pushfd                 ; Save result
    pop eax                ; Load into EAX
    push ecx               ; Restore original EFLAGS
    popfd
    
    ; Compare to see if the bit stayed changed
    xor eax, ecx
    test eax, 0x200000
    jz .no_cpuid           ; If it didn't change, CPUID not available
    
    ; CPUID available, get information
    mov eax, 0             ; Function 0: Get vendor ID
    cpuid
    
    ; Save vendor ID
    mov [cpu_vendor], ebx
    mov [cpu_vendor+4], edx
    mov [cpu_vendor+8], ecx
    mov byte [cpu_vendor+12], 0 ; Ensure NULL-terminated
    
    ; Get CPU features
    mov eax, 1             ; Function 1: Get features
    cpuid
    
    ; Save features
    mov [cpu_features], edx
    
    ; Print information
    mov esi, cpu_info_msg
    call print_string
    mov esi, cpu_vendor
    call print_string
    call print_newline
    
    ; Check specific features
    test edx, 1<<0
    jz .no_fpu
    mov esi, cpu_fpu_msg
    call print_string
    call print_newline
.no_fpu:
    
    test edx, 1<<25
    jz .no_sse
    mov esi, cpu_sse_msg
    call print_string
    call print_newline
.no_sse:
    
    test edx, 1<<26
    jz .no_sse2
    mov esi, cpu_sse2_msg
    call print_string
    call print_newline
.no_sse2:
    
    popa
    ret
    
.no_cpuid:
    mov esi, cpu_no_cpuid_msg
    call print_string
    call print_newline
    
    popa
    ret

; Trampoline callback implementation using FastCall convention
; This function can be called from C# code to request bootloader services
global __trampoline_callback
__trampoline_callback:
    ; Preserve registers
    push ebp
    mov ebp, esp
    push ebx
    push esi
    push edi
    
    ; Get parameters (FastCall convention)
    ; ECX = function code
    ; EDX = parameter 1
    ; [ebp+8] = parameter 2 (if needed)
    
    cmp ecx, 0             ; Function 0: text output
    je .print_function
    cmp ecx, 1             ; Function 1: memory control
    je .memory_function
    jmp .unknown_function
    
.print_function:
    ; Print function
    mov esi, edx           ; Text pointer from EDX
    call print_string
    mov eax, 1             ; Success
    jmp .done
    
.memory_function:
    ; Memory function (example: get available memory)
    mov eax, [multiboot_ptr]
    cmp eax, 0
    je .memory_error
    
    ; Check if we have memory information
    mov ebx, [eax]         ; Load flags
    test ebx, 1            ; Check MEMORY flag
    jz .memory_error
    
    ; Get memory information and return it
    mov eax, edx           ; Pointer where to store result
    cmp eax, 0
    je .memory_error
    
    mov ebx, [multiboot_ptr]
    mov ecx, [ebx+4]       ; mem_lower
    mov edx, [ebx+8]       ; mem_upper
    
    mov [eax], ecx         ; Store mem_lower
    mov [eax+4], edx       ; Store mem_upper
    
    mov eax, 1             ; Success
    jmp .done
    
.memory_error:
    xor eax, eax           ; Error
    jmp .done
    
.unknown_function:
    ; Unknown function
    xor eax, eax           ; Return 0 to indicate error
    
.done:
    ; Restore registers and return
    pop edi
    pop esi
    pop ebx
    pop ebp
    ret

; Legacy compatibility wrapper for trampoline callback
; This ensures backward compatibility with code that uses cdecl convention
global _trampoline_callback
_trampoline_callback:
    ; Preserve registers
    push ebp
    mov ebp, esp
    push ebx
    push esi
    push edi
    
    ; Get parameters (cdecl convention)
    ; [ebp+8] = function to execute
    ; [ebp+12] = parameter 1
    ; [ebp+16] = parameter 2
    
    ; Convert to FastCall convention
    mov ecx, [ebp+8]       ; Function code to ECX
    mov edx, [ebp+12]      ; Parameter 1 to EDX
    push dword [ebp+16]    ; Parameter 2 to stack if needed
    
    ; Call FastCall version
    call __trampoline_callback
    
    ; Clean up
    add esp, 4             ; Remove parameter from stack
    
    ; Restore registers and return
    pop edi
    pop esi
    pop ebx
    pop ebp
    ret

; Service handler for kernel calls - using FastCall
global _trampoline_service
_trampoline_service:
    ; Save registers
    push ebp
    mov ebp, esp
    push ebx
    push esi
    push edi
    
    ; Convert to FastCall parameters
    mov ecx, [ebp+8]       ; Service code to ECX
    mov edx, [ebp+12]      ; Parameter 1 to EDX
    
    ; Determine which service is requested
    cmp ecx, 0             ; Service 0: Debug output
    je .debug_service
    cmp ecx, 1             ; Service 1: Hardware information
    je .hardware_service
    cmp ecx, 2             ; Service 2: Paging management
    je .paging_service
    
    ; Unknown service
    xor eax, eax
    jmp .exit_service
    
.debug_service:
    ; Debug service - print message
    mov esi, edx           ; Text pointer in EDX
    test esi, esi
    jz .debug_error
    
    call print_string
    call print_newline
    
    mov eax, 1             ; Success
    jmp .exit_service
    
.debug_error:
    xor eax, eax           ; Error
    jmp .exit_service
    
.hardware_service:
    ; Hardware information service
    mov eax, [ebp+12]      ; Subservice
    
    ; Subservice 0: CPU information
    cmp eax, 0
    je .cpu_info
    
    ; Subservice 1: Available memory
    cmp eax, 1
    je .memory_info
    
    ; Unknown subservice
    xor eax, eax
    jmp .exit_service
    
.cpu_info:
    ; Get and return CPU information
    call get_cpu_info
    mov eax, [cpu_features]
    jmp .exit_service
    
.memory_info:
    ; Get and return memory information
    mov eax, [multiboot_ptr]
    test eax, eax
    jz .mem_info_error
    
    mov ebx, [eax]         ; Flags
    test ebx, 1            ; MEMORY flag
    jz .mem_info_error
    
    ; Return information
    mov edx, [ebp+16]      ; Pointer where to store result
    mov ecx, [eax+4]       ; mem_lower
    mov [edx], ecx
    mov ecx, [eax+8]       ; mem_upper
    mov [edx+4], ecx
    
    mov eax, 1             ; Success
    jmp .exit_service
    
.mem_info_error:
    xor eax, eax           ; Error
    jmp .exit_service
    
.paging_service:
    ; Paging service
    mov eax, [ebp+12]      ; Subservice
    
    ; Subservice 0: Enable paging
    cmp eax, 0
    je .enable_paging
    
    ; Subservice 1: Map page
    cmp eax, 1
    je .map_page
    
    ; Unknown subservice
    xor eax, eax
    jmp .exit_service
    
.enable_paging:
    ; Enable basic paging
    call setup_paging
    mov eax, 1             ; Success
    jmp .exit_service
    
.map_page:
    ; Map a page
    ; [ebp+16] = virtual address
    ; [ebp+20] = physical address
    
    mov eax, [ebp+16]      ; Virtual address
    mov ebx, [ebp+20]      ; Physical address
    
    ; Calculate page table address
    mov ecx, eax
    shr ecx, 22            ; Get directory index (top 10 bits)
    and ecx, 0x3FF         ; Ensure only 10 bits
    
    ; Calculate page table entry
    mov edx, eax
    shr edx, 12            ; Discard page offset
    and edx, 0x3FF         ; Get page index (middle 10 bits)
    
    ; Get page directory from CR3
    mov esi, cr3
    
    ; Check if table exists
    mov edi, [esi + ecx*4]
    test edi, 1            ; Check present bit
    jnz .table_exists
    
    ; Create new page table
    ; Here we would need to allocate memory for the table, but for simplicity
    ; we'll assume it already exists or use a predefined location
    
    ; For now, just set an error code
    xor eax, eax
    jmp .exit_service
    
.table_exists:
    ; Get table address
    and edi, 0xFFFFF000    ; Clear flags
    
    ; Write entry to table
    or ebx, 3              ; Present and writable
    mov [edi + edx*4], ebx
    
    ; Update TLB
    invlpg [eax]
    
    mov eax, 1             ; Success
    
.exit_service:
    ; Restore registers and return
    pop edi
    pop esi
    pop ebx
    pop ebp
    ret
