; paging.asm
; Funciones de bajo nivel para manejo de paginación en x86
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
    push ebp
    mov ebp, esp
    
    ; Cargar GDT
    mov eax, [ebp+8]     ; gdtPointer
    lgdt [eax]
    
    ; Recargar CS a través de un far jump
    jmp 0x08:.reload_cs  ; 0x08 es el selector de código del kernel
    
.reload_cs:
    ; Recargar registros de segmento de datos
    mov ax, 0x10         ; 0x10 es el selector de datos del kernel
    mov ds, ax
    mov es, ax
    mov fs, ax
    mov gs, ax
    mov ss, ax
    
    ; Retornar
    pop ebp
    ret

; Carga la IDT
; void _LoadIDT(void* idtPointer)
_LoadIDT:
    push ebp
    mov ebp, esp
    
    ; Cargar IDT
    mov eax, [ebp+8]     ; idtPointer
    lidt [eax]
    
    ; Retornar
    pop ebp
    ret

; Lee el valor del registro CR0
; uint _ReadCR0()
_ReadCR0:
    mov eax, cr0
    ret

; Escribe un valor en el registro CR0
; void _WriteCR0(uint value)
_WriteCR0:
    push ebp
    mov ebp, esp
    
    mov eax, [ebp+8]     ; value
    mov cr0, eax
    
    pop ebp
    ret

; Lee el valor del registro CR2 (dirección que causó una falla de página)
; uint _ReadCR2()
_ReadCR2:
    mov eax, cr2
    ret

; Lee el valor del registro CR3 (directorio de páginas)
; uint _ReadCR3()
_ReadCR3:
    mov eax, cr3
    ret

; Escribe un valor en el registro CR3 (carga un nuevo directorio de páginas)
; void _WriteCR3(uint value)
_WriteCR3:
    push ebp
    mov ebp, esp
    
    mov eax, [ebp+8]     ; value
    mov cr3, eax
    
    pop ebp
    ret

; Lee el valor del registro CR4
; uint _ReadCR4()
_ReadCR4:
    mov eax, cr4
    ret

; Escribe un valor en el registro CR4
; void _WriteCR4(uint value)
_WriteCR4:
    push ebp
    mov ebp, esp
    
    mov eax, [ebp+8]     ; value
    mov cr4, eax
    
    pop ebp
    ret

; Invalida una entrada en la TLB para una dirección específica
; void _Invlpg(void* address)
_Invlpg:
    push ebp
    mov ebp, esp
    
    mov eax, [ebp+8]     ; address
    invlpg [eax]
    
    pop ebp
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
; uint _GetEFlags()
_GetEFlags:
    pushfd                  ; Empujar EFLAGS a la pila
    pop eax                 ; Obtener el valor en EAX
    ret

; Establece el estado de las banderas (EFLAGS)
; void _SetEFlags(uint flags)
_SetEFlags:
    push ebp
    mov ebp, esp
    
    mov eax, [ebp+8]        ; flags
    push eax                ; Poner el valor en la pila
    popfd                   ; Cargar EFLAGS desde la pila
    
    pop ebp
    ret