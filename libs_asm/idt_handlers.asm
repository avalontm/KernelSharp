; Funciones de ensamblador para manejar la IDT (x86-64)

section .text

global _WriteInterruptHandler
_WriteInterruptHandler:
    ret
    
global _CLI
_CLI:
    cli
    ret

; Habilita interrupciones
; void STI()
global _STI
_STI:
    sti                 ; Set Interrupt Flag
    ret   

; Detiene el CPU
; void Halt()
global _Halt
_Halt:
    hlt                 ; Halt instruction
    ret
