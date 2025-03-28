; kernel_functions.asm - Funciones básicas de kernel en ensamblador para x86

section .text
global _panic
global _kernel_print

; Constantes para el buffer de video
%define VIDEO_MEMORY 0xB8000
%define SCREEN_WIDTH 80
%define SCREEN_HEIGHT 25
%define DEFAULT_ATTR 0x0F      ; Blanco brillante sobre fondo negro

; Variables para mantener la posición del cursor
section .data
cursor_x dd 0                  ; Posición X del cursor (columna)
cursor_y dd 0                  ; Posición Y del cursor (fila)

; Función de pánico - detiene el sistema
_panic:
    ; Deshabilitar interrupciones
    cli
    
    ; Bucle infinito
.hang:
    hlt                        ; Detener el procesador hasta la próxima interrupción
    jmp .hang                  ; Bucle infinito

; Función para imprimir texto en la pantalla
; Parámetros:
;   - ebp+8: puntero al texto (char*)
;   - ebp+12: longitud del texto (int)
_kernel_print:
    push ebp
    mov ebp, esp
    push ebx
    push esi
    push edi
    
    ; Obtener parámetros
    mov esi, [ebp+8]          ; Puntero al texto
    mov ecx, [ebp+12]         ; Longitud del texto
    
    ; Si la longitud es cero, salir
    test ecx, ecx
    jz .done
    
    ; Configurar el puntero al buffer de video
    mov edi, VIDEO_MEMORY
    
    ; Calcular la posición inicial basada en cursor_x y cursor_y
    mov eax, [cursor_y]
    imul eax, SCREEN_WIDTH    ; eax = cursor_y * SCREEN_WIDTH
    add eax, [cursor_x]       ; eax = cursor_y * SCREEN_WIDTH + cursor_x
    imul eax, 2              ; eax = (cursor_y * SCREEN_WIDTH + cursor_x) * 2 (cada carácter ocupa 2 bytes)
    add edi, eax              ; edi = VIDEO_MEMORY + posición calculada
    
    ; Procesar cada carácter
.print_loop:
    lodsb                     ; Cargar byte de [esi] en al, incrementar esi
    
    ; Verificar si es un carácter especial
    cmp al, 10                ; Comprobar si es '\n' (nueva línea)
    je .newline
    
    ; Carácter normal, escribir en la pantalla
    mov [edi], al             ; Guardar el carácter
    mov byte [edi+1], DEFAULT_ATTR ; Establecer el atributo
    add edi, 2                ; Avanzar al siguiente carácter en el buffer
    
    ; Actualizar posición X
    inc dword [cursor_x]
    cmp dword [cursor_x], SCREEN_WIDTH
    jl .next_char
    
    ; Si llegamos al final de la línea, hacer salto de línea
.newline:
    mov dword [cursor_x], 0   ; cursor_x = 0
    inc dword [cursor_y]      ; cursor_y++
    
    ; Comprobar si hemos llegado al final de la pantalla
    cmp dword [cursor_y], SCREEN_HEIGHT
    jl .calc_position
    
    ; Desplazar todo el contenido hacia arriba (scroll)
    call .scroll_screen
    dec dword [cursor_y]      ; Ajustar cursor_y después del scroll
    
.calc_position:
    ; Recalcular la posición del buffer de video
    mov edi, VIDEO_MEMORY
    mov eax, [cursor_y]
    imul eax, SCREEN_WIDTH
    add eax, [cursor_x]
    imul eax, 2
    add edi, eax
    
.next_char:
    dec ecx                   ; Decrementar contador de caracteres
    jnz .print_loop           ; Si no hemos terminado, continuar
    
.done:
    pop edi
    pop esi
    pop ebx
    pop ebp
    ret
    
; Función para desplazar la pantalla hacia arriba (scroll)
.scroll_screen:
    push esi
    push edi
    push ecx
    
    ; Copiar líneas 1-24 a las líneas 0-23
    mov esi, VIDEO_MEMORY + (SCREEN_WIDTH * 2) ; Fuente: segunda línea
    mov edi, VIDEO_MEMORY                     ; Destino: primera línea
    mov ecx, (SCREEN_HEIGHT - 1) * SCREEN_WIDTH ; Número de caracteres a copiar
    rep movsw                                 ; Copiar palabra por palabra
    
    ; Limpiar la última línea
    mov edi, VIDEO_MEMORY + ((SCREEN_HEIGHT - 1) * SCREEN_WIDTH * 2)
    mov ecx, SCREEN_WIDTH
    
.clear_last_line:
    mov word [edi], (DEFAULT_ATTR << 8) | ' ' ; Espacio con atributo predeterminado
    add edi, 2
    loop .clear_last_line
    
    pop ecx
    pop edi
    pop esi
    ret