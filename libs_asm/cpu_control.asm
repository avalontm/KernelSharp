; cpu_control.asm
; Funciones de bajo nivel para acceder a registros de control del CPU en 64 bits
; Para ser compilado con NASM en un entorno de 64 bits

section .text
global _GetCR0              ; Lee el registro CR0
global _SetCR0              ; Escribe en el registro CR0
global _GetCR3              ; Lee el registro CR3 (directorio de páginas)
global _SetCR3              ; Escribe en el registro CR3
global _CPUID
global _Nop

; Pause instruction (for busy-waiting)
_Pause:
    pause
    ret

; Nop instruction (for busy-waiting)
_Nop:
    nop
    ret
    
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
    ; Prólogo: Preservar todos los registros no volátiles
    push rbx
    push r12
    push r13
    push r14
    push r15
    push rbp
    
    ; Preparación de argumentos
    ; RCX = leaf
    ; RDX = puntero a eax
    ; R8  = puntero a ebx
    ; R9  = puntero a ecx
    ; [rsp+64] = puntero a edx

    ; Inicializar valores
    mov eax, ecx            ; Mover leaf a EAX
    xor ebx, ebx            ; Limpiar EBX
    xor ecx, ecx            ; Limpiar ECX
    xor edx, edx            ; Limpiar EDX

    ; Verificaciones de punteros
    test rdx, rdx           ; Verificar puntero a EAX
    jz .error_invalid_pointer

    ; Cargar valor inicial de EAX
    mov eax, [rdx]

    ; Verificar puntero a ECX
    test r9, r9
    jz .skip_ecx_load
    mov ecx, [r9]
.skip_ecx_load:

    ; Ejecutar CPUID
    cpuid

    ; Guardar resultados
    mov [rdx], eax          ; Guardar EAX
    
    ; Verificar puntero a EBX
    test r8, r8
    jz .skip_ebx_store
    mov [r8], ebx           ; Guardar EBX
.skip_ebx_store:

    ; Verificar puntero a ECX
    test r9, r9
    jz .skip_ecx_store
    mov [r9], ecx           ; Guardar ECX
.skip_ecx_store:

    ; Verificar puntero a EDX
    mov r12, [rsp+64]       ; Puntero a EDX
    test r12, r12
    jz .cleanup
    mov [r12], edx          ; Guardar EDX
    jmp .cleanup

.error_invalid_pointer:
    ; Manejo de error de puntero inválido
    ; Podrías añadir código para manejar este caso, como establecer un código de error
    xor eax, eax            ; Establecer EAX a cero
    jmp .cleanup

.cleanup:
    ; Restaurar registros
    pop rbp
    pop r15
    pop r14
    pop r13
    pop r12
    pop rbx
    
    ret