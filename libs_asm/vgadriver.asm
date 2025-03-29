section .text
global _SetVGAMode

; void SetVGAMode(byte mode)
_SetVGAMode:
    mov ah, 0x00       ; Función 0x00 - Establecer modo de video
    mov al, [esp + 4]  ; Obtener el modo deseado
    int 0x10           ; Llamar a la interrupción BIOS
    ret