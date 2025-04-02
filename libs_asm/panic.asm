; kernel_functions64.asm - Funciones básicas de kernel en ensamblador para x86_64

section .text
global _panic
global _kernel_print

; Constantes para el buffer de video en modo texto
%define VIDEO_MEMORY 0xB8000
%define SCREEN_WIDTH 80
%define SCREEN_HEIGHT 25
%define DEFAULT_ATTR 0x0F      ; Blanco brillante sobre fondo negro

; Variables para mantener la posición del cursor
section .bss
cursor_x resq 1                ; Posición X del cursor (columna)
cursor_y resq 1                ; Posición Y del cursor (fila)

; Función de pánico - detiene el sistema
_panic:
    cli                        ; Deshabilitar interrupciones

.hang:
    hlt                        ; Detener el procesador hasta la próxima interrupción
    jmp .hang                  ; Bucle infinito

; Función para imprimir texto en la pantalla
; Parámetros:
;   - rdi: puntero al texto (char*)
;   - rsi: longitud del texto (int)
_kernel_print:
    push rbx
    push rsi
    push rdi
    push rbp
    mov rbp, rsp
    
    ; Si la longitud es cero, salir
    test rsi, rsi
    jz .done

    ; Calcular la dirección base del buffer de video
    mov rdi, VIDEO_MEMORY
    
    ; Calcular la posición inicial basada en cursor_x y cursor_y
    mov rax, [cursor_y]
    imul rax, SCREEN_WIDTH
    add rax, [cursor_x]
    shl rax, 1                ; Multiplicar por 2 (cada carácter ocupa 2 bytes)
    add rdi, rax              ; Ajustar el puntero de escritura
    
.print_loop:
    lodsb                      ; Cargar byte desde [rsi] en al, incrementar rsi
    
    ; Verificar si es un carácter especial
    cmp al, 10                 ; Comprobar si es '\n' (nueva línea)
    je .newline
    
    ; Carácter normal, escribir en la pantalla
    mov [rdi], al              ; Guardar el carácter
    mov byte [rdi+1], DEFAULT_ATTR ; Establecer el atributo de color
    add rdi, 2                 ; Avanzar en el buffer de video
    
    ; Actualizar posición X
    inc qword [cursor_x]
    cmp qword [cursor_x], SCREEN_WIDTH
    jl .next_char
    
    ; Si llegamos al final de la línea, hacer salto de línea
.newline:
    mov qword [cursor_x], 0    ; Reiniciar cursor_x
    inc qword [cursor_y]       ; Avanzar cursor_y
    
    ; Si llegamos al final de la pantalla, hacer scroll
    cmp qword [cursor_y], SCREEN_HEIGHT
    jl .calc_position
    
    ; Desplazar el contenido hacia arriba (scroll)
    call .scroll_screen
    dec qword [cursor_y]       ; Ajustar cursor_y después del scroll
    
.calc_position:
    ; Recalcular la posición en el buffer de video
    mov rdi, VIDEO_MEMORY
    mov rax, [cursor_y]
    imul rax, SCREEN_WIDTH
    add rax, [cursor_x]
    shl rax, 1
    add rdi, rax
    
.next_char:
    dec rsi
    jnz .print_loop

.done:
    pop rbp
    pop rdi
    pop rsi
    pop rbx
    ret

; Función para desplazar la pantalla hacia arriba (scroll)
.scroll_screen:
    push rsi
    push rdi
    push rcx
    
    ; Copiar líneas 1-24 a las líneas 0-23
    mov rsi, VIDEO_MEMORY + (SCREEN_WIDTH * 2) ; Origen: segunda línea
    mov rdi, VIDEO_MEMORY                      ; Destino: primera línea
    mov rcx, (SCREEN_HEIGHT - 1) * SCREEN_WIDTH
    rep movsw                                  ; Copiar palabra por palabra
    
    ; Limpiar la última línea
    mov rdi, VIDEO_MEMORY + ((SCREEN_HEIGHT - 1) * SCREEN_WIDTH * 2)
    mov rcx, SCREEN_WIDTH

.clear_last_line:
    mov word [rdi], (DEFAULT_ATTR << 8) | ' '  ; Espacio con atributo de color
    add rdi, 2
    loop .clear_last_line
    
    pop rcx
    pop rdi
    pop rsi
    ret
