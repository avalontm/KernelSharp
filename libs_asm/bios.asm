; bios.asm - Implementación de interfaz de la BIOS para KernelSharp
; Este archivo proporciona la implementación en ensamblador para acceder a las
; interrupciones de la BIOS desde el código C#

section .text
global _Int10h
global _Int12h
global _Int13h
global _Int15h
global _Int16h
global _CPUID
global _RDTSC
global _PAUSE
global _LoadIDT
global _LoadGDT
global _Interrupt
global _CPU_ReadCR0
global _CPU_WriteCR0
global _CPU_ReadCR3
global _CPU_WriteCR3
global _CPU_ReadCR4
global _CPU_WriteCR4
global _EnableInterrupts
global _DisableInterrupts
global _Halt
global _RDMSR
global _WRMSR
global _Invlpg

; Estructura del registro para llamadas a la BIOS
; struct RegistersX86 {
;   uint32_t EAX, EBX, ECX, EDX, ESI, EDI, EBP;
;   uint16_t DS, ES, FS, GS;
;   uint32_t EFLAGS;
; }

; Offset para cada registro en la estructura
%define REG_EAX     0
%define REG_EBX     4
%define REG_ECX     8
%define REG_EDX     12
%define REG_ESI     16
%define REG_EDI     20
%define REG_EBP     24
%define REG_DS      28
%define REG_ES      30
%define REG_FS      32
%define REG_GS      34
%define REG_EFLAGS  36

; Macro para simplificar la implementación de interrupciones de la BIOS
%macro BIOS_INT 1
    ; Preservar registros
    pushad
    pushfd
    
    ; Obtener el puntero a la estructura RegistersX86
    mov ebp, [esp + 40]    ; 8 registros (pushad) + flags (pushfd) + dirección de retorno
    
    ; Cargar valores desde la estructura a registros
    mov eax, [ebp + REG_EAX]
    mov ebx, [ebp + REG_EBX]
    mov ecx, [ebp + REG_ECX]
    mov edx, [ebp + REG_EDX]
    mov esi, [ebp + REG_ESI]
    mov edi, [ebp + REG_EDI]
    
    ; Configurar segmentos si es necesario
    mov ax, [ebp + REG_DS]
    test ax, ax
    jz %%skip_ds
    push ds
    mov ds, ax
%%skip_ds:
    
    mov ax, [ebp + REG_ES]
    test ax, ax
    jz %%skip_es
    push es
    mov es, ax
%%skip_es:
    
    mov ax, [ebp + REG_FS]
    test ax, ax
    jz %%skip_fs
    push fs
    mov fs, ax
%%skip_fs:
    
    mov ax, [ebp + REG_GS]
    test ax, ax
    jz %%skip_gs
    push gs
    mov gs, ax
%%skip_gs:
    
    ; Restaurar EAX para la interrupción
    mov eax, [ebp + REG_EAX]
    
    ; Ejecutar la interrupción BIOS
    int %1
    
    ; Restaurar segmentos si se cambiaron
    mov cx, [ebp + REG_GS]
    test cx, cx
    jz %%skip_restore_gs
    pop gs
%%skip_restore_gs:
    
    mov cx, [ebp + REG_FS]
    test cx, cx
    jz %%skip_restore_fs
    pop fs
%%skip_restore_fs:
    
    mov cx, [ebp + REG_ES]
    test cx, cx
    jz %%skip_restore_es
    pop es
%%skip_restore_es:
    
    mov cx, [ebp + REG_DS]
    test cx, cx
    jz %%skip_restore_ds
    pop ds
%%skip_restore_ds:
    
    ; Guardar los resultados de vuelta en la estructura
    mov [ebp + REG_EAX], eax
    mov [ebp + REG_EBX], ebx
    mov [ebp + REG_ECX], ecx
    mov [ebp + REG_EDX], edx
    mov [ebp + REG_ESI], esi
    mov [ebp + REG_EDI], edi
    
    ; Guardar EFLAGS
    pushfd
    pop dword [ebp + REG_EFLAGS]
    
    ; Restaurar registros y volver
    popfd
    popad
    ret
%endmacro

; Implementación de las interrupciones BIOS
_Int10h:
    BIOS_INT 0x10

_Int12h:
    BIOS_INT 0x12

_Int13h:
    BIOS_INT 0x13

_Int15h:
    BIOS_INT 0x15

_Int16h:
    BIOS_INT 0x16

; Instrucción CPUID
; void CPUID(uint32_t function, uint32_t subfunction, uint32_t* eax, uint32_t* ebx, uint32_t* ecx, uint32_t* edx)
_CPUID:
    push ebp
    mov ebp, esp
    push ebx
    push esi
    push edi
    
    ; Obtener parámetros
    mov eax, [ebp + 8]     ; function
    mov ecx, [ebp + 12]    ; subfunction
    
    ; Ejecutar CPUID
    cpuid
    
    ; Guardar resultados en los punteros proporcionados
    mov esi, [ebp + 16]    ; eax
    mov [esi], eax
    
    mov esi, [ebp + 20]    ; ebx
    mov [esi], ebx
    
    mov esi, [ebp + 24]    ; ecx
    mov [esi], ecx
    
    mov esi, [ebp + 28]    ; edx
    mov [esi], edx
    
    pop edi
    pop esi
    pop ebx
    pop ebp
    ret

