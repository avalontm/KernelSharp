; PIT (Programmable Interval Timer) interrupt handler
; This file provides the assembly stub for the PIT interrupt

[BITS 64]
[global _GetPITHandlerAddress]
[global _PITInterruptStub]
[extern HandlePITInterrupt]

section .text

; Function to get the address of the PIT handler
_GetPITHandlerAddress:
    mov rax, _PITInterruptStub
    ret

; PIT interrupt handler stub
_PITInterruptStub:
    ; Save all registers
    push rax
    push rcx
    push rdx
    push rbx
    push rbp
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

    ; Call the C# handler
    call HandlePITInterrupt

    ; Restore all registers
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
    pop rbp
    pop rbx
    pop rdx
    pop rcx
    pop rax

    ; Return from interrupt
    iretq