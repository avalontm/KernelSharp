; CPU operations for x86 (32-bit)
section .text
global _Stosb
global _Movsb
global _Memcpy
global _Memset

; void _Stosb(void* p, byte value, unsigned int count)
_Stosb:
    ; Prólogo estándar para función x86 de 32 bits
    push ebp
    mov ebp, esp
    
    ; Preservar registros que se van a modificar
    push edi
    push ecx

    ; Cargar parámetros
    mov edi, [ebp+8]   ; Puntero de destino (p)
    mov al, [ebp+12]   ; Valor byte a escribir
    mov ecx, [ebp+16]  ; Contador (count) - unsigned int de 32 bits

    ; Configurar dirección de incremento
    cld                ; Limpiar bandera de dirección (incremento)

    ; Repetir almacenamiento de byte
    rep stosb

    ; Restaurar registros
    pop ecx
    pop edi

    ; Epílogo
    mov esp, ebp
    pop ebp
    ret

; void _movsb(void* src, void* dest)
_Movsb:
    ; Cargar las direcciones de origen (src) y destino (dest) desde la pila
    mov esi, [esp+4]  ; Cargar la dirección de origen (src) en ESI
    mov edi, [esp+8]  ; Cargar la dirección de destino (dest) en EDI

    ; Cargar el byte desde la dirección de origen (ESI) en AL
    mov al, [esi]     ; Cargar el byte desde ESI en AL

    ; Guardar el byte en la dirección de destino (EDI)
    mov [edi], al     ; Guardar el byte en la dirección de destino

    ; Incrementar o decrementar ESI y EDI dependiendo del DF
    ; Si DF = 0, incrementar ESI y EDI; si DF = 1, decrementar ESI y EDI
    ; Esto puede manejarse automáticamente si la dirección de avance/retroceso ya está configurada
    inc esi           ; Incrementar ESI (avanzar en la memoria de origen)
    inc edi           ; Incrementar EDI (avanzar en la memoria de destino)

    ret

; void* _memcpy(void* dest, const void* src, size_t count)
_Memcpy:
    push ebp
    mov ebp, esp
    push esi
    push edi
    
    mov edi, [ebp+8]   ; dest
    mov esi, [ebp+12]  ; src
    mov ecx, [ebp+16]  ; count
    
    cld                ; Asegurar que la dirección sea incremental (DF=0)
    
    ; Guardar el valor original de dest para retornarlo
    mov eax, edi
    
    ; Si count es 0, terminar
    test ecx, ecx
    jz .done
    
    ; Copiar byte por byte
    rep movsb
    
.done:
    pop edi
    pop esi
    pop ebp
    ret

; void* _memset(void* dest, int value, size_t count)
_Memset:
    push ebp
    mov ebp, esp
    push edi
    
    mov edi, [ebp+8]   ; dest
    mov al, [ebp+12]   ; value (solo usamos un byte)
    mov ecx, [ebp+16]  ; count
    
    cld                ; Asegurar que la dirección sea incremental (DF=0)
    
    ; Guardar el valor original de dest para retornarlo
    mov eax, edi
    
    ; Si count es 0, terminar
    test ecx, ecx
    jz .done
    
    ; Llenar byte por byte
    rep stosb
    
.done:
    pop edi
    pop ebp
    ret