; Interrupt Handlers for x86_64 OS Kernel
; This file defines 256 unique interrupt stubs for the IDT
; Each stub preserves registers and calls the common C# handler

global _SetupInterrupts
global _GetInterruptStub
global _CLI
global _STI
global _Halt
global _Pause
global _InterruptCommon
global _GetInterruptStubAddress

; Declare external references to C# functions
extern HandleInterrupt     ; Main handler function in C#
extern _handlerTable       ; Table of interrupt handlers in C#

section .data
; Table of interrupt stub addresses
InterruptStubTable:
    %assign i 0
    %rep 256
        dq _InterruptStub %+ i     ; Create an entry for each stub
    %assign i i+1
    %endrep

section .text

; Sets up the interrupt system
_SetupInterrupts:
    ret

; Returns the address of a specific interrupt stub
; RCX = Interrupt number (0-255)
_GetInterruptStub:
    cmp rcx, 256          ; Check if number is valid
    jae .invalid
    mov rax, [InterruptStubTable + rcx * 8]  ; Get address from table
    ret
.invalid:
    xor rax, rax          ; Return NULL if invalid
    ret

; Disable interrupts (CLI instruction)
_CLI:
    cli
    ret

; Enable interrupts (STI instruction)
_STI:
    sti
    ret

; Halt the CPU until next interrupt
_Halt:
    hlt
    ret

; Pause instruction (for busy-waiting)
_Pause:
    pause
    ret

; Common part of the interrupt handler
; RCX contains the interrupt number
_InterruptCommon:
    ; Save all general purpose registers
    push rax
    push rcx
    push rdx
    push rbx
    push rsp
    push rbp
    push rsi
    push rdi
    
    ; Save interrupt number and error code on stack
    sub rsp, 16
    mov [rsp], rcx        ; Store interrupt number
    mov qword [rsp+8], 0  ; Default error code (0)
    
    ; Call C# handler function
    mov rcx, rsp          ; Pass pointer to register stack as first parameter
    sub rsp, 32           ; Shadow space for MS x64 calling convention
    call HandleInterrupt
    add rsp, 32
    
    ; Restore stack (remove interrupt number and error code)
    add rsp, 16
    
    ; Restore all registers
    pop rdi
    pop rsi
    pop rbp
    pop rsp
    pop rbx
    pop rdx
    pop rcx
    pop rax
    
    ; Return from interrupt
    iretq

section .text
_GetInterruptStubAddress:
    ; Devolver la dirección del primer stub de interrupción
    mov rax, _InterruptStub0
    ret
; Now define 256 unique interrupt stubs
; Each loads its own interrupt number in RCX and then jumps to common code

%macro INTERRUPT_STUB 1
_InterruptStub%1:
    %if (%1 = 8) || (%1 >= 10 && %1 <= 14) || (%1 = 17) || (%1 = 30)
        ; These interrupts push an error code
        ; So we leave it on the stack
    %else
        ; For interrupts without error code, push a dummy value
        push qword 0
    %endif
    
    ; Push interrupt number and jump to common code
    mov rcx, %1           ; Load interrupt number
    jmp _InterruptCommon
%endmacro

; Generate all 256 interrupt stubs
%assign i 0
%rep 256
    INTERRUPT_STUB i
%assign i i+1
%endrep