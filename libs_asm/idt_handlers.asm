; Funciones de ensamblador para manejar la IDT

section .text

; Función para que C# escriba en la tabla de manejadores
global _WriteInterruptHandler
_WriteInterruptHandler:
    push ebx
    mov eax, [esp+8]    ; índice
    mov ebx, [esp+12]   ; puntero al manejador
    mov [_interrupt_handlers + eax*4], ebx
    pop ebx
    ret

; Habilita interrupciones
; void STI()
global _STI
_STI:
    sti                 ; Set Interrupt Flag
    ret

; Deshabilita interrupciones
; void CLI()
global _CLI
_CLI:
    cli                 ; Clear Interrupt Flag
    ret

; Ahora debemos crear stubs de entrada para cada uno de los manejadores de interrupción
; Estos stubs guardarán el estado del CPU y llamarán a nuestros manejadores en C#

; Macro para crear un stub de interrupción sin código de error
%macro ISR_NO_ERROR_CODE 1
global _isr%1
_isr%1:
    ; Empujar un código de error falso (las interrupciones sin código de error no proporcionan uno)
    push dword 0
    ; Empujar el número de interrupción
    push dword %1
    ; Saltar al manejador común
    jmp isr_common_stub
%endmacro

; Macro para crear un stub de interrupción con código de error (el CPU lo empuja automáticamente)
%macro ISR_ERROR_CODE 1
global _isr%1
_isr%1:
    ; El CPU ya empujó el código de error
    ; Empujar el número de interrupción
    push dword %1
    ; Saltar al manejador común
    jmp isr_common_stub
%endmacro

; Stub común para todos los manejadores de interrupción
isr_common_stub:
    ; Guardar todos los registros
    pusha
    
    ; Guardar registros de segmento de datos
    push ds
    push es
    push fs
    push gs
    
    ; Cargar los selectores de segmento del kernel
    mov ax, 0x10        ; Selector de datos del kernel
    mov ds, ax
    mov es, ax
    mov fs, ax
    mov gs, ax
    
    ; Llamar al manejador de C#
    ; Pasamos la dirección de la estructura de pila como parámetro
    mov eax, esp
    push eax
    call [_interrupt_handlers + eax * 4]  ; Llama al manejador apropiado
    add esp, 4          ; Limpia el parámetro
    
    ; Restaurar registros de segmento
    pop gs
    pop fs
    pop es
    pop ds
    
    ; Restaurar registros de propósito general
    popa
    
    ; Limpiar el número de interrupción y código de error
    add esp, 8
    
    ; Retornar de la interrupción
    iret

; Definir los stubs para las primeras 32 interrupciones (excepciones de CPU)
ISR_NO_ERROR_CODE 0     ; División por cero
ISR_NO_ERROR_CODE 1     ; Depuración
ISR_NO_ERROR_CODE 2     ; NMI
ISR_NO_ERROR_CODE 3     ; Breakpoint
ISR_NO_ERROR_CODE 4     ; Overflow
ISR_NO_ERROR_CODE 5     ; Bound Range Exceeded
ISR_NO_ERROR_CODE 6     ; Invalid Opcode
ISR_NO_ERROR_CODE 7     ; Device Not Available
ISR_ERROR_CODE 8        ; Double Fault
ISR_NO_ERROR_CODE 9     ; Coprocessor Segment Overrun
ISR_ERROR_CODE 10       ; Invalid TSS
ISR_ERROR_CODE 11       ; Segment Not Present
ISR_ERROR_CODE 12       ; Stack Fault
ISR_ERROR_CODE 13       ; General Protection Fault
ISR_ERROR_CODE 14       ; Page Fault
ISR_NO_ERROR_CODE 15    ; Reservado
ISR_NO_ERROR_CODE 16    ; x87 FPU Error
ISR_ERROR_CODE 17       ; Alignment Check
ISR_NO_ERROR_CODE 18    ; Machine Check
ISR_NO_ERROR_CODE 19    ; SIMD Floating-Point Exception
ISR_NO_ERROR_CODE 20    ; Virtualization Exception
ISR_ERROR_CODE 21       ; Control Protection Exception
ISR_NO_ERROR_CODE 22    ; Reservado
ISR_NO_ERROR_CODE 23    ; Reservado
ISR_NO_ERROR_CODE 24    ; Reservado
ISR_NO_ERROR_CODE 25    ; Reservado
ISR_NO_ERROR_CODE 26    ; Reservado
ISR_NO_ERROR_CODE 27    ; Reservado
ISR_NO_ERROR_CODE 28    ; Reservado
ISR_NO_ERROR_CODE 29    ; Reservado
ISR_ERROR_CODE 30       ; Security Exception
ISR_NO_ERROR_CODE 31    ; Reservado

; Sección para almacenar los punteros a nuestros manejadores de C#
section .data
global _interrupt_handlers
_interrupt_handlers:
    times 256 dd 0      ; 256 punteros a manejadores, inicializados a 0