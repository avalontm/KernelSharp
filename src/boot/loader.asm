; loader.asm - Enhanced loader for CoreLib initialization with improved multiboot handling
; Modified to give full memory control to the kernel instead of the loader
bits 32
extern Entry                ; External entry point from C#
global _start

; Multiboot Constants
MODULEALIGN       equ     1<<0    ; Align modules on page boundaries
MEMINFO           equ     1<<1    ; Provide memory map
FLAGS             equ     MODULEALIGN | MEMINFO  ; Only request essential info
MODS_FLAG         equ     1<<3    ; Multiboot provides module info (checked later)
MAGIC             equ     0x1BADB002  ; Multiboot magic number
CHECKSUM          equ     -(MAGIC + FLAGS)  ; Checksum required by multiboot

; Memory management constants - removed heap constants as kernel will manage memory
PAGE_SIZE       equ     0x1000         ; 4KB - Page size (kept for reference)

section .data
    ; Debug messages
    init_msg db "KernelSharp: Initializing loader...", 0
    sse_msg db "SSE enabled successfully", 0
    entry_msg db "Calling Entry function...", 0
    modules_msg db "CoreLib address: ", 0
    error_msg db "Error: ", 0
    modules_prep_msg db "Preparing modules...", 0
    modules_done_msg db "Modules prepared successfully", 0
    multiboot_info_msg db "MultibootInfo: ", 0
    multiboot_mods_msg db "Multiboot Modules found: ", 0
    multiboot_mods_addr_msg db "Multiboot Modules address: ", 0
    multiboot_mods_count_msg db "Multiboot Modules count: ", 0
    trampoline_msg db "Trampoline address: ", 0
    debug_msg db "Debugging address: ", 0
    no_mods_msg db "No Multiboot modules found", 0
    ebx_msg db "Original EBX value: ", 0
    multiboot_data_msg db "Multiboot flags: ", 0
    magic_msg db "Possible magic value: ", 0
    success_msg db "Operation completed successfully", 0
    multiboot_flag_check_msg db "Checking multiboot flags: ", 0
    memory_msg db "Multiboot memory map passed to kernel", 0

section .rodata
    ; CoreLib module definition
    corelib_module_def:
        dd 0x52523A52       ; Magic signature 'RR:R'
        dd 2                ; Number of sections
        dd corelib_sections ; Pointer to sections

    ; CoreLib sections
    corelib_sections:
        dd 0x01             ; SectionId for GCStaticRegion
        dd gc_static_data   ; Region start
        dd gc_static_data + 1024  ; Region end

        dd 0x02             ; SectionId for EagerCctor
        dd cctor_data       ; Constructor start
        dd cctor_data + 1024 ; Constructor end
        
        dd 0                ; Section terminator

section .bss
    ; Module initialization areas
    gc_static_data: resb 1024  ; 1KB for GC statics
    cctor_data: resb 1024      ; 1KB for constructors
    
    ; Important pointers
    multiboot_ptr: resd 1    ; Store multiboot info pointer
    modules_ptr: resd 1      ; Store modules pointer
    modules_count: resd 1    ; Store modules count
    trampoline_ptr: resd 1   ; Store return address
    
    ; Screen pointer for output
    screen_ptr: resd 1      ; Current screen position
    
    ; Stack space
    resb 8192               ; 8KB for stack
stack_space:

section .text
    ; Multiboot header - must be in first 8KB of the file
    align 4                
    dd MAGIC               ; 0x1BADB002
    dd FLAGS               ; Multiboot flags
    dd CHECKSUM            ; Checksum

