; loader.asm - Bootloader completo para KernelSharp
bits 32
extern Entry                ; Punto de entrada externo desde C#
global _start

; Constantes Multiboot
MODULEALIGN       equ     1<<0    ; Alinear módulos en límites de página
MEMINFO           equ     1<<1    ; Proporcionar mapa de memoria
FLAGS             equ     MODULEALIGN | MEMINFO  ; Solicitar info esencial
MAGIC             equ     0x1BADB002  ; Número mágico Multiboot
CHECKSUM          equ     -(MAGIC + FLAGS)  ; Checksum requerido por multiboot

; Constantes para manejo de memoria
PAGE_SIZE       equ     0x1000         ; 4KB - Tamaño de página

section .multiboot_header
    ; Encabezado Multiboot - debe estar en los primeros 8KB del archivo
    align 4                
    dd MAGIC               ; 0x1BADB002
    dd FLAGS               ; Flags Multiboot
    dd CHECKSUM            ; Checksum

section .data
    ; Mensajes de depuración
    init_msg db "KernelSharp: Inicializando loader...", 0
    stack_msg db "Configurando pila...", 0
    multiboot_msg db "Guardando informacion Multiboot...", 0
    gdt_setup_msg db "Configurando GDT...", 0
    gdt_success_msg db "GDT configurado correctamente", 0
    idt_setup_msg db "Configurando IDT...", 0
    idt_success_msg db "IDT configurado correctamente", 0
    a20_msg db "Verificando linea A20...", 0
    a20_enabled_msg db "Linea A20 habilitada", 0
    sse_setup_msg db "Configurando SSE...", 0
    sse_enabled_msg db "SSE habilitado correctamente", 0
    entry_msg db "Llamando a la funcion Entry...", 0
    error_msg db "ERROR: ", 0
    halt_msg db "Sistema detenido", 0

section .bss
    ; Punteros importantes
    multiboot_ptr: resd 1    ; Almacenar puntero de información multiboot
    
    ; Puntero de pantalla para salida
    screen_ptr: resd 1      ; Posición actual de la pantalla
    screen_col: resb 1      ; Columna actual
    screen_row: resb 1      ; Fila actual
    
    ; Espacio para la pila
    align 16
    stack_bottom: resb 32768 ; 32KB para la pila
    stack_space:            ; Apunta al tope de la pila

section .text
_start:
    ; Deshabilitar interrupciones al inicio
    cli
    
    ; Salvar inmediatamente el puntero Multiboot
    mov [multiboot_ptr], ebx
    
    ; Inicializar pantalla
    mov dword [screen_ptr], 0xB8000
    mov byte [screen_col], 0
    mov byte [screen_row], 0
    
    ; Limpiar pantalla
    call clear_screen
    
    ; Mostrar mensaje de inicialización
    mov esi, init_msg
    call print_string
    call print_newline
    
    ; Mostrar mensaje de configuración de pila
    mov esi, stack_msg
    call print_string
    call print_newline
    
    ; Configurar la pila
    mov esp, stack_space    ; Configurar puntero de pila
    
    ; Mostrar mensaje sobre información multiboot
    mov esi, multiboot_msg
    call print_string
    call print_newline
    
    ; Verificar si tenemos información Multiboot válida
    cmp dword [multiboot_ptr], 0
    je .no_multiboot
    
    ; Configurar GDT
    mov esi, gdt_setup_msg
    call print_string
    call print_newline
    call setup_gdt
    mov esi, gdt_success_msg
    call print_string
    call print_newline
    
    ; Verificar y habilitar SSE
    mov esi, sse_setup_msg
    call print_string
    call print_newline
    call enable_sse
    mov esi, sse_enabled_msg
    call print_string
    call print_newline
    
    ; Mostrar mensaje antes de llamar a Entry
    mov esi, entry_msg
    call print_string
    call print_newline
    
    ; Preparar argumentos para Entry
    ; Recordar que en la convención cdecl, los argumentos se pasan de derecha a izquierda
    push dword 0x2BADB002           ; Pasar magic number como segundo argumento
    push dword [multiboot_ptr]      ; Pasar el puntero Multiboot como primer argumento
    
    ; Llamar a la función Entry en C#
    call Entry
    
    ; Limpiar la pila si regresamos (aunque normalmente no deberíamos regresar)
    add esp, 8                      ; Limpiar 2 parámetros (8 bytes)
    
    ; Si llegamos aquí, algo salió mal, mostrar mensaje de error y detener
    jmp system_halt

