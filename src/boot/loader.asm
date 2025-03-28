; loader.asm - Loader para inicialización de CoreLib con mejoras para encontrar los módulos
bits 32
extern Entry                ; Punto de entrada externo de C#
global _start

; Constantes Multiboot
MODULEALIGN       equ     1<<0
MEMINFO           equ     1<<1
FLAGS             equ     MODULEALIGN | MEMINFO
MAGIC             equ     0x1BADB002
CHECKSUM          equ     -(MAGIC + FLAGS)

; Constantes para Flags de Multiboot
MODS_FLAG         equ     1<<3  ; Bit 3 indica presencia de módulos

section .data
    ; Mensajes de inicialización
    init_msg db "KernelSharp: Iniciando loader...", 0
    sse_msg db "SSE habilitado", 0
    entry_msg db "Llamando a Entry...", 0
    modules_msg db "Direccion de CoreLib: ", 0
    error_msg db "Error: ", 0
    modules_prep_msg db "Preparando modulos...", 0
    modules_done_msg db "Modulos preparados", 0
    multiboot_info_msg db "MultibootInfo: ", 0
    multiboot_mods_msg db "Multiboot Modules encontrados: ", 0
    multiboot_mods_addr_msg db "Direccion de Modulos Multiboot: ", 0
    trampoline_msg db "Trampoline: ", 0
    debug_msg db "Verificando direccion: ", 0
    no_mods_msg db "No se encontraron modulos Multiboot", 0

section .rodata
    ; Definición de módulo de CoreLib 
    corelib_module_def:
        dd 0x52523A52       ; Firma mágica 'RR:R'
        dd 2                ; Número de secciones
        dd corelib_sections ; Puntero a secciones

    ; Secciones de CoreLib
    corelib_sections:
        dd 0x01             ; SectionId para GCStaticRegion
        dd gc_static_data   ; Inicio de región
        dd gc_static_data + 1024  ; Final de región

        dd 0x02             ; SectionId para EagerCctor
        dd cctor_data       ; Inicio de constructores
        dd cctor_data + 1024 ; Final de constructores
        
        dd 0                ; Terminador de secciones

section .bss
    ; Áreas para inicialización de módulos
    gc_static_data: resb 1024  ; 1KB para GC estáticos
    cctor_data: resb 1024      ; 1KB para constructores
    
    ; Variables para almacenar punteros importantes
    multiboot_ptr: resd 1    ; Guardar puntero a información multiboot
    modules_ptr: resd 1      ; Guardar puntero a módulos
    trampoline_ptr: resd 1   ; Guardar dirección de retorno
    
    ; Espacio para stack
    resb 8192               ; 8KB para stack
stack_space:

section .text
    ; Multiboot header - debe estar en los primeros 8KB del archivo
    align 4                ; Asegurar alineación a 4 bytes
    dd MAGIC               ; 0x1BADB002
    dd FLAGS               ; Flags de Multiboot (típicamente 0x00000003)
    dd CHECKSUM            ; Debe hacer que los tres valores sumen 0

