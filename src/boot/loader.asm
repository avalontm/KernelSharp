BITS 32

; Constantes Multiboot
MODULEALIGN       equ     1<<0
MEMINFO           equ     1<<1
FLAGS             equ     MODULEALIGN | MEMINFO
MAGIC             equ     0x1BADB002
CHECKSUM          equ     -(MAGIC + FLAGS)

section .text
global _start
global kernel_trampoline
extern Entry

; Cabecera Multiboot (primeros 8KB)
align 4
mb_header:
    dd MAGIC
    dd FLAGS
    dd CHECKSUM

_start:
    cli
    mov [multiboot_ptr], ebx  ; Guardar puntero Multiboot
    mov esp, stack_top        ; Configurar stack

    ; Limpiar pantalla
    mov edi, 0xB8000
    mov ecx, 80*25
    mov ax, 0x0720
    rep stosw

    ; Mensaje inicial
    mov esi, init_msg
    mov edi, 0xB8000
    call print_string

    ; Verificar CPUID
    call check_cpuid
    test eax, eax
    jz no_cpuid

    ; Verificar Long Mode
    call check_long_mode
    test eax, eax
    jz no_long_mode

    ; Configurar paginación
    call setup_paging

    ; Habilitar SSE
    call enable_sse

    ; Entrar en modo 64-bit
    call enter_long_mode

    ; Nunca debería llegar aquí
    jmp system_halt

;----------------------------------------------------------
; Funciones básicas
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
; Verificaciones del sistema
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
; Configuración para 64-bit
;----------------------------------------------------------

setup_paging:
    ; Mensaje
    mov esi, paging_msg
    mov edi, 0xB8000 + 160
    call print_string

    ; Limpiar tablas de páginas
    mov edi, pml4_table
    xor eax, eax
    mov ecx, 4096*3        ; PML4, PDPT, PD
    rep stosb

    ; Mapeo recursivo (opcional para depuración)
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

    ; Mapear 1GB con páginas de 2MB
    mov edi, pd_table
    mov eax, 0x00000083    ; Presente + RW + Tamaño de página (2MB)
    mov ecx, 512           ; 512 entradas = 1GB

.map_pd_entry:
    mov [edi], eax
    add eax, 0x200000      ; Siguiente página de 2MB
    add edi, 8
    loop .map_pd_entry
    ret

enable_sse:
    mov eax, cr0
    and ax, 0xFFFB         ; Clear CR0.EM
    or ax, 0x2             ; Set CR0.MP
    mov cr0, eax
    mov eax, cr4
    or eax, (1 << 9) | (1 << 10) ; OSFXSR y OSXMMEXCPT
    mov cr4, eax
    ret

enter_long_mode:
    ; Cargar tablas de páginas
    mov eax, pml4_table
    mov cr3, eax

    ; Habilitar PAE
    mov eax, cr4
    or eax, 1 << 5
    mov cr4, eax

    ; Habilitar Long Mode
    mov ecx, 0xC0000080
    rdmsr
    or eax, 1 << 8
    wrmsr

    ; Habilitar paginación
    mov eax, cr0
    or eax, 1 << 31
    mov cr0, eax

    ; Cargar GDT 64-bit
    lgdt [gdt64.pointer]

    ; Saltar a modo 64-bit
    jmp gdt64.code:long_mode_start

;----------------------------------------------------------
; Trampolín 64-bit
;----------------------------------------------------------
bits 64
kernel_trampoline:
    push rbp
    mov rbp, rsp

    ; Preservar registros importantes
    push rbx
    push r12
    push r13
    push r14
    push r15

    ; Pasar parámetros a Entry (FastCall)
    mov rcx, [multiboot_ptr]  ; Primer parámetro en RCX
    lea rdx, [rel kernel_trampoline]  ; Segundo parámetro en RDX

    ; Llamar al punto de entrada del kernel
    extern Entry
    call Entry

    ; Restaurar registros
    pop r15
    pop r14
    pop r13
    pop r12
    pop rbx

    ; Restaurar stack
    mov rsp, rbp
    pop rbp
    ret

;----------------------------------------------------------
; Código 64-bit
;----------------------------------------------------------
long_mode_start:
    ; Configurar segmentos
    mov ax, gdt64.data
    mov ds, ax
    mov es, ax
    mov fs, ax
    mov gs, ax
    mov ss, ax

    ; Limpiar pantalla
    mov rdi, 0xB8000
    mov rcx, 80*25/4       ; Usamos STOSQ para mayor eficiencia
    mov rax, 0x0720072007200720
    rep stosq

    ; Mensaje de éxito
    mov rdi, 0xB8000
    mov rsi, success64_msg
    call print_string64

    ; Llamar al trampolín
    call kernel_trampoline

    ; En caso de retorno
    cli
    hlt
    jmp $

print_string64:
    push rax
    push rcx
    mov ah, 0x0F
.loop:
    lodsb
    test al, al
    jz .done
    mov [rdi], al
    mov [rdi+1], ah
    add rdi, 2
    jmp .loop
.done:
    pop rcx
    pop rax
    ret


;----------------------------------------------------------
; Sección de datos
;----------------------------------------------------------
section .data
init_msg          db "KernelSharp: Inicializando loader...", 0
cpuid_err_msg     db "ERROR: CPUID no soportado!", 0
long_mode_err_msg db "ERROR: Modo 64-bit no soportado!", 0
paging_msg        db "Configurando paginacion...", 0
success64_msg     db "Modo 64-bit activado correctamente", 0
halt_msg          db "Sistema detenido.", 0

; GDT 64-bit
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
; Sección BSS
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