.no_multiboot:
    mov esi, error_msg
    call print_string
    mov esi, multiboot_msg
    call print_string
    call print_newline
    jmp system_halt

; Función para limpiar la pantalla
clear_screen:
    pusha
    mov ecx, 80*25          ; Total de caracteres en pantalla (80x25)
    mov edi, 0xB8000        ; Dirección base de la memoria de video
    mov ax, 0x0720          ; Atributo (7) y espacio (ASCII 32)
    rep stosw               ; Repetir STOSW ECX veces (llena toda la pantalla)
    
    ; Resetear posición del cursor
    mov byte [screen_col], 0
    mov byte [screen_row], 0
    mov dword [screen_ptr], 0xB8000
    
    popa
    ret

; Función para imprimir strings
print_string:
    pusha
    
.loop:
    lodsb                       ; Cargar byte de ESI en AL e incrementar ESI
    test al, al                 ; Verificar si es fin de cadena (0)
    jz .done
    
    call print_char
    jmp .loop
    
.done:
    popa
    ret

; Función para imprimir un carácter
print_char:
    pusha
    
    ; Verificar si es un salto de línea
    cmp al, 10                  ; Salto de línea (ASCII 10)
    je .newline
    
    ; Calcular posición en memoria de video
    movzx edx, byte [screen_row] ; Fila actual
    imul edx, 80*2              ; 80 caracteres por fila, 2 bytes por carácter
    movzx ecx, byte [screen_col] ; Columna actual
    imul ecx, 2                 ; 2 bytes por carácter
    add edx, ecx                ; Posición total
    add edx, 0xB8000            ; Sumar dirección base
    
    ; Escribir carácter
    mov [edx], al               ; Carácter
    mov byte [edx+1], 0x0F      ; Atributo (blanco sobre negro)
    
    ; Incrementar columna
    inc byte [screen_col]
    
    ; Verificar si llegamos al final de la línea
    cmp byte [screen_col], 80
    jl .done
    
.newline:
    ; Ir a la siguiente línea
    mov byte [screen_col], 0
    inc byte [screen_row]
    
    ; Verificar si necesitamos hacer scroll
    cmp byte [screen_row], 25
    jl .done
    
    ; Hacer scroll (no implementado para simplicidad)
    ; Generalmente esto implicaría mover todas las líneas hacia arriba
    mov byte [screen_row], 24
    
.done:
    ; Actualizar el puntero de pantalla
    movzx edx, byte [screen_row] ; Fila actual
    imul edx, 80*2              ; 80 caracteres por fila, 2 bytes por carácter
    movzx ecx, byte [screen_col] ; Columna actual
    imul ecx, 2                 ; 2 bytes por carácter
    add edx, ecx                ; Posición total
    add edx, 0xB8000            ; Sumar dirección base
    mov [screen_ptr], edx
    
    popa
    ret

; Función para imprimir nueva línea
print_newline:
    pusha
    mov al, 10                  ; Salto de línea
    call print_char
    popa
    ret

; Función para configurar GDT
setup_gdt:
    ; Cargar GDT
    lgdt [gdt_descriptor]
    
    ; Recargar registros de segmento
    mov ax, 0x10            ; Segmento de datos
    mov ds, ax
    mov es, ax
    mov fs, ax
    mov gs, ax
    mov ss, ax

    ; Salto lejano para recargar CS
    jmp 0x08:.reload_cs

.reload_cs:
    ret

; Función para habilitar SSE
enable_sse:
    ; Verificar soporte de SSE
    mov eax, 0x1
    cpuid
    test edx, 1<<25
    jz .sse_not_supported
    
    ; Habilitar SSE
    mov eax, cr0
    and ax, 0xFFFB          ; Limpiar emulación de coprocesador CR0.EM
    or ax, 0x2              ; Establecer monitoreo de coprocesador CR0.MP
    mov cr0, eax
    mov eax, cr4
    or ax, 3 << 9           ; Establecer CR4.OSFXSR y CR4.OSXMMEXCPT
    mov cr4, eax
    
    ret
    
.sse_not_supported:
    ret

; Función para imprimir un valor hexadecimal en EAX
print_hex:
    pusha
    mov ecx, 8              ; 8 dígitos para un valor de 32 bits
    
    ; Imprimir prefijo "0x"
    mov al, '0'
    call print_char
    mov al, 'x'
    call print_char
    
