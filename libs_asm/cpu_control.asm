; cpu_control.asm
; Funciones de bajo nivel para acceder a registros de control del CPU en 64 bits
; Para ser compilado con NASM en un entorno de 64 bits

section .text
global _GetCR0              ; Lee el registro CR0
global _SetCR0              ; Escribe en el registro CR0
global _GetCR3              ; Lee el registro CR3 (directorio de páginas)
global _SetCR3              ; Escribe en el registro CR3
global _CPUID


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

; void _CPUID(uint leaf, ref uint eax, ref uint ebx, ref uint ecx, ref uint edx)
_CPUID:
    ; Save non-volatile registers
    push rbx                    ; rbx is a non-volatile register and must be preserved
    
    ; Get function arguments
    ; En x86-64:
    ; rdi = primer argumento (leaf)
    ; rsi = segundo argumento (puntero a eax)
    ; rdx = tercer argumento (puntero a ebx)
    ; rcx = cuarto argumento (puntero a ecx)
    ; r8 = quinto argumento (puntero a edx)
    
    ; Mover leaf a eax para ejecutar CPUID
    mov eax, edi
    
    ; Guardar los punteros de referencia
    mov r9, rdx                 ; Guardar puntero a ebx en r9
    mov r10, rcx                ; Guardar puntero a ecx en r10
    mov r11, r8                 ; Guardar puntero a edx en r11
    
    ; Si necesitamos un valor ecx inicial, cargarlo
    xor ecx, ecx                ; Inicializar ecx a 0 por defecto
    cmp r10, 0                  ; Verificar si el puntero a ecx es nulo
    je .execute_cpuid           ; Saltar si es nulo
    mov ecx, [r10]              ; Cargar el valor inicial de ecx
    
.execute_cpuid:
    ; Ejecutar CPUID
    cpuid
    
    ; Guardar los resultados en los punteros
    mov [rsi], eax              ; Guardar eax en el puntero a eax
    mov [r9], ebx               ; Guardar ebx en el puntero a ebx
    mov [r10], ecx              ; Guardar ecx en el puntero a ecx
    mov [r11], edx              ; Guardar edx en el puntero a edx
    
    ; Restaurar registros no volátiles
    pop rbx
    
    ; Retornar
    ret