; GetTime.asm - Implementaciones de funciones de tiempo para kernel de 32 bits
;
; Este archivo contiene las implementaciones de GetTime y GetUtcTime
; necesarias para el funcionamiento del DateTime en el kernel.
;

section .text
global _GetTime
global _GetUtcTime

; -------------------------------------------------------
; _GetTime - Obtiene la fecha y hora actual del sistema
; 
; Salida:
;   EAX:EDX - Valor de 64 bits con formato:
;     Bits 56-63: Siglo (1-99)
;     Bits 48-55: Año (0-99)
;     Bits 40-47: Mes (1-12)
;     Bits 32-39: Día (1-31)
;     Bits 24-31: Hora (0-23)
;     Bits 16-23: Minuto (0-59)
;     Bits 8-15: Segundo (0-59)
;     Bits 0-7: Centésimas (0-99) - No utilizado actualmente
; -------------------------------------------------------
_GetTime:
    push ebx
    push ecx
    
    ; Leer valores del RTC (Real-Time Clock) de CMOS
    ; Primero deshabilitar interrupciones para operación atómica
    cli
    
    ; Leer siglo (algunos BIOSes lo soportan)
    mov al, 0x32         ; Registro del siglo (si está disponible)
    out 0x70, al         ; Seleccionar registro
    in al, 0x71          ; Leer valor
    mov bl, al           ; Guardar siglo
    
    ; Leer año
    mov al, 0x09         ; Registro del año
    out 0x70, al         ; Seleccionar registro
    in al, 0x71          ; Leer valor
    mov bh, al           ; Guardar año
    
    ; Leer mes
    mov al, 0x08         ; Registro del mes
    out 0x70, al         ; Seleccionar registro
    in al, 0x71          ; Leer valor
    mov cl, al           ; Guardar mes
    
    ; Leer día
    mov al, 0x07         ; Registro del día
    out 0x70, al         ; Seleccionar registro
    in al, 0x71          ; Leer valor
    mov ch, al           ; Guardar día
    
    ; Leer hora
    mov al, 0x04         ; Registro de la hora
    out 0x70, al         ; Seleccionar registro
    in al, 0x71          ; Leer valor
    shl eax, 8           ; Desplazar a posición correcta
    
    ; Leer minuto
    mov al, 0x02         ; Registro del minuto
    out 0x70, al         ; Seleccionar registro
    in al, 0x71          ; Leer valor
    shl eax, 8           ; Desplazar a posición correcta
    
    ; Leer segundo
    mov al, 0x00         ; Registro del segundo
    out 0x70, al         ; Seleccionar registro
    in al, 0x71          ; Leer valor
    shl eax, 8           ; Desplazar a posición correcta
    
    ; Habilitar interrupciones
    sti
    
    ; Preparar el valor de retorno de 64 bits (EAX:EDX)
    xor edx, edx         ; Limpiar EDX para parte alta
    mov dh, bl           ; Siglo en bits 56-63
    shl edx, 8
    mov dh, bh           ; Año en bits 48-55
    shl edx, 8
    mov dh, cl           ; Mes en bits 40-47
    shl edx, 8
    mov dh, ch           ; Día en bits 32-39
    
    ; EAX ya contiene hora, minuto y segundo en posiciones correctas
    
    ; Convertir desde BCD a binario si es necesario
    ; Leer el registro de estado B para verificar formato
    mov al, 0x0B         ; Registro de estado B
    out 0x70, al
    in al, 0x71
    
    test al, 0x04        ; Bit 2 indica si está en BCD (0) o binario (1)
    jnz .done            ; Si es binario, no se necesita conversión
    
    ; Convertir de BCD a binario
    call _ConvertBCDtoBinary
    
.done:
    pop ecx
    pop ebx
    ret

; -------------------------------------------------------
; _GetUtcTime - Obtiene la fecha y hora UTC del sistema
; 
; Es igual que _GetTime, pero ajustado para UTC si es posible.
; En un kernel básico, podría devolver la misma hora que _GetTime
; ya que aún no hay información de zona horaria.
; -------------------------------------------------------
_GetUtcTime:
    ; Para un kernel simple, podemos usar la misma función que _GetTime
    ; En un kernel más complejo, se podría ajustar según la zona horaria
    jmp _GetTime

; -------------------------------------------------------
; _ConvertBCDtoBinary - Convierte los valores BCD en binario
;
; Entrada/Salida:
;   EDX:EAX - Valor a convertir
; -------------------------------------------------------
_ConvertBCDtoBinary:
    push ecx
    
    ; Convertir siglo (EDX bits 56-63)
    mov ecx, edx
    shr ecx, 24
    and ecx, 0xFF00
    call _BCDtoBin
    and edx, 0x00FFFFFF
    shl ecx, 24
    or edx, ecx
    
    ; Convertir año (EDX bits 48-55)
    mov ecx, edx
    shr ecx, 16
    and ecx, 0xFF00
    call _BCDtoBin
    and edx, 0xFF00FFFF
    shl ecx, 16
    or edx, ecx
    
    ; Convertir mes (EDX bits 40-47)
    mov ecx, edx
    shr ecx, 8
    and ecx, 0xFF00
    call _BCDtoBin
    and edx, 0xFFFF00FF
    shl ecx, 8
    or edx, ecx
    
    ; Convertir día (EDX bits 32-39)
    mov ecx, edx
    and ecx, 0xFF00
    call _BCDtoBin
    and edx, 0xFFFFFF00
    or edx, ecx
    
    ; Convertir hora (EAX bits 24-31)
    mov ecx, eax
    shr ecx, 24
    call _BCDtoBin
    and eax, 0x00FFFFFF
    shl ecx, 24
    or eax, ecx
    
    ; Convertir minuto (EAX bits 16-23)
    mov ecx, eax
    shr ecx, 16
    and ecx, 0xFF
    call _BCDtoBin
    and eax, 0xFF00FFFF
    shl ecx, 16
    or eax, ecx
    
    ; Convertir segundo (EAX bits 8-15)
    mov ecx, eax
    shr ecx, 8
    and ecx, 0xFF
    call _BCDtoBin
    and eax, 0xFFFF00FF
    shl ecx, 8
    or eax, ecx
    
    pop ecx
    ret

; -------------------------------------------------------
; _BCDtoBin - Convierte un byte BCD a binario
;
; Entrada:
;   CL - Valor BCD
; Salida:
;   CL - Valor binario
; -------------------------------------------------------
_BCDtoBin:
    push eax
    
    movzx eax, cl
    mov cl, al                  ; Guardar valor original
    shr al, 4                   ; Aislar decenas
    mov ah, 10
    mul ah                      ; Decenas * 10
    and cl, 0x0F                ; Aislar unidades
    add al, cl                  ; Decenas*10 + unidades
    mov cl, al                  ; Guardar resultado
    
    pop eax
    ret