; Advanced Interrupt Handling for 64-bit Kernel
; Optimized for native C# low-level kernel with APIC support

global _LoadIDT
global _GetInterruptStub
global _GetInterruptStubAddress
global _CLI
global _STI
global _Halt
global _Pause

extern HandleInterrupt

section .data
align 8
InterruptStubTable:
    %assign i 0
    %rep 256
        dq _InterruptStub %+ i
    %assign i i+1
    %endrep

section .text

; Macro para crear stubs de interrupciones
%macro CREATE_INTERRUPT_STUB 1
_InterruptStub%1:
    ; Si tiene error code, no se hace nada. Si no, se pushea 0
    %if (%1 == 8) || (%1 >= 10 && %1 <= 14) || (%1 == 17) || (%1 == 30)
        ; Interrupts con error code, no hacer push extra
    %else
        push qword 0
    %endif
    push qword %1        ; Interrupt Number
    jmp _InterruptCommonHandler
%endmacro

; Generar los 256 stubs
%assign i 0
%rep 256
    CREATE_INTERRUPT_STUB i
%assign i i+1
%endrep

; Handler común para todas las interrupciones
_InterruptCommonHandler:
    ; Salvar registros generales
    push rax
    push rbx
    push rcx
    push rdx
    push rsi
    push rdi
    push rbp
    push r8
    push r9
    push r10
    push r11
    push r12
    push r13
    push r14
    push r15

    ; Guardar puntero al stack actual (para construir el frame)
    mov rax, rsp

    ; Reservar espacio para InterruptFrame (128 bytes)
    sub rsp, 128

    ; Guardar registros en InterruptFrame
    mov [rsp],     rdi
    mov [rsp+8],   rsi
    mov [rsp+16],  rbp
    mov [rsp+24],  rax       ; Stack base
    mov [rsp+32],  rbx
    mov [rsp+40],  rdx
    mov [rsp+48],  rcx
    mov [rsp+56],  rax       ; RAX duplicado para seguridad

    ; Cargar interrupt number y error code
    mov rax, [rsp+128 + 8]   ; Número de interrupción
    mov [rsp+64], rax
    mov rax, [rsp+128]       ; Error code
    mov [rsp+72], rax

    ; RIP, CS, RFLAGS, RSP (User), SS
    mov rax, [rsp+128 + 16]
    mov [rsp+80], rax
    mov rax, [rsp+128 + 24]
    mov [rsp+88], rax
    mov rax, [rsp+128 + 32]
    mov [rsp+96], rax
    mov rax, [rsp+128 + 40]
    mov [rsp+104], rax
    mov rax, [rsp+128 + 48]
    mov [rsp+112], rax

    ; Fastcall RCX = ptr a InterruptFrame
    mov rcx, rsp
    and rsp, -16
    call HandleInterrupt

    ; Restaurar registros
    mov rsp, [rsp+24]  ; restaurar RSP desde frame original
    add rsp, 128       ; eliminar espacio del InterruptFrame

    pop r15
    pop r14
    pop r13
    pop r12
    pop r11
    pop r10
    pop r9
    pop r8
    pop rbp
    pop rdi
    pop rsi
    pop rdx
    pop rcx
    pop rbx
    pop rax

    add rsp, 16        ; eliminar interrupt number + error code
    iretq

; ------------------------------
; Funciones auxiliares del kernel
; ------------------------------

_LoadIDT:
    cli
    lidt [rcx]   ; RCX = ptr a IDT (struct)
    sti
    ret

_GetInterruptStub:
    cmp rcx, 256
    jae .invalid
    mov rax, [InterruptStubTable + rcx * 8]
    ret
.invalid:
    xor rax, rax
    ret

_GetInterruptStubAddress:
    mov rax, _InterruptStub0
    ret

_CLI:
    cli
    ret

_STI:
    sti
    ret

_Halt:
    hlt
    ret

_Pause:
    pause
    ret
