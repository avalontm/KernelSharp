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

; CPUID implementation for x86-64 architecture
; Executes CPUID instruction with the specified leaf number
; and returns results in output parameters

; void _CPUID(uint leaf, ref uint eax, ref uint ebx, ref uint ecx, ref uint edx)
_CPUID:
    ; Save non-volatile registers
    push rbx                    ; rbx is a non-volatile register and must be preserved
    
    ; Get function arguments
    mov eax, edi               ; First argument (leaf) is passed in edi
    mov r10, rsi               ; Second argument (eax pointer) is passed in rsi
    mov r11, rdx               ; Third argument (ebx pointer) is passed in rdx
    mov r8, rcx                ; Fourth argument (ecx pointer) is passed in rcx
    mov r9, r8                 ; Fifth argument (edx pointer) is passed in r8
    
    ; Extract input ecx value if provided (for some CPUID leaves that need it)
    mov ecx, 0                 ; Default to 0
    cmp r8, 0                  ; Check if ecx pointer is null
    je .skip_ecx               ; Skip if null
    mov ecx, [r8]              ; Load input ecx value
    
.skip_ecx:
    ; Execute CPUID instruction
    cpuid                      ; CPU ID with leaf in EAX
    
    ; Store the results in the output parameters
    mov [r10], eax             ; Store eax result
    mov [r11], ebx             ; Store ebx result
    mov [r8], ecx              ; Store ecx result
    mov [r9], edx              ; Store edx result
    
    ; Restore non-volatile registers
    pop rbx
    
    ; Return
    ret