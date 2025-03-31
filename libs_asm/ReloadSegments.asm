; ReloadSegments.asm - Función para recargar los selectores de segmento

section .text

global _ReloadSegments
_ReloadSegments:
    ; Recargar CS requiere un salto lejano (far jump)
    ; jmp 0x08:.reload_CS  ; 0x08 es el selector de código (primer descriptor después del nulo)
    ; Método alternativo usando push y retf
    push dword 0x08        ; Selector de código
    push dword .reload_CS  ; Dirección de retorno
    retf                   ; Retorno lejano (far return) - cambia CS:IP

.reload_CS:
    ; Recargar los otros registros de segmento
    mov ax, 0x10           ; 0x10 es el selector de datos (segundo descriptor después del nulo)
    mov ds, ax
    mov es, ax
    mov fs, ax
    mov gs, ax
    mov ss, ax
    ret