_start:
    ; Store EBX (multiboot info pointer) immediately
    mov dword [multiboot_ptr], ebx
    
    ; Set up the stack
    cli                     ; Disable interrupts
    mov esp, stack_space    ; Set up stack pointer
    
    ; Initialize screen pointer
    mov dword [screen_ptr], 0xB8000
    
    ; Display initialization message
    mov esi, init_msg
    call print_string
    call print_newline
    
    ; Show the original EBX value
    mov esi, ebx_msg
    call print_string
    mov eax, ebx
    call print_hex_eax
    call print_newline
    
    ; Show the saved multiboot_ptr value
    mov esi, multiboot_info_msg
    call print_string
    mov eax, [multiboot_ptr]
    call print_hex_eax
    call print_newline
    
    ; Check and enable SSE
    call enable_sse
    jc error_handler        ; If CF=1, SSE enable failed

    ; Properly detect and validate multiboot info
    call validate_multiboot_info
    
    ; Detect multiboot modules
    call detect_multiboot_modules
    
    ; Prepare CoreLib modules
    call prepare_corelib_modules
    
    ; Select the best modules pointer to use
    call select_best_modules
    
    ; Setup trampoline address
    mov eax, kernel_return
    mov [trampoline_ptr], eax
    
    ; Show module address
    mov esi, modules_msg
    call print_string
    mov eax, [modules_ptr]
    call print_hex_eax
    call print_newline
    
    ; Show trampoline address
    mov esi, trampoline_msg
    call print_string
    mov eax, [trampoline_ptr]
    call print_hex_eax
    call print_newline
    
    ; Message before calling Entry
    mov esi, entry_msg
    call print_string
    call print_newline
    
    ; Show memory management message - memory control now passed to kernel
    mov esi, memory_msg
    call print_string
    call print_newline

    ; Prepare to call Entry with determined values
    push dword [trampoline_ptr]  ; IntPtr Trampoline
    push dword [modules_ptr]     ; IntPtr Modules
    push dword [multiboot_ptr]   ; MultibootInfo* Info - Includes memory map for kernel
    
    ; Call Entry - kernel will initialize its own memory management
    call Entry
    
    ; Clean up stack if we return
    add esp, 12             ; Clean up 3 parameters (4 bytes each)

kernel_return:
    ; Check return code in eax
    test eax, eax
    jnz error_handler
    
    ; All correct, jump to system_halt
    jmp system_halt

error_handler:
    ; Handle errors
    mov esi, error_msg
    call print_string
    
    ; If eax contains an error message, display it
    test eax, eax          ; If eax is 0, no message
    jz .no_message
    mov esi, eax
    call print_string
    
.no_message:
    call print_newline
    jmp system_halt

system_halt:
    cli                     ; Disable interrupts
    hlt                     ; Stop CPU
    jmp system_halt         ; In case of unexpected interrupt

; Validate multiboot information structure
validate_multiboot_info:
    push eax
    push ebx
    push ecx
    
    ; Check if we have a valid multiboot pointer
    mov ebx, [multiboot_ptr]
    test ebx, ebx
    jz .invalid_multiboot
    
    ; Check multiboot magic value if present
    ; Some bootloaders store it at a negative offset
    mov esi, magic_msg
    call print_string
    mov eax, [ebx-4]     ; Check 4 bytes before flags
    call print_hex_eax
    call print_newline
    
    ; Display multiboot flags (first dword)
    mov esi, multiboot_flag_check_msg
    call print_string
    mov eax, [ebx]       ; Load flags
    call print_hex_eax
    call print_newline
    
    ; Print structure size if available (some bootloaders provide this)
    add ebx, 4           ; Skip flags field
    mov esi, multiboot_data_msg
    call print_string
    mov eax, [ebx]       ; May contain structure size or other data
    call print_hex_eax
    call print_newline
    
    ; Success - structure exists even if format may vary
    pop ecx
    pop ebx
    pop eax
    ret
    
.invalid_multiboot:
    mov esi, error_msg
    call print_string
    mov esi, multiboot_info_msg
    call print_string
    call print_newline
    
    ; Continue anyway - we'll use default module if needed
    pop ecx
    pop ebx
    pop eax
    ret

; Function to detect multiboot modules with flexible structure handling
detect_multiboot_modules:
    push eax
    push ebx
    push ecx
    push edx
    
    ; Verify we have multiboot info
    mov ebx, [multiboot_ptr]
    test ebx, ebx
    jz .no_multiboot
    
    ; Print the first 32 bytes of the multiboot structure for debugging
    ; This helps identify the actual structure format provided by your bootloader
    mov ecx, 8           ; Print 8 dwords (32 bytes)
    mov esi, ebx         ; Start of multiboot structure
    
    mov esi, debug_msg
    call print_string
    call print_newline
    
    mov esi, ebx
