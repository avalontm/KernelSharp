BITS 32

; Multiboot Constants
MODULEALIGN       equ     1<<0
MEMINFO           equ     1<<1
MAGIC             equ     0x1BADB002
FLAGS             equ     MODULEALIGN | MEMINFO
CHECKSUM          equ     -(MAGIC + FLAGS)

section .text
global _start
extern Entry

; Multiboot Header
align 4
mb_header:
    dd MAGIC
    dd FLAGS
    dd CHECKSUM

_start:
    cli                         ; Disable interrupts
    mov [multiboot_ptr], ebx    ; Save Multiboot pointer
    mov esp, stack_top          ; Configure stack

    ; Clear screen
    mov edi, 0xB8000
    mov ecx, 80*25
    mov ax, 0x0720
    rep stosw

    ; Initial message
    mov esi, init_msg
    mov edi, 0xB8000
    call print_string

    ; Mask all PIC interrupts
    mov al, 0xFF
    out 0xA1, al  ; Slave PIC
    out 0x21, al  ; Master PIC

    ; Verify minimum environment requirements
    call check_cpuid
    test eax, eax
    jz no_cpuid

    call check_long_mode
    test eax, eax
    jz no_long_mode

    ; Setup paging
    call setup_paging

    ; Enable SSE
    call enable_sse

    ; Enter 64-bit mode
    call enter_long_mode

    ; Should never reach here
    jmp system_halt

;----------------------------------------------------------
; Basic Functions
;----------------------------------------------------------

print_string:
    pusha
.loop:
    lodsb
    test al, al
    jz .done
    mov ah, 0x07
    mov [edi], ax
    add edi, 2
    jmp .loop
.done:
    popa
    ret

;----------------------------------------------------------
; System Checks
;----------------------------------------------------------

check_cpuid:
    pushfd
    pop eax
    mov ecx, eax
    xor eax, 1 << 21
    push eax
    popfd
    pushfd
    pop eax
    push ecx
    popfd
    xor eax, ecx
    test eax, 1 << 21
    jz .no_cpuid
    mov eax, 1
    ret
.no_cpuid:
    xor eax, eax
    ret

no_cpuid:
    mov esi, cpuid_err_msg
    mov edi, 0xB8000 + 160
    call print_string
    jmp system_halt

check_long_mode:
    mov eax, 0x80000000
    cpuid
    cmp eax, 0x80000001
    jb .no_long_mode
    mov eax, 0x80000001
    cpuid
    test edx, 1 << 29
    jz .no_long_mode
    mov eax, 1
    ret
.no_long_mode:
    xor eax, eax
    ret

no_long_mode:
    mov esi, long_mode_err_msg
    mov edi, 0xB8000 + 160
    call print_string
    jmp system_halt

system_halt:
    mov esi, halt_msg
    mov edi, 0xB8000 + 320
    call print_string
    cli
    hlt
    jmp system_halt

;----------------------------------------------------------
; 64-bit Configuration
;----------------------------------------------------------

setup_paging:
    ; Message
    mov esi, paging_msg
    mov edi, 0xB8000 + 160
    call print_string

    ; Clear page tables
    mov edi, pml4_table
    xor eax, eax
    mov ecx, 4096*3        ; PML4, PDPT, PD
    rep stosb

    ; PML4 -> PDPT
    mov eax, pdpt_table
    or eax, 0b11           ; Present + RW
    mov [pml4_table], eax

    ; PDPT -> PD
    mov eax, pd_table
    or eax, 0b11           ; Present + RW
    mov [pdpt_table], eax

    ; Map first 1GB with 2MB pages
    mov edi, pd_table
    mov eax, 0x00000083    ; Present + RW + Page Size (2MB)
    mov ecx, 512           ; 512 entries = 1GB

.map_pd_entry:
    mov [edi], eax
    add eax, 0x200000      ; Next 2MB page
    add edi, 8
    loop .map_pd_entry
    ret

enable_sse:
    mov eax, cr0
    and ax, 0xFFFB         ; Clear CR0.EM
    or ax, 0x2             ; Set CR0.MP
    mov cr0, eax
    mov eax, cr4
    or eax, (1 << 9) | (1 << 10) ; OSFXSR and OSXMMEXCPT
    mov cr4, eax
    ret

