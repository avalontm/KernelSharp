; Asegúrate de que estas funciones estén expuestas globalmente
global _malloc
global _free
global free
; Constantes para heap
HEAP_START      equ 0x00800000     ; 8MB - Inicio del heap
HEAP_SIZE       equ 0x00800000     ; 8MB - Tamaño del heap

section .data
    heap_start_ptr dd HEAP_START    ; Inicio del heap
    heap_current  dd HEAP_START     ; Posición actual
    heap_end_ptr  dd HEAP_START + HEAP_SIZE ; Fin del heap

; ===================================================================
; malloc - Asigna un bloque de memoria
; ===================================================================
_malloc:
    push ebp
    mov ebp, esp
    push ebx
    
    ; Obtener tamaño solicitado y ajustar a múltiplo de 8
    mov ebx, [ebp+8]        ; Tamaño solicitado
    add ebx, 7
    and ebx, ~7             ; Alinear a 8 bytes
    
    ; Verificar espacio disponible
    mov eax, [heap_current]
    add eax, ebx
    cmp eax, [heap_end_ptr]
    ja .no_memory
    
    ; Asignar memoria y actualizar puntero
    mov eax, [heap_current]
    add [heap_current], ebx
    jmp .done
    
.no_memory:
    xor eax, eax            ; Devolver NULL
    
.done:
    pop ebx
    mov esp, ebp
    pop ebp
    ret

; ===================================================================
; free - Libera un bloque de memoria
; Parámetros:
;   [ESP+4] - Dirección del bloque a liberar
; ===================================================================
free:
_free:
    push ebp
    mov ebp, esp
    push ebx
    push esi
    push edi
    
    ; Obtener el puntero a liberar
    mov ebx, [ebp+8]
    
    ; Verificar si el puntero es NULL
    test ebx, ebx
    jz .done           ; Si es NULL, no hacer nada
    
    ; Verificar si está dentro del rango de nuestro heap
    cmp ebx, [heap_start_ptr]
    jb .done           ; Si está por debajo del inicio del heap, ignorar
    cmp ebx, [heap_end_ptr]
    jae .done          ; Si está por encima del final del heap, ignorar
    
    ; En esta versión simplemente marcamos la memoria como disponible
    ; Para un sistema real, aquí implementarías una estrategia de liberación
    ; como coalescing, free lists, etc.
    
    ; Para esta implementación básica, simplemente limpiamos la memoria
    ; si está cerca del final del heap
    
    ; Verificar si este bloque es el último asignado
    mov ecx, [heap_current]
    sub ecx, 8         ; Retroceder un poco para considerar el tamaño de encabezado
    cmp ebx, ecx
    jb .not_last_block
    
    ; Es el último bloque, podemos reclamar la memoria
    ; ajustando heap_current
    mov [heap_current], ebx
    jmp .done
    
.not_last_block:
    ; Para bloques que no son el último, simplemente los dejamos
    ; Esta implementación no recupera memoria fragmentada
    
.done:
    pop edi
    pop esi
    pop ebx
    mov esp, ebp
    pop ebp
    ret