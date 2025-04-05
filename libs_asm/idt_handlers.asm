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
global _LoadIDT              ; Load IDT

; Declare external references to C# functions
extern HandleInterrupt     ; Main handler function in C#
extern _handlerTable       ; Table of interrupt handlers in C#

section .data

; Interrupt Descriptor Table (IDT)
; Define 256 interrupt stubs (address array for IDT)
InterruptStubTable:
    ; Assign 256 stubs
    %assign i 0
    %rep 256
        dq _InterruptStub %+ i  ; Add address of each stub to the table
    %assign i i+1
    %endrep

section .text

; Load the IDT
_LoadIDT:
    push rbp                 ; Save base pointer
    mov rbp, rsp             ; Set up new base pointer
    
    ; Save critical registers that might be affected
    push rax
    
    ; Disable interrupts while loading IDT
    cli
    
    ; Load the IDT using LIDT instruction
    ; RDI already contains the address of the IDTR structure
    lidt [rdi]
    
    ; Brief delay to ensure IDT is properly loaded
    nop
    nop
    nop
    
    ; Re-enable interrupts
    sti
    
    ; Restore registers
    pop rax
    
    ; Restore stack frame
    pop rbp
    
    ; Return
    ret

; Setup the interrupt system
_SetupInterrupts:
    ret

; Returns the address of a specific interrupt stub
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
_InterruptCommon:
    ; Save all general-purpose registers
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

; Address of the first interrupt stub
_GetInterruptStubAddress:
    mov rax, _InterruptStub0
    ret

; Macro for generating interrupt stubs
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