.debug_loop:
    mov eax, [esi]
    call print_hex_eax
    call print_newline
    add esi, 4
    loop .debug_loop
    
    ; Try to find modules - first check flags
    mov eax, [ebx]         ; Get flags
    
    ; Check both known locations for mods_count
    ; Standard multiboot: offset 20
    mov ecx, [ebx + 20]
    mov esi, multiboot_mods_count_msg
    call print_string
    mov eax, ecx
    call print_hex_eax
    call print_newline
    
    ; Check standard location for mods_addr
    mov eax, [ebx + 24]
    mov esi, multiboot_mods_addr_msg
    call print_string
    call print_hex_eax
    call print_newline
    
    ; If we found modules at standard offset, use them
    test ecx, ecx
    jz .check_alternative
    test eax, eax
    jz .check_alternative
    
    mov [modules_count], ecx
    jmp .done
    
.check_alternative:
    ; Try alternative offsets - some bootloaders use different structure layouts
    ; Alternative layout 1: mods at offset 12/16
    mov ecx, [ebx + 12]
    mov esi, multiboot_mods_count_msg
    call print_string
    mov eax, ecx
    call print_string
    call print_hex_eax
    call print_newline
    
    ; Check alternative mods_addr
    mov eax, [ebx + 16]
    mov esi, multiboot_mods_addr_msg
    call print_string
    call print_hex_eax
    call print_newline
    
    ; If this looks valid, use it
    test ecx, ecx
    jz .no_modules
    test eax, eax
    jz .no_modules
    
    mov [modules_count], ecx
    jmp .done
    
.no_multiboot:
    mov esi, error_msg
    call print_string
    mov esi, debug_msg
    call print_string
    call print_newline
    jmp .done
    
.no_modules:
    mov esi, no_mods_msg
    call print_string
    call print_newline
    
.done:
    pop edx
    pop ecx
    pop ebx
    pop eax
    ret

; Choose the best module pointer available with more flexibility
select_best_modules:
    push eax
    push ebx
    push ecx
    push edx
    
    ; Default to our CoreLib module def (always have a fallback)
    lea eax, [corelib_module_def]
    mov [modules_ptr], eax
    
    ; Display the default module address
    mov esi, modules_msg
    call print_string
    mov eax, [modules_ptr]
    call print_hex_eax
    call print_newline
    
    ; Check if multiboot info exists at all
    mov ebx, [multiboot_ptr]
    test ebx, ebx
    jz .use_default
    
    ; Print first 8 bytes of the multiboot structure
    mov esi, multiboot_data_msg
    call print_string
    mov eax, [ebx]
    call print_hex_eax
    call print_newline
    
    mov esi, debug_msg
    call print_string
    mov eax, [ebx+4]
    call print_hex_eax
    call print_newline
    
    ; Try both standard and alternative offsets for modules
    
    ; Standard layout: mods at offset 20/24
    mov ecx, [ebx + 20]    ; mods_count
    mov edx, [ebx + 24]    ; mods_addr
    
    ; Check if standard offsets have valid data
    test ecx, ecx
    jz .try_alternative
    test edx, edx
    jz .try_alternative
    
    ; Use standard multiboot modules
    mov [modules_ptr], edx
    mov esi, modules_msg
    call print_string
    mov eax, [modules_ptr]
    call print_hex_eax
    call print_string
    call print_newline
    jmp .done
    
.try_alternative:
    ; Alternative layout: try offset 12/16
    mov ecx, [ebx + 12]    ; alt mods_count
    mov edx, [ebx + 16]    ; alt mods_addr
    
    ; Check if alternative offsets have valid data
    test ecx, ecx
    jz .use_default
    test edx, edx
    jz .use_default
    
    ; Use alternative multiboot modules
    mov [modules_ptr], edx
    mov esi, modules_msg
    call print_string
    mov eax, [modules_ptr]
    call print_hex_eax
    call print_newline
    jmp .done

.use_default:
    ; We're already using the default, just continue
    
.done:
    ; Final module address being used
    mov esi, modules_msg
    call print_string
    mov eax, [modules_ptr]
    call print_hex_eax
    call print_newline
    
    pop edx
    pop ecx
    pop ebx
    pop eax
    ret

