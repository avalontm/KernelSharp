; GetTime.asm - Implementaciones de funciones de tiempo para kernel de 64 bits

section .text
global _GetTime
global _GetUtcTime

section .data
; Obtener la hora actual desde el RTC (Real-Time Clock)
_GetTime:
    push rbx
    push rcx
    
    ; Deshabilitar interrupciones para operación atómica
    cli
    
    ; Leer siglo (si está disponible en algunos BIOS)
    mov al, 0x32         ; Registro del siglo
    out 0x70, al         ; Seleccionar registro
    in al, 0x71          ; Leer valor
    mov bl, al           ; Guardar siglo en BL
    
    ; Leer año
    mov al, 0x09         ; Registro del año
    out 0x70, al         ; Seleccionar registro
    in al, 0x71          ; Leer valor
    mov bh, al           ; Guardar año en BH
    
    ; Leer mes
    mov al, 0x08         ; Registro del mes
    out 0x70, al         ; Seleccionar registro
    in al, 0x71          ; Leer valor
    mov cl, al           ; Guardar mes en CL
    
    ; Leer día
    mov al, 0x07         ; Registro del día
    out 0x70, al         ; Seleccionar registro
    in al, 0x71          ; Leer valor
    mov ch, al           ; Guardar día en CH
    
    ; Leer hora
    mov al, 0x04         ; Registro de la hora
    out 0x70, al         ; Seleccionar registro
    in al, 0x71          ; Leer valor
    shl rax, 8           ; Desplazar a la posición correcta (hora)
    
    ; Leer minuto
    mov al, 0x02         ; Registro del minuto
    out 0x70, al         ; Seleccionar registro
    in al, 0x71          ; Leer valor
    shl rax, 8           ; Desplazar a la posición correcta (minuto)
    
    ; Leer segundo
    mov al, 0x00         ; Registro del segundo
    out 0x70, al         ; Seleccionar registro
    in al, 0x71          ; Leer valor
    shl rax, 8           ; Desplazar a la posición correcta (segundo)
    
    ; Habilitar interrupciones
    sti
    
    ; Preparar el valor de retorno en RAX:EDX (64 bits)
    xor rdx, rdx         ; Limpiar RDX para la parte alta
    mov dl, bl           ; Siglo en bits 56-63
    shl rdx, 8
    mov dl, bh           ; Año en bits 48-55
    shl rdx, 8
    mov dl, cl           ; Mes en bits 40-47
    shl rdx, 8
    mov dl, ch           ; Día en bits 32-39
    
    mov al, 0x0B         ; Registro de estado B
    out 0x70, al
    in al, 0x71
    
    test al, 0x04        ; Bit 2 indica si está en BCD (0) o binario (1)
    jnz .done            ; Si está en binario, no se necesita conversión
    
    call _ConvertBCDtoBinary
    
.done:
    pop rcx
    pop rbx
    ret

_GetUtcTime:
    jmp _GetTime

; Función para convertir BCD a binario
_ConvertBCDtoBinary:
    push rcx
    
    ; Convertir siglo (RDX bits 56-63)
    mov rcx, rdx
    shr rcx, 56
    and rcx, 0xFF
    call _BCDtoBin
    and rdx, 0x00FFFFFFFFFFFFFF
    shl rcx, 56
    or rdx, rcx
    
    ; Convertir año (RDX bits 48-55)
    mov rcx, rdx
    shr rcx, 48
    and rcx, 0xFF
    call _BCDtoBin
    and rdx, 0xFF00FFFFFFFFFFFF
    shl rcx, 48
    or rdx, rcx
    
    ; Convertir mes (RDX bits 40-47)
    mov rcx, rdx
    shr rcx, 40
    and rcx, 0xFF
    call _BCDtoBin
    and rdx, 0xFFFF00FFFFFFFFFF
    shl rcx, 40
    or rdx, rcx
    
    ; Convertir día (RDX bits 32-39)
    mov rcx, rdx
    shr rcx, 32
    and rcx, 0xFF
    call _BCDtoBin
    and rdx, 0xFFFFFF00FFFFFFFF
    shl rcx, 32
    or rdx, rcx
    
    ; Convertir hora (RAX bits 24-31)
    mov rcx, rax
    shr rcx, 24
    call _BCDtoBin
    and rax, 0x00FFFFFF
    shl rcx, 24
    or rax, rcx
    
    ; Convertir minuto (RAX bits 16-23)
    mov rcx, rax
    shr rcx, 16
    and rcx, 0xFF
    call _BCDtoBin
    and rax, 0xFF00FFFF
    shl rcx, 16
    or rax, rcx
    
    ; Convertir segundo (RAX bits 8-15)
    mov rcx, rax
    shr rcx, 8
    and rcx, 0xFF
    call _BCDtoBin
    and rax, 0xFFFF00FF
    shl rcx, 8
    or rax, rcx
    
    pop rcx
    ret

; Función para convertir un valor BCD a binario
_BCDtoBin:
    push rax
    
    movzx rax, cl
    mov cl, al                  ; Guardar valor original
    shr al, 4                   ; Aislar decenas
    mov ah, 10
    mul ah                      ; Decenas * 10
    and cl, 0x0F                ; Aislar unidades
    add al, cl                  ; Decenas*10 + unidades
    mov cl, al                  ; Guardar resultado
    
    pop rax
    ret
