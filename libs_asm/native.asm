; CPU operations for x86_64 (64-bit)
section .text
global _Stosb
global _Movsb
global _Pause
global _ReadMSR
global _WriteMSR
global _GetAPICID
global _KBControllerSendCommand

; Lee un registro MSR
; ulong ReadMSR(uint msr)
_ReadMSR:
    mov ecx, ecx         ; MSR a leer está en ECX (primer parámetro)
    rdmsr                ; Leer MSR (resultado en EDX:EAX)
    shl rdx, 32          ; Desplazar EDX para formar parte alta de 64 bits
    or rax, rdx          ; Combinar los 64 bits en RAX para retorno
    ret

; Escribe un valor en un registro MSR
; void WriteMSR(uint msr, ulong value)
_WriteMSR:
    mov ecx, ecx         ; MSR a escribir está en ECX (primer parámetro)
    mov rax, rdx         ; Valor a escribir está en RDX (segundo parámetro)
    mov rdx, rax         ; Separar los 64 bits en EDX:EAX
    shr rdx, 32          ; Parte alta en EDX
    wrmsr                ; Escribir en MSR
    ret

; Obtiene el ID del APIC Local a través de CPUID
; byte GetAPICID()
_GetAPICID:
    mov eax, 1           ; Función CPUID 1 (información del procesador)
    cpuid                ; Ejecutar CPUID
    shr ebx, 24          ; El ID APIC está en los bits 24-31 de EBX
    mov al, bl           ; Mover el resultado a AL para retorno
    ret

; Envía un comando al controlador de teclado
; void KBControllerSendCommand(byte command)
_KBControllerSendCommand:
    mov al, cl           ; Comando está en CL (primer parámetro)
    
    ; Esperar a que el búfer de entrada esté vacío
.wait_input_buffer:
    in al, 64h           ; Leer estado del controlador KB
    test al, 2           ; Probar bit 1 (búfer de entrada lleno)
    jnz .wait_input_buffer
    
    ; Enviar comando
    mov al, cl           ; Restaurar el comando
    out 64h, al          ; Enviar al puerto de comando (0x64)
    ret

; Instrucción PAUSE para esperas activas eficientes
; void Pause()
_Pause:
    pause                ; Instrucción PAUSE (ahorra energía en esperas activas)
    ret


; Función para obtener el valor del registro CR2 (dirección que causó la falta de página)
global _GetCR2
_GetCR2:
    mov rax, cr2    ; Mueve el contenido de CR2 a RAX (valor de retorno)
    ret

; void _Stosb(void* p, byte value, unsigned int count)
_Stosb:
    ; Prolog estándar para función x86_64 de 64 bits
    push rbx
    push rbp
    push rdi
    push rsi
    push rdx

    ; Cargar parámetros
    mov rdi, rdi      ; Puntero de destino (p)
    mov al, dl        ; Valor byte a escribir
    mov rcx, rdx      ; Contador (count) - unsigned int de 64 bits

    ; Configurar dirección de incremento
    cld                ; Limpiar bandera de dirección (incremento)

    ; Repetir almacenamiento de byte
    rep stosb

    ; Restaurar registros
    pop rdx
    pop rsi
    pop rdi
    pop rbp
    pop rbx

    ret

; void _movsb(void* src, void* dest)
_Movsb:
    ; Cargar las direcciones de origen (src) y destino (dest) desde los registros
    mov rsi, rdi      ; Cargar la dirección de origen (src) en RSI
    mov rdi, rsi      ; Cargar la dirección de destino (dest) en RDI

    ; Cargar el byte desde la dirección de origen (RSI) en AL
    mov al, byte [rsi] ; Cargar el byte desde RSI en AL

    ; Guardar el byte en la dirección de destino (RDI)
    mov byte [rdi], al ; Guardar el byte en la dirección de destino

    ; Incrementar o decrementar RSI y RDI dependiendo del DF
    ; Si DF = 0, incrementar RSI y RDI; si DF = 1, decrementar RSI y RDI
    ; Esto puede manejarse automáticamente si la dirección de avance/retroceso ya está configurada
    inc rsi            ; Incrementar RSI (avanzar en la memoria de origen)
    inc rdi            ; Incrementar RDI (avanzar en la memoria de destino)

    ret
