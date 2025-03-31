; Memory Detection for KernelSharp
bits 32

; Global function exports to match C# DllImport
global _detect_memory
global _get_memory_map
global _get_total_memory
global _get_lower_memory
global _get_upper_memory

; Memory region types
%define MEMORY_USABLE           1
%define MEMORY_RESERVED         2
%define MEMORY_ACPI_RECLAIMABLE 3
%define MEMORY_NVS              4
%define MEMORY_BAD_MEMORY       5

section .data
    ; Debug messages
    memory_detect_msg db "Detecting System Memory...", 0
    lower_memory_msg db "Lower Memory: ", 0
    upper_memory_msg db "Upper Memory: ", 0
    memory_map_msg db "Generating Memory Map...", 0
    memory_detect_error_msg db "Memory Detection Failed!", 0

section .bss
    ; Memory map storage
    memory_map_entries: resb (32 * 24)  ; Space for 32 memory map entries
    memory_map_count: resd 1            ; Number of memory map entries
    lower_memory: resd 1                ; Lower memory size in KB
    upper_memory: resd 1                ; Upper memory size in KB
    total_memory: resq 1                ; Total memory size in bytes

section .text

; Main memory detection routine
_detect_memory:
    ; Disable interrupts for safe detection
    cli

    ; Reset memory values
    mov dword [memory_map_count], 0
    mov dword [lower_memory], 0
    mov dword [upper_memory], 0
    mov dword [total_memory], 0
    mov dword [total_memory + 4], 0

    ; Detect lower memory
    call detect_lower_memory

    ; Detect extended memory
    call detect_extended_memory

    ; Generate memory map using E820
    call generate_memory_map

    ; Re-enable interrupts
    sti

    ret

; Detect lower memory using INT 0x12 (safe method)
detect_lower_memory:
    ; Preserve registers
    push eax
    push ebx

    ; INT 0x12 - Get lower memory size
    mov ah, 0x88
    int 0x15

    ; Check for carry (error)
    jc .error

    ; Store lower memory size in KB
    mov [lower_memory], ax
    jmp .done

.error:
    ; Set lower memory to a default safe value (e.g., 640)
    mov word [lower_memory], 640

.done:
    pop ebx
    pop eax
    ret

; Detect extended memory using INT 0x15, AX=0xE801 (safe method)
detect_extended_memory:
    ; Preserve registers
    push eax
    push ebx
    push ecx
    push edx

    ; INT 0x15, AX=0xE801 - Get memory size for >64MB configurations
    mov ax, 0xE801
    int 0x15

    ; Check for carry (error)
    jc .error

    ; Check if BX/CX registers are used (some BIOSes use AX/CX)
    cmp ax, 0
    jnz .use_ax_cx

    ; Use BX/CX registers (BX = KB between 1-16MB, CX = KB above 16MB)
    mov ax, bx
    mov cx, dx

.use_ax_cx:
    ; Store upper memory size
    mov [upper_memory], ax
    jmp .done

.error:
    ; Set upper memory to a default safe value
    mov word [upper_memory], 0xF000  ; ~15MB

.done:
    pop edx
    pop ecx
    pop ebx
    pop eax
    ret

; Generate memory map using INT 0x15, AX=0xE820
generate_memory_map:
    push edi
    push ebx
    push ecx
    push edx

    ; Prepare for memory map generation
    mov dword [memory_map_count], 0
    mov edi, memory_map_entries
    xor ebx, ebx            ; Start with 0 for first call
    mov edx, 0x534D4150     ; 'SMAP' signature

.e820_loop:
    ; Prepare for INT 0x15 call
    mov eax, 0xE820
    mov ecx, 24             ; Buffer size
    int 0x15

    ; Check for error or end of list
    jc .e820_done           ; Carry flag set means error
    cmp eax, 0x534D4150     ; Verify 'SMAP' signature
    jne .e820_done

    ; Increment memory map entry count
    inc dword [memory_map_count]

    ; Add usable memory to total
    cmp dword [edi + 16], MEMORY_USABLE
    jne .skip_memory_add

    ; Add length to total memory
    mov eax, [edi + 8]      ; Low 32-bits of length
    mov edx, [edi + 12]     ; High 32-bits of length
    add [total_memory], eax
    adc [total_memory + 4], edx

.skip_memory_add:
    ; Move to next entry
    add edi, 24

    ; Check if more entries
    test ebx, ebx
    jnz .e820_loop

.e820_done:
    pop edx
    pop ecx
    pop ebx
    pop edi
    ret

; Get memory map entries
_get_memory_map:
    ; Return memory map details
    mov eax, memory_map_entries   ; Base address of memory map
    mov ebx, [memory_map_count]   ; Number of entries
    ret

; Get total memory size
_get_total_memory:
    ; Return total memory size in bytes
    mov eax, [total_memory]
    mov edx, [total_memory + 4]
    ret

; Get lower memory size
_get_lower_memory:
    ; Return lower memory size in KB
    mov eax, [lower_memory]
    ret

; Get upper memory size
_get_upper_memory:
    ; Return upper memory size in KB
    mov eax, [upper_memory]
    ret