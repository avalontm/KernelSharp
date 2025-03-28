; =====================================================================
; SecurityCookie.asm - Implementación de security cookie para x86
; =====================================================================

section .data
; Variable global para almacenar el valor de la cookie
global __security_cookie
__security_cookie dd 0xBB40E64E ; Valor inicial predeterminado (se actualizará)

; Variable para verificar si ya ha sido inicializada
global __security_cookie_initialized
__security_cookie_initialized dd 0

section .text
; Función para inicializar la cookie con un valor pseudoaleatorio
global __security_cookie_init
global ___security_cookie_init
__security_cookie_init:
___security_cookie_init:
    ; Verificar si ya ha sido inicializada
    mov eax, dword [__security_cookie_initialized]
    test eax, eax
    jnz .already_initialized
    
    ; Marcar como inicializada
    mov dword [__security_cookie_initialized], 1
    
    ; Capturar el timestamp counter
    rdtsc                   ; EDX:EAX = Timestamp counter
    
    ; Mezclar con valores adicionales para más entropía
    xor eax, esp            ; Mezclar con el puntero de pila
    xor eax, ebp            ; Mezclar con el puntero base
    
    ; Si obtenemos el valor predeterminado (0xBB40E64E) o 0, incrementar
    cmp eax, 0xBB40E64E
    je .add_entropy
    test eax, eax
    jz .add_entropy
    jmp .set_cookie
    
.add_entropy:
    ; Añadir otro bit de entropía
    inc eax
    
.set_cookie:
    ; Guardar el valor calculado como nuestra cookie
    mov dword [__security_cookie], eax
    
.already_initialized:
    ret

; Función que verifica la cookie
global __security_check_cookie
global ___security_check_cookie
__security_check_cookie:
___security_check_cookie:
    ; EAX = cookie a verificar
    cmp eax, dword [__security_cookie]
    je .cookie_ok
    
    ; La cookie no coincide - violación de seguridad
    call __security_failure
    
.cookie_ok:
    ret

; Función que maneja la violación de seguridad
global __security_failure
global ___security_failure
__security_failure:
___security_failure:
    ; Podemos llamar a Panic aquí si está disponible
    ; o directamente manejar el error grave
    
    ; Desactivar interrupciones
    cli
    
    ; Mostrar mensaje en el VGA buffer (0xB8000)
    mov eax, 0xB8000
    mov dword [eax], 0x4F534F53      ; "SS" en rojo brillante
    mov dword [eax+4], 0x4F2D4F2D    ; "--" en rojo brillante
    mov dword [eax+8], 0x4F454F45    ; "EE" en rojo brillante
    mov dword [eax+12], 0x4F524F52   ; "RR" en rojo brillante
    mov dword [eax+16], 0x4F4F4F4F   ; "OO" en rojo brillante
    mov dword [eax+20], 0x4F524F52   ; "RR" en rojo brillante
    
    ; Detener sistema
.halt:
    hlt
    jmp .halt