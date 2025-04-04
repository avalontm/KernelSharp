BITS 32

; Multiboot Constants
MODULEALIGN       equ     1<<0
MEMINFO           equ     1<<1
FLAGS             equ     MODULEALIGN | MEMINFO
MAGIC             equ     0x1BADB002
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
    cli
    mov [multiboot_ptr], ebx  ; Save Multiboot pointer
    mov esp, stack_top        ; Configure stack

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

    ; Check CPUID
    call check_cpuid
    test eax, eax
    jz no_cpuid

    ; Check Long Mode
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

    ; Recursive mapping (optional for debugging)
    mov eax, pml4_table
    or eax, 0b11
    mov [pml4_table + 511*8], eax

    ; PML4 -> PDPT
    mov eax, pdpt_table
    or eax, 0b11
    mov [pml4_table], eax

    ; PDPT -> PD
    mov eax, pd_table
    or eax, 0b11
    mov [pdpt_table], eax

    ; Map 1GB with 2MB pages
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

    ; Load 64-bit GDT
    lgdt [gdt64.pointer]

    ; Jump to 64-bit mode
    jmp gdt64.code:long_mode_start

;----------------------------------------------------------
; 64-bit Code
;----------------------------------------------------------
bits 64
long_mode_start:
    ; Disable interrupts
    cli

    ; Clear segment registers
    xor ax, ax
    mov ds, ax
    mov es, ax
    mov fs, ax
    mov gs, ax
    mov ss, ax

    ; Clear general-purpose registers
    xor rax, rax
    xor rbx, rbx
    xor rcx, rcx
    xor rdx, rdx
    xor rsi, rsi
    xor rdi, rdi
    xor rbp, rbp
    xor r8, r8
    xor r9, r9
    xor r10, r10
    xor r11, r11
    xor r12, r12
    xor r13, r13
    xor r14, r14
    xor r15, r15

    ; Set stack pointer
    mov rsp, stack_top

    ; Clear flags
    push 0
    popfq

    ; Invalidate TLB
    mov rax, cr3
    mov cr3, rax

    ; Prepare parameters for FastCall
    ; RCX: Multiboot pointer
    mov rcx, [multiboot_ptr]

    ; Call kernel entry point
    extern Entry
    call Entry

    ; In case of return
    cli
    hlt
    jmp $

;----------------------------------------------------------
; Data Section
;----------------------------------------------------------
section .data
init_msg          db "KernelSharp: Initializing loader...", 0
cpuid_err_msg     db "ERROR: CPUID not supported!", 0
long_mode_err_msg db "ERROR: 64-bit mode not supported!", 0
paging_msg        db "Configuring paging...", 0
halt_msg          db "System halted.", 0

; 64-bit GDT
align 16
gdt64:
    .null: equ $ - gdt64
        dq 0
    .code: equ $ - gdt64
        dq (1 << 43) | (1 << 44) | (1 << 47) | (1 << 53)
    .data: equ $ - gdt64
        dq (1 << 44) | (1 << 47) | (1 << 41)
    .pointer:
        dw $ - gdt64 - 1
        dq gdt64

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