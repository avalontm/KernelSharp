; Interrupt Handlers for x86_64 OS Kernel
; This file defines 256 unique interrupt stubs for the IDT
; Each stub preserves registers and calls the common C# handler

global _SetupInterrupts
global _GetInterruptStub
global _CLI
global _STI
global _Halt
global _Pause
global _Nop
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

; Nop instruction (for busy-waiting)
_Nop:
    nop
    ret

; Get the address of the first interrupt handler
_GetInterruptStubAddress:
    mov rax, _InterruptStub0
    ret

; Common part of the interrupt handler
_InterruptCommon:
    ; Create a stack frame (for debugging)
    push rbp
    mov rbp, rsp
    
    ; Save all general purpose registers
    push rax
    push rbx
    push rcx
    push rdx
    push rsi
    push rdi
    push r8
    push r9
    push r10
    push r11
    push r12
    push r13
    push r14
    push r15
    
    ; Calculate original stack pointer (for frame.RSP)
    mov rax, rbp
    add rax, 16           ; Skip RBP and return address
    
    ; Create InterruptFrame structure on stack
    sub rsp, 120          ; Reserve space for frame (15 * 8 = 120 bytes)
    
    ; RCX contains interrupt number
    ; Create InterruptFrame structure
    mov [rsp], rdi        ; Frame.RDI
    mov [rsp+8], rsi      ; Frame.RSI
    mov [rsp+16], rbp     ; Frame.RBP
    mov [rsp+24], rax     ; Frame.RSP (original value)
    mov [rsp+32], rbx     ; Frame.RBX
    mov [rsp+40], rdx     ; Frame.RDX
    mov [rsp+48], rcx     ; Frame.RCX
    mov [rsp+56], rax     ; Frame.RAX
    
    ; Interrupt number is already in RCX
    mov [rsp+64], rcx     ; Frame.InterruptNumber
    
    ; Error code (if any)
    mov rax, [rbp+8]      ; Get error code from stack
    mov [rsp+72], rax     ; Frame.ErrorCode
    
    ; CPU-saved registers (these are approximations, might need adjustment)
    mov rax, [rbp+16]     ; Get RIP from stack
    mov [rsp+80], rax     ; Frame.RIP
    
    mov rax, [rbp+24]     ; Get CS from stack
    mov [rsp+88], rax     ; Frame.CS
    
    mov rax, [rbp+32]     ; Get RFLAGS from stack
    mov [rsp+96], rax     ; Frame.RFLAGS
    
    mov rax, [rbp+40]     ; Get UserRSP from stack
    mov [rsp+104], rax    ; Frame.UserRSP
    
    mov rax, [rbp+48]     ; Get SS from stack
    mov [rsp+112], rax    ; Frame.SS
    
    ; Prepare for call to C# handler
    ; Windows x64 calling convention - First parameter in RCX
    mov rcx, rsp          ; Pass pointer to InterruptFrame
    
    ; Ensure 16-byte stack alignment
    sub rsp, 16
    and rsp, -16
    
    ; Reserve shadow space (Windows x64 calling convention)
    sub rsp, 32
    
    ; Call C# handler
    call HandleInterrupt
    
    ; Clean up shadow space
    add rsp, 32
    
    ; Restore stack alignment padding
    mov rsp, rbp
    sub rsp, 120          ; Point back to our frame

    ; Restore all registers
    mov rsp, rbp          ; Reset stack to base of our frame
    sub rsp, 120          ; Calculate position of saved registers
    
    ; Restore all general registers
    pop r15
    pop r14
    pop r13
    pop r12
    pop r11
    pop r10
    pop r9
    pop r8
    pop rdi
    pop rsi
    pop rdx
    pop rcx
    pop rbx
    pop rax
    
    ; Restore stack frame
    pop rbp
    
    ; Skip error code
    add rsp, 8
    
    ; Return from interrupt
    iretq

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
    
    ; Push interrupt number
    mov rcx, %1           ; Load interrupt number
    
    ; Jump to common handler
    jmp _InterruptCommon
%endmacro

; Generate all 256 interrupt stubs
%assign i 0
%rep 256
    INTERRUPT_STUB i
%assign i i+1
%endrep