.print_digit:
    rol eax, 4              ; Rotar a la izquierda para obtener el dígito más significativo
    mov edx, eax
    and edx, 0xF            ; Aislar el dígito
    
    ; Convertir a ASCII
    cmp edx, 10
    jl .decimal
    add edx, 'A' - 10 - '0'
    
.decimal:
    add edx, '0'
    
    ; Imprimir el dígito
    mov al, dl
    call print_char
    
    loop .print_digit
    
    popa
    ret

; Detención del sistema
system_halt:
    ; Mostrar mensaje de detención
    mov esi, halt_msg
    call print_string
    call print_newline
    
    ; Deshabilitar interrupciones
    cli
    
    ; Detener CPU
    hlt
    
    ; Por si acaso, bucle infinito
    jmp system_halt

; Definición de GDT
align 8
gdt_start:
    ; Descriptor nulo
    dq 0                    ; 8 bytes de ceros
    
    ; Segmento de código
    dw 0xFFFF               ; Límite [0:15]
    dw 0x0000               ; Base [0:15]
    db 0x00                 ; Base [16:23]
    db 10011010b            ; Acceso (P=1, DPL=00, S=1, E=1, DC=0, RW=1, A=0)
    db 11001111b            ; Granularidad (G=1, D=1, L=0, AVL=0) + Límite [16:19]
    db 0x00                 ; Base [24:31]
    
    ; Segmento de datos
    dw 0xFFFF               ; Límite [0:15]
    dw 0x0000               ; Base [0:15]
    db 0x00                 ; Base [16:23]
    db 10010010b            ; Acceso (P=1, DPL=00, S=1, E=0, DC=0, RW=1, A=0)
    db 11001111b            ; Granularidad (G=1, D=1, L=0, AVL=0) + Límite [16:19]
    db 0x00                 ; Base [24:31]
gdt_end:

; Continuación desde:
 gdt_descriptor: 
 dw gdt_end - gdt_start - 1 ; Tamaño de GDT (menos 1) 
 dd gdt_start ; Dirección de inicio de GDT

; Funciones de manejo PIC (Programmable Interrupt Controller)
pic_remap:
    ; Enviar comandos de inicialización a los PICs
    mov al, 0x11           ; Inicialización en cascada
    out 0x20, al           ; Enviar a PIC 1
    out 0xA0, al           ; Enviar a PIC 2
    
    ; Establecer los vectores de offset
    mov al, 0x20           ; El PIC 1 comienza en 0x20 (32)
    out 0x21, al
    mov al, 0x28           ; El PIC 2 comienza en 0x28 (40)
    out 0xA1, al
    
    ; Decirle a los PICs cómo están conectados
    mov al, 0x04           ; PIC 1 tiene el PIC 2 en IRQ 2
    out 0x21, al
    mov al, 0x02           ; PIC 2 está conectado a través de IRQ 2
    out 0xA1, al
    
    ; Poner los PICs en modo 8086
    mov al, 0x01           ; Modo 8086
    out 0x21, al
    out 0xA1, al
    
    ; Enmascarar todas las interrupciones excepto IRQ 1 (teclado)
    mov al, 0xFD           ; Habilitar solo IRQ 1
    out 0x21, al
    mov al, 0xFF           ; Deshabilitar todos los IRQs del PIC 2
    out 0xA1, al
    
    ret

; Función para verificar y habilitar línea A20
check_a20:
    pushad                  ; Guardar todos los registros

    ; Primero intentamos a través del controlador de teclado
    call .try_keyboard_controller
    test eax, eax
    jnz .done              ; Si está habilitado, salir
    
    ; Si no funciona, intentamos con el Fast A20 Gate
    call .try_fast_a20
    test eax, eax
    jnz .done              ; Si está habilitado, salir
    
    ; Si aún no funciona, reportamos un error
    mov esi, error_msg
    call print_string
    mov esi, a20_msg
    call print_string
    call print_newline
    
    ; Devolver 0 para indicar fallo
    mov eax, 0
    jmp .exit
    
.done:
    ; Línea A20 está habilitada
    mov esi, a20_enabled_msg
    call print_string
    call print_newline
    
    ; Devolver 1 para indicar éxito
    mov eax, 1
    
.exit:
    popad                   ; Restaurar todos los registros
    ret

