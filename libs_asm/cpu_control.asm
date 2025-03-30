; cpu_control.asm
; Funciones de bajo nivel para acceder a registros de control del CPU
; Para ser compilado con NASM

section .text
global _GetCR0              ; Lee el registro CR0
global _SetCR0              ; Escribe en el registro CR0
global _GetCR3              ; Lee el registro CR3 (directorio de páginas)
global _SetCR3              ; Escribe en el registro CR3

; Lee el valor del registro CR0
; uint _GetCR0()
_GetCR0:
    mov eax, cr0
    ret

; Escribe un valor en el registro CR0
; void _SetCR0(uint value)
_SetCR0:
    push ebp
    mov ebp, esp
    
    mov eax, [ebp+8]     ; value
    mov cr0, eax
    
    pop ebp
    ret

; Lee el valor del registro CR3 (directorio de páginas)
; uint _GetCR3()
_GetCR3:
    mov eax, cr3
    ret

; Escribe un valor en el registro CR3 (carga un nuevo directorio de páginas)
; void _SetCR3(uint value)
_SetCR3:
    push ebp
    mov ebp, esp
    
    mov eax, [ebp+8]     ; value
    mov cr3, eax
    
    pop ebp
    ret