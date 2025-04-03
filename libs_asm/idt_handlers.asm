global _GetInterruptStubAddress
global _InterruptStub
global _WriteInterruptHandler
global _CLI
global _STI
global _Halt

; Declarar el símbolo externo para la tabla de handlers definida en C#
extern _handlerTable

section .text

; Devuelve la dirección del stub de interrupción
_GetInterruptStubAddress:
    lea rax, [_InterruptStub]
    ret

; Stub de interrupción común para todas las entradas de la IDT
_InterruptStub:
    ; Guardar registros
    push rax
    push rcx
    push rdx
    push rbx
    push rsp
    push rbp
    push rsi
    push rdi
    
    ; Número de interrupción (en una implementación real, cada entrada tendría su propio valor)
    mov rax, 0
    push rax
    
    ; Código de error (ficticio para consistencia)
    push qword 0
    
    ; Llamar a HandleInterrupt en C#
    mov rcx, rsp
    sub rsp, 32    ; Shadow space para convención Microsoft x64
    extern HandleInterrupt
    call HandleInterrupt
    add rsp, 32
    
    ; Quitar número de interrupción y código de error
    add rsp, 16
    
    ; Restaurar registros
    pop rdi
    pop rsi
    pop rbp
    pop rsp
    pop rbx
    pop rdx
    pop rcx
    pop rax
    
    ; Retornar de la interrupción
    iretq


; Deshabilita interrupciones
_CLI:
    cli
    ret

; Habilita interrupciones
_STI:
    sti
    ret

; Detiene el CPU
_Halt:
    hlt
    ret