enter_long_mode:
    ; Load page tables
    mov eax, pml4_table
    mov cr3, eax

    ; Enable PAE
    mov eax, cr4
    or eax, 1 << 5
    mov cr4, eax

    ; Enable Long Mode
    mov ecx, 0xC0000080
    rdmsr
    or eax, 1 << 8
    wrmsr

    ; Enable paging
    mov eax, cr0
    or eax, 1 << 31
    mov cr0, eax

    ; Set up a temporary GDT that will be replaced by the kernel
    ; Keep this minimal - just enough to get into long mode
    mov eax, gdt_temp      ; Get address of temporary GDT
    mov [gdt_temp_ptr + 2], eax ; Store in GDT pointer
    lgdt [gdt_temp_ptr]    ; Load the temporary GDT

    ; Jump to 64-bit mode
    jmp 0x08:long_mode_start  ; 0x08 is the code segment selector

;----------------------------------------------------------
; 64-bit Code
;----------------------------------------------------------
bits 64
long_mode_start:
    ; Clear segment registers
    mov ax, 0x10           ; 0x10 is the data segment selector
    mov ds, ax
    mov es, ax
    mov ss, ax
    mov fs, ax
    mov gs, ax

    ; Set stack pointer
    mov rsp, stack_top

    ; Display a message to confirm we're in 64-bit mode
    mov rdi, 0xB8000 + 240
    mov rsi, long_mode_msg
    call print_string_64

    ; Save the temporary GDT information for the kernel
    mov qword [gdt_info_base], gdt_temp
    mov word [gdt_info_limit], gdt_temp_size - 1
    
    ; Setup parameters for Entry according to fastcall convention
    mov rcx, 0                         ; Zero-extend RCX
    mov ecx, [rel multiboot_ptr]       ; First parameter (RCX) - multiboot info
    
    mov rdx, 0                         ; Initialize RDX to 0
    mov rbx, rcx
    test dword [rbx], 0x8              ; Check MODULES flag (bit 3)
    jz .no_modules
    
    mov edx, [rbx + 24]                ; ModAddr is at offset 24 (0x18) of MultibootInfo
    
.no_modules:
    ; Store GDT info as the 3rd parameter (used by some C# kernels)
    mov r8, gdt_info
    
    ; Display calling kernel message
    push rcx
    push rdx
    push r8
    mov rdi, 0xB8000 + 320
    mov rsi, call_kernel_msg
    call print_string_64
    pop r8
    pop rdx
    pop rcx
    
    ; Ensure 16-byte stack alignment
    and rsp, -16
    
    ; Reserve shadow space (required by Windows x64 calling convention)
    sub rsp, 32
    
    ; Call kernel entry point
    call Entry
    
    ; If Entry returns, display message and halt
    mov rdi, 0xB8000 + 400
    mov rsi, kernel_returned_msg
    call print_string_64
    
    ; Halt the system
    cli
    hlt
    jmp $

; Simple 64-bit string printing function
print_string_64:
    push rax
    push rcx
.loop:
    lodsb                   ; Load byte from RSI into AL
    test al, al
    jz .done
    mov ah, 0x0F            ; White text on black background
    mov [rdi], ax
    add rdi, 2
    jmp .loop
.done:
    pop rcx
    pop rax
    ret

;----------------------------------------------------------
; Data Section
;----------------------------------------------------------
section .data
init_msg          db "KernelSharp: Minimal loader initializing...", 0
cpuid_err_msg     db "ERROR: CPUID not supported!", 0
long_mode_err_msg db "ERROR: 64-bit mode not supported!", 0
paging_msg        db "Setting up paging...", 0
halt_msg          db "System halted.", 0
long_mode_msg     db "Successfully entered 64-bit mode.", 0
call_kernel_msg   db "Calling kernel Entry function...", 0
kernel_returned_msg db "Kernel Entry function returned - system halted.", 0

; Temporary GDT structure - Minimal and meant to be replaced by kernel
align 8
gdt_temp:
    dq 0                    ; Null descriptor
    dq 0x00AF9A000000FFFF   ; 64-bit code segment (0x08)
    dq 0x00AF92000000FFFF   ; 64-bit data segment (0x10)
gdt_temp_size equ $ - gdt_temp

; Temporary GDT pointer
gdt_temp_ptr:
    dw gdt_temp_size - 1    ; Size
    dd 0                    ; Base - to be filled at runtime

; Structure to pass GDT info to kernel
gdt_info:
    gdt_info_limit: dw 0    ; Size of GDT
    gdt_info_base: dq 0     ; Base address of GDT

;----------------------------------------------------------
; BSS Section
;----------------------------------------------------------
section .bss
align 4096
pml4_table: resb 4096
pdpt_table: resb 4096
pd_table:   resb 4096

align 16
stack_bottom:
    resb 16384  ; 16KB stack
stack_top:

multiboot_ptr: resq 1