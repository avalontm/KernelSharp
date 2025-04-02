; cpu_control.asm
; Funciones de bajo nivel para acceder a registros de control del CPU en 64 bits
; Para ser compilado con NASM en un entorno de 64 bits

section .text
global _GetCR0              ; Lee el registro CR0
global _SetCR0              ; Escribe en el registro CR0
global _GetCR3              ; Lee el registro CR3 (directorio de páginas)
global _SetCR3              ; Escribe en el registro CR3


; Lee el valor del registro CR0
; uint _GetCR0()
_GetCR0:
    mov rax, cr0
    ret

; Escribe un valor en el registro CR0
; void _SetCR0(uint value)
_SetCR0:
    push rbx                 ; Guardar el registro rbx, si es necesario
    mov rbx, [rsp+8]         ; Obtener el valor de 'value' pasado como argumento
    mov cr0, rbx             ; Escribir el valor en CR0
    pop rbx                  ; Restaurar rbx
    ret

; Lee el valor del registro CR3 (directorio de páginas)
; uint _GetCR3()
_GetCR3:
    mov rax, cr3
    ret

; Escribe un valor en el registro CR3 (carga un nuevo directorio de páginas)
; void _SetCR3(uint value)
_SetCR3:
    push rbx                 ; Guardar el registro rbx, si es necesario
    mov rbx, [rsp+8]         ; Obtener el valor de 'value' pasado como argumento
    mov cr3, rbx             ; Escribir el valor en CR3
    pop rbx                  ; Restaurar rbx
    ret