; Leer Time Stamp Counter
; uint64_t RDTSC()
_RDTSC:
    rdtsc                   ; EDX:EAX = TSC
    ret

; Instrucción PAUSE (mejora rendimiento en bucles de espera)
; void PAUSE()
_PAUSE:
    pause
    ret

; Carga una IDT (Interrupt Descriptor Table)
; void LoadIDT(void* base, uint16_t limit)
_LoadIDT:
    push ebp
    mov ebp, esp
    
    ; Crear descriptor de 6 bytes en la pila
    sub esp, 6
    mov eax, [ebp + 8]     ; base
    mov [esp], ax          ; Límite (parte baja)
    mov [esp+2], eax       ; Base

    ; Cargar la IDT
    lidt [esp]
    
    add esp, 6
    pop ebp
    ret

; Carga una GDT (Global Descriptor Table)
; void LoadGDT(void* base, uint16_t limit)
_LoadGDT:
    push ebp
    mov ebp, esp
    
    ; Crear descriptor de 6 bytes en la pila
    sub esp, 6
    mov eax, [ebp + 12]    ; limit
    mov [esp], ax          ; Límite
    mov eax, [ebp + 8]     ; base
    mov [esp+2], eax       ; Base

    ; Cargar la GDT
    lgdt [esp]
    
    ; Actualizar selectores de segmento
    jmp 0x08:.reload_cs    ; Selector de código (índice 1)
.reload_cs:
    mov ax, 0x10           ; Selector de datos (índice 2)
    mov ds, ax
    mov es, ax
    mov fs, ax
    mov gs, ax
    mov ss, ax
    
    add esp, 6
    pop ebp
    ret

; Genera una interrupción software
; void Interrupt(uint8_t interrupt)
_Interrupt:
    push ebp
    mov ebp, esp
    
    ; Guardar la dirección de retorno
    mov eax, [ebp + 8]     ; número de interrupción
    
    ; Tabla de saltos para manejar las diferentes interrupciones
    ; (se pueden agregar más casos según sea necesario)
    cmp al, 0
    je .int0
    cmp al, 1
    je .int1
    ; ... más casos si es necesario
    
    ; Interrupción genérica para otros casos
    int 0x80
    jmp .done
    
.int0:
    int 0
    jmp .done
.int1:
    int 1
    jmp .done
    
.done:
    pop ebp
    ret

; Leer el registro CR0
; uint32_t CPU_ReadCR0()
_CPU_ReadCR0:
    mov eax, cr0
    ret

; Escribir en el registro CR0
; void CPU_WriteCR0(uint32_t value)
_CPU_WriteCR0:
    mov eax, [esp + 4]
    mov cr0, eax
    ret

; Leer el registro CR3
; uint32_t CPU_ReadCR3()
_CPU_ReadCR3:
    mov eax, cr3
    ret

; Escribir en el registro CR3
; void CPU_WriteCR3(uint32_t value)
_CPU_WriteCR3:
    mov eax, [esp + 4]
    mov cr3, eax
    ret

; Leer el registro CR4
; uint32_t CPU_ReadCR4()
_CPU_ReadCR4:
    mov eax, cr4
    ret

; Escribir en el registro CR4
; void CPU_WriteCR4(uint32_t value)
_CPU_WriteCR4:
    mov eax, [esp + 4]
    mov cr4, eax
    ret

; Habilitar interrupciones
; void EnableInterrupts()
_EnableInterrupts:
    sti
    ret

; Deshabilitar interrupciones
; void DisableInterrupts()
_DisableInterrupts:
    cli
    ret

; Detener la CPU hasta la próxima interrupción
; void Halt()
_Halt:
    hlt
    ret

; Leer un registro MSR (Model-Specific Register)
; void RDMSR(uint32_t msr, uint64_t* value)
_RDMSR:
    push ebp
    mov ebp, esp
    
    mov ecx, [ebp + 8]     ; Número de MSR
    rdmsr                   ; EDX:EAX = Valor MSR
    
    ; Guardar el resultado en el puntero proporcionado
    mov esi, [ebp + 12]    ; Puntero al valor de 64 bits
    mov [esi], eax         ; Parte baja (32 bits)
    mov [esi + 4], edx     ; Parte alta (32 bits)
    
    pop ebp
    ret

; Escribir en un registro MSR (Model-Specific Register)
; void WRMSR(uint32_t msr, uint64_t value)
_WRMSR:
    push ebp
    mov ebp, esp
    
    mov ecx, [ebp + 8]     ; Número de MSR
    mov eax, [ebp + 12]    ; Parte baja del valor (32 bits)
    mov edx, [ebp + 16]    ; Parte alta del valor (32 bits)
    wrmsr
    
    pop ebp
    ret

; Invalidar una entrada en la TLB
; void Invlpg(void* address)
_Invlpg:
    mov eax, [esp + 4]     ; Dirección a invalidar
    invlpg [eax]
    ret