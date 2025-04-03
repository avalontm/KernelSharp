; GDT.asm - Funciones para cargar la GDT y recargar segmentos en x86_64
; Este archivo contiene las implementaciones de las funciones _LoadGDT y _ReloadSegments
; utilizadas por el GDTManager.cs

section .text

; Constantes para los selectores de segmento
KERNEL_CODE_SEL equ 0x08    ; Selector de código del kernel (segundo descriptor)
KERNEL_DATA_SEL equ 0x10    ; Selector de datos del kernel (tercer descriptor)


global _LoadGDT          ; Función para cargar la GDT
global _ReloadSegments   ; Función para recargar los registros de segmento
global _SetSegmentRegisters
section .text

; void _LoadGDT(GDTPointer* gdtPtr)
; Carga la GDT especificada por el puntero
_LoadGDT:
    ; La convención de llamada en x86_64 pasa el primer parámetro en RDI
    ; RDI contiene GDTPointer*
    lgdt [rdi]       ; Cargar la GDT usando el puntero
    ret

; void _ReloadSegments(void)
; Recarga los registros de segmento después de cambiar la GDT
_ReloadSegments:
    ; Guardar el estado original
    pushfq              ; Guardar flags
    cli                 ; Deshabilitar interrupciones durante el cambio

    ; Preparar el salto lejos
    lea rax, [rel .reload_data_segments]  ; Obtener dirección de la próxima etiqueta
    
    ; Empujar selector de código y dirección de retorno
    push KERNEL_CODE_SEL    ; Nuevo selector de código
    push rax                ; Dirección donde continuar
    
    ; Realizar un far return para cambiar CS
    ; Esto cambiará CS y RIP atómicamente
    retfq

.reload_data_segments:
    ; En este punto, CS ya ha sido cambiado a KERNEL_CODE_SEL
    
    ; Recargar los segmentos de datos
    mov ax, KERNEL_DATA_SEL
    mov ds, ax
    mov es, ax
    mov fs, ax
    mov gs, ax
    mov ss, ax
    
    ; Restaurar flags (incluyendo el estado de interrupción)
    popfq
    
    ret

    ; void _SetSegmentRegisters(ushort selector)
; Establece los registros de segmento DS, ES, FS, GS y SS con el valor dado
_SetSegmentRegisters:
    ; La convención de llamada en x86_64 pone el primer parámetro en DI
    ; DI ya contiene el selector como valor de 16 bits
    mov ax, di
    
    ; Guardar el estado de interrupciones
    pushfq
    cli                 ; Deshabilitar interrupciones durante el cambio
    
    ; Cambiar todos los segmentos de datos
    mov ds, ax
    mov es, ax
    mov fs, ax
    mov gs, ax
    mov ss, ax          ; Cambiar SS puede requerir estar en una pila válida
    
    ; Restaurar el estado de interrupciones
    popfq
    
    ret