_start:
    ; Guarda inmediatamente el valor de EBX (contiene el puntero Multiboot)
    ; antes de que se pueda alterar por cualquier otra operación
    mov dword [multiboot_ptr], ebx
    
    ; Configurar el stack (necesario antes de cualquier CALL)
    cli                     ; Deshabilitar interrupciones
    mov esp, stack_space    ; Configurar puntero de pila
    
    ; Mostrar mensaje de inicialización
    mov esi, init_msg
    call print_string
    call print_newline
    
    ; Mostrar el valor de EBX tal como fue recibido
    mov esi, ebx_msg
    call print_string
    mov eax, ebx            ; Usar el registro original
    call print_hex_eax
    call print_newline
    
    ; Mostrar el valor guardado en multiboot_ptr
    mov esi, multiboot_info_msg
    call print_string
    mov eax, [multiboot_ptr]
    call print_hex_eax
    call print_newline
    

    ; Verificar y habilitar SSE
    call enable_sse
    jc error_handler        ; Si CF=1, falló la habilitación de SSE

    ; Detectar módulos de Multiboot si están disponibles
    call detect_multiboot_modules
    
    ; Preparar módulos de CoreLib - nuestra implementación
    call prepare_corelib_modules
    
    ; Determinar qué puntero de módulos usar
    call select_best_modules
    
    ; Configurar dirección de trampoline
    mov eax, kernel_return
    mov [trampoline_ptr], eax
    
    ; Mostrar dirección del módulo
    mov esi, modules_msg
    call print_string
    mov eax, [modules_ptr]
    call print_hex_eax
    call print_newline
    
    ; Mostrar la dirección de trampoline
    mov esi, trampoline_msg
    call print_string
    mov eax, [trampoline_ptr]
    call print_hex_eax
    call print_newline
    
    ; Mensaje antes de llamar a Entry
    mov esi, entry_msg
    call print_string
    call print_newline
    
    ; Antes de llamar a Entry, inicializa el heap
    call init_heap

    ; Preparar para llamar a Entry con los valores determinados
    push dword [trampoline_ptr]  ; IntPtr Trampoline
    push dword [modules_ptr]     ; IntPtr Modules
    push dword [multiboot_ptr]   ; MultibootInfo* Info
    
    ; Llamar a Entry
    call Entry
    
    ; Si llegamos aquí, limpiar stack y continuar
    add esp, 12             ; Limpiar 3 parámetros (4 bytes cada uno)

kernel_return:
    ; Verificar código de retorno en eax
    test eax, eax
    jnz error_handler
    
    ; Todo correcto, saltar a system_halt
    jmp system_halt

error_handler:
    ; Manejar errores
    mov esi, error_msg
    call print_string
    
    ; Si eax contiene un mensaje de error, mostrarlo
    test eax, eax          ; Si eax es 0, no hay mensaje
    jz .no_message
    mov esi, eax
    call print_string
    
.no_message:
    call print_newline
    jmp system_halt

system_halt:
    cli                     ; Deshabilitar interrupciones
    hlt                     ; Detener CPU
    jmp system_halt         ; En caso de interrupción inesperada

; Función para detectar módulos Multiboot
detect_multiboot_modules:
    push eax
    push ebx
    push ecx
    
    ; Verificar que tenemos información multiboot
    mov ebx, [multiboot_ptr]
    test ebx, ebx
    jz .no_multiboot
    
    ; Imprimir los primeros bytes del puntero Multiboot para diagnóstico
    mov esi, multiboot_data_msg
    call print_string
    
    ; Mostrar los primeros 4 bytes (flags)
    mov eax, [ebx]
    call print_hex_eax
    call print_newline
    
    ; Verificar la firma magic si existe
    ; (Algunos bootloaders incluyen esta firma)
    mov esi, magic_msg
    call print_string
    mov eax, [ebx-4]  ; 4 bytes antes de los flags a veces contiene una firma
    call print_hex_eax
    call print_newline
    
    ; Continuar con el código normal...
    
    ; Guardar dirección de módulos multiboot como opción
    ; pero no sobreescribir modules_ptr todavía
    ; Eso se decide en select_best_modules
    mov eax, [ebx + 24]     ; mods_addr
    cmp eax, 0              ; Verificar que no sea nulo
    je .no_modules
    
    ; Por ahora solo guardaremos la información, no la usaremos aún
    jmp .done
    
.no_multiboot:
    mov esi, error_msg
    call print_string
    mov esi, debug_msg
    call print_string
    call print_newline
    jmp .done
    
.no_modules:
    mov esi, no_mods_msg
    call print_string
    call print_newline
    
.done:
    pop ecx
    pop ebx
    pop eax
    ret