.try_keyboard_controller:
    ; Deshabilitar interrupciones
    cli
    
    ; Deshabilitar teclado
    call .wait_input
    mov al, 0xAD
    out 0x64, al
    
    ; Leer puerto de salida
    call .wait_input
    mov al, 0xD0
    out 0x64, al
    
    ; Leer resultados
    call .wait_output
    in al, 0x60
    push eax
    
    ; Escribir puerto de salida
    call .wait_input
    mov al, 0xD1
    out 0x64, al
    
    ; Activar línea A20
    call .wait_input
    pop eax
    or al, 2               ; Establecer bit 1 (A20)
    out 0x60, al
    
    ; Habilitar teclado nuevamente
    call .wait_input
    mov al, 0xAE
    out 0x64, al
    
    ; Esperar a que esté listo
    call .wait_input
    
    ; Verificar si A20 está habilitada
    call .check_a20_enabled
    ret

.try_fast_a20:
    ; Intento a través del Fast A20 Gate
    in al, 0x92
    test al, 2
    jnz .fast_done         ; Ya está habilitada
    
    ; Habilitar A20
    or al, 2
    out 0x92, al
    
.fast_done:
    ; Esperar un poco
    mov ecx, 0x100000
.delay:
    loop .delay
    
    ; Verificar si A20 está habilitada
    call .check_a20_enabled
    ret

.wait_input:
    ; Esperar hasta que el controlador esté listo para entrada
    in al, 0x64
    test al, 2
    jnz .wait_input
    ret

.wait_output:
    ; Esperar hasta que haya datos de salida
    in al, 0x64
    test al, 1
    jz .wait_output
    ret

.check_a20_enabled:
    ; Verificar si A20 está habilitada
    push ds
    push es
    push edi
    push esi
    
    ; Configurar segmentos
    xor ax, ax             ; AX = 0
    mov ds, ax             ; DS = 0
    
    not ax                 ; AX = 0xFFFF
    mov es, ax             ; ES = 0xFFFF
    
    ; Establecer direcciones para la prueba
    mov edi, 0x00500       ; ES:DI = 0xFFFF:0x0500 = 0x100500
    mov esi, 0x00510       ; DS:SI = 0x0000:0x0510 = 0x000510
    
    ; Guardar datos originales
    mov dl, [es:edi]
    mov dh, [ds:esi]
    
    ; Cambiar datos
    mov byte [es:edi], 0x00
    mov byte [ds:esi], 0xFF
    
    ; Verificar si se afectan entre sí (si A20 está deshabilitada)
    cmp byte [es:edi], 0xFF
    
    ; Restaurar datos originales
    mov byte [ds:esi], dh
    mov byte [es:edi], dl
    
    ; Restaurar registros
    pop esi
    pop edi
    pop es
    pop ds
    
    ; Si son iguales, A20 está deshabilitada
    mov eax, 0
    je .check_done
    
    ; Si son diferentes, A20 está habilitada
    mov eax, 1
    
.check_done:
    ret

; Tabla de descriptores de interrupción (IDT)
align 8
idt_start:
    times 256 dq 0         ; 256 entradas vacías
idt_end:

idt_descriptor:
    dw idt_end - idt_start - 1   ; Tamaño de IDT (menos 1)
    dd idt_start                 ; Dirección de inicio de IDT

; Función para configurar la IDT
setup_idt:
    ; Cargar IDT
    lidt [idt_descriptor]
    ret

; Función para probar si podemos escribir en memoria
test_memory:
    pusha
    
    ; Prueba simple: intentar escribir en algunas direcciones
    ; y verificar si se mantiene el valor
    
    ; Dirección base
    mov edi, 0x100000      ; 1MB
    
    ; Guardar valor original
    mov edx, [edi]
    
    ; Escribir un patrón
    mov dword [edi], 0xAA55AA55
    
    ; Verificar si se mantuvo el valor
    cmp dword [edi], 0xAA55AA55
    jne .failed
    
    ; Restaurar valor original
    mov [edi], edx
    
    ; Prueba exitosa
    mov esi, memory_test_success
    call print_string
    call print_newline
    
    popa
    ret
    
.failed:
    ; Prueba fallida
    mov esi, memory_test_failed
    call print_string
    call print_newline
    
    popa
    ret

; Mensajes adicionales
section .data
memory_test_success db "Prueba de memoria exitosa", 0
memory_test_failed db "Prueba de memoria fallida", 0
paging_setup_msg db "Configurando paginacion...", 0
paging_enabled_msg db "Paginacion habilitada", 0

