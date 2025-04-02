; paging.asm
; Funciones de bajo nivel para manejo de paginación en x86-64
; Para ser compilado con NASM

section .text
global _LoadGDT              ; Carga la GDT y actualiza registros de segmento
global _LoadIDT              ; Carga la IDT
global _ReadCR0              ; Lee el registro CR0
global _WriteCR0             ; Escribe en el registro CR0
global _ReadCR2              ; Lee el registro CR2 (dirección de fallo de página)
global _ReadCR3              ; Lee el registro CR3 (directorio de páginas)
global _WriteCR3             ; Escribe en el registro CR3
global _ReadCR4              ; Lee el registro CR4
global _WriteCR4             ; Escribe en el registro CR4
global _Invlpg               ; Invalida una entrada en la TLB
global _EnableInterrupts     ; Habilita interrupciones
global _DisableInterrupts    ; Deshabilita interrupciones
global _Hlt                  ; Instrucción HLT
global _Reset                ; Reinicio del sistema
global _GetEFlags            ; Obtiene EFLAGS
global _SetEFlags            ; Establece EFLAGS

; Carga la GDT y actualiza los registros de segmento
; void _LoadGDT(void* gdtPointer)
_LoadGDT:
    push rbx
    mov rbx, [rsp+8]        ; gdtPointer
    lgdt [rbx]

    ; Saltamos directamente, ya que no es necesario modificar CS en 64 bits
    jmp .reload_cs

.reload_cs:
    ; Recargar registros de segmento de datos
    mov rax, 0x10           ; 0x10 es el selector de datos del kernel
    mov ds, rax
    mov es, rax
    mov fs, rax
    mov gs, rax
    mov ss, rax

    ; Retornar
    pop rbx
    ret

; Carga la IDT
; void _LoadIDT(void* idtPointer)
_LoadIDT:
    push rbx
    mov rbx, [rsp+8]        ; idtPointer
    lidt [rbx]

    ; Retornar
    pop rbx
    ret

; Lee el valor del registro CR0
; uint64_t _ReadCR0()
_ReadCR0:
    mov rax, cr0
    ret

; Escribe un valor en el registro CR0
; void _WriteCR0(uint64_t value)
_WriteCR0:
    push rbx
    mov rbx, [rsp+8]        ; value
    mov cr0, rbx
    pop rbx
    ret

; Lee el valor del registro CR2 (dirección que causó una falla de página)
; uint64_t _ReadCR2()
_ReadCR2:
    mov rax, cr2
    ret

; Lee el valor del registro CR3 (directorio de páginas)
; uint64_t _ReadCR3()
_ReadCR3:
    mov rax, cr3
    ret

; Escribe un valor en el registro CR3 (carga un nuevo directorio de páginas)
; void _WriteCR3(uint64_t value)
_WriteCR3:
    push rbx
    mov rbx, [rsp+8]        ; value
    mov cr3, rbx
    pop rbx
    ret

; Lee el valor del registro CR4
; uint64_t _ReadCR4()
_ReadCR4:
    mov rax, cr4
    ret

; Escribe un valor en el registro CR4
; void _WriteCR4(uint64_t value)
_WriteCR4:
    push rbx
    mov rbx, [rsp+8]        ; value
    mov cr4, rbx
    pop rbx
    ret

; Invalida una entrada en la TLB para una dirección específica
; void _Invlpg(void* address)
_Invlpg:
    push rbx
    mov rbx, [rsp+8]        ; address
    invlpg [rbx]
    pop rbx
    ret

; Habilita las interrupciones
; void _EnableInterrupts()
_EnableInterrupts:
    sti
    ret

; Deshabilita las interrupciones
; void _DisableInterrupts()
_DisableInterrupts:
    cli
    ret

; Detiene la ejecución del CPU hasta que ocurra una interrupción
; void _Hlt()
_Hlt:
    hlt
    ret

; Reinicia el sistema usando el controlador de teclado
; void _Reset()
_Reset:
    ; Método 1: Usando el controlador de teclado del 8042
    mov al, 0xFE
    out 0x64, al

    ; Si el método anterior falla, intentar con un salto infinito
    jmp _Reset

; Obtiene el estado actual de las banderas (EFLAGS)
; uint64_t _GetEFlags()
_GetEFlags:
    pushfq                 ; Empujar EFLAGS a la pila
    pop rax                ; Obtener el valor en RAX
    ret

; Establece el estado de las banderas (EFLAGS)
; void _SetEFlags(uint64_t flags)
_SetEFlags:
    push rbx
    mov rbx, [rsp+8]        ; flags
    push rbx                ; Poner el valor en la pila
    popfq                   ; Cargar EFLAGS desde la pila
    pop rbx
    ret