; Seleccionar el mejor puntero a módulos disponible
select_best_modules:
    push eax
    
    ; Por defecto, usar nuestro CoreLib module def
    lea eax, [corelib_module_def]
    mov [modules_ptr], eax
    
    ; Aquí podrías añadir lógica para seleccionar entre
    ; tus módulos y los módulos multiboot si es necesario
    
    pop eax
    ret

; Preparar módulos de CoreLib
prepare_corelib_modules:
    ; Inicializar áreas de memoria si es necesario
    push eax
    push edi
    push ecx
    
    ; Limpiar GC Static Data
    mov edi, gc_static_data
    mov ecx, 1024
    xor eax, eax
    rep stosb
    
    ; Limpiar CCTOR Data
    mov edi, cctor_data
    mov ecx, 1024
    xor eax, eax
    rep stosb
    
    ; Mostrar mensaje después de configurar
    mov esi, modules_done_msg
    call print_string
    call print_newline
    
    pop ecx
    pop edi
    pop eax
    ret

; Antes de tu sección existente, agrega estas constantes
; Constantes para gestión de memoria
HEAP_START      equ 0x00400000     ; 4MB - Inicio del heap
HEAP_SIZE       equ 0x00400000     ; 4MB - Tamaño del heap
PAGE_SIZE       equ 0x1000         ; 4KB - Tamaño de página

; En tu sección .data, agrega estas variables
section .data
    ; Variables para gestión de memoria
    heap_start_ptr dd 0    ; Dirección de inicio del heap
    heap_current dd 0      ; Puntero actual (siguiente asignación)
    heap_end_ptr dd 0      ; Fin del heap (límite)
    heap_msg db "Inicializando heap en: ", 0
    heap_size_msg db "Tamanio del heap: ", 0

; Agrega estas funciones a tu código

; Inicializar el heap - llamar antes de Entry
global init_heap
init_heap:
    push ebp
    mov ebp, esp
    push ebx
    push esi
    push edi
    
    ; Mostrar mensaje
    mov esi, heap_msg
    call print_string
    mov eax, HEAP_START
    call print_hex_eax
    call print_newline
    
    ; Inicializar estructura de heap
    mov dword [heap_start_ptr], HEAP_START
    mov dword [heap_current], HEAP_START
    mov eax, HEAP_START
    add eax, HEAP_SIZE
    mov dword [heap_end_ptr], eax
    
    ; Limpiar el área del heap
    mov edi, HEAP_START
    mov ecx, HEAP_SIZE / 4    ; Dividir por 4 porque stosd escribe dwords
    xor eax, eax
    rep stosb
    
    ; Mostrar información del heap
    mov esi, heap_size_msg
    call print_string
    mov eax, HEAP_SIZE
    call print_hex_eax
    call print_newline
    
    pop edi
    pop esi
    pop ebx
    mov esp, ebp
    pop ebp
    ret