; Función para habilitar paginación básica (identidad)
; Nota: Esta función es opcional y puede no ser necesaria para un kernel C#
; que maneja su propia paginación
setup_paging:
    ; Mostrar mensaje
    mov esi, paging_setup_msg
    call print_string
    call print_newline
    
    ; Reservar espacio para las tablas de páginas
    ; Esta es una configuración muy básica para una paginación de identidad
    ; (cada dirección virtual mapea a la misma dirección física)
    
    ; 1. Limpiar el directorio de páginas
    mov edi, 0x100000      ; Directorio en 1MB
    mov ecx, 1024          ; 1024 entradas
    xor eax, eax           ; Valor 0
    rep stosd              ; Repetir STOSD (store doubleword) ECX veces
    
    ; 2. Configurar la primera tabla de páginas (0-4MB)
    mov edi, 0x101000      ; Primera tabla de páginas (después del directorio)
    mov eax, 0x003         ; Present + Read/Write
    mov ecx, 1024          ; 1024 entradas
    
.setup_page_table:
    stosd                  ; Guardar EAX y avanzar EDI
    add eax, 0x1000        ; Siguiente página (4KB)
    loop .setup_page_table
    
    ; 3. Apuntar la primera entrada del directorio a la tabla de páginas
    mov dword [0x100000], 0x101003   ; Present + Read/Write + tabla en 0x101000
    
    ; 4. Cargar CR3 con la dirección del directorio
    mov eax, 0x100000
    mov cr3, eax
    
    ; 5. Habilitar paginación
    mov eax, cr0
    or eax, 0x80000000     ; Establecer bit PG
    mov cr0, eax
    
    ; Mostrar mensaje de éxito
    mov esi, paging_enabled_msg
    call print_string
    call print_newline
    
    ret

; Función para leer configuración del CPUID
get_cpu_info:
    pusha
    
    ; Verificar si CPUID está disponible
    pushfd                 ; Guardar EFLAGS
    pop eax                ; Cargar EFLAGS en EAX
    mov ecx, eax           ; Guardar original
    xor eax, 0x200000      ; Invertir bit de ID (bit 21)
    push eax               ; Guardar modificado
    popfd                  ; Cargar EFLAGS modificado
    pushfd                 ; Guardar resultado
    pop eax                ; Cargar en EAX
    push ecx               ; Restaurar EFLAGS original
    popfd
    
    ; Comparar para ver si el bit se mantuvo cambiado
    xor eax, ecx
    test eax, 0x200000
    jz .no_cpuid           ; Si no cambió, CPUID no está disponible
    
    ; CPUID disponible, obtener información
    mov eax, 0             ; Función 0: Obtener vendor ID
    cpuid
    
    ; Guardar vendor ID
    mov [cpu_vendor], ebx
    mov [cpu_vendor+4], edx
    mov [cpu_vendor+8], ecx
    mov byte [cpu_vendor+12], 0 ; Asegurar NULL-terminated
    
    ; Obtener características del CPU
    mov eax, 1             ; Función 1: Obtener características
    cpuid
    
    ; Guardar características
    mov [cpu_features], edx
    
    ; Imprimir información
    mov esi, cpu_info_msg
    call print_string
    mov esi, cpu_vendor
    call print_string
    call print_newline
    
    ; Verificar características específicas
    test edx, 1<<0
    jz .no_fpu
    mov esi, cpu_fpu_msg
    call print_string
    call print_newline
.no_fpu:
    
    test edx, 1<<25
    jz .no_sse
    mov esi, cpu_sse_msg
    call print_string
    call print_newline
.no_sse:
    
    test edx, 1<<26
    jz .no_sse2
    mov esi, cpu_sse2_msg
    call print_string
    call print_newline
.no_sse2:
    
    popa
    ret
    
.no_cpuid:
    mov esi, cpu_no_cpuid_msg
    call print_string
    call print_newline
    
    popa
    ret

; Variables para información del CPU
section .data
cpu_vendor: times 16 db 0      ; Espacio para vendor ID + NULL
cpu_features: dd 0             ; Características del CPU
cpu_info_msg db "CPU: ", 0
cpu_fpu_msg db "FPU disponible", 0
cpu_sse_msg db "SSE disponible", 0
cpu_sse2_msg db "SSE2 disponible", 0
cpu_no_cpuid_msg db "CPUID no disponible", 0                ; Dirección de inicio de GDT