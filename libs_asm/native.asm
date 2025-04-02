; CPU operations for x86_64 (64-bit)
section .text
global _Stosb
global _Movsb
global _Memcpy
global _Memset

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

; void* _memcpy(void* dest, const void* src, size_t count)
_Memcpy:
    push rbx
    push rbp
    push rsi
    push rdi

    mov rdi, rdi      ; dest
    mov rsi, rsi      ; src
    mov rcx, rdx      ; count
    
    cld                ; Asegurar que la dirección sea incremental (DF=0)
    
    ; Guardar el valor original de dest para retornarlo
    mov rax, rdi
    
    ; Si count es 0, terminar
    test rcx, rcx
    jz .done
    
    ; Copiar byte por byte
    rep movsb
    
.done:
    pop rdi
    pop rsi
    pop rbp
    pop rbx
    ret

; void* _memset(void* dest, int value, size_t count)
_Memset:
    push rbx
    push rbp
    push rdi

    mov rdi, rdi      ; dest
    mov al, dl        ; value (solo usamos un byte)
    mov rcx, rdx      ; count
    
    cld                ; Asegurar que la dirección sea incremental (DF=0)
    
    ; Guardar el valor original de dest para retornarlo
    mov rax, rdi
    
    ; Si count es 0, terminar
    test rcx, rcx
    jz .done
    
    ; Llenar byte por byte
    rep stosb
    
.done:
    pop rdi
    pop rbp
    pop rbx
    ret