; Prepare CoreLib modules
prepare_corelib_modules:
    push eax
    push edi
    push ecx
    
    mov esi, modules_prep_msg
    call print_string
    call print_newline
    
    ; Clear GC Static Data
    mov edi, gc_static_data
    mov ecx, 1024
    xor eax, eax
    rep stosb
    
    ; Clear CCTOR Data
    mov edi, cctor_data
    mov ecx, 1024
    xor eax, eax
    rep stosb
    
    ; Show message after setup
    mov esi, modules_done_msg
    call print_string
    call print_newline
    
    pop ecx
    pop edi
    pop eax
    ret

; Function to enable SSE
enable_sse:
    ; Check SSE support
    mov eax, 0x1
    cpuid
    test edx, 1<<25
    jz .sse_not_supported   ; If SSE not supported, fail
    
    ; Enable SSE
    mov eax, cr0
    and ax, 0xFFFB          ; Clear coprocessor emulation CR0.EM
    or ax, 0x2              ; Set coprocessor monitoring CR0.MP
    mov cr0, eax
    mov eax, cr4
    or ax, 3 << 9           ; Set CR4.OSFXSR and CR4.OSXMMEXCPT
    mov cr4, eax
    
    ; Show success message
    mov esi, sse_msg
    call print_string
    call print_newline
    
    clc                     ; Clear carry flag to indicate success
    ret
    
.sse_not_supported:
    ; SSE not supported - show error
    mov esi, error_msg
    call print_string
    call print_newline
    stc                     ; Set carry flag to indicate error
    ret

; Function to print newline
print_newline:
    push eax
    push ebx
    push ecx
    push edx
    
    ; Get current position
    mov ebx, [screen_ptr]
    
    ; Calculate next line position
    mov eax, ebx
    sub eax, 0xB8000        ; Subtract video base
    mov edx, 0              ; Clear high part for division
    mov ecx, 160            ; 80 characters * 2 bytes per line
    div ecx                 ; eax = current line, edx = offset in line
    
    inc eax                 ; Next line
    mul ecx                 ; eax = offset of next line
    add eax, 0xB8000        ; Add base
    
    ; Update screen pointer
    mov [screen_ptr], eax
    
    pop edx
    pop ecx
    pop ebx
    pop eax
    ret

; Function to print EAX as hexadecimal
print_hex_eax:
    push eax
    push ebx
    push ecx
    push edx
    
    mov ecx, 8              ; 8 digits for 32-bit value
    mov ebx, [screen_ptr]   ; Get current screen position
    
    ; Write "0x" prefix
    mov byte [ebx], '0'
    mov byte [ebx+1], 0x0F
    mov byte [ebx+2], 'x'
    mov byte [ebx+3], 0x0F
    add ebx, 4
    
    ; Save original eax value
    push eax
    
    ; Loop for each digit
.print_digit:
    rol eax, 4              ; Rotate left to get most significant digit
    mov edx, eax
    and edx, 0xF            ; Isolate the digit
    
    ; Convert to ASCII
    cmp edx, 10
    jl .decimal
    add edx, 'A' - 10 - '0'
    
.decimal:
    add edx, '0'
    
    ; Write the character
    mov byte [ebx], dl
    mov byte [ebx+1], 0x0F
    add ebx, 2
    
    loop .print_digit
    
    ; Restore original eax value
    pop eax
    
    ; Update screen pointer
    mov [screen_ptr], ebx
    
    pop edx
    pop ecx
    pop ebx
    pop eax
    ret

; Function to print strings
print_string:
    push eax
    push ebx
    push esi
    
    mov ebx, [screen_ptr]   ; Get current screen position
    
.loop:
    mov al, [esi]           ; Load character
    test al, al             ; Check for end of string
    jz .done
    
    mov byte [ebx], al      ; Write character
    mov byte [ebx+1], 0x0F  ; Attribute (white on black)
    
    inc esi                 ; Next character
    add ebx, 2              ; Next screen position
    jmp .loop
    
.done:
    ; Update screen pointer
    mov [screen_ptr], ebx
    
    pop esi
    pop ebx
    pop eax
    ret