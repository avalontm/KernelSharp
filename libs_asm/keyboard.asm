; keyboard.asm - Funciones de teclado para KernelSharp
section .text
global _CheckKeyAvailable
global _ReadKeyFromBIOS

; bool CheckKeyAvailable() - Verifica si hay una tecla disponible en el buffer
_CheckKeyAvailable:
    push ebp
    mov ebp, esp
    
    ; Usar INT 16h, AH=01 para verificar disponibilidad de tecla
    mov ah, 01h
    int 16h
    
    ; Si ZF=0, hay una tecla disponible
    ; Convertir resultado a valor booleano para C#
    setnz al        ; AL = 1 si ZF=0 (hay tecla), AL = 0 si ZF=1 (no hay tecla)
    movzx eax, al   ; Extender AL a EAX sin signo
    
    pop ebp
    ret

; char ReadKeyFromBIOS() - Lee una tecla del buffer de teclado
_ReadKeyFromBIOS:
    push ebp
    mov ebp, esp
    
    ; Usar INT 16h, AH=00 para leer una tecla
    ; Esta función espera hasta que haya una tecla disponible
    mov ah, 00h
    int 16h
    
    ; El carácter ASCII está en AL
    ; Descartar el código de escaneo que está en AH
    movzx eax, al   ; Extender AL a EAX sin signo
    
    pop ebp
    ret