; Asignar memoria del heap
global malloc
malloc:
    push ebp
    mov ebp, esp
    push ebx
    
    ; Obtener tamaño solicitado
    mov ebx, [ebp+8]
    
    ; Alinear a 8 bytes (para compatibilidad con C#)
    add ebx, 7
    and ebx, ~7
    
    ; Verificar si hay espacio suficiente
    mov eax, [heap_current]
    add eax, ebx
    cmp eax, [heap_end_ptr]
    ja .no_memory
    
    ; Hay espacio, asignar memoria
    mov eax, [heap_current]
    add dword [heap_current], ebx
    jmp .done
    
.no_memory:
    ; No hay memoria suficiente
    xor eax, eax
    
.done:
    pop ebx
    mov esp, ebp
    pop ebp
    ret
; Función para habilitar SSE
enable_sse:
    ; Verificar soporte de SSE
    mov eax, 0x1
    cpuid
    test edx, 1<<25
    jz .sse_not_supported   ; Si SSE no está soportado, fallar
    
    ; Habilitar SSE
    mov eax, cr0
    and ax, 0xFFFB          ; Limpiar bit de emulación de coprocesador CR0.EM
    or ax, 0x2              ; Establecer monitoreo de coprocesador CR0.MP
    mov cr0, eax
    mov eax, cr4
    or ax, 3 << 9           ; Establecer CR4.OSFXSR y CR4.OSXMMEXCPT
    mov cr4, eax
    
    ; Mostrar mensaje de éxito
    mov esi, sse_msg
    call print_string
    call print_newline
    
    clc                     ; Limpiar flag de acarreo para indicar éxito
    ret
    
.sse_not_supported:
    ; SSE no soportado - mostrar error
    mov esi, error_msg
    call print_string
    call print_newline
    stc                     ; Establecer flag de acarreo para indicar error
    ret

; Función para imprimir nueva línea
print_newline:
    push eax
    push ebx
    push ecx
    push edx
    
    ; Obtener posición actual
    mov ebx, [screen_ptr]
    
    ; Calcular posición de la siguiente línea
    mov eax, ebx
    sub eax, 0xB8000        ; Restar base de video
    mov edx, 0              ; Limpiar parte alta para división
    mov ecx, 160            ; 80 caracteres * 2 bytes por línea
    div ecx                 ; eax = línea actual, edx = offset en línea
    
    inc eax                 ; Siguiente línea
    mul ecx                 ; eax = offset de la siguiente línea
    add eax, 0xB8000        ; Añadir base
    
    ; Actualizar puntero de pantalla
    mov [screen_ptr], eax
    
    pop edx
    pop ecx
    pop ebx
    pop eax
    ret

; Función para imprimir EAX como valor hexadecimal
print_hex_eax:
    push eax
    push ebx
    push ecx
    push edx
    
    mov ecx, 8              ; 8 dígitos para un valor de 32 bits
    mov ebx, [screen_ptr]   ; Obtener posición actual de pantalla
    
    ; Escribir "0x" prefijo
    mov byte [ebx], '0'
    mov byte [ebx+1], 0x0F
    mov byte [ebx+2], 'x'
    mov byte [ebx+3], 0x0F
    add ebx, 4
    
    ; Guardar valor original de eax
    push eax
    
    ; Bucle para cada dígito
.print_digit:
    rol eax, 4              ; Rotar a la izquierda para obtener el dígito más significativo
    mov edx, eax
    and edx, 0xF            ; Aislar el dígito
    
    ; Convertir a carácter ASCII
    cmp edx, 10
    jl .decimal
    add edx, 'A' - 10 - '0'
    
.decimal:
    add edx, '0'
    
    ; Escribir el carácter
    mov byte [ebx], dl
    mov byte [ebx+1], 0x0F
    add ebx, 2
    
    loop .print_digit
    
    ; Restaurar valor original de eax
    pop eax
    
    ; Actualizar puntero de pantalla
    mov [screen_ptr], ebx
    
    pop edx
    pop ecx
    pop ebx
    pop eax
    ret

; Función de impresión de cadenas
print_string:
    push eax
    push ebx
    push esi
    
    mov ebx, [screen_ptr]   ; Obtener posición actual de pantalla
    
.loop:
    mov al, [esi]           ; Cargar carácter
    test al, al             ; Verificar fin de cadena
    jz .done
    
    mov byte [ebx], al      ; Escribir carácter
    mov byte [ebx+1], 0x0F  ; Atributo (blanco sobre negro)
    
    inc esi                 ; Siguiente carácter
    add ebx, 2              ; Siguiente posición en pantalla
    jmp .loop
    
.done:
    ; Actualizar puntero de pantalla
    mov [screen_ptr], ebx
    
    pop esi
    pop ebx
    pop eax
    ret

section .data
    ; Variable para seguir la posición actual en la pantalla
    screen_ptr dd 0xB8000
    ebx_msg db "EBX original: ", 0
    multiboot_data_msg db "Multiboot flags: ", 0
    magic_msg db "Magic posible